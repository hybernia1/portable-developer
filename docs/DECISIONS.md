# Architektonická rozhodnutí

Rozhodnutí mají trvalé identifikátory. Nové významné rozhodnutí přidej na konec souboru a neměň historii; případné zrušení vyjádři novým záznamem.

## ADR-001 — C# / .NET 8 / WPF pro desktopovou aplikaci

- Stav: přijato
- Datum: 2026-08-21

**Kontext:** Cílové prostředí je Windows a koncový uživatel nemusí mít Python ani .NET.

**Rozhodnutí:** Aplikace bude psaná v C# jako WPF aplikace cílená na .NET 8 a publikovaná self-contained pro `win-x64`.

**Důsledky:** Distribuce je samostatná složka s vyšší velikostí. Vývojový počítač potřebuje .NET SDK 8. Python + PyInstaller není základní architekturou, ale lze jej použít pro oddělené vývojové nástroje.

## ADR-002 — Bez Dockeru a bez systémových služeb

- Stav: přijato
- Datum: 2026-08-21

**Kontext:** Projekt má fungovat z USB či libovolné složky bez instalace a zásahů do Windows.

**Rozhodnutí:** Apache, PHP, databáze, Selenium a JRE poběží jako dítě hlavní aplikace z přenosných modulů. Docker, instalátory a Windows služby nejsou součástí první verze.

**Důsledky:** Je nutné vlastnit správu procesů, konfiguraci a životní cyklus serverů.

## ADR-003 — Relativní cesty a data uvnitř kořene aplikace

- Stav: přijato
- Datum: 2026-08-21

**Rozhodnutí:** Konfigurace je relativní vůči kořeni Portable Developeru. Runtime data patří do `instances/`, sdílená data do definovaných podadresářů kořene.

**Důsledky:** Přesun složky mezi disky je podporovaný scénář a musí být testován.

## ADR-004 — Přechod z .NET 8 na .NET 10

- Stav: přijato; nahrazuje cílovou verzi z ADR-001
- Datum: 2026-08-21

**Kontext:** Vývojové prostředí obsahuje .NET SDK 10.0.400, nikoliv SDK 8. V projektu se zatím nenachází produkční implementace závislá na .NET 8.

**Rozhodnutí:** Aplikace cílí na .NET 10 a přesná verze SDK je připnutá v `global.json`.

**Důsledky:** Lokální build je ihned reprodukovatelný se současným SDK. Distribuce i nadále bude self-contained, takže koncový uživatel .NET instalovat nemusí.

## ADR-005 — Normalizovaný layout ručně vložených modulů

- Stav: přijato
- Datum: 2026-08-21

**Kontext:** ZIP archivy Apache, PHP a dalších komponent mají rozdílnou vnitřní strukturu. Runtime ani konfigurace nemají záviset na konkrétním archivu.

**Rozhodnutí:** Každý modul používá tvar `modules/<druh>/<verze>/` s pevně definovaným vstupním souborem. Například Apache používá `bin/httpd.exe` a PHP `php-cgi.exe`. Složky typu junction a symbolické odkazy se ignorují.

**Důsledky:** Ruční vložení je předvídatelné a budoucí instalátor může každý archiv normalizovat do stejného tvaru. Pouhá detekce modulu nezakládá oprávnění jej spustit; to vyžaduje budoucí katalog s ověřením hashe.

## ADR-006 — Absolutní serverové cesty jen v dočasně generované konfiguraci

- Stav: přijato
- Datum: 2026-08-21

**Kontext:** Apache pro své `ServerRoot`, logy a PID soubory vyžaduje praktické absolutní cesty, zatímco přenosná konfigurace nesmí obsahovat písmeno aktuálního disku.

**Rozhodnutí:** Trvalé nastavení Apache/PHP obsahuje jen relativní cesty. Konfigurační soubory specifické pro běh se vytvářejí pod `temp/generated/` z aktuálního kořene aplikace a před každým startem se nahradí.

**Důsledky:** Přesun celé složky nevyžaduje úpravu uživatelské konfigurace. Dočasné soubory mohou obsahovat absolutní cesty pouze uvnitř aktuálního kořene aplikace.

## ADR-007 — Lokální katalog a instalace přes ověřený archiv

- Stav: přijato
- Datum: 2026-08-21

**Kontext:** Aplikace musí umět stáhnout komponenty, ale nesmí důvěřovat libovolné URL ani rozbalit škodlivý ZIP mimo vlastní složku.

**Rozhodnutí:** Katalog je přibalený v `catalog/modules.json`. Každý balíček uvádí HTTPS zdroj, verzi, licenci, očekávaný vstupní soubor, kořen archivu a SHA-256 celého ZIPu. Instalátor nejdřív ověří archiv, rozbalí jej do `temp/install/` a až potom přesune do `modules/`.

**Důsledky:** Vydání aplikace musí aktualizovat katalog společně s ověřenými hashi. Vzdálené aktualizace katalogu nejsou podporované, dokud nezavedeme podpisový řetězec.

## ADR-008 — App-local nativní runtime místo instalace do Windows

- Stav: přijato
- Datum: 2026-08-21

**Kontext:** Oficiální Windows build PHP potřebuje Microsoft Visual C++ runtime. Systémový redistributable by zapsal závislost mimo portable složku a vyžadoval by jiný životní cyklus než aplikace.

**Rozhodnutí:** PHP se považuje za připravené pouze tehdy, když jsou `vcruntime140.dll` a `vcruntime140_1.dll` vedle `php-cgi.exe` ve složce modulu. Dashboard provede pouze čtecí preflight; budoucí balíček runtime bude instalován app-local přes lokální hashovaný katalog.

**Důsledky:** Aplikace nespouští `vc_redist*.exe`, nemění systém a může přesně oznámit chybějící soubory. Do doby licenčního a hashového schválení runtime balíčku se PHP po samotné instalaci z katalogu nespouští.

## ADR-009 — Portable volba jazyka rozhraní

- Stav: přijato
- Datum: 2026-08-21

**Kontext:** Uživatel musí volit mezi češtinou a angličtinou, ale portable aplikace nesmí pro toto nastavení používat registr ani profil Windows.

**Rozhodnutí:** Jazyk dashboardu se uchová v `state/settings.json` pod kořenem aplikace. Výchozí jazyk je čeština; změna se okamžitě projeví v UI a zapíše provozní událost do logu.

**Důsledky:** Nastavení se přenese se složkou aplikace. Neplatný či chybějící soubor bezpečně vrací výchozí češtinu.

## ADR-010 — Apache/PHP start pouze přes ověřený stack controller

- Stav: přijato
- Datum: 2026-08-21

**Kontext:** WPF nesmí řídit `Process` přímo a ručně vložená binárka nemá být automaticky považována za důvěryhodnou.

**Rozhodnutí:** Jediný controller řídí pořadí PHP FastCGI → Apache, volné porty, transientní konfiguraci, TCP health check a rollback. Spuštění vyžaduje záznam instalace shodný s přibaleným katalogem a kompletní app-local PHP runtime. Při zavření aplikace controller nejprve zastaví Apache, poté PHP.

**Důsledky:** UI pouze zobrazuje snapshot a volá aplikační rozhraní. Neověřené či jen ručně vložené moduly se nespouštějí; katalogové moduly navíc čekají na kompletní app-local runtime.

## ADR-011 — Apache Lounge jako zdroj Windows httpd

- Stav: přijato
- Datum: 2026-08-21

**Kontext:** Apache Software Foundation vydává httpd jako zdrojový kód a Windows binárky odkazuje na třetí strany. Potřebujeme reprodukovatelný x64 ZIP bez instalátoru a služby.

**Rozhodnutí:** Katalog používá Apache Lounge httpd 2.4.68 VS18 x64. URL a SHA-256 odpovídají Microsoft WinGet manifestu; SHA-256 byl navíc vypočten z lokálně staženého archivu. Archiv se normalizuje z kořene `Apache24/` a zachovává přiložené `LICENSE.txt` a `NOTICE.txt`.

**Důsledky:** Apache je třetí stranou sestavená binární distribuce a musí být při každé aktualizaci znovu ověřena. Protože ZIP neobsahuje importovaný `VCRUNTIME140.dll`, spuštění blokuje app-local runtime preflight; DLL lze dodat ověřeným importem podle ADR-012.

## ADR-012 — Uživatelský import podepsaného Visual C++ runtime

- Stav: přijato
- Datum: 2026-08-21

**Kontext:** Apache i PHP potřebují Visual C++ runtime. Spuštění `vc_redist.exe` by provedlo systémovou instalaci a automatické šíření DLL projektem vyžaduje oprávnění podle licenčních podmínek Microsoftu.

**Rozhodnutí:** Aplikace umožní import retail x64 DLL z uživatelem zvolené složky. Před app-local kopírováním ověří WinTrust řetězec, Microsoft signer, x64 PE a minimální verzi 14.50; k cílovému modulu zapíše SHA-256 metadata. Zdrojovou absolutní cestu neukládá a žádný externí program nespouští.

**Důsledky:** Portable hranice zůstává zachovaná a projekt nešíří neprověřené Microsoft DLL. Distributor musí dodat licencovaný zdroj runtime nebo připravit výslednou složku pomocí importéru; odpovědnost za bezpečnostní aktualizace app-local souborů zůstává na distributorovi.

## ADR-013 — MariaDB jako ZIP modul s transakční inicializací

- Stav: přijato
- Datum: 2026-08-22

**Kontext:** MariaDB musí běžet z portable složky bez MSI a služby Windows. Její inicializační nástroj vytváří datové soubory i `my.ini` s absolutními cestami a při přerušení může zanechat neúplný adresář.

**Rozhodnutí:** Katalog používá oficiální MariaDB 12.3.2 x64 ZIP a SHA-256 z REST API MariaDB Foundation. `mariadb-install-db.exe` se spouští přes obecný jednorázový command runner bez parametru služby, s timeoutem a daty ve staging složce. Po úspěchu se odstraní nepřenosný `my.ini`, data se atomicky přesunou do instance a náhodné root heslo se uloží pod `instances/<id>/state/`.

**Důsledky:** Neúplná inicializace nepřepíše existující data a nepřidá službu, registr ani systémovou konfiguraci. Start controller musí před každým během vytvořit nový transientní `my.ini` z aktuálního kořene aplikace. Soubor s heslem je tajemství a nesmí se dostat do Gitu ani logů.

## ADR-014 — Offline distribuce se všemi runtime komponentami

- Stav: přijato; nahrazuje runtime download z ADR-007 a uživatelský import z ADR-012
- Datum: 2026-08-22

**Kontext:** Download různých vendor archivů je křehký a uživatelský import Visual C++ DLL zhoršuje první spuštění. Cílem je intuitivní složka, která funguje bez instalace a bez sítě hned po rozbalení.

**Rozhodnutí:** Release obsahuje přesně připnuté verze Apache, PHP, MariaDB, Selenium, JRE a Composeru. Release skript je sestaví z předem připravených zdrojů, ověří SHA-256 serverových vstupních souborů a přidá podepsané app-local Microsoft VC++ DLL. Spuštěná aplikace nenabízí downloader ani import runtime a pouze ověřuje integritu přibaleného obsahu.

**Důsledky:** Výstup je větší, ale na cílovém počítači nevyžaduje síť, Python, .NET, Javu ani VC++ instalátor. Distributor odpovídá za aktualizace, kontrolní součty a licenční oprávnění všech přibalených komponent. ADR-007 zůstává historickým popisem původního směru; jeho runtime download workflow se dále nepoužívá. ADR-012 je nahrazené a importní UI bylo odstraněno.

## ADR-015 — Detailní stránky sdílejí jeden stav služeb

- Stav: přijato
- Datum: 2026-08-22

**Kontext:** Dashboard je vhodný pro rychlý přehled, ale konfigurace PHP, Apache, databází a Selenium se na jednu plochu nevejde. Samostatné stránky nesmí vytvořit duplicitní controllery nebo rozdílné informace o stejné službě.

**Rozhodnutí:** Aplikace používá trvalou boční navigaci a stránky Přehled, PHP, Apache, Databáze, Selenium a Nastavení. Všechny stránky čtou jeden sdílený view model a existující lifecycle controllery. První databázové nástroje budou určené jen pro lokální vývoj a použijí účet `root`; správa dalších DB uživatelů není součástí první verze. Heslo root se nezobrazuje v běžném UI ani v logu.

**Důsledky:** Server lze ovládat z kontextové stránky i přehledu bez rozcházení stavů. Editory `php.ini`, Apache konfigurace a správa databází mají stabilní místo v navigaci, ale budou zpřístupněné až s validací a transakčním zápisem. Root-only model zjednoduší první lokální databázové workflow, nesmí však být prezentován jako produkční bezpečnostní model.

## ADR-016 — Automatická localhost MariaDB s výchozí databází

- Stav: přijato; nahrazuje část ADR-013 o náhodném root hesle
- Datum: 2026-08-22

**Kontext:** První spuštění má být okamžitě použitelné bez ručního inicializačního kroku. Vlastník projektu požaduje účet `root` bez hesla, předem vytvořenou databázi a jednoduchou správu dalších lokálních databází.

**Rozhodnutí:** Aplikace při prvním načtení transakčně inicializuje datový adresář, spustí ověřený `mariadbd.exe` bez Windows služby, odstraní pouze čerstvě vygenerované historické schéma `test` a vytvoří databázi `portable_dev` s `utf8mb4`. U existující instance se `test` nikdy automaticky nemaže. Nové instance ukládají prázdné root heslo; starší instance s dříve uloženým náhodným heslem zůstávají čitelné. Transientní `my.ini` vždy používá `bind-address=127.0.0.1`, pevný port instance a data pod portable kořenem. UI dovolí pouze validované názvy databází a zobrazuje orientační velikost bez systémových schémat.

**Důsledky:** První spuštění nevyžaduje databázové rozhodnutí ani kliknutí a celé prostředí zůstává přenosné. Každý lokální proces se však může pokusit připojit k účtu bez hesla; proto se server nesmí vystavit na síťové rozhraní a UI výslovně označuje konfiguraci za neprodukční. Ukončení aplikace nejprve žádá MariaDB o normální shutdown a teprve po timeoutu použije procesní fallback.

## ADR-017 — Volitelné root heslo a přibalený phpMyAdmin

- Stav: přijato
- Datum: 2026-08-22

**Kontext:** Výchozí lokální prostředí má zůstat použitelné bez úvodního nastavování hesla, ale uživatel musí mít možnost účet `root` později zabezpečit. Současně je potřeba lehká grafická správa databází bez dalšího stahování či instalace.

**Rozhodnutí:** Nová instance nadále začíná s prázdným root heslem. UI dovolí nastavit nebo změnit heslo o délce 8 až 128 znaků. Heslo se předává MariaDB klientu přes krátkodobý defaults soubor a změnový SQL příkaz přes standardní vstup; nesmí být v argumentech procesu ani logu. Po úspěšné změně se portable credential state nahradí atomicky a při selhání zápisu se databázová změna pokusí vrátit. Offline release obsahuje phpMyAdmin 5.2.3 bez setup adresáře a lokálního vendor configu. Apache alias je omezený na `Require local`; phpMyAdmin používá cookie autentizaci, náhodný 32znakový secret uvnitř instance a neobsahuje uložené databázové heslo.

**Důsledky:** První start zůstává bez kroků navíc a pozdější heslo okamžitě používají přehled databází, shutdown i phpMyAdmin přihlášení. Heslo je kvůli přenositelnosti uložené v souboru instance bez vazby na Windows účet, takže ochrana celé portable složky zůstává odpovědností uživatele. phpMyAdmin je dostupný jen během běhu lokálních Apache, PHP a MariaDB a není určený k publikování do sítě.

## ADR-018 — Explicitní portable WebDrivery a lokální správa Selenium relací

- Stav: přijato
- Datum: 2026-08-22

**Kontext:** Selenium má fungovat offline bez systémové Javy, globálního `PATH` a automatického stahování driverů. Uživatel současně potřebuje doplnit další běžné drivery a spravovat relace z aplikace.

**Rozhodnutí:** Release obsahuje hashově ověřený geckodriver 0.37.1. Selenium Manager a automatická detekce driverů jsou vypnuté; controller generuje explicitní TOML konfiguraci z ověřeného `drivers/bundled/` a uživatelského `drivers/custom/`. Podporované názvy jsou `geckodriver.exe`, `chromedriver.exe` a `msedgedriver.exe`. Grid je vázaný na `127.0.0.1`, používá přibalené JRE a serverové limity ukládá portable. UI čte relace přes lokální GraphQL a ukončuje je standardním WebDriver DELETE po potvrzení.

**Důsledky:** Běh Selenium nevyžaduje síť ani změnu hostitelského Windows. Přibalený driver prochází SHA-256 kontrolou; vlastní driver je vědomě uživatelem dodaný spustitelný kód a UI jej jako ověřený neoznačuje. Kompatibilní prohlížeč musí být na cílovém počítači dostupný samostatně. Selenium `session-timeout` omezuje neaktivitu relace, nikoli její absolutní stáří.

## ADR-019 — Projektové balíčky bez systémového shellu

- Stav: přijato
- Datum: 2026-08-22

**Kontext:** Uživatel potřebuje z UI přidávat a odebírat PHP i Python knihovny, například `php-webdriver/webdriver` nebo `selenium`. Nástroje musí zůstat přenosné, nesmí používat systémovou instalaci a budoucí obecný terminál nemá oslabit validaci dnešních formulářů. Původně přibalený Composer 2.9.4 navíc spadal do rozsahu opraveného bezpečnostního problému s možným zápisem mimo `vendor`.

**Rozhodnutí:** Release obsahuje hashově připnutý Composer 2.10.2 a čistý Python 3.13.0 s pip 24.2. Oba nástroje mají vlastní ověřovaná metadata a spouštějí se přes společný portable command runner bez shellu. UI přijímá pouze validovaný název balíčku a volitelné verzovací omezení; URL a cesty odmítá. Composer pracuje v `instances/default/www`, vypíná pluginy i instalační skripty a ukládá domov i cache pod portable kořen. Python nepoužívá virtuální prostředí s absolutními cestami, ale instaluje přes `pip --target` do `instances/default/python/packages`, s vypnutými uživatelskými site-packages a globální konfigurací. Odebrání vyžaduje potvrzení.

**Důsledky:** Základní runtime zůstávají po instalaci knihoven beze změny a projekt lze přenést mezi disky. Instalace balíčku je výslovná síťová operace a cizí knihovna může obsahovat vlastní instalační logiku; UI na to upozorňuje. Budoucí terminál smí znovu použít inventář runtime a řízení procesů, ale bude oddělenou funkcí s explicitním pracovním adresářem a uživatelskou akcí; správci balíčků se kvůli němu nestanou obecným interpretem příkazů.

## ADR-020 — Strukturované PHP nastavení místo volné editace php.ini

- Stav: přijato
- Datum: 2026-08-22

**Kontext:** PHP stránka potřebuje měnit běžné limity a rozšíření, ale přímá editace libovolného `php.ini` by umožnila uložit absolutní cesty, načíst nepřibalenou DLL nebo snadno rozbít start. Runtime konfigurace se navíc musí po přesunu portable složky regenerovat z nového kořene.

**Rozhodnutí:** UI ukládá pouze typované a validované hodnoty do `instances/<id>/config/php-settings.json`: paměť, upload a POST limit, timeout, `max_input_vars`, zobrazení chyb a seznam povolených rozšíření. Rozšíření se přijímají jen z pevného katalogu a generátor před zápisem ověří existenci příslušné DLL v katalogově ověřeném PHP modulu. Povinná rozšíření `mbstring`, `mysqli`, `openssl` a `zip` nelze vypnout. Runtime `php.ini` se vždy celý vytvoří pod `temp/generated/`; vendor konfigurace ani libovolné uživatelské řádky se nekopírují.

**Důsledky:** Konfigurace je čitelná, přenositelná a odolná vůči vložení direktivy či cesty mimo kořen aplikace. UI zatím nepokrývá každou direktivu PHP ani načítání vlastních DLL; nové bezpečné volby se musí přidat do modelu, validace, allowlistu a testů. Změna uložená za běhu se projeví až při příštím startu webového stacku.

## ADR-021 — Portable editor a explicitní pokročilé PHP override

- Stav: přijato; rozšiřuje ADR-020
- Datum: 2026-08-22

**Kontext:** Validovaný PHP formulář má zůstat výchozí a bezpečný, ale pokročilý uživatel potřebuje upravit direktivy, které UI zatím nepokrývá. Spoléhat na systémový editor by porušilo očekávání okamžitě použitelného portable balíku a mohlo by zapisovat nastavení mimo jeho kořen.

**Rozhodnutí:** Offline release obsahuje hashově připnutý Notepad++ 8.9.2 v minimálním portable režimu s lokální konfigurací a bez updateru, pluginů, session či zdrojových uživatelských dat. Aplikace jej spouští přes servisní vrstvu explicitní cestou a `ArgumentList`, bez shellu a registrace asociací. Volitelný `instances/<id>/config/php-custom.ini` se při startu po základních kontrolách připojí za typovanou generovanou konfiguraci. UI jej označuje jako pokročilé nastavení a upozorňuje, že může přepsat hodnoty formuláře a projeví se až po restartu stacku.

**Důsledky:** Běžný uživatel dál používá validovaný formulář, zatímco ruční konfigurace je dostupná bez externí instalace. Obsah override souboru je vědomě důvěryhodný uživatelský vstup: může načíst vlastní rozšíření, použít absolutní cestu nebo rozbít start, a tím oslabit přenositelnost či bezpečné výchozí hodnoty. Editor po zavření hlavní aplikace není násilně ukončen, aby se neztratily neuložené změny; jeho stav však díky `doLocalConf.xml` zůstává uvnitř portable adresáře.

## ADR-022 — Omezený terminál a projektový správce souborů

- Stav: přijato
- Datum: 2026-08-22

**Kontext:** Uživatel potřebuje ovládat služby a spouštět přibalené vývojové nástroje z jednoho rozhraní. Současně potřebuje vytvářet a upravovat webové soubory bez rizika, že běžnou akcí ve správci smaže runtime nebo core aplikace.

**Rozhodnutí:** Terminál je vlastní příkazový interpret, nikoli hostovaný `cmd.exe` nebo PowerShell. Povoluje interní navigaci pouze pod `instances/default/www`, přímé ověřené entrypointy PHP, Composeru a Pythonu a typované lifecycle požadavky předávané existujícím controllerům. Shellové operátory jsou odmítnuté a `PATH` obsahuje pouze adresáře přibalených runtime. Správce souborů má pevný kořen `instances/default/www`; validuje každou cestu, chrání kořen, odmítá reparse pointy a destruktivní akce potvrzuje. Editace používá přibalený Notepad++.

**Důsledky:** Běžné ovládání služeb ani souborů nepotřebuje externí konzoli nebo systémový editor a UI nemůže přes správce souborů odstranit aplikaci. Terminál záměrně není plnohodnotný systémový shell. Spuštěný PHP, Composer nebo Python kód zůstává procesem s oprávněními aktuálního Windows uživatele; bez samostatného OS sandboxu nelze slíbit, že důvěryhodný projektový kód nikdy nepřistoupí mimo portable kořen.

## ADR-023 — Veřejný portable správce souborů a přímá konzole

- Stav: přijato; nahrazuje část ADR-022 o vlastním správci souborů
- Datum: 2026-08-22

**Kontext:** Vlastní jednoduchý CRUD přehled duplikoval běžné funkce existujících správců souborů a práce s terminálem přes samostatné vstupní pole nepůsobila jako přirozená konzole. Cílem aplikace je pohodlně propojit kvalitní portable nástroje, nikoli znovu implementovat samostatný file manager.

**Rozhodnutí:** Offline distribuce přibaluje oficiální portable Windows x64 ZIP Double Commanderu 1.2.8 pod `modules/filemanager/1.2.8`. Archiv i vstupní EXE mají připnutý SHA-256 a runtime inventář před spuštěním ověřuje metadata. Aplikace předá oba panely do `instances/default/www`, konfiguraci izoluje přes `--config-dir` do `state/doublecmd` a F4 propojí s Notepad++ přes procesní `%PORTABLE_DEVELOPER_EDITOR%`, nikoli trvalou absolutní cestu. Terminál zůstává omezeným vlastním interpretem, ale příkaz se píše přímo za prompt do jediné konzolové plochy; UI drží historii bez změny bezpečnostního modelu parseru.

**Důsledky:** Uživatel získává osvědčené dvoupanelové operace, klávesové zkratky a práci s archivy bez dalšího vlastního backendu. Double Commander je plnohodnotný externí proces a počáteční `www` není sandbox; může přejít i mimo kořen aplikace a UI na to upozorňuje. Portable Developer nevlastní jeho procesní životní cyklus a při zavření jej násilně neukončuje, aby nepřerušil rozpracované souborové operace.

## ADR-024 — Single-file aplikace bez extrakce nativních knihoven

- Stav: přijato
- Datum: 2026-08-22

**Kontext:** Self-contained WPF publish ukládal do kořene distribuce více než dvě stě spravovaných .NET knihoven. Ty jsou nutné pro běh bez nainstalovaného .NET, ale znepřehledňují uživatelský kořen a ztěžují nalezení hlavního EXE.

**Rozhodnutí:** Release publikuje spravovanou aplikaci a .NET knihovny do jediného `PortableDeveloper.exe` pomocí `PublishSingleFile=true`. `IncludeNativeLibrariesForSelfExtract` zůstává vypnuté a trimming se nepoužívá. Nativní WPF knihovny proto zůstávají vedle EXE a aplikace při startu nerozbaluje runtime do `%TEMP%` ani uživatelského profilu.

**Důsledky:** Kořen distribuce obsahuje místo stovek DLL pouze hlavní EXE a několik nativních WPF knihoven. Výsledná velikost celé offline distribuce se zásadně nesníží, ale uživatelský layout bude přehlednější. Plně jediný self-extracting EXE není podporovanou release variantou, protože by zapisoval runtime mimo portable kořen.

## ADR-025 — Vestavěný projektový správce místo Double Commanderu

- Stav: přijato; nahrazuje část ADR-023 o správci souborů
- Datum: 2026-08-22

**Kontext:** Double Commander přidával desítky megabajtů a otevíral samostatné okno mimo hlavní workflow. Jeho plná volnost navíc vyžadovala výrazná upozornění, přestože běžný scénář potřebuje jen jednoduchou správu souborů webového projektu.

**Rozhodnutí:** Stránka Soubory znovu používá lehký interní `IWorkspaceFileManager` nad `instances/default/www`. Podporuje navigaci, vytvoření souboru či složky, přejmenování, potvrzené smazání a otevření souboru v přibaleném Notepad++. Normalizace cest chrání kořen projektu a odmítá operace přes reparse point; nejde však o obecný Windows sandbox pro spuštěný projektový kód. Double Commander, jeho metadata, konfigurace a binárky se z release odstraňují.

**Důsledky:** Správa běžných projektových souborů zůstává v jednom okně, distribuce je menší a uživatelské UI nepotřebuje vysvětlovat externí správce. Vestavěný správce záměrně nenahrazuje všechny funkce plnohodnotných nástrojů, například dvoupanelové kopírování nebo práci s archivy.

## ADR-026 — Retence dvou posledních release artefaktů

- Stav: přijato
- Datum: 2026-08-22

**Kontext:** Opakované offline publishování vytváří přibližně gigabajtové adresáře a historické preview sestavy rychle zaplnily desítky gigabajtů pracovního disku.

**Rozhodnutí:** Po úspěšném publishi spustí release skript `Cleanup-Releases.ps1` s výchozí retencí dvou nejnovějších release adresářů. Cleanup je omezený na repozitářové `artifacts/publish`, odmítá neočekávané názvy a vždy zachová adresář, z něhož běží libovolný proces. Počet uchovaných adresářů lze při publishi zvýšit.

**Důsledky:** Běžný vývoj automaticky neakumuluje staré binární stromy ani ZIPy. Pokud je starší release právě používán, dočasně zůstane nad retenční limit; odstraní se až při některém dalším publishi po ukončení jeho procesů.

## ADR-027 — Nezávislé služby a explicitní runtime závislosti UI

- Stav: přijato
- Datum: 2026-08-22

**Kontext:** Jeden globální přepínač nevyhovuje scénářům, kdy uživatel potřebuje jen web s databází, pouze Selenium nebo jinou kombinaci. Současně by samostatné řízení Apache a PHP porušilo jejich skutečnou FastCGI vazbu a automatické spouštění serverů při otevření phpMyAdminu by bylo překvapivé.

**Rozhodnutí:** Apache a PHP zůstávají jedním webovým lifecycle celkem. Start/stop webu je pouze na Přehledu, restart lze vyvolat z dashboardové karty a detailů Apache/PHP; uložení PHP konfigurace za běhu provede stejný bezpečný restart. MariaDB a Selenium se ovládají nezávisle. Bootstrap nové MariaDB vytvoří `portable_dev` pomocí dočasného startu a server následně zastaví. phpMyAdmin se zpřístupní pouze při současně běžícím webu a MariaDB a nikdy tyto služby nespustí bez explicitní uživatelské akce.

**Důsledky:** Stav služeb je předvídatelný a libovolné kombinace neplýtvají prostředky. Apache bez PHP není podporovaný režim, protože generovaná konfigurace je záměrně stavěná jako jeden ověřovaný webový stack. UI musí u závislých nástrojů vždy zobrazit, která služba chybí.

## ADR-028 — Centrální port manager bez zásahů do cizích procesů

- Stav: přijato
- Datum: 2026-08-22

**Kontext:** Portable balík se spouští na různých počítačích, kde mohou výchozí porty již používat systémové služby, jiný vývojový stack nebo druhá instance aplikace. Rozdělená konfigurace portů navíc umožňovala, aby UI zobrazovalo jinou hodnotu než controller.

**Rozhodnutí:** Apache HTTP, PHP FastCGI, MariaDB a Selenium používají jeden validovaný model uložený atomicky v `state/port-settings.json`. Samostatná stránka zobrazuje čtecí snímek TCP listenerů a ověřuje skutečnou možnost svázat vybraný localhost port. Porty musí být jedinečné, v rozsahu 1024–65535 a lze je uložit pouze při zastavených službách. Stávající Selenium nastavení slouží při prvním načtení jako migrační fallback. Každý lifecycle controller nadále provádí vlastní poslední kontrolu těsně před startem.

**Důsledky:** Uživatel vidí kolize před spuštěním a všechny obrazovky i generované konfigurace pracují se stejnými hodnotami. Snímek je časově omezený a závod mezi kontrolou a startem nelze odstranit, proto zůstává kontrola i v controllerech. Portable Developer pouze čte síťový stav; nikdy neukončuje cizí proces, neuvolňuje jeho port, nemění Windows služby, firewall ani registr.

## ADR-029 — Copyleft licence a oddělené podepisování vlastního kódu

- Stav: přijato
- Datum: 2026-08-22

**Kontext:** Projekt má být zcela otevřený a svobodně upravitelný, současně však nepodepsaný hlavní EXE blokuje Windows Smart App Control. Offline distribuce obsahuje více nezávislých open-source programů a Microsoft VC++ runtime, které Portable Developer nevlastní a nesmí přepodepisovat vlastním certifikátem.

**Rozhodnutí:** Vlastní zdrojový kód Portable Developeru se zveřejňuje pod `GPL-3.0-or-later`; příspěvky zůstávají pod stejnou licencí bez CLA a převodu copyrightu. Komponenty třetích stran zůstávají samostatnými programy pod vlastními licencemi a jsou evidovány v `THIRD-PARTY-NOTICES.md`. Veřejná CI ověřuje zdrojový build a testy. Projekt požádá SignPath Foundation o bezplatné podepisování pouze vlastních release binárek; upstream binárky si ponechají svůj původní podpis nebo nepodepsaný stav. Každý podpis musí navazovat na veřejný commit nebo tag a projít ručním schválením.

**Důsledky:** Každý může kód používat, auditovat, forkovat a distribuovat při zachování svobod GPL. Veřejný zdroj a CI zlepšují důvěryhodnost, samy však neodstraní blokaci Smart App Control. Dokud není schválený certifikát, reprodukovatelný release workflow a úplný licenční audit balíku, nepodepsané lokální sestavy se neoznačují jako oficiální veřejné binární releasy.

## ADR-030 — Reprodukovatelný online bootstrap release závislostí

- Stav: přijato; rozšiřuje ADR-014 a ADR-016
- Datum: 2026-08-22

**Kontext:** Zdrojový repozitář neobsahuje téměř gigabajt binárek, ale dosavadní publish vyžadoval ruční cache, konkrétní instalaci Laragonu na `E:` a VC DLL z `System32`. Nový přispěvatel proto nedokázal vytvořit shodný offline release pouze z veřejného repozitáře.

**Rozhodnutí:** Build-time skript `Fetch-Dependencies.ps1` načte jen jedenáct aktuálně používaných vstupů z `catalog/dependencies.lock.json`, stahuje přes HTTPS z omezených hostů do ignorované cache a každý soubor přijme pouze při přesné shodě SHA-256. Runtime aplikace downloader nadále neobsahuje. Apache používá dostupný přesný build 2.4.68-260617 VS18. Microsoft VC++ Redistributable se ověří hashem i Authenticode podpisem, připnutý WiX 6.0.2 z něj bez instalace vyjme x64 CAB a balení přijme jen allowlist sedmi DLL s přesným hashem, verzí a podpisem. `Publish-Windows.ps1 -OfflineDependencies` dovolí reprodukovat build pouze z již ověřené cache.

**Důsledky:** Čerstvý klon již nepotřebuje Laragon, ruční kopírování binárek ani stav hostitelského `System32`; první build potřebuje síť a další mohou být plně offline. Lock je záměrně úzký a není katalogem všech technologií. Aktualizace komponent je vědomá změna zdroje, verze a hashe v Gitu a musí projít skutečným publish a runtime smoke testem.
