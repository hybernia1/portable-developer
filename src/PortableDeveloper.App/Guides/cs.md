# Portable Developer v praxi

Tyto návody platí pro prostředí spravované aplikací. Ukázky používají aktuální porty z Port Manageru a fungují bez systémového PATH, Dockeru nebo browseru nainstalovaného ve Windows.

> Návody jsou součástí konkrétní verze aplikace a fungují offline. ID profilů a cookie vaultů vždy kopírujte z rozhraní aplikace.

## Kapitoly

1. Příprava prostředí a lokální endpointy
2. Selenium s Pythonem
3. Selenium s PHP
4. Master profily a cookie vaulty
5. Stahování souborů
6. PHP s MariaDB
7. Portable pravidla pro vlastní skripty
8. Interaktivní portable terminál

## 1. Příprava prostředí

Štítky: začínáme, moduly, selenium

1. V Modulech nainstalujte Selenium a alespoň jeden kompletní browser pack.
2. Spusťte Selenium Server.
3. Pro Python nainstalujte runtime a na stránce Python přidejte přímý balíček selenium.
4. Pro PHP nainstalujte Composer a v aktivním projektu přidejte php-webdriver/webdriver.
5. Pokud používáte master profil nebo cookie vault, zkopírujte jeho ID z karty v Selenium.

Projekty jsou společné pracovní prostory. V záložce Projekty vyberte položku ze seznamu a její nástroje i webové nastavení najdete v jediném detailu vpravo. Webový kořen, zapnutí v Apache a `.htaccess` se ukládají společně; změny běžícího Apache použijte samostatným tlačítkem pro restart. Při zapnutí webové podpory aplikace vytvoří výchozí `index.html`, pokud ještě neexistuje, takže úvodní stránka funguje i bez PHP.

Portable Python je záměrně čistý runtime. Knihovna selenium není součástí základního modulu a její explicitní instalace udržuje prostředí menší a předvídatelné.

### Aktuální lokální endpointy

- Apache: http://127.0.0.1:{{APACHE_PORT}}
- MariaDB: 127.0.0.1:{{MARIADB_PORT}}
- Selenium: http://127.0.0.1:{{SELENIUM_PORT}}

## 2. Selenium s Pythonem

Štítky: selenium, python, master profil

Ukázka používá spravovaný Firefox. Pro Chrome změňte import Options na selenium.webdriver.chrome.options. PROFILE_ID nahraďte hodnotou zkopírovanou z aplikace.

```python
from selenium import webdriver
from selenium.webdriver.firefox.options import Options

options = Options()
options.set_capability("portable:profile", "PROFILE_ID")

driver = webdriver.Remote(
    command_executor="http://127.0.0.1:{{SELENIUM_PORT}}",
    options=options,
)
try:
    driver.get("https://example.com/")
    print(driver.title)
finally:
    driver.quit()
```

Relaci vždy ukončete pomocí quit(), ideálně v bloku finally. Aplikace pak může odstranit její dočasnou pracovní kopii profilu.

## 3. Selenium s PHP

Štítky: selenium, php, composer

Nejprve na stránce Composer přidejte php-webdriver/webdriver. Balíček i adresář vendor zůstanou u aktivního projektu.

```php
<?php
require __DIR__ . '/vendor/autoload.php';

use Facebook\WebDriver\Remote\DesiredCapabilities;
use Facebook\WebDriver\Remote\RemoteWebDriver;

$capabilities = DesiredCapabilities::firefox();
$capabilities->setCapability('portable:profile', 'PROFILE_ID');

$driver = RemoteWebDriver::create(
    'http://127.0.0.1:{{SELENIUM_PORT}}',
    $capabilities
);
try {
    $driver->get('https://example.com/');
    echo $driver->getTitle();
} finally {
    $driver->quit();
}
```

## 4. Master profil a cookie vault

Štítky: selenium, master profil, vault, cookies

portable:profile načte kompletní neměnný master profil. Obsahuje přihlášení, rozšíření, záložky a další stav browseru. Každá relace používá vlastní dočasnou kopii a nikdy nezapisuje zpět do masteru. Profil musí patřit stejnému typu spravovaného browseru.

portable:vault vloží pouze normalizované cookies. Je lehčí a vhodný pro jedno přihlášení bez přenosu celého profilu. Vault nepotřebuje účet v prohlížeči ani cloudovou synchronizaci a lze jej použít samostatně nebo společně s master profilem. Potřebuje však platné exportované cookies; pokud web relaci zruší nebo cookies vyprší, je nutné vault znovu importovat.

```python
options.set_capability("portable:profile", "PROFILE_ID")
options.set_capability("portable:vault", "VAULT_ID")
```

Název profilu ani vaultu není jeho capability ID. Použijte tlačítko Kopírovat ID na příslušné kartě.

## 5. Stahování souborů

Štítky: selenium, downloads, soubory

Vlastní download adresář nenastavujte ve Firefox nebo Chrome options. Nejdříve povolte stahování v nastavení Selenium. Server potom uloží soubory do složky seldownloads aktivního projektu nezávisle na profilu a relaci.

```python
from pathlib import Path

project_root = Path(__file__).resolve().parent
downloads = project_root / "seldownloads"

for downloaded_file in downloads.iterdir():
    print(downloaded_file.name)
```

Obsah seldownloads je trvalý uživatelský obsah. Ukončení relace jej nemaže a Apache k této složce nemá přístup.

## 6. PHP a MariaDB

Štítky: php, mariadb, databáze

Výchozí lokální účet je root bez hesla a první databáze je portable_dev. Pokud jste údaje změnili, upravte je také ve skriptu.

```php
<?php
$db = new mysqli(
    '127.0.0.1',
    'root',
    '',
    'portable_dev',
    {{MARIADB_PORT}}
);
$db->set_charset('utf8mb4');

$rows = $db->query('SELECT NOW() AS server_time');
echo $rows->fetch_assoc()['server_time'];
```

## 7. Portable pravidla pro vlastní skripty

Štítky: portable, bezpečnost, cesty

- Používejte 127.0.0.1 a porty z Port Manageru.
- Nespoléhejte na systémový PATH ani hostitelský browser.
- Cesty sestavujte relativně k projektu, ne pomocí pevného písmene disku.
- Citlivá ID nevypisujte do veřejných logů a necommitujte profily, vaulty ani databázová data.
- Dlouhé operace ukončujte korektně, aby nezůstávaly browsery a pracovní relace.

## 8. Interaktivní portable terminál

Štítky: terminál, python, php, portable

Programy spuštěné přibaleným Pythonem nebo PHP mohou průběžně vypisovat výstup a číst vstup po řádcích přímo v terminálu aplikace. Fungují tedy i skripty používající Python `input()` nebo standardní vstup PHP. Enter odešle aktuální řádek. Ctrl+C bez označeného textu ukončí běžící proces i jeho vlastněné podprocesy.

Python běží v UTF-8 a nebufferovaném režimu, takže se správně zobrazí české znaky i výzvy bez koncového odřádkování. Terminál záměrně nezpřístupňuje `cmd.exe`, PowerShell ani libovolné spustitelné soubory. Znaky `<`, `>`, `|`, `&` a zpětný apostrof předává jako běžný text; nevytvářejí roury, přesměrování ani řetězení shellových příkazů.

Úplný seznam příkazů zobrazí `help`. Mezi bezpečné projektové příkazy patří `ls`, `find`, `grep`, `tree`, `cd`, `mkdir`, `cat`, `touch`, `write`, `append`, `cp`, `mv`, `rm`, `rmdir` a `echo`. `grep` čte jen soubory UTF-8 do 1 MiB, zatímco `find` a `tree` mají omezený výstup. `write` vytvoří nový soubor UTF-8, `write --force` jej explicitně přepíše a `append` přidá text. Mazání je omezené na jeden soubor nebo jednu prázdnou složku; rekurzivní mazání a cesty mimo aktivní projekt jsou zablokované.

Python balíčky instalujte a odebírejte pouze na stránce Python. Terminál odmítne `python -m pip` i `python -m ensurepip`, aby zůstal ověřený Python runtime a portable evidence balíčků konzistentní. Python a PHP kód projektu stále běží s oprávněními aktuálního uživatele Windows; terminál pomáhá držet hranici projektu, není to sandbox operačního systému.
