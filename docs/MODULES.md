# Layout modulů

Offline distribuce používá normalizovaný tvar `modules/<druh>/<verze>/`. Aplikace nikdy nehledá servery v systémovém `PATH` ani v Laragonu na cílovém počítači.

```text
modules/
  apache/2.4.66/bin/httpd.exe
  php/8.4.12/php-cgi.exe
  mariadb/12.3.2/bin/mariadbd.exe
  selenium/4.47.0/selenium-server.jar
  jre/25.0.3/bin/java.exe
  composer/2.10.2/composer.phar
  composer/2.10.2/.portable-developer-tool.json
  python/3.13.0/python.exe
  python/3.13.0/.portable-developer-tool.json
drivers/
  bundled/drivers.json
  bundled/firefox/0.37.1/geckodriver.exe
  custom/
```

Každý ze čtyř serverových modulů obsahuje `.portable-developer-module.json`. Apache a PHP mají navíc `.portable-developer-runtime.json`. Inventář ignoruje junctions a symbolické odkazy a přijímá jen bezpečné cesty uvnitř kořene aplikace.

Composer a Python nejsou síťové servery, proto používají oddělená metadata `.portable-developer-tool.json`. Inventář ověřuje druh nástroje, verzi, relativní vstupní soubor a jeho SHA-256. Python release obsahuje čistý základ a pip; projektové balíčky patří do `instances/default/python/packages`, ne do `modules/python/`.

Samotné vložení souboru do `modules/` nestačí. Controller vyžaduje přesnou verzi v katalogu, odpovídající metadata a SHA-256 vstupního souboru. Přibalené verze jsou Apache 2.4.66, PHP 8.4.12, MariaDB 12.3.2 a Selenium 4.47.0.

WebDrivery mají vlastní layout mimo serverové moduly. Přibalené drivery musí být uvedené v `drivers/bundled/drivers.json` a při každém načtení se kontroluje jejich SHA-256. Vlastní `geckodriver.exe`, `chromedriver.exe` a `msedgedriver.exe` patří do `drivers/custom/` nebo jeho běžných podadresářů; reparse points se neprocházejí. Z každého typu prohlížeče se použije nejvyšší rozpoznaná verze.
