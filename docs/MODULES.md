# Layout modulů

Offline distribuce používá normalizovaný tvar `modules/<druh>/<verze>/`. Aplikace nikdy nehledá servery v systémovém `PATH` ani v Laragonu na cílovém počítači.

```text
modules/
  apache/2.4.66/bin/httpd.exe
  php/8.4.12/php-cgi.exe
  mariadb/12.3.2/bin/mariadbd.exe
  selenium/4.47.0/selenium-server.jar
  jre/25.0.3/bin/java.exe
  composer/2.9.4/composer.phar
```

Každý ze čtyř serverových modulů obsahuje `.portable-developer-module.json`. Apache a PHP mají navíc `.portable-developer-runtime.json`. Inventář ignoruje junctions a symbolické odkazy a přijímá jen bezpečné cesty uvnitř kořene aplikace.

Samotné vložení souboru do `modules/` nestačí. Controller vyžaduje přesnou verzi v katalogu, odpovídající metadata a SHA-256 vstupního souboru. Přibalené verze jsou Apache 2.4.66, PHP 8.4.12, MariaDB 12.3.2 a Selenium 4.47.0.
