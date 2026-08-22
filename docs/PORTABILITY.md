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
- během release buildu stahovat explicitní HTTPS zdroje z připnutého locku do repozitářové ignorované cache. Tento krok není součástí běžící aplikace a stažený obsah se přijme pouze při shodě SHA-256.
- po výslovné akci uživatele stáhnout projektovou knihovnu přes ověřený Composer nebo pip výhradně do portable projektu a cache.
- spustit hashově ověřený editor z `modules/editor/` s lokální konfigurací bez registrace asociací souborů.
- použít omezený terminál bez systémového shellu s pracovním adresářem pod `instances/default/www` a čistým `PATH` složeným jen z přibalených runtime;
- spravovat soubory pod `instances/default/www` přímo ve WPF aplikaci a otevírat je v přibaleném editoru.

Správci projektových knihoven přepisují domovské a cache cesty procesu pod kořen distribuce. Nepoužívají systémový shell, globální Composer/pip konfiguraci ani uživatelské site-packages. Instalovaná knihovna je cizí kód a může mít vlastní instalační chování; UI na tuto hranici upozorňuje před provedením operace.

Vestavěný terminál omezuje vlastní navigaci a volbu spustitelného souboru, ale nepředstavuje bezpečnostní sandbox Windows. Důvěryhodný PHP či Python program může používat běžná oprávnění uživatele a přistupovat i mimo portable kořen. Vestavěný správce souborů naproti tomu pracuje pouze pod projektovým `www`; tato hranice chrání hlavně před omylem v UI, nikoli před kódem spuštěným uživatelem.

Validované PHP volby se ukládají do `instances/<id>/config/php-settings.json`. Pokročilý uživatel může výslovně upravit `instances/<id>/config/php-custom.ini`; jeho obsah se připojí za bezpečně generovanou část a může ji přepsat. Skutečný `php.ini` vzniká před startem pod `temp/generated/<id>/apache-php/`, používá aktuální absolutní cesty uvnitř portable kořene a po přesunu disku se znovu vygeneruje. Za přenositelnost a bezpečnost ručních direktiv odpovídá uživatel.

Přesun celé složky mezi disky nesmí vyžadovat reinstalaci. Po přesunu aplikace regeneruje transientní konfiguraci a pokračuje se stejnými relativními daty.
