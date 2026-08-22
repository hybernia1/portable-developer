# Portable Developer

**English** · [Čeština](README.md)

Portable Developer is a portable local development environment for Windows 10/11 x64. The application, its configuration, projects, databases, and optional server modules remain inside one folder or external drive. It does not install Windows services or modify the system `PATH`, registry, or firewall.

The project is free software licensed under [GPL-3.0-or-later](LICENSE). The current release is 0.8.0; published binaries remain unsigned until the code-signing process is complete. Windows Smart App Control or SmartScreen may block an unsigned executable; the project does not recommend disabling Windows security. See the public [Code signing policy](docs/CODE_SIGNING_POLICY.md).

## Features

- self-contained WPF application; no system .NET or Python installation is required;
- explicit in-app installation of Apache/PHP, MariaDB, Selenium, Composer, Python, Notepad++, and phpMyAdmin;
- pinned HTTPS sources, SHA-256 verification, safe archive extraction, and portable staging with rollback;
- conditional navigation: pages for uninstalled server modules are not shown;
- local Apache/PHP projects with `.localhost` virtual hosts and optional `.htaccess` support;
- local MariaDB database management and phpMyAdmin;
- Selenium Standalone management with compatible browser environments, an optional pinned Chrome for Testing + ChromeDriver pair, immutable profile masters, session limits, and session termination;
- Composer and Python package management scoped to portable project directories;
- a restricted project terminal and a project-rooted file manager;
- Czech and English user interfaces.

The port manager only reads local TCP listeners to prevent collisions with ports selected for Portable Developer services. It does not scan remote hosts, probe vulnerabilities, change unrelated listeners, or bypass security controls.

## Download and verification

Download the latest Windows x64 ZIP from [GitHub Releases](https://github.com/hybernia1/portable-developer/releases/latest). Each release provides a separate `.sha256` file. The small base ZIP contains the application, dependency catalog, notices, and app-local Visual C++ support; optional modules are downloaded only after a user clicks their install action.

Module downloads are restricted to the versioned catalog shipped with the release. The application does not accept arbitrary download URLs or remotely replace its catalog.

## Build and test

The required SDK is pinned in `global.json`.

```powershell
dotnet restore PortableDeveloper.slnx
dotnet format PortableDeveloper.slnx --verify-no-changes --no-restore
dotnet build PortableDeveloper.slnx --configuration Release --no-restore
dotnet test PortableDeveloper.slnx --configuration Release --no-build --no-restore
```

`scripts/Publish-Online-Windows.ps1` creates the same self-contained portable layout used by the public tag workflow. Release inputs are versioned and hash-pinned; the public workflow rebuilds from a public tag.

## Privacy and security

Portable Developer contains no telemetry, analytics, advertising SDK, automatic crash upload, or automatic update check. Network access happens only because of a user action or code in a user project. See [Privacy](PRIVACY.md), [Security](SECURITY.md), and [third-party notices](THIRD-PARTY-NOTICES.md).

The application is not an operating-system sandbox. PHP, Python, Composer packages, Selenium tests, and user projects run with the permissions of the current Windows user.

## Removal

Stop all running services, close Portable Developer, and delete its folder. This also deletes projects, databases, configuration, and logs stored there, so back up `instances/` first if those data should be retained. No Windows service, registry entry, firewall rule, or system `PATH` entry remains.

## Contributing and governance

Contributions are welcome under the same GPL-3.0-or-later license without a CLA or copyright assignment. See [Contributing](CONTRIBUTING.md), [Governance](GOVERNANCE.md), and the [Code of Conduct](CODE_OF_CONDUCT.md).

## Code signing policy

Future official binaries will be signed after project approval under the public [Code signing policy](docs/CODE_SIGNING_POLICY.md). **Free code signing provided by [SignPath.io](https://signpath.io/), certificate by [SignPath Foundation](https://signpath.org/).** Only the project-owned `PortableDeveloper.exe` will be signed; upstream runtimes and tools will retain their original signatures or unsigned state.
