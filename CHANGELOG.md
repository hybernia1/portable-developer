# Změny

Formát vychází z principů [Keep a Changelog](https://keepachangelog.com/) a data používají ISO 8601.

## [Unreleased]

### Added

- Anglický vstupní dokument, veřejná governance, signing role, uninstall postup, Code of Conduct a GitHub šablony pro bezpečnější příspěvky.
- Dokumentovaný postup pro rozlišení Defender Antivirus detekce od SmartScreen/Smart App Control reputační blokace a přípravu false-positive hlášení.

### Changed

- PE metadata budoucích buildů používají jednotný název produktu `Portable Developer` a popis vhodný pro pravidla podpisu.
- Code signing policy a release dokumentace nyní obsahují přesnou SignPath atribuci, odpovědné role, MFA, ruční approval a pravidla pro signing-sensitive soubory.

### Fixed

- Selenium controller test již nepředpokládá volný výchozí port 4444 a používá dočasný lokální port, takže výsledek nezávisí na současně běžícím Selenium serveru uživatele.

## [0.6.0] - 2026-08-22

### Added

- Veřejná projektová pravidla pro GPL-3.0-or-later, soukromí, bezpečnost, komponenty třetích stran a budoucí podepisování releasů.
- GitHub Actions CI pro kontrolu dependency locku, restore, formátování, release build a automatické testy na Windows a měsíční Dependabot kontrola NuGet a Actions závislostí.
- Přesný `dependencies.lock.json` a online bootstrap, který stáhne všech jedenáct release vstupů do ignorované lokální cache a před použitím ověří SHA-256.
- Správce sedmi logických runtime balíčků přímo v aplikaci: Web, Databáze, Selenium, Composer, Python, Editor a phpMyAdmin.
- Podmíněná navigace rozdělená na Prostředí, Servery, Vývoj a Aplikaci; detail nenainstalovaného serveru nebo nástroje se nezobrazuje.
- Tagový GitHub Actions workflow, který vytvoří self-contained Windows ZIP, SHA-256 a skutečný GitHub Release.

### Changed

- Každý budoucí offline release přibalí licenci projektu, zásady soukromí a přehled licencí komponent třetích stran.
- Release build už nečte Laragon ani `System32`; Apache byl aktualizován na přesný Windows build 2.4.68-260617 VS18 a ostatní komponenty se připravují přímo z připnutých upstream archivů.
- Veřejný online release má přibližně 54 MiB a obsahuje pouze aplikaci, katalogy, dokumenty a portable VC++ podporu; moduly se instalují do stejného kořene až po explicitní akci uživatele.
- Nepodepsané hotové verze se od 0.6.0 vydávají s viditelným upozorněním místo dřívějšího odkládání všech binárních releasů.

### Security

- Dokumentace výslovně označuje nepodepsanou sestavu 0.4.0 a nedoporučuje obcházet Windows Smart App Control; projekt připravuje podpis pouze vlastních binárek přes SignPath Foundation.
- Downloader přijímá pouze HTTPS zdroje z allowlistu, zapisuje přes dočasný soubor a při neshodě hashe selže. Podepsaný Microsoft VC++ bundle se ověří a rozbalí bez instalace či kopírování DLL z hostitelského Windows.
- Runtime instalace kontroluje cílový host redirectu, opakuje dočasně neúspěšné stažení nejvýše třikrát, odmítá archive traversal, odkazy a reparse pointy a při chybě vrací pouze nově vytvořené cíle.

## [0.5.0] - 2026-08-22

### Added

- Apache stránka spravuje více webových projektů s vlastní adresou `<id>.localhost`, document rootem, zapnutím virtual hostu a samostatnou volbou podpory `.htaccess`.
- Nové projekty používají strukturu `instances/default/projects/<id>/public`; vytvoří se s jednoduchým `index.php` a konfigurace zůstává relativní a přenositelná.

### Changed

- Composer, terminál a správce souborů sdílejí aktivní webový projekt. Composer ukládá `composer.json` a `vendor` do kořene projektu, zatímco Apache standardně zveřejní pouze jeho `public`.
- Existující `instances/default/www` se bez přesunu a ztráty dat zachovává jako projekt Default na `localhost`. Odebrání dalšího projektu odstraní pouze registraci; soubory na disku zůstanou zachované.
- Apache načítá `mod_rewrite`, používá `AccessFileName .htaccess`, generuje localhost virtual hosty a omezuje přístup na lokální počítač bez změny Windows `hosts`.

### Security

- ID projektu, relativní web root a spravované projektové cesty jsou validované; projektový katalog odmítá únik z portable kořene a reparse pointy a Apache u projektů nepovoluje následování odkazů.

## [0.4.0] - 2026-08-22

### Added

- Centrální stránka Porty pro Apache HTTP, PHP FastCGI, MariaDB a Selenium se společným portable uložením.
- Čtecí přehled aktuálních TCP listenerů Windows a živá kontrola dostupnosti, rozsahu i duplicit zvolených portů.

### Changed

- Všechny čtyři serverové komponenty nyní čtou port ze stejného `state/port-settings.json`; dřívější vlastní port Selenium se při prvním načtení zachová jako migrační výchozí hodnota.
- Porty lze měnit pouze při zastavených službách. Aplikace kolizi oznámí, ale nikdy neukončuje ani nepřenastavuje cizí proces.
- Stránka Selenium spravuje jen relace a timeout; její port se nastavuje výhradně v centrálním správci.

## [0.3.0] - 2026-08-22

### Added

- Restart Apache/PHP z dashboardu i detailních stránek. Uložení PHP nastavení za běhu web automaticky bezpečně restartuje.
- Jednotný vzhled záložek pro stránky PHP, Apache, MariaDB a Selenium.
- Integrovaná lišta správce souborů, historie Zpět, dialogy pro názvy a vlastní sada lehkých WPF vektorových ikon.

### Changed

- Tlačítko start/stop celého webového stacku je pouze na Přehledu; MariaDB a Selenium lze nadále spouštět nezávisle v libovolné kombinaci.
- První bootstrap MariaDB připraví `portable_dev`, ale server potom zastaví. Další spuštění aplikace databázi samo nespouští.
- phpMyAdmin již skrytě nespouští závislosti. Je dostupný jen při současně běžícím Apache/PHP a MariaDB a zobrazuje konkrétní chybějící službu.
- Terminál zabírá celou stránku bez samostatné horní lišty; čištění zůstává dostupné příkazem `clear` nebo `cls`.

## [0.2.3] - 2026-08-22

### Added

- Lehký správce projektových souborů přímo v aplikaci s navigací, vytvořením, přejmenováním, potvrzovaným mazáním a otevřením souboru v Notepad++.
- Bezpečný release cleanup, který po úspěšném publishi ponechá dva nejnovější release adresáře a navíc chrání každý release s běžícím procesem.

### Fixed

- Composer refresh nyní přijímá prázdné pole `[]`, které Composer vrací po odebrání posledního balíčku; úspěšné odebrání se už falešně nehlásí jako chyba.
- Správce souborů při prázdném názvu zobrazí srozumitelnou lokalizovanou výzvu a po úspěšné operaci odstraní předchozí chybový stav.

### Removed

- Double Commander a jeho externí proces, portable konfigurace, binárky a release závislost.

## [0.2.2] - 2026-08-22

### Changed

- Hlavní aplikace se publikuje jako přehledný self-contained single-file `PortableDeveloper.exe`; spravované .NET a projektové knihovny již nezaplňují kořen distribuce.
- Nativní WPF knihovny zůstávají vedle EXE, takže se při spuštění nic nerozbaluje do `%TEMP%` ani uživatelského profilu.

### Fixed

- Test MariaDB controlleru již nekoliduje s portem 3307 používaným současně spuštěnou portable instancí.
- Offline balení PHP odstraňuje všechny zdrojové varianty `php.ini*`, takže do distribuce nepronikne lokální konfigurace ani absolutní cesta z vývojového stroje.

## [0.2.1] - 2026-08-22

### Added

- Přibalený Double Commander 1.2.8 x64 z oficiálního portable archivu, ověřený připnutými SHA-256 archivu i vstupního EXE.
- Portable konfigurace Double Commanderu v `state/doublecmd`; oba panely startují v `instances/default/www` a F4 otevírá soubor v přibaleném Notepad++.
- Historie příkazů terminálu ovládaná šipkami nahoru a dolů.

### Changed

- Terminál je jedna konzolová plocha: příkaz se píše přímo za prompt a potvrzuje Enterem, bez samostatného vstupního pole a tlačítka Spustit.
- Vlastní správce souborů byl nahrazen plnohodnotným veřejným portable nástrojem; stránka Soubory nyní zobrazuje jeho ověřený stav a slouží jako bezpečný spouštěč.

### Security

- Double Commander zapisuje konfiguraci a dočasná data jen pod kořen Portable Developeru. Jde však o plnohodnotný externí správce, který může z vůle uživatele přejít mimo výchozí `www`; UI tuto hranici výslovně uvádí.

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
