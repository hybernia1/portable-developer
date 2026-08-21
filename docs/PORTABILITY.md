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

Přesun celé složky mezi disky nesmí vyžadovat reinstalaci. Po přesunu aplikace regeneruje transientní konfiguraci a pokračuje se stejnými relativními daty.
