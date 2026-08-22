# Zásady soukromí

Portable Developer ve výchozím stavu neodesílá autorům projektu žádná data. Neobsahuje telemetrii, analytiku, reklamní SDK, automatické hlášení pádů ani automatickou kontrolu aktualizací.

Konfigurace, databáze, logy, dočasné soubory a stav procesů zůstávají v adresáři přenosné aplikace. Projekt záměrně nepoužívá uživatelský profil Windows jako úložiště svých dat.

## Síťová komunikace vyvolaná uživatelem

K síťové komunikaci může dojít pouze jako přímý důsledek funkce spuštěné uživatelem nebo kódu v jeho projektu:

- Apache, MariaDB, PHP FastCGI a Selenium naslouchají na lokálně nastavených portech;
- správce modulů může po kliknutí uživatele stáhnout přesně připnutý archiv Apache, PHP, MariaDB, Selenium, OpenJDK, geckodriveru, Composeru, Pythonu, Notepad++, phpMyAdminu nebo jejich závislosti z upstream serveru uvedeného v katalogu;
- Composer může při instalaci, aktualizaci nebo odebrání balíčku komunikovat s Packagist a zdroji daného balíčku;
- pip může při správě Python balíčků komunikovat s Python Package Indexem a zdroji daného balíčku;
- otevření phpMyAdminu, webového projektu nebo Selenium Gridu předá lokální adresu výchozímu prohlížeči;
- aplikace spuštěná uživatelem v PHP, Pythonu či Selenium může komunikovat podle svého vlastního kódu.

Portable Developer tato data nezprostředkovává autorům projektu. Upstream server při stažení modulu standardně uvidí síťové údaje spojení, například IP adresu. Provoz příslušných registrů, webů a uživatelského projektového kódu se řídí jejich vlastními zásadami.

Mezi možné provozovatele upstream služeb patří [GitHub](https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement), [Microsoft](https://www.microsoft.com/privacy/privacystatement), [PHP](https://www.php.net/privacy.php), [MariaDB](https://mariadb.com/privacy-policy/), [Apache Software Foundation](https://privacy.apache.org/policies/privacy-policy-public.html), [Mozilla](https://www.mozilla.org/privacy/), [Python Package Index](https://policies.python.org/pypi.org/Privacy-Notice/) a zdroje zvoleného Composer/Python balíčku. Přesný seznam hostů základních modulů je veřejný v `catalog/dependencies.lock.json`; aplikace jej na dálku nerozšiřuje.

## Diagnostické údaje

Logy vznikají lokálně v adresáři `logs/`. Uživatel je odesílá jiné osobě pouze tehdy, když se pro to sám rozhodne. Před zveřejněním logu je vhodné zkontrolovat cesty, názvy projektů a výstup spuštěného kódu.

## Kontakt a změny

Dotazy a návrhy lze založit v [GitHub Issues](https://github.com/hybernia1/portable-developer/issues). Podstatné změny těchto zásad budou uvedeny v changelogu projektu.
