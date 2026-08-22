# Architektura

## Technologický směr

- Windows 10/11 x64.
- C#, .NET 10 a WPF.
- Self-contained složková distribuce `win-x64`.
- Offline přibalené serverové moduly, žádný runtime downloader.
- Žádný Docker, MSI, Windows služba, systémový `PATH`, registr ani firewall.

Koncový uživatel nepotřebuje .NET, Python, Java ani Visual C++ Redistributable nainstalované v systému. Všechny nutné runtime soubory jsou součástí distribuce.

## Vrstvy

```text
WPF UI
  |
  +-- Application / use cases
  |     +-- stack a instance controllery
  |     +-- katalog a ověření modulů
  |     +-- konfigurace a lokalizace
  |
  +-- Infrastructure
  |     +-- process supervisor a command runner
  |     +-- health checks
  |     +-- portable cesty a inventář modulů
  |     +-- SHA-256 a runtime preflight
  |     +-- strukturované logování
  |
  +-- Portable files
        modules, instances, state, logs, temp
```

UI nepracuje přímo s `Process`. Každý server řídí aplikační controller, který před startem zkontroluje katalog, vstupní soubor, runtime závislosti, porty a konfiguraci.

Hlavní okno používá trvalou boční navigaci. Přehled pouze agreguje stav; PHP, Apache, Databáze a Selenium mají vlastní detailní stránky, ale čtou stejný service model a volají stejné controllery. Změna stavu na jedné stránce se proto projeví všude a nevznikají paralelní kopie lifecycle logiky.

## Offline build a runtime

Balicí skript je vývojový/release nástroj, ne funkce spuštěné aplikace. Z předem připravených zdrojů vytvoří `modules/<druh>/<verze>/`, doplní metadata a ověří SHA-256 vstupních souborů. Spuštěná aplikace už síť nepotřebuje a žádné balíčky nestahuje.

Katalog `catalog/modules.json` je allowlist přesných verzí a hashů. Soubor `.portable-developer-module.json` v každém modulu dokládá, ke které katalogové položce patří. Samotná přítomnost stejně pojmenovaného EXE nestačí ke spuštění.

## Instance a porty

První instance se jmenuje `default`. Obsahuje vlastní konfiguraci, webový kořen, databázová data, stav a logy. Výchozí lokální porty jsou Apache `8080`, MariaDB `3307` a Selenium `4444`; před startem se kontroluje jejich dostupnost.

Absolutní cesty mohou vzniknout jen v dočasné konfiguraci pod `temp/` pro konkrétní běh. Trvalá nastavení zůstávají relativní vůči kořenu aplikace.

MariaDB se při prvním startu inicializuje automaticky, spustí se pouze na `127.0.0.1:3307` a založí databázi `portable_dev`. Nová instance používá účet `root` bez hesla podle lokálního vývojového modelu; uživatel může heslo později nastavit v UI. Databázové příkazy dostávají aktuální heslo přes krátkodobý defaults soubor pod `temp/`, nikoli argument procesu nebo log. Databáze není vystavena síti a toto nastavení není produkční bezpečnostní model. Přehled velikostí čte metadata z `information_schema`, systémová schémata skrývá a uvádí součet dat a indexů jako orientační hodnotu.

phpMyAdmin je přibalený jako nástroj pod `tools/` a Apache jej zpřístupní jen z lokálního počítače na `/phpmyadmin/`. Používá cookie autentizaci: generovaná konfigurace obsahuje host a port MariaDB, ale nikdy databázové heslo. Její 32znakový cookie secret vzniká lokálně při prvním použití a zůstává ve stavu portable instance.

## Logování a jazyk

JSONL logy jsou pod `logs/` a nesmí obsahovat hesla ani tokeny. MariaDB heslo je uložené pouze v portable state souboru instance a není chráněné šifrováním hostitelského účtu, aby balík zůstal přenositelný. Volba češtiny/angličtiny je v `state/settings.json`, takže se přenáší spolu s aplikací.
