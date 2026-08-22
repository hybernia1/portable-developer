# Nativní runtime závislosti

Portable Developer nespoléhá na globálně nainstalovaný Visual C++ Redistributable ani Java runtime. Potřebné soubory jsou app-local přímo v offline distribuci.

## PHP a Apache

Minimální kontrolovaná sada je:

```text
modules/php/8.4.12/
  php-cgi.exe
  vcruntime140.dll
  vcruntime140_1.dll

modules/apache/2.4.68/bin/
  httpd.exe
  vcruntime140.dll
```

Release skript přidává i související `msvcp140*.dll`, aby nativní moduly nemusely hledat tyto knihovny v systému.

## Kontrola při balení

`Bundle-OfflineDependencies.ps1` ověří podpis a SHA-256 přesné verze Microsoft VC++ Redistributable, připnutým WiX nástrojem z ní bez instalace vyjme x64 runtime CAB a před kopírováním každé DLL ověřuje:

- přesnou verzi `14.51.36247.0`;
- platný Authenticode podpis;
- certifikát Microsoft Corporation;
- SHA-256 každého požadovaného souboru po zkopírování.

K modulu se zapisuje `.portable-developer-runtime.json` s verzí, hashem a signerem, nikoli absolutní cesta zdroje. Spuštěná aplikace už nenabízí import ani instalaci runtime.

## Kontrola před startem

Runtime preflight znovu ověří přítomnost DLL a shodu jejich SHA-256 s metadaty. Změněný nebo chybějící soubor zablokuje spuštění modulu a zobrazí konkrétní chybu. Aplikace nikdy nespouští `vc_redist.exe` a nic nekopíruje do `System32`.

## Java

Selenium používá přibalený Microsoft OpenJDK pod `modules/jre/25.0.3/`. Z ověřeného JDK archivu se balí pouze runtime obraz, konfigurace a licence; vývojové `jmods`, hlavičky, manuály a `src.zip` se vynechají. Controller Javu spouští výhradně explicitní cestou z této složky a nepoužívá systémový `java.exe` ani globální `PATH`. Selenium Manager je vypnutý; WebDrivery se vybírají pouze z portable složky `drivers/`. Samotné prohlížeče nejsou runtime součástí projektu.

## Python

Python 3.13.0 je přibalený pod `modules/python/3.13.0/`. Release z vývojového zdroje nekopíruje `Scripts` ani `Lib/site-packages`; následně offline vytvoří pouze pip 24.2 pomocí vestavěného `ensurepip`. Aplikace spouští konkrétní `python.exe`, izoluje ho od uživatelských site-packages a ukládá projektové knihovny do `instances/default/python/packages`.

Tím se nemění systémový Python, profil Windows ani základní portable runtime. Při přesunu celé složky se nepřenáší virtuální prostředí s absolutní cestou; projektový adresář se znovu připojí explicitními argumenty a prostředím procesu.

## Portable editor

Notepad++ 8.9.2 je pod `modules/editor/8.9.2/` jako hashově ověřený nástroj. Release obsahuje jen editor, syntax data, českou lokalizaci a marker `doLocalConf.xml`; nekopíruje updater, pluginy, session ani zálohy. Editor se spouští explicitní cestou bez shellu, systémového `PATH`, registrace asociací nebo zápisu do profilu Windows.

## Redistribuce

Repozitář serverové binárky a Microsoft DLL neukládá. Release skript je připravuje z přesně připnutých upstream souborů v ignorované lokální cache; Laragon, `System32` ani instalace VC runtime nejsou build vstupem. Před veřejným vydáním musí distributor potvrdit licenční oprávnění a přiložit požadované licence a notices všech komponent.
