# Hranice portability

Spuštěná aplikace smí zapisovat pouze pod vlastní kořenovou složku. Typickými cíli jsou `instances/`, `logs/`, `state/` a `temp/`.

## Aplikace nesmí

- instalovat nebo registrovat Windows služby;
- zapisovat do registru, `System32`, uživatelského profilu nebo systémového `PATH`;
- měnit firewall, hosts soubor nebo systémová síťová nastavení;
- spouštět MSI, `vc_redist.exe` ani jiný systémový instalátor;
- za běhu stahovat serverové moduly nebo runtime;
- ukládat trvalé absolutní cesty k aktuálnímu disku.

## Povolené chování

- spouštět explicitní binárky pod `modules/` jako podřízené procesy;
- poslouchat pouze na nakonfigurovaných lokálních portech;
- při každém startu vytvořit dočasnou konfiguraci s aktuální absolutní cestou pod `temp/`;
- vytvářet data a tajemství konkrétní instance pouze pod `instances/<id>/`;
- během release buildu číst explicitní externí zdroje komponent. Tento balicí krok není součástí běžící aplikace.
- po výslovné akci uživatele stáhnout projektovou knihovnu přes ověřený Composer nebo pip výhradně do portable projektu a cache.

Správci projektových knihoven přepisují domovské a cache cesty procesu pod kořen distribuce. Nepoužívají systémový shell, globální Composer/pip konfiguraci ani uživatelské site-packages. Instalovaná knihovna je cizí kód a může mít vlastní instalační chování; UI na tuto hranici upozorňuje před provedením operace.

PHP volby se ukládají pouze do `instances/<id>/config/php-settings.json`. Skutečný `php.ini` vzniká před startem pod `temp/generated/<id>/apache-php/`, používá aktuální absolutní cesty uvnitř portable kořene a po přesunu disku se znovu vygeneruje.

Přesun celé složky mezi disky nesmí vyžadovat reinstalaci. Po přesunu aplikace regeneruje transientní konfiguraci a pokračuje se stejnými relativními daty.
