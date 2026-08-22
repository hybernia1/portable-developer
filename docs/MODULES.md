# Layout modulů

Online i offline distribuce používá normalizovaný tvar `modules/<druh>/<verze>/`. Online základ složky doplní až po explicitní instalaci ve správci modulů. Aplikace nikdy nehledá servery v systémovém `PATH` ani v Laragonu na cílovém počítači.

```text
modules/
  apache/2.4.68/bin/httpd.exe
  php/8.4.12/php-cgi.exe
  mariadb/12.3.2/bin/mariadbd.exe
  selenium/4.47.0/selenium-server.jar
  jre/25.0.3/bin/java.exe
  composer/2.10.2/composer.phar
  composer/2.10.2/.portable-developer-tool.json
  python/3.13.0/python.exe
  python/3.13.0/.portable-developer-tool.json
  editor/8.9.2/notepad++.exe
  editor/8.9.2/doLocalConf.xml
  editor/8.9.2/.portable-developer-tool.json
drivers/
  bundled/drivers.json
  bundled/edge/151.0.4129.101/msedgedriver.exe
  bundled/chrome/152.0.7977.54/chromedriver.exe
  bundled/firefox/0.37.1/geckodriver.exe
  custom/
profiles/
  selenium/<id>/profile.json
  selenium/<id>/profile.properties
  selenium/<id>/master/
```

Každý ze čtyř serverových modulů obsahuje `.portable-developer-module.json`. Apache a PHP mají navíc `.portable-developer-runtime.json`. Inventář ignoruje junctions a symbolické odkazy a přijímá jen bezpečné cesty uvnitř kořene aplikace.

Composer, Python a editor nejsou síťové servery, proto používají oddělená metadata `.portable-developer-tool.json`. Inventář ověřuje druh nástroje, verzi, relativní vstupní soubor a jeho SHA-256. Python release obsahuje čistý základ a pip; projektové balíčky patří do `instances/default/python/packages`, ne do `modules/python/`. Editor používá lokální konfiguraci ve svém adresáři a nemění systémové asociace souborů. Správce souborů je součást aplikace a nemá samostatný binární modul.

Samotné vložení souboru do `modules/` nestačí. Controller i podmíněná navigace vyžadují přesnou verzi v katalogu, odpovídající metadata a SHA-256 vstupního souboru. Připnuté verze jsou Apache 2.4.68, PHP 8.4.12, MariaDB 12.3.2 a Selenium 4.47.0.

WebDrivery mají vlastní layout mimo serverové moduly a žádný z uvedených adresářů nemusí na čisté instalaci existovat. Každý katalogově stažený driver se přidá do `drivers/bundled/drivers.json` a při každém načtení se kontroluje jeho SHA-256. Vlastní `geckodriver.exe`, `chromedriver.exe` a `msedgedriver.exe` patří do `drivers/custom/` nebo jeho běžných podadresářů; reparse points se neprocházejí. Z každého typu prohlížeče se použije nejvyšší rozpoznaná verze.

Portable browser používá layout `modules/browsers/chrome-for-testing/<verze>/chrome.exe`. Inventář přijme jen katalogovou verzi s odpovídajícím SHA-256 a spáruje ji s kompatibilním driverem. Systémový Edge, Chrome a Firefox se do portable adresářů nekopírují; jejich absolutní cesta existuje pouze v paměti a v transientní Selenium konfiguraci konkrétního běhu.
