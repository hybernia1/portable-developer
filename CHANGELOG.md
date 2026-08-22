# Změny

Formát vychází z principů [Keep a Changelog](https://keepachangelog.com/) a data používají ISO 8601.

## [Unreleased]

## [0.2.0] - 2026-08-22

### Added

- Ověřený portable Notepad++ 8.9.2 bez updateru a uživatelských dat, dostupný na samostatné stránce Nástroje.
- Volitelný `instances/default/config/php-custom.ini` pro pokročilé ruční PHP direktivy, otevíraný přibaleným editorem a připojovaný za generovanou konfiguraci při startu stacku.
- Samostatný omezený terminál s čistým portable `PATH`, přímým spouštěním přibaleného PHP, Composeru a Pythonu bez systémového shellu a příkazy pro stav či řízení služeb.
- Správce projektových souborů omezený na `instances/default/www`, s vytvářením, přejmenováním, potvrzovaným mazáním a otevřením souboru v přibaleném Notepad++.

### Security

- Správce souborů odmítá absolutní i unikající cesty, smazání kořene projektu a práci přes reparse pointy; nemůže zpřístupnit ani odstranit core aplikace.
- Terminál odmítá roury, přesměrování a řetězení shellových příkazů. PHP a Python projektový kód je nadále běžný uživatelský proces, nikoli operačním systémem izolovaný sandbox.

## [0.1.0] - 2026-08-22

### Added

- WPF aplikace na .NET 10 se self-contained `win-x64` publikací.
- Portable resolver cest, process supervisor, command runner, TCP health check a JSONL logování.
- Český/anglický dashboard a portable nastavení jazyka.
- Apache/PHP FastCGI controller s generovanou konfigurací, kontrolou portů a rollbackem.
- Transakční inicializace MariaDB bez Windows služby.
- Offline balicí skript pro Apache 2.4.66, PHP 8.4.12, MariaDB 12.3.2, Selenium 4.47.0, Microsoft OpenJDK 25.0.3, Composer 2.10.2 a Python 3.13.0 s pip 24.2.
- App-local Microsoft VC++ runtime s kontrolou podpisu, minimální verze a SHA-256 metadat.
- Ověření serverových vstupních souborů proti lokálnímu katalogu; dashboard zobrazuje pouze ověřený stav **Připraveno**.
- Boční navigace a samostatné stránky Přehled, PHP, Apache, Databáze, Selenium a Nastavení se sdíleným stavem služeb.
- Detailní stránky PHP/Apache s aktuální verzí, portem a společným ovládáním webového stacku; databázová stránka zobrazuje lokální připojení `127.0.0.1:3307` a účet `root` bez zveřejnění hesla.
- Automatický první start MariaDB, výchozí databáze `portable_dev`, řízený start/stop a localhost TCP health check.
- Přehled uživatelských databází s orientační velikostí dat a indexů a formulář pro vytváření dalších databází.
- Volitelné nastavení hesla účtu `root`; nové instance nadále začínají bez hesla.
- Přibalený phpMyAdmin 5.2.3 s lokálním Apache aliasem, cookie přihlášením a automatickým spuštěním potřebných serverů.
- Selenium Standalone Grid controller s explicitní přibalenou Javou, localhost portem, health checkem a bezpečným ukončením procesního stromu.
- Nastavení Selenium portu, maximálního počtu souběžných relací a limitu neaktivity relace v portable state souboru.
- Přehled běžících Selenium relací přes GraphQL, proklik do Hubu a potvrzované ukončení relace přes standardní WebDriver endpoint.
- Ověřený geckodriver 0.37.1 v offline balíku a inventář uživatelských Firefox, Chrome a Edge driverů ze složky `drivers/custom/`.
- Samostatné stránky Composer a Python s přehledem nainstalovaných projektových knihoven, volitelným omezením verze a potvrzovaným odebráním.
- Ověřený inventář portable nástrojů a oddělené projektové adresáře `instances/default/www` a `instances/default/python/packages`.
- Editor PHP nastavení s validovanými limity, zobrazením chyb a allowlistem skutečně přibalených rozšíření; hodnoty se ukládají do konfigurace instance a při startu generují nový `php.ini`.
- Explicitní verze aplikace 0.1.0 viditelná v rozhraní a vlastní ikona aplikace.

### Changed

- Distribuce je od prvního spuštění plně offline; serverové moduly a jejich runtime jsou součástí výsledné složky.
- Katalog nyní popisuje přibalené komponenty a SHA-256 vstupních souborů, nikoli instalaci stažených archivů.
- Výchozí publish cesta je `artifacts/publish/PortableDeveloper-offline-win-x64/` a existující výstup se z bezpečnostních důvodů nepřepisuje.
- Dashboard používá jediný stavový ovladač webového stacku místo samostatných tlačítek Start/Stop.
- Karty Apache a PHP zobrazují skutečný provozní stav a port; MariaDB má přípravu dat přímo ve své kartě a Selenium otevřeně rozlišuje přibalenou binárku od dosud nezapojeného řízení serveru.
- Nové MariaDB instance používají podle rozhodnutí vlastníka lokální účet `root` bez hesla; server je pevně svázaný s `127.0.0.1` a není určený pro produkci.
- Ruční obnovení pevného offline inventáře bylo odstraněno a technická cesta aplikace je schovaná v rozbalovacích informacích.
- Selenium již není pouze informativní karta; dashboard a detailní stránka sdílejí skutečný stav řízeného Gridu.
- Composer byl aktualizován z 2.9.4 na 2.10.2 a jeho příkazy běží bez pluginů a instalačních skriptů; Python knihovny se instalují přes `pip --target` bez změny základního runtime nebo profilu Windows.
- Výchozí PHP konfigurace nově aktivuje běžná rozšíření `curl`, `fileinfo`, `gd`, `intl` a `pdo_mysql`; základní `mbstring`, `mysqli`, `openssl` a `zip` nelze v UI vypnout.
- Self-contained publish obsahuje pouze české satelitní prostředky; angličtina zůstává neutrálním jazykem aplikace.

### Removed

- Download katalog a instalace serverových balíčků z dashboardu.
- Tlačítko a aplikační workflow pro uživatelský import Visual C++ runtime.
- Runtime HTTP downloader, ZIP instalátor a související testovací implementace.

### Fixed

- Přirozené ukončení spravovaného procesu se již v logu nesprávně neoznačuje jako neočekávané; závažnost vyhodnocuje jeho lifecycle controller.
- Opraveno zablokování WPF vlákna při startovním logování.
- Opraveno kopírování obsahu modulových adresářů v offline balicím skriptu.
- Metadata katalogu i modulů nyní používají shodnou řetězcovou serializaci druhu modulu.
- Generovaná Apache konfigurace už nenačítá neexistující `mod_mpm_winnt.so`; Windows MPM je v přibaleném Apache staticky vestavěné.
- Vlastní styl akčních tlačítek zachovává čitelný popisek a barvu také během hoveru a deaktivovaného průběhového stavu.
- MariaDB release staging používá krátkou jednoznačnou systémovou dočasnou cestu, takže dlouhý název cílové složky nepřekročí limit `Expand-Archive`.
- Windows FastCGI mapování odstraňuje úvodní lomítko z diskové cesty před předáním skriptu PHP, takže fungují i PHP aplikace mimo hlavní document root.
- Offline balení odstraňuje zdrojové Apache access/error logy a PID soubor, aby release neobsahoval provozní data z build prostředí.
- Python balení ignoruje zdrojové `site-packages` a `Scripts`, takže nepřenáší lokální vývojové knihovny ani problematické dlouhé cesty; pip se vytvoří offline pomocí `ensurepip`.
- Stavový řádek stránek Composer a Python již nepřebírá hlášku z druhého správce balíčků.
- Release již nekopíruje Laragon `php.ini` s absolutní build cestou ani nepotřebné `.pdb` ladicí symboly.
- Obnovení Composer knihoven po odebrání poslední přímé závislosti přijímá i prázdný kořen `composer.json`, takže úspěšná operace již nekončí chybou typu JSON elementu.
