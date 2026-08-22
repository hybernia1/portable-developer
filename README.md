# Portable Developer

[English](README.en.md) · **Čeština**

Portable Developer je přenosné lokální vývojové prostředí pro Windows 10/11 x64. Celá aplikace včetně serverů běží z jedné složky nebo externího disku. Neinstaluje Windows služby, neupravuje systémový `PATH`, registr ani firewall.

> **Otevřený projekt:** zdrojový kód je svobodný software pod licencí [GNU GPL v3 nebo novější](LICENSE). Binární verze 0.6.0 je hotový, ale zatím nepodepsaný release; Windows Smart App Control nebo SmartScreen jej může zablokovat. Ochranu Windows kvůli aplikaci nevypínej; stav, odpovědné osoby a plán podpisu popisuje [Code signing policy](docs/CODE_SIGNING_POLICY.md).

> Verze aplikace: **0.6.0**. Přibližně 54MiB self-contained základ obsahuje aplikaci, katalog a portable VC++ podporu. Apache 2.4.68, PHP 8.4.12, MariaDB 12.3.2, Selenium Server 4.47.0, geckodriver 0.37.1, Microsoft OpenJDK 25.0.3, Composer 2.10.2, Python 3.13.0, phpMyAdmin 5.2.3 a Notepad++ 8.9.2 si uživatel vybírá ve správci modulů.

## Co dnes funguje

- self-contained WPF aplikace; na cílovém počítači není potřeba .NET ani systémový Python;
- správce sedmi logických balíčků přímo v aplikaci s průběhem stahování, třemi pokusy, kontrolou HTTPS redirectu, SHA-256 a bezpečným portable stagingem;
- levé menu rozdělené na Prostředí, Servery, Vývoj a Aplikaci; stránky nenainstalovaných serverů a nástrojů se nezobrazují;
- český a anglický dashboard se stavem a kontrolou integrity modulů;
- řízený start/stop Apache + PHP FastCGI;
- více Apache webových projektů s vlastními `<id>.localhost` virtual hosty, document rootem a výchozí podporou `.htaccess` bez změny Windows `hosts`;
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
- malý online release přes `scripts/Publish-Online-Windows.ps1` a volitelná plně offline sestava přes `scripts/Publish-Windows.ps1`;
- konfigurace, data, logy i procesní stav pouze pod kořenem distribuce.

Správce modulů používá internet pouze po kliknutí na instalaci. Nepřijímá vlastní URL ani vzdálenou změnu katalogu: verze, zdroj a SHA-256 jsou součástí konkrétního vydání aplikace. Composer i pip mohou při samostatné instalaci projektové knihovny spustit její instalační logiku. Pro vytvoření Firefox relace musí být na cílovém počítači dostupný samotný Firefox; Selenium balíček obsahuje WebDriver, ne celý prohlížeč.

Composer pracuje s právě vybraným webovým projektem a podporuje například `php-webdriver/webdriver`. Nové projekty oddělují `composer.json` a `vendor` v projektovém kořeni od veřejného `public`; původní `instances/default/www` zůstává jako bezztrátový Default. Python ukládá projektové knihovny do `instances/default/python/packages`; základní runtime ani uživatelský profil Windows se tím nemění.

Vestavěný terminál nevolá `cmd.exe` ani PowerShell, nepřijímá roury, přesměrování či řetězení příkazů a sestavuje `PATH` jen z ověřených přibalených runtime. Spuštěný PHP nebo Python program je ale stále běžný uživatelský kód, nikoli Windows sandbox; terminál je proto určený pouze pro důvěryhodný projektový kód.

Vlastní `geckodriver.exe`, `chromedriver.exe` nebo `msedgedriver.exe` lze vložit do `drivers/custom/` a znovu načíst na stránce Selenium. Aplikace je nepřidává do systémového `PATH`; explicitní portable cesty zapisuje pouze do transientní konfigurace aktuálního běhu.

## Vývojové sestavení

```powershell
dotnet test PortableDeveloper.slnx --configuration Release
& .\scripts\Publish-Online-Windows.ps1 -Version 0.6.0
& .\scripts\Publish-Windows.ps1
```

Online skript vytvoří `artifacts/publish/PortableDeveloper-win-x64-0.6.0/`, odpovídající ZIP a `.sha256`; stáhne při tom pouze podepsaný Microsoft VC++ balík a vyjme z něj připnuté app-local DLL bez systémové instalace. Offline skript navíc předem stáhne a přibalí všechny serverové moduly. Obě varianty odmítnou přepsat existující portable data a po úspěchu ponechají dva nejnovější release výstupy.

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
  runtime/vcredist/
  modules/                 # vzniká instalací vybraných modulů
  drivers/                 # vzniká instalací Selenium / vlastního driveru
  tools/                   # vzniká instalací phpMyAdmin
  instances/
  logs/
  state/
  temp/
  release-manifest.json
```

Spravované .NET knihovny jsou součástí `PortableDeveloper.exe`; vedle něj zůstávají pouze nativní WPF knihovny, které se při startu nerozbalují do profilu ani `%TEMP%`. Runtime složky aplikace se vytvářejí pouze uvnitř distribuce. Po přesunu na jiný disk se konfigurace generuje z nového kořene.

## Odebrání aplikace

Portable Developer nemá instalátor ani systémovou odinstalaci. Zastav všechny spuštěné služby v Přehledu, zavři aplikaci a smaž její složku. Tím se odstraní také lokální projekty, databáze, konfigurace a logy uložené uvnitř této složky; před smazáním si proto zazálohuj `instances/`. Aplikace po sobě nezanechává Windows službu, položku v registru ani systémový `PATH`.

## Code signing policy

Budoucí oficiální binárky budou po schválení podepisovány podle veřejné [Code signing policy](docs/CODE_SIGNING_POLICY.md). **Free code signing provided by [SignPath.io](https://signpath.io/), certificate by [SignPath Foundation](https://signpath.org/).** Podpis se bude vztahovat pouze na vlastní `PortableDeveloper.exe`, nikoli na upstream runtime a nástroje.

## Licence

Portable Developer je poskytován pod licencí `GPL-3.0-or-later`. Můžeš jej používat, studovat, upravovat a sdílet; při distribuci odvozené verze musí příjemci dostat odpovídající zdrojový kód a stejné svobody podle GPL. Příspěvky přijímáme pod stejnou licencí bez převodu autorských práv a bez CLA.

Přibalené servery a nástroje jsou samostatné projekty s vlastními licencemi. Jejich přehled je v [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md).
