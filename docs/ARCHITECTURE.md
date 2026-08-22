# Architektura

## Technologický směr

- Windows 10/11 x64.
- C#, .NET 10 a WPF.
- Self-contained složková distribuce `win-x64`.
- Malý online portable základ a volitelné hashově ověřené runtime moduly.
- Žádný Docker, MSI, Windows služba, systémový `PATH`, registr ani firewall.

Koncový uživatel nepotřebuje .NET, Python, Java ani Visual C++ Redistributable nainstalované v systému. Self-contained základ obsahuje aplikaci a malé app-local VC++ DLL; ostatní runtime se instalují do portable kořene pouze na vyžádání.

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

Hlavní okno používá trvalou boční navigaci rozdělenou na Prostředí, Servery, Vývoj a Aplikaci. Přehled pouze agreguje skutečně nainstalované komponenty. Detail Apache, PHP, Databáze, Selenium, Composeru, Pythonu či editoru se do navigace přidá až po úspěšné instalaci a ověření příslušného balíčku.

Apache a PHP tvoří jeden technický webový celek: controller vždy spouští PHP FastCGI před Apachem a zastavuje je v opačném pořadí. Jeho start/stop je kvůli jednoznačnosti dostupný pouze na Přehledu; restart je dostupný i z detailu Apache/PHP a uložení PHP nastavení za běhu jej vyvolá automaticky. MariaDB a Selenium mají nezávislý lifecycle, takže lze provozovat web s databází, samotné Selenium nebo libovolnou jinou kombinaci. phpMyAdmin je pouze odkaz nad dvěma explicitními závislostmi a nic skrytě nespouští.

## Runtime balíčky a offline build

`RuntimePackageManager` nabízí sedm logických balíčků: Web, Databáze, Selenium, Composer, Python, Editor a phpMyAdmin. Uživatel vždy zahájí instalaci explicitně. Aplikace načte pouze `catalog/dependencies.lock.json` přibalený k vydání, povolí připnuté HTTPS hosty, sleduje i cílový host redirectu, stahuje přes jedinečný `.part` soubor a archiv přijme až po shodě SHA-256. Rozbalení odmítá únik z adresáře, symbolické odkazy a reparse pointy; normalizace probíhá pod `temp/package-installs` a hotový adresář se přesune do cíle až po ověření vstupního souboru. Při chybě se nově vytvořené cíle vrátí zpět a cizí či stávající data se nepřepisují.

`Publish-Online-Windows.ps1` vytváří přibližně 54MiB self-contained základ, ZIP a SHA-256. Přibalí jen vlastní aplikaci, katalogy, dokumenty a app-local VC++ podporu ověřenou z podepsaného Microsoft balíku. Původní `Publish-Windows.ps1` zůstává pro plně offline sestavu: `Fetch-Dependencies.ps1` stáhne a ověří všechny upstream archivy a balicí krok je předem normalizuje do release.

Katalog `catalog/modules.json` je runtime allowlist přesných verzí a hashů serverových vstupních souborů. `catalog/dependencies.lock.json` zamyká stahované archivy, normalizované vstupní soubory, zdroje a licence. Soubor `.portable-developer-module.json` či `.portable-developer-tool.json` dokládá původ konkrétní instalace. Samotná přítomnost stejně pojmenovaného EXE nestačí ke spuštění ani zobrazení stránky v menu.

## Composer, Python, editor, správce souborů a portable terminál

Composer 2.10.2 a Python 3.13.0 jsou nástroje s vlastním `.portable-developer-tool.json`; inventář ověřuje bezpečnou relativní cestu a SHA-256 vstupního souboru. Composer se spouští přes katalogově ověřené PHP CLI, Python přes explicitní `modules/python/<verze>/python.exe`. Oba používají společný portable command runner s `ArgumentList`, bez shellu, s pracovním adresářem, timeoutem, přesměrovaným výstupem a ukončením procesního stromu.

Composer spravuje vždy aktivní webový projekt, používá vlastní `state/composer` a `cache/composer` a pro UI operace vypíná pluginy i instalační skripty. Každý nový projekt má samostatný kořen `instances/default/projects/<id>`, vlastní `composer.json` a `vendor`; Apache standardně zpřístupní pouze jeho `public`. Původní `instances/default/www` zůstává beze změny jako projekt Default. Python knihovny se instalují pomocí `pip --target` do `instances/default/python/packages`; `PYTHONHOME`, uživatelské site-packages a globální pip konfigurace se nepoužívají. Základní Python zůstává čistý a přenosný i po přesunu disku.

Notepad++ 8.9.2 používá stejný hashově ověřovaný inventář jako ostatní nástroje. Balení přebírá jen minimální portable obsah s `doLocalConf.xml`, bez updateru, pluginů a zdrojových uživatelských dat. Samostatná spouštěcí služba předává soubor přes `ArgumentList`, nastaví pracovní adresář editoru a nepoužívá systémový shell ani asociace souborů. Pro české UI přidá dokumentovaný přepínač `-Lcs`; angličtina je vestavěná výchozí lokalizace. Editor je výslovně spuštěná uživatelská aplikace a po zavření Portable Developeru může zůstat otevřený, aby uživatel nepřišel o rozepsané změny.

Vstup balíčku je omezený na běžný název a volitelné verzovací omezení; URL, lokální cesty a libovolný shellový příkaz nejsou přijímány. Samostatný portable terminál používá vlastní parser, explicitní allowlist `php`, `composer`, `python` a interní příkazy pro soubory a lifecycle služeb. UI terminálu je jediná konzolová plocha s promptem, přímým vstupem a lokální historií; příkazový model se tím nemění. Nevolá `cmd.exe` ani PowerShell, odmítá shellové operátory a pracovní adresář omezuje na kořen aktivního webového projektu. `PATH` předávaný procesům sestavuje jen z ověřených runtime adresářů. Interpretovaný projektový kód však není OS sandbox a uživatel mu musí důvěřovat.

Správce souborů je lehká součást WPF aplikace nad aplikačním rozhraním `IWorkspaceFileManager`. Pracuje pouze s kořenem aktivního webového projektu, normalizuje relativní cesty, chrání kořen projektu a nepovolí operaci přes odkaz nebo reparse point. Jedna integrovaná lišta nabízí výběr projektu, historii Zpět, vytvoření a obnovení; názvy se zadávají v malém účelovém dialogu a vektorové ikony jsou lokální WPF resources bez další runtime závislosti. Běžné přejmenování a potvrzované odstranění probíhá přímo v UI; soubor se k editaci předá ověřenému portable Notepad++.

## Apache projekty

Projektový katalog se atomicky ukládá do `instances/default/config/web-projects.json`. Výchozí projekt ukazuje na existující `instances/default/www`, takže upgrade nic nepřesouvá. Nové projekty mají pevně spravovaný kořen `instances/default/projects/<id>` a volitelný relativní web root uvnitř něj; ID tvoří zároveň bezpečný host `<id>.localhost`. Katalog dovolí vyřadit projekt z Apache nebo jej odebrat z registrace, ale soubory nikdy automaticky nemaže.

Při každém startu Apache vzniknou nové name-based virtual hosty pod `temp/`. `localhost` obsluhuje Default a další projekty používají rezervovanou doménu `.localhost`, takže aplikace nemění systémový `hosts`. Apache načítá `mod_rewrite`; `AllowOverride All` a `AccessFileName .htaccess` jsou výchozí, ale podporu lze pro každý projekt vypnout. Všechny hosty poslouchají pouze na `127.0.0.1`, jejich adresáře používají `Require local` a `Options None`, takže Apache automaticky nenásleduje odkazy mimo projekt.

## Instance a porty

První instance se jmenuje `default`. Obsahuje vlastní konfiguraci, webový kořen, databázová data, stav a logy. Výchozí lokální porty jsou Apache `8080`, PHP FastCGI `9000`, MariaDB `3307` a Selenium `4444`.

Centrální `PortSettings` je jediným zdrojem portů pro všechny controllery a atomicky se ukládá do `state/port-settings.json`. Změna je povolena pouze při zastaveném Apache/PHP, MariaDB i Selenium. Validace vyžaduje čtyři různé neprivilegované porty a před uložením ověří jak aktuální TCP listenery, tak skutečnou možnost svázat localhost socket. Snímek listenerů je čistě čtecí diagnostika hostitelského Windows; aplikace cizí proces neukončuje, nemění jeho konfiguraci ani firewall. Každý controller navíc dostupnost svého portu znovu kontroluje těsně před startem, protože stav se může po uložení změnit.

Absolutní cesty mohou vzniknout jen v dočasné konfiguraci pod `temp/` pro konkrétní běh. Trvalá nastavení zůstávají relativní vůči kořenu aplikace.

## PHP nastavení

Uživatelská konfigurace PHP je strukturovaný model v `instances/<id>/config/php-settings.json`, nikoli volně editovaný vendor `php.ini`. Store před atomickým zápisem validuje číselné rozsahy, vztah `post_max_size >= upload_max_filesize` a názvy rozšíření proti pevnému allowlistu. Neznámý či poškozený JSON se nespouští a načtení bezpečně použije výchozí hodnoty.

Při startu stacku generátor vytvoří `temp/generated/<id>/apache-php/php.ini` z aktuálního portable kořene. Zapnout lze jen známé rozšíření, jehož `php_<název>.dll` skutečně existuje v ověřeném PHP modulu. `mbstring`, `mysqli`, `openssl` a `zip` jsou povinný základ a normalizace je vždy doplní. Volitelný `instances/<id>/config/php-custom.ini` se po kontrole typu souboru, nulových znaků a limitu 256 KiB připojí až za generovanou část. Jde o vědomý pokročilý override, který může přepsat hodnoty formuláře nebo porušit přenositelnost. Uložení za běhu nemění aktivní proces; nové hodnoty se použijí až po restartu webového stacku.

MariaDB se při prvním spuštění aplikace inicializuje automaticky, krátce se spustí pouze na `127.0.0.1:3307`, založí databázi `portable_dev` a opět se zastaví. Při dalších spuštěních aplikace zůstává zastavená až do explicitní uživatelské akce. Nová instance používá účet `root` bez hesla podle lokálního vývojového modelu; uživatel může heslo později nastavit v UI. Databázové příkazy dostávají aktuální heslo přes krátkodobý defaults soubor pod `temp/`, nikoli argument procesu nebo log. Databáze není vystavena síti a toto nastavení není produkční bezpečnostní model. Přehled velikostí čte metadata z `information_schema`, systémová schémata skrývá a uvádí součet dat a indexů jako orientační hodnotu.

phpMyAdmin je přibalený jako nástroj pod `tools/` a Apache jej zpřístupní jen z lokálního počítače na `/phpmyadmin/`. Používá cookie autentizaci: generovaná konfigurace obsahuje host a port MariaDB, ale nikdy databázové heslo. Její 32znakový cookie secret vzniká lokálně při prvním použití a zůstává ve stavu portable instance.

## Selenium a WebDriver

Selenium controller používá výhradně katalogově ověřený `selenium-server.jar` a explicitní `modules/jre/<verze>/bin/java.exe`. Balíček Selenium instaluje pouze Server a zmenšený Microsoft OpenJDK. Doporučený samostatný balíček přidá verzově shodný Chrome for Testing + ChromeDriver; EdgeDriver a geckodriver zůstávají explicitní volby. Samotný běžící Selenium Server nic nestahuje, Selenium Manager je vypnutý a systémový `PATH` se nepoužívá.

Katalogové drivery se ukládají pod `drivers/bundled/<browser>/<verze>/`, zapisují do společného `drivers.json` a inventář při každém načtení ověří jejich SHA-256. Portable Chrome leží pod `modules/browsers/chrome-for-testing/<verze>/`; systémový Edge, Chrome a Firefox se pouze detekuje ve známých Windows umístěních. `SeleniumBrowserEnvironmentInventory` spáruje browser s driverem a stav Ready přidělí jen kompatibilní dvojici. Grid stereotype obsahuje explicitní browser binárku i driver. Uživatel může do `drivers/custom/` vložit standardně pojmenovaný driver, který UI odliší jako uživatelský spustitelný kód. Aplikace hostitelský browser nemění ani neaktualizuje.

Master profily jsou vytvářené nebo importované pod `profiles/selenium/<id>/master`, po validaci se označí pouze ke čtení a zdrojová složka se dál nepoužívá. Doporučený workflow spustí browser s čistým profilem pod `temp/`; pokročilý import přijímá normalizovaný Chromium user-data root nebo Firefox profile root. Sidecar manifest eviduje browser, verzi, layout, počet a velikost souborů a SHA-256 každého souboru. C# jej ověří při načtení a vlastní Node znovu před relací. Capability `portable:profile=<id>` vytvoří kopii pod `temp/selenium-profiles/<token>`; Chromium dostane `--user-data-dir` i `--profile-directory`, Firefox `-profile`. Kopie se smaže při neúspěšném startu, WebDriver DELETE, Node stopu nebo periodickém zjištění zaniklé relace; osiřelé kopie po pádu se odstraní při příštím startu aplikace.

Počet souběžných relací a Selenium `session-timeout` se ukládá do `state/selenium-settings.json`; port pochází z centrálního `state/port-settings.json`. Při prvním přechodu se dříve uložený Selenium port použije jako migrační výchozí hodnota. Timeout představuje maximální neaktivitu, nikoli absolutní dobu běhu. Běžící relace UI načítá z lokálního GraphQL endpointu a ukončuje standardním `DELETE /session/{id}` až po potvrzení uživatele. Základní distribuce neobsahuje browser; volitelný Chrome for Testing se stáhne pouze po explicitní akci.

## Logování a jazyk

JSONL logy jsou pod `logs/` a nesmí obsahovat hesla ani tokeny. MariaDB heslo je uložené pouze v portable state souboru instance a není chráněné šifrováním hostitelského účtu, aby balík zůstal přenositelný. Volba češtiny/angličtiny je v `state/settings.json`, takže se přenáší spolu s aplikací.
