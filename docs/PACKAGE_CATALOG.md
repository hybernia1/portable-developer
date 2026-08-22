# Offline katalog komponent

`catalog/modules.json` je lokální allowlist komponent přibalený do aplikace. Neslouží jako download katalog a dashboard nenabízí žádné stahování. Release skript podle něj při sestavení ověřuje binárky a aplikace podle něj při každém použití kontroluje integritu modulů.

## Položka katalogu

```json
{
  "kind": "php",
  "version": "8.4.12",
  "sourceUrl": "https://windows.php.net/.../php-8.4.12-nts-Win32-vs17-x64.zip",
  "entrypointSha256": "64-znakový-hexadecimální-SHA-256",
  "entrypointRelativePath": "php-cgi.exe",
  "licenseUrl": "https://www.php.net/license/"
}
```

Položka používá HTTPS, bezpečnou relativní cestu, unikátní dvojici druhu/verze a přesně 64 znaků SHA-256. Aktuálně připnuté serverové moduly jsou:

| Modul | Verze | Vstupní soubor |
|---|---:|---|
| Apache | 2.4.66 | `bin/httpd.exe` |
| PHP | 8.4.12 | `php-cgi.exe` |
| MariaDB | 12.3.2 | `bin/mariadbd.exe` |
| Selenium Server | 4.47.0 | `selenium-server.jar` |

JRE 25.0.3, geckodriver 0.37.1, Composer 2.10.2, Python 3.13.0 s pip 24.2, Notepad++ 8.9.2, Double Commander 1.2.8 a phpMyAdmin 5.2.3 jsou přibalené závislosti či nástroje evidované v `bundle-manifest.json`. Archivy geckodriveru a Double Commanderu pro Windows x64 se před rozbalením ověřují připnutým SHA-256; u Double Commanderu se navíc ověří výsledný EXE. Composer, Python, editor a správce souborů dostávají samostatná hashová metadata nástroje. Notepad++ se balí v minimálním portable režimu bez updateru a zdrojových uživatelských dat. phpMyAdmin se přebírá z připraveného zdrojového stromu, ověřuje pomocí hashů release markeru a `composer.lock` a balí se bez lokálního `config.inc.php`, adresáře `setup` a dočasných dat.

## Release postup

1. `dotnet publish` vytvoří self-contained aplikaci do nové složky.
2. `Bundle-OfflineDependencies.ps1` načte předem připravené zdroje.
3. MariaDB archiv, Selenium JAR a geckodriver archiv se ověří proti připnutému SHA-256 ještě před kopírováním.
4. Apache, PHP, JRE, Composer, Python, Notepad++, Double Commander, MariaDB a Selenium se normalizují do `modules/`; geckodriver do `drivers/bundled/` a phpMyAdmin do `tools/phpmyadmin/`.
5. Podepsané Microsoft VC++ DLL se ověří a přidají app-local k Apache a PHP.
6. Každý serverový modul dostane `.portable-developer-module.json`; celý výstup dostane `bundle-manifest.json`.
7. Hash vstupního souboru každého serveru se znovu ověří v cílové složce.
8. Python se zkopíruje bez zdrojových `Scripts` a `site-packages`; pip se vytvoří offline přes `ensurepip` a výsledek se znovu ověří.
9. Notepad++ se zkopíruje bez updateru, pluginů, session a záloh; vedle vestavěné angličtiny zůstane pouze česká lokalizace.
10. Double Commander se rozbalí z oficiálního portable ZIPu a dostane metadata s hashem `doublecmd.exe`; jeho uživatelská konfigurace vzniká až při spuštění pod `state/doublecmd`.

Skript odmítne existující cílovou složku. Tím se release build nemůže omylem smíchat s uživatelskými daty nebo staršími moduly.

## Runtime ověření

Dashboard nejprve najde normalizovaný modul, ověří shodu metadat s katalogem a vypočítá aktuální SHA-256 jeho vstupního souboru. Apache a PHP navíc procházejí kontrolou app-local VC++ DLL. Úspěšný modul se zobrazí jako **Připraveno**; změněný nebo neúplný modul se nespustí.
