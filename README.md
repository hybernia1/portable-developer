# Portable Developer

Portable Developer je přenosné lokální vývojové prostředí pro Windows 10/11 x64. Celá aplikace včetně serverů běží z jedné složky nebo externího disku. Neinstaluje Windows služby, neupravuje systémový `PATH`, registr ani firewall.

> Verze aplikace: **0.2.0**. Aktivní prototyp s offline distribucí již obsahuje Apache 2.4.66, PHP 8.4.12, MariaDB 12.3.2, Selenium Server 4.47.0, geckodriver 0.37.1, Microsoft OpenJDK 25.0.3, Composer 2.10.2, Python 3.13.0 s pip 24.2, phpMyAdmin 5.2.3, Notepad++ 8.9.2 a app-local Microsoft Visual C++ runtime. První spuštění serverů nic nestahuje ani neimportuje.

## Co dnes funguje

- self-contained WPF aplikace; na cílovém počítači není potřeba .NET ani systémový Python;
- český a anglický dashboard se stavem a kontrolou integrity modulů;
- řízený start/stop Apache + PHP FastCGI;
- validované nastavení `php.ini`: paměť, upload/POST limity, timeout, vstupní proměnné, vývojové chyby a allowlist přibalených rozšíření;
- automatická transakční inicializace MariaDB, localhost start/stop a výchozí databáze `portable_dev`;
- přehled orientačních velikostí a vytváření dalších lokálních databází;
- volitelné heslo lokálního účtu `root` a přibalený phpMyAdmin s cookie přihlášením;
- řízený Selenium Standalone Grid s nastavením portu, počtu relací a limitu neaktivity;
- přehled běžících WebDriver relací, bezpečné ukončení relace a proklik do Selenium Hubu;
- ověřený přibalený Firefox driver a načítání vlastních Firefox, Chrome a Edge driverů;
- samostatná stránka Composeru s přehledem, přidáním a odebráním projektových PHP knihoven;
- samostatná stránka Pythonu s čistým přibaleným runtime a správou knihoven jen pod portable projektem;
- omezený terminál pro přibalené PHP, Composer a Python a pro start, stop, restart či stav lokálních služeb;
- správce souborů omezený na `instances/default/www`, napojený na přibalený Notepad++, s ochranou kořene a blokováním reparse pointů;
- stránka Nástroje s přibaleným portable Notepad++ a přímou editací volitelného `php-custom.ini`;
- plně offline sestavení přes `scripts/Publish-Windows.ps1`;
- konfigurace, data, logy i procesní stav pouze pod kořenem distribuce.

Composer i pip mohou při výslovné instalaci knihovny použít internet a spustit instalační logiku balíčku; serverové komponenty a základní runtime jsou nadále přibalené offline. Pro vytvoření Firefox relace musí být na cílovém počítači dostupný samotný Firefox; přibalený je WebDriver, ne celý prohlížeč.

Composer pracuje s projektem `instances/default/www` a podporuje například `php-webdriver/webdriver`. Python ukládá projektové knihovny do `instances/default/python/packages`; základní runtime ani uživatelský profil Windows se tím nemění.

Vestavěný terminál nevolá `cmd.exe` ani PowerShell, nepřijímá roury, přesměrování či řetězení příkazů a sestavuje `PATH` jen z ověřených přibalených runtime. Spuštěný PHP nebo Python program je ale stále běžný uživatelský kód, nikoli Windows sandbox; terminál je proto určený pouze pro důvěryhodný projektový kód.

Vlastní `geckodriver.exe`, `chromedriver.exe` nebo `msedgedriver.exe` lze vložit do `drivers/custom/` a znovu načíst na stránce Selenium. Aplikace je nepřidává do systémového `PATH`; explicitní portable cesty zapisuje pouze do transientní konfigurace aktuálního běhu.

## Vývojové sestavení

```powershell
dotnet test PortableDeveloper.slnx --configuration Release
& .\scripts\Publish-Windows.ps1
```

Balicí skript při vývoji čte Apache, PHP, JRE, čistý základ Pythonu a Notepad++ z `E:\laragon\bin`; Composer, MariaDB, Selenium a geckodriver bere z lokální ignorované cache. Vstupy ověří připnutými hashi a vytvoří nový výstup v `artifacts/publish/PortableDeveloper-offline-win-x64/`. Existující výstup úmyslně nepřepisuje, aby nezničil portable data.

## Dokumentace

- [Architektura](docs/ARCHITECTURE.md)
- [Portabilita](docs/PORTABILITY.md)
- [Offline katalog komponent](docs/PACKAGE_CATALOG.md)
- [Nativní runtime](docs/RUNTIMES.md)
- [Roadmapa](docs/ROADMAP.md)
- [Architektonická rozhodnutí](docs/DECISIONS.md)
- [Vývoj](docs/DEVELOPMENT.md)
- [Změny](CHANGELOG.md) a [pracovní záznam](docs/WORKLOG.md)

## Struktura distribuce

```text
PortableDeveloper/
  PortableDeveloper.App.exe
  catalog/
  modules/
    apache/ php/ mariadb/ selenium/ jre/ composer/ python/ editor/
  drivers/
    bundled/ custom/
  tools/
    phpmyadmin/
  instances/
  logs/
  state/
  temp/
  bundle-manifest.json
```

Runtime složky se vytvářejí pouze uvnitř distribuce. Po přesunu na jiný disk se konfigurace generuje z nového kořene.
