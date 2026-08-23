# Zásady soukromí

Portable Developer ve výchozím stavu neodesílá autorům projektu žádná data. Neobsahuje telemetrii, analytiku, reklamní SDK, automatické hlášení pádů ani automatickou kontrolu aktualizací.

Konfigurace, databáze, logy, dočasné soubory a stav procesů zůstávají v adresáři přenosné aplikace. Projekt záměrně nepoužívá uživatelský profil Windows jako úložiště svých dat.

## Síťová komunikace vyvolaná uživatelem

K síťové komunikaci může dojít pouze jako přímý důsledek funkce spuštěné uživatelem nebo kódu v jeho projektu:

- Apache, MariaDB, PHP FastCGI a Selenium naslouchají na lokálně nastavených portech;
- správce modulů může po kliknutí uživatele stáhnout přesně připnutý archiv Apache, PHP, MariaDB, Selenium, OpenJDK, spravovaného Firefoxu/geckodriveru nebo Chrome for Testing/ChromeDriveru, Composeru, Pythonu, Notepad++, phpMyAdminu či jejich závislosti z upstream serveru uvedeného v katalogu;
- Composer může při instalaci, aktualizaci nebo odebrání balíčku komunikovat s Packagist a zdroji daného balíčku;
- pip může při správě Python balíčků komunikovat s Python Package Indexem a zdroji daného balíčku;
- otevření phpMyAdminu, webového projektu nebo Selenium Gridu předá lokální adresu výchozímu prohlížeči;
- aplikace spuštěná uživatelem v PHP, Pythonu či Selenium může komunikovat podle svého vlastního kódu.
- vytvoření Selenium relace s cookie vaultem navštíví domény obsažené ve vaultu, aby WebDriver mohl cookies vložit do správného původu.

Portable Developer tato data nezprostředkovává autorům projektu. Upstream server při stažení modulu standardně uvidí síťové údaje spojení, například IP adresu. Provoz příslušných registrů, webů a uživatelského projektového kódu se řídí jejich vlastními zásadami.

Mezi možné provozovatele upstream služeb patří [GitHub](https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement), [Microsoft](https://www.microsoft.com/privacy/privacystatement), [PHP](https://www.php.net/privacy.php), [MariaDB](https://mariadb.com/privacy-policy/), [Apache Software Foundation](https://privacy.apache.org/policies/privacy-policy-public.html), [Mozilla](https://www.mozilla.org/privacy/), [Python Package Index](https://policies.python.org/pypi.org/Privacy-Notice/) a zdroje zvoleného Composer/Python balíčku. Přesný seznam hostů základních modulů je veřejný v `catalog/dependencies.lock.json`; aplikace jej na dálku nerozšiřuje.

## Cookie vault

Importovaný JSON se zpracuje lokálně. Aplikace ponechá pouze název a hodnotu cookie, doménu, cestu, expiraci, `httpOnly`, `secure` a `sameSite`; prošlé, neplatné, duplicitní a pomocné položky rozšíření zahodí. Hodnoty se ukládají pomocí AES-256-GCM a automaticky vytvořeného 256bitového klíče pod `state/selenium-cookie-vault.key`. Název vaultu, počet cookies, domény a čas importu zůstávají v obálce čitelné pro UI. Původní export zůstává na zvoleném místě beze změny; po úspěšném importu jej musí zabezpečit nebo odstranit uživatel.

Java Node rozšifruje payload přímo v paměti pouze při vytváření relace; čitelný dočasný soubor nevzniká. Klíč je kvůli přenositelnosti uložený ve stejné portable složce, takže šifrování chrání hlavně samostatně zkopírovaný vault a odhalí jeho poškození, nikoli krádež celé složky. Operační systém ani aplikace nejsou bezpečnostní sandbox. Cookies mohou představovat plnohodnotné přihlašovací údaje, proto exporty, `profiles/` ani `state/` nezveřejňujte a nezahrnujte do Gitu.

Browser master profil může obsahovat aktivní přihlášení, lokálně uložená hesla, záložky, rozšíření a data synchronizovaného browser účtu. Portable Developer tato data nečte do UI ani je neodesílá autorům projektu. Při Selenium relaci se používá zahoditelná kopie s vypnutou cloudovou synchronizací; samotný master ale zůstává citlivý a při přenosu celé portable složky se přenese spolu s ní. Stažené soubory zůstávají ve společném projektovém `seldownloads`, dokud je uživatel neodstraní.

## Diagnostické údaje

Logy vznikají lokálně v adresáři `logs/`. Cookie hodnoty, názvy cookies ani šifrovací klíče se do aplikačních logů nezapisují. Uživatel logy odesílá jiné osobě pouze tehdy, když se pro to sám rozhodne. Před zveřejněním logu je vhodné zkontrolovat cesty, názvy projektů a výstup spuštěného kódu.

## Kontakt a změny

Dotazy a návrhy lze založit v [GitHub Issues](https://github.com/hybernia1/portable-developer/issues). Podstatné změny těchto zásad budou uvedeny v changelogu projektu.
