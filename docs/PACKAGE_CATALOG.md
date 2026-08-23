# Katalog stahovaných komponent

`catalog/modules.json` je lokální allowlist serverových vstupních souborů. `catalog/dependencies.lock.json` obsahuje přesné upstream archivy, jejich SHA-256, verze, normalizované vstupní soubory a licence. Oba katalogy jsou součástí aplikačního release; běžící aplikace nepřijímá vzdálenou aktualizaci katalogu ani uživatelskou URL.

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
| Apache | 2.4.68 | `bin/httpd.exe` |
| PHP | 8.4.12 | `php-cgi.exe` |
| MariaDB | 12.3.2 | `bin/mariadbd.exe` |
| Selenium Server | 4.47.0 | `selenium-server.jar` |

JRE 25.0.3, Composer 2.10.2, Python 3.13.0 s pip 24.2, Notepad++ 8.9.2 a phpMyAdmin 5.2.3 jsou přibalené závislosti či nástroje evidované v `bundle-manifest.json`. Katalog navíc nabízí dvě celé browser sestavy: Chrome for Testing + ChromeDriver 152.0.7977.54 a Mozilla Firefox 142.0 + geckodriver 0.37.1. Všechny stažené soubory se ověřují jako celek ještě v cache a normalizované EXE druhým hashem; Firefox instalátor se navíc přijme jen s očekávaným podpisem Mozilla a spustí se výhradně v režimu `/ExtractDir`. Notepad++ se balí v minimálním portable režimu bez updateru, pluginů a zdrojových uživatelských dat. phpMyAdmin se ověřuje také pomocí release markeru a `composer.lock` a balí se bez lokálního `config.inc.php`, adresáře `setup` a dočasných dat.

## Instalace za běhu

1. Uživatel zvolí jeden z hlavních balíčků Web, Databáze, Selenium, Composer, Python, Editor či phpMyAdmin nebo celý spravovaný browser balíček na kartě Selenium.
2. Downloader použije pouze povolené HTTPS zdroje z locku, kontroluje i cílový host redirectu a při dočasné chybě provede nejvýše tři pokusy.
3. Archiv se zapisuje do `downloads/packages/<id>/<verze>/` přes jedinečný `.part` soubor a do cache se přesune až po shodě SHA-256.
4. Bezpečné rozbalení pod `temp/package-installs/<guid>` odmítne traversal, symbolické odkazy a reparse pointy.
5. Normalizovaný vstupní soubor se ověří druhým hashem; server nebo nástroj dostane lokální metadata o verzi a původu.
6. Ověřený adresář se atomicky přesune do `modules/`, `modules/browsers/`, `drivers/` nebo `tools/`. Existující cíl se nikdy nepřepisuje a při chybě se nově vytvořené cíle odstraní.

Apache a PHP používají app-local VC++ DLL z malého základního release. Selenium je jeden logický balíček složený ze Selenium Serveru a Microsoft OpenJDK; žádný driver se neinstaluje automaticky. phpMyAdmin doplní chybějící Apache, PHP a MariaDB; Composer doplní chybějící PHP.

## Plně offline release postup

1. `Fetch-Dependencies.ps1` stáhne chybějící přesné upstream soubory přes HTTPS do ignorované cache, použije dočasný `.part` soubor a přijme je pouze při shodě SHA-256.
2. `dotnet publish` vytvoří self-contained single-file aplikaci do nové složky; nativní WPF knihovny ponechá vedle EXE bez runtime extrakce při startu.
3. `Bundle-OfflineDependencies.ps1` znovu ověří všechny soubory v cache a rozbalí je do jednorázového staging adresáře.
4. Apache, PHP, JRE, Composer, Python, Notepad++, MariaDB a Selenium se normalizují do `modules/`; phpMyAdmin do `tools/phpmyadmin/`. Plný offline výstup stejně jako čistý online základ začíná bez driveru; uživatel jej doplní na kartě Selenium. Ze zdrojového PHP se odstraní všechny varianty `php.ini*`, protože runtime konfiguraci generuje aplikace. Z OpenJDK se ponechá obraz včetně `javac`/`jar` potřebných pro transparentní Selenium profilové rozšíření, ale ne `jmods`, hlavičky, manuály ani zdrojový archiv JDK.
5. Podepsaný Microsoft VC++ Redistributable se ověří, připnutý WiX nástroj z něj bez instalace vyjme x64 CAB a pouze přesně povolené, podepsané a hashově ověřené DLL přidá app-local k Apache a PHP.
6. Každý serverový modul dostane `.portable-developer-module.json`; celý výstup dostane `bundle-manifest.json`.
7. Hash vstupního souboru každého serveru se znovu ověří v cílové složce.
8. Python se zkopíruje bez zdrojových `Scripts` a `site-packages`; pip se vytvoří offline přes `ensurepip` a výsledek se znovu ověří.
9. Notepad++ se zkopíruje bez updateru, pluginů, session a záloh; vedle vestavěné angličtiny zůstane pouze česká lokalizace.
10. Po úspěšném sestavení se bezpečný cleanup pokusí ponechat dva nejnovější release adresáře; release s běžícím procesem vždy zachová.
11. Kořen distribuce dostane `LICENSE`, `PRIVACY.md` a `THIRD-PARTY-NOTICES.md`; licenční a NOTICE soubory uvnitř převzatých komponent se při normalizaci nesmí odstranit.

Skript odmítne existující cílovou složku. Tím se release build nemůže omylem smíchat s uživatelskými daty nebo staršími moduly.

## Runtime ověření

Dashboard nejprve najde normalizovaný modul, ověří shodu metadat s katalogem a vypočítá aktuální SHA-256 jeho vstupního souboru. Apache a PHP navíc procházejí kontrolou app-local VC++ DLL. Úspěšný modul se zobrazí jako **Připraveno**; změněný nebo neúplný modul se nespustí.
