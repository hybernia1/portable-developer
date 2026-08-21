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

Selenium používá přibalený Microsoft OpenJDK pod `modules/jre/25.0.3/`. Controller Selenium bude Javu spouštět výhradně explicitní cestou z této složky; nesmí použít systémový `java.exe` ani globální `PATH`.

## Redistribuce

Repozitář serverové binárky a Microsoft DLL neukládá. Připravuje je release skript z lokálních, předem ověřených zdrojů. Před veřejným vydáním musí distributor potvrdit licenční oprávnění a přiložit požadované licence a notices všech komponent.
