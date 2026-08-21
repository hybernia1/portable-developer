# Portable Developer

Portable Developer je přenosné lokální vývojové prostředí pro Windows 10/11 x64. Celá aplikace včetně serverů běží z jedné složky nebo externího disku. Neinstaluje Windows služby, neupravuje systémový `PATH`, registr ani firewall.

> Stav: aktivní prototyp s offline distribucí. Výsledný balík již obsahuje Apache 2.4.66, PHP 8.4.12, MariaDB 12.3.2, Selenium Server 4.47.0, Microsoft OpenJDK 25.0.3, Composer 2.9.4 a app-local Microsoft Visual C++ runtime. Uživatel nic nestahuje ani neimportuje.

## Co dnes funguje

- self-contained WPF aplikace; na cílovém počítači není potřeba .NET ani Python;
- český a anglický dashboard se stavem a kontrolou integrity modulů;
- řízený start/stop Apache + PHP FastCGI;
- transakční inicializace MariaDB do portable instance;
- plně offline sestavení přes `scripts/Publish-Windows.ps1`;
- konfigurace, data, logy i procesní stav pouze pod kořenem distribuce.

MariaDB start/stop controller, Selenium controller a uživatelská konfigurace PHP/Composeru jsou další kroky. Binárky a jejich runtime závislosti jsou už v balíku.

## Vývojové sestavení

```powershell
dotnet test PortableDeveloper.slnx --configuration Release
& .\scripts\Publish-Windows.ps1
```

Balicí skript při vývoji čte Apache, PHP, JRE a Composer z `E:\laragon\bin`, MariaDB a Selenium z lokální ignorované cache, ověří připnuté hashe a vytvoří nový výstup v `artifacts/publish/PortableDeveloper-offline-win-x64/`. Existující výstup úmyslně nepřepisuje, aby nezničil portable data.

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
    apache/ php/ mariadb/ selenium/ jre/ composer/
  instances/
  logs/
  state/
  temp/
  bundle-manifest.json
```

Runtime složky se vytvářejí pouze uvnitř distribuce. Po přesunu na jiný disk se konfigurace generuje z nového kořene.
