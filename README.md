# Portable Developer

Portable Developer is a self-contained Windows development environment for web apps and browser automation. Run Apache, PHP, MariaDB, Python, Composer and managed Selenium browsers directly from one folder — without installation, admin rights or system changes.

The application, configuration, projects, databases, optional modules, managed browsers, and automation profiles stay inside that folder or an external drive. It does not install Windows services or modify the system `PATH`, registry, file associations, hosts file, or firewall.

The project is free software under [GPL-3.0-or-later](LICENSE). Version **1.2.1** is a complete but currently unsigned release. Windows Smart App Control or SmartScreen may block it; do not disable Windows security to run the application. See the [code-signing policy](docs/CODE_SIGNING_POLICY.md).

## Highlights

- Self-contained WPF application; the host does not need .NET, Python, Java, or a Visual C++ runtime installation.
- Explicit in-app installation of Apache/PHP, MariaDB, Selenium/OpenJDK, Composer, Python, Notepad++, and phpMyAdmin.
- Pinned HTTPS sources, SHA-256 verification, safe archive extraction, atomic installation, repair, and cleanup of obsolete managed runtimes.
- Conditional navigation: pages appear only when their required module is installed and verified.
- Apache/PHP projects with `.localhost` virtual hosts, per-project web roots, and optional `.htaccess` support.
- MariaDB database management and local phpMyAdmin.
- Selenium with app-managed Chrome for Testing or Firefox, matching drivers, immutable authenticated master profiles, encrypted cookie vaults, persistent project downloads, session limits, and cleanup of transient session data.
- Composer and Python package management scoped to portable project directories.
- Restricted project terminal, project-rooted file manager, and optional portable editor.
- Czech and English application UI; English is the canonical documentation language.
- Storage management for disposable download, package, Composer, pip, and Selenium caches without touching projects, databases, profiles, cookie vaults, or downloads.

Portable Developer is not an operating-system sandbox. PHP, Python, Composer packages, Selenium tests, and project code run with the current Windows user's permissions.

## Download

Download the Windows x64 ZIP from [GitHub Releases](https://github.com/hybernia1/portable-developer/releases/latest) and verify it with the adjacent `.sha256` file. Extract the whole archive to a writable folder or external drive, then run `PortableDeveloper.exe`.

The small base ZIP contains the application, catalogs, notices, and app-local Visual C++ support. Optional modules are downloaded only after an explicit install action. The application accepts neither arbitrary package URLs nor remote catalog replacement.

## Build and test

The required .NET SDK is pinned in `global.json`.

```powershell
dotnet restore PortableDeveloper.slnx
dotnet format PortableDeveloper.slnx --verify-no-changes --no-restore
dotnet build PortableDeveloper.slnx --configuration Release --no-restore
dotnet test PortableDeveloper.slnx --configuration Release --no-build --no-restore
```

Create the public-style online package with:

```powershell
.\scripts\Publish-Online-Windows.ps1 -Version 1.2.1
```

The tag workflow rebuilds the same self-contained layout from public source and publishes the ZIP and checksum.

## Privacy, security, and removal

Portable Developer contains no telemetry, analytics, advertising SDK, automatic crash upload, or automatic update check. Network access occurs only because of an explicit user action or user project code. See [Privacy](PRIVACY.md), [Security](SECURITY.md), and [third-party notices](THIRD-PARTY-NOTICES.md).

To remove the application, stop its services, close it, and delete its folder. Back up `instances/`, `profiles/`, and project downloads first if they should be retained. No service, registry entry, firewall rule, or system `PATH` entry remains.

## Contributing

Contributions are welcome under GPL-3.0-or-later without a CLA or copyright assignment. See [Contributing](CONTRIBUTING.md), [Governance](GOVERNANCE.md), and the [Code of Conduct](CODE_OF_CONDUCT.md).

Future official binaries will be signed after project approval under the public [code-signing policy](docs/CODE_SIGNING_POLICY.md). **Free code signing provided by [SignPath.io](https://signpath.io/), certificate by [SignPath Foundation](https://signpath.org/).** Only project-owned binaries will be signed; upstream tools retain their original signatures or unsigned state.
