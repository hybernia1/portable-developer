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
  |     +-- projektové služby Composer a Python
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

Hlavní okno používá trvalou boční navigaci. Přehled pouze agreguje stav; PHP, Apache, Databáze a Selenium mají vlastní detailní stránky, ale čtou stejný service model a volají stejné controllery. Composer a Python mají samostatné projektové služby a vlastní stav operací. Změna stavu na jedné stránce se proto projeví všude a nevznikají paralelní kopie lifecycle logiky.

## Offline build a runtime

Balicí skript je vývojový/release nástroj, ne funkce spuštěné aplikace. Z předem připravených zdrojů vytvoří `modules/<druh>/<verze>/`, doplní metadata a ověří SHA-256 vstupních souborů. Spuštěná aplikace nestahuje serverové moduly ani runtime. Síť může použít pouze výslovná uživatelská instalace projektové knihovny přes Composer nebo pip.

Katalog `catalog/modules.json` je allowlist přesných verzí a hashů. Soubor `.portable-developer-module.json` v každém modulu dokládá, ke které katalogové položce patří. Samotná přítomnost stejně pojmenovaného EXE nestačí ke spuštění.

## Composer, Python a budoucí terminál

Composer 2.10.2 a Python 3.13.0 jsou nástroje s vlastním `.portable-developer-tool.json`; inventář ověřuje bezpečnou relativní cestu a SHA-256 vstupního souboru. Composer se spouští přes katalogově ověřené PHP CLI, Python přes explicitní `modules/python/<verze>/python.exe`. Oba používají společný portable command runner s `ArgumentList`, bez shellu, s pracovním adresářem, timeoutem, přesměrovaným výstupem a ukončením procesního stromu.

Composer spravuje projekt `instances/default/www`, používá vlastní `state/composer` a `cache/composer` a pro UI operace vypíná pluginy i instalační skripty. Python knihovny se instalují pomocí `pip --target` do `instances/default/python/packages`; `PYTHONHOME`, uživatelské site-packages a globální pip konfigurace se nepoužívají. Základní Python zůstává čistý a přenosný i po přesunu disku.

Vstup balíčku je omezený na běžný název a volitelné verzovací omezení; URL, lokální cesty a libovolný shellový příkaz nejsou přijímány. Budoucí obecný terminál může znovu použít inventář runtime a command runner, ale bude samostatnou explicitní funkcí s vlastní bezpečnostní hranicí, nikoli skrytým rozšířením správců balíčků.

## Instance a porty

První instance se jmenuje `default`. Obsahuje vlastní konfiguraci, webový kořen, databázová data, stav a logy. Výchozí lokální porty jsou Apache `8080`, MariaDB `3307` a Selenium `4444`; před startem se kontroluje jejich dostupnost.

Absolutní cesty mohou vzniknout jen v dočasné konfiguraci pod `temp/` pro konkrétní běh. Trvalá nastavení zůstávají relativní vůči kořenu aplikace.

MariaDB se při prvním startu inicializuje automaticky, spustí se pouze na `127.0.0.1:3307` a založí databázi `portable_dev`. Nová instance používá účet `root` bez hesla podle lokálního vývojového modelu; uživatel může heslo později nastavit v UI. Databázové příkazy dostávají aktuální heslo přes krátkodobý defaults soubor pod `temp/`, nikoli argument procesu nebo log. Databáze není vystavena síti a toto nastavení není produkční bezpečnostní model. Přehled velikostí čte metadata z `information_schema`, systémová schémata skrývá a uvádí součet dat a indexů jako orientační hodnotu.

phpMyAdmin je přibalený jako nástroj pod `tools/` a Apache jej zpřístupní jen z lokálního počítače na `/phpmyadmin/`. Používá cookie autentizaci: generovaná konfigurace obsahuje host a port MariaDB, ale nikdy databázové heslo. Její 32znakový cookie secret vzniká lokálně při prvním použití a zůstává ve stavu portable instance.

## Selenium a WebDriver

Selenium controller používá výhradně katalogově ověřený `selenium-server.jar` a explicitní `modules/jre/<verze>/bin/java.exe`. Spouští Standalone Grid na `127.0.0.1`, generuje TOML pod `temp/generated/<instance>/selenium/` a při ukončení vlastní celý procesní strom. Selenium Manager i automatická detekce driverů jsou vypnuté, takže běžící aplikace nic nestahuje a nesahá do systémového `PATH`.

Offline release obsahuje hashově ověřený geckodriver pod `drivers/bundled/`. Uživatel může do `drivers/custom/` vložit standardně pojmenovaný `geckodriver.exe`, `chromedriver.exe` nebo `msedgedriver.exe`; inventář ignoruje reparse points a použije explicitní cestu uvnitř portable kořene. Vlastní driver je uživatelský spustitelný kód a UI jej proto odlišuje od ověřeného přibaleného driveru.

Nastavení portu, počtu souběžných relací a Selenium `session-timeout` se ukládá do `state/selenium-settings.json`; timeout představuje maximální neaktivitu, nikoli absolutní dobu běhu. Běžící relace UI načítá z lokálního GraphQL endpointu a ukončuje standardním `DELETE /session/{id}` až po potvrzení uživatele. Samotné prohlížeče nejsou součástí distribuce.

## Logování a jazyk

JSONL logy jsou pod `logs/` a nesmí obsahovat hesla ani tokeny. MariaDB heslo je uložené pouze v portable state souboru instance a není chráněné šifrováním hostitelského účtu, aby balík zůstal přenositelný. Volba češtiny/angličtiny je v `state/settings.json`, takže se přenáší spolu s aplikací.
