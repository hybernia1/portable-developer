# Portable Developer

Portable Developer je přenosné lokální vývojové prostředí pro Windows 10/11 x64. Celá aplikace včetně serverů běží z jedné složky nebo externího disku. Neinstaluje Windows služby, neupravuje systémový `PATH`, registr ani firewall.

> **Otevřený projekt:** zdrojový kód je svobodný software pod licencí [GNU GPL v3 nebo novější](LICENSE). Aktuální binární sestava 0.4.0 zatím není digitálně podepsaná a Windows Smart App Control ji může zablokovat. Ochranu Windows kvůli aplikaci nevypínej; stav a plán podpisu popisují [zásady podepisování](docs/CODE_SIGNING_POLICY.md).

> Verze aplikace: **0.4.0**. Aktivní prototyp s offline distribucí již obsahuje Apache 2.4.68, PHP 8.4.12, MariaDB 12.3.2, Selenium Server 4.47.0, geckodriver 0.37.1, Microsoft OpenJDK 25.0.3, Composer 2.10.2, Python 3.13.0 s pip 24.2, phpMyAdmin 5.2.3, Notepad++ 8.9.2 a app-local Microsoft Visual C++ runtime. První spuštění serverů nic nestahuje ani neimportuje.

## Co dnes funguje

- self-contained WPF aplikace; na cílovém počítači není potřeba .NET ani systémový Python;
- český a anglický dashboard se stavem a kontrolou integrity modulů;
- řízený start/stop Apache + PHP FastCGI;
- centrální správce portů se živou kontrolou kolizí a čtecím přehledem TCP listenerů hostitelského Windows;
- validované nastavení `php.ini`: paměť, upload/POST limity, timeout, vstupní proměnné, vývojové chyby a allowlist přibalených rozšíření;
- automatická transakční inicializace MariaDB, nezávislý localhost start/stop a výchozí databáze `portable_dev`;
- přehled orientačních velikostí a vytváření dalších lokálních databází;
- volitelné heslo lokálního účtu `root` a přibalený phpMyAdmin s cookie přihlášením;
- řízený Selenium Standalone Grid s centrálně spravovaným portem, počtem relací a limitem neaktivity;
- přehled běžících WebDriver relací, bezpečné ukončení relace a proklik do Selenium Hubu;
- ověřený přibalený Firefox driver a načítání vlastních Firefox, Chrome a Edge driverů;
- samostatná stránka Composeru s přehledem, přidáním a odebráním projektových PHP knihoven;
- samostatná stránka Pythonu s čistým přibaleným runtime a správou knihoven jen pod portable projektem;
- omezený terminál s přímým psaním do konzole, historií a příkazy pro přibalené PHP, Composer, Python i lokální služby;
- lehký správce souborů s integrovanou lištou, historií, vektorovými ikonami, vytvářením, přejmenováním, mazáním a otevřením projektových souborů v Notepad++;
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

Balicí skript nejprve podle `catalog/dependencies.lock.json` stáhne pouze přesné upstream archivy do ignorované složky `downloads/dependencies/`. Každý archiv ověří připnutým SHA-256; další sestavení používají cache a lze je vynutit bez sítě přepínačem `-OfflineDependencies`. Laragon, systémový Python ani DLL z `System32` nejsou potřeba. Výstup vznikne v `artifacts/publish/PortableDeveloper-offline-win-x64/`; existující složku skript úmyslně nepřepíše a po úspěchu ponechá dva nejnovější releasy i každý právě spuštěný release.

## Dokumentace

- [Architektura](docs/ARCHITECTURE.md)
- [Portabilita](docs/PORTABILITY.md)
- [Offline katalog komponent](docs/PACKAGE_CATALOG.md)
- [Komponenty třetích stran](THIRD-PARTY-NOTICES.md)
- [Nativní runtime](docs/RUNTIMES.md)
- [Roadmapa](docs/ROADMAP.md)
- [Architektonická rozhodnutí](docs/DECISIONS.md)
- [Vývoj](docs/DEVELOPMENT.md)
- [Přispívání](CONTRIBUTING.md), [bezpečnost](SECURITY.md), [soukromí](PRIVACY.md) a [podepisování](docs/CODE_SIGNING_POLICY.md)
- [Změny](CHANGELOG.md) a [pracovní záznam](docs/WORKLOG.md)

## Struktura distribuce

```text
PortableDeveloper/
  PortableDeveloper.exe
  D3DCompiler_47_cor3.dll
  PenImc_cor3.dll
  PresentationNative_cor3.dll
  vcruntime140_cor3.dll
  wpfgfx_cor3.dll
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

Spravované .NET knihovny jsou součástí `PortableDeveloper.exe`; vedle něj zůstávají pouze nativní WPF knihovny, které se při startu nerozbalují do profilu ani `%TEMP%`. Runtime složky aplikace se vytvářejí pouze uvnitř distribuce. Po přesunu na jiný disk se konfigurace generuje z nového kořene.

## Licence

Portable Developer je poskytován pod licencí `GPL-3.0-or-later`. Můžeš jej používat, studovat, upravovat a sdílet; při distribuci odvozené verze musí příjemci dostat odpovídající zdrojový kód a stejné svobody podle GPL. Příspěvky přijímáme pod stejnou licencí bez převodu autorských práv a bez CLA.

Přibalené servery a nástroje jsou samostatné projekty s vlastními licencemi. Jejich přehled je v [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
