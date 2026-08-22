# Nativní runtime závislosti

Portable Developer nespoléhá na globálně nainstalovaný Visual C++ Redistributable ani Java runtime. Potřebné soubory jsou app-local přímo v offline distribuci.

## PHP a Apache

Minimální kontrolovaná sada je:

```text
modules/php/8.4.12/
  php-cgi.exe
  vcruntime140.dll
  vcruntime140_1.dll

modules/apache/2.4.66/bin/
  httpd.exe
  vcruntime140.dll
```

Release skript přidává i související `msvcp140*.dll`, aby nativní moduly nemusely hledat tyto knihovny v systému.

## Kontrola při balení

`Bundle-OfflineDependencies.ps1` čte DLL z explicitního build zdroje a před kopírováním ověřuje:

- minimální verzi `14.50.0.0`;
- platný Authenticode podpis;
- certifikát Microsoft Corporation;
- SHA-256 každého požadovaného souboru po zkopírování.

K modulu se zapisuje `.portable-developer-runtime.json` s verzí, hashem a signerem, nikoli absolutní cesta zdroje. Spuštěná aplikace už nenabízí import ani instalaci runtime.

## Kontrola před startem

Runtime preflight znovu ověří přítomnost DLL a shodu jejich SHA-256 s metadaty. Změněný nebo chybějící soubor zablokuje spuštění modulu a zobrazí konkrétní chybu. Aplikace nikdy nespouští `vc_redist.exe` a nic nekopíruje do `System32`.

## Java

Selenium používá přibalený Microsoft OpenJDK pod `modules/jre/25.0.3/`. Controller Javu spouští výhradně explicitní cestou z této složky a nepoužívá systémový `java.exe` ani globální `PATH`. Selenium Manager je vypnutý; WebDrivery se vybírají pouze z portable složky `drivers/`. Samotné prohlížeče nejsou runtime součástí projektu.

## Python

Python 3.13.0 je přibalený pod `modules/python/3.13.0/`. Release z vývojového zdroje nekopíruje `Scripts` ani `Lib/site-packages`; následně offline vytvoří pouze pip 24.2 pomocí vestavěného `ensurepip`. Aplikace spouští konkrétní `python.exe`, izoluje ho od uživatelských site-packages a ukládá projektové knihovny do `instances/default/python/packages`.

Tím se nemění systémový Python, profil Windows ani základní portable runtime. Při přesunu celé složky se nepřenáší virtuální prostředí s absolutní cestou; projektový adresář se znovu připojí explicitními argumenty a prostředím procesu.

## Portable editor

Notepad++ 8.9.2 je pod `modules/editor/8.9.2/` jako hashově ověřený nástroj. Release obsahuje jen editor, syntax data, českou lokalizaci a marker `doLocalConf.xml`; nekopíruje updater, pluginy, session, zálohy ani uživatelské nastavení z Laragonu. Editor se spouští explicitní cestou bez shellu, systémového `PATH`, registrace asociací nebo zápisu do profilu Windows.

## Portable správce souborů

Double Commander 1.2.8 x64 je pod `modules/filemanager/1.2.8/`. Release ověřuje SHA-256 oficiálního portable ZIPu i `doublecmd.exe` a zachovává přiloženou licenci GPL-2.0 a dokumentaci. Aplikace jej spouští bez shellu s oběma panely v `instances/default/www`, dočasnou složkou pod kořenem aplikace a parametrem `--config-dir` směřujícím do `state/doublecmd`.

Konfigurace editoru obsahuje pouze `%PORTABLE_DEVELOPER_EDITOR%`; aplikace tuto procesní proměnnou před každým startem nastaví na aktuální ověřený Notepad++. Přesun celé distribuce na jiné písmeno disku proto nerozbije F4 ani nezanechá trvalou absolutní cestu. Double Commander je samostatná plnohodnotná aplikace a uživatel v něm může vědomě přejít i mimo výchozí projektovou složku.

## Redistribuce

Repozitář serverové binárky a Microsoft DLL neukládá. Připravuje je release skript z lokálních, předem ověřených zdrojů. Před veřejným vydáním musí distributor potvrdit licenční oprávnění a přiložit požadované licence a notices všech komponent.
