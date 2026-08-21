# Změny

Formát vychází z principů [Keep a Changelog](https://keepachangelog.com/) a data používají ISO 8601.

## [Unreleased]

### Added

- WPF aplikace na .NET 10 se self-contained `win-x64` publikací.
- Portable resolver cest, process supervisor, command runner, TCP health check a JSONL logování.
- Český/anglický dashboard a portable nastavení jazyka.
- Apache/PHP FastCGI controller s generovanou konfigurací, kontrolou portů a rollbackem.
- Transakční inicializace MariaDB bez Windows služby.
- Offline balicí skript pro Apache 2.4.66, PHP 8.4.12, MariaDB 12.3.2, Selenium 4.47.0, Microsoft OpenJDK 25.0.3 a Composer 2.9.4.
- App-local Microsoft VC++ runtime s kontrolou podpisu, minimální verze a SHA-256 metadat.
- Ověření serverových vstupních souborů proti lokálnímu katalogu; dashboard zobrazuje pouze ověřený stav **Připraveno**.

### Changed

- Distribuce je od prvního spuštění plně offline; serverové moduly a jejich runtime jsou součástí výsledné složky.
- Katalog nyní popisuje přibalené komponenty a SHA-256 vstupních souborů, nikoli instalaci stažených archivů.
- Výchozí publish cesta je `artifacts/publish/PortableDeveloper-offline-win-x64/` a existující výstup se z bezpečnostních důvodů nepřepisuje.

### Removed

- Download katalog a instalace serverových balíčků z dashboardu.
- Tlačítko a aplikační workflow pro uživatelský import Visual C++ runtime.
- Runtime HTTP downloader, ZIP instalátor a související testovací implementace.

### Fixed

- Opraveno zablokování WPF vlákna při startovním logování.
- Opraveno kopírování obsahu modulových adresářů v offline balicím skriptu.
- Metadata katalogu i modulů nyní používají shodnou řetězcovou serializaci druhu modulu.
- Generovaná Apache konfigurace už nenačítá neexistující `mod_mpm_winnt.so`; Windows MPM je v přibaleném Apache staticky vestavěné.
