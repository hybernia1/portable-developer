# Architecture

## Direction

Portable Developer targets Windows 10/11 x64 with C#, .NET 10, WPF, and a self-contained `win-x64` folder distribution. The base is small and optional runtimes are installed on demand from a hash-pinned catalog. The design excludes Docker, MSI installation, Windows services, system `PATH`, registry, hosts-file, and firewall changes.

```text
WPF UI
  -> Application contracts and use cases
       -> stack and instance controllers
       -> package, configuration, project, and Selenium services
  -> Infrastructure
       -> process supervision and health checks
       -> portable paths, inventories, persistence, and logging
  -> Portable root
       -> catalog, modules, drivers, instances, profiles, state, cache, logs, temp
```

The UI does not directly own `Process` objects or persist vendor-specific state. Controllers validate inventory, runtime dependencies, ports, and generated configuration before starting a service.

Navigation is capability-driven. Apache, PHP, Database, Selenium, Composer, Python, and Editor pages appear only after their required packages pass inventory verification. Apache and PHP form one web-stack lifecycle: PHP FastCGI starts before Apache and stops after it. MariaDB and Selenium remain independent. phpMyAdmin is an action that requires a running web stack and database; it does not silently start them.

## Packages and inventories

`catalog/modules.json` allowlists exact server entrypoints. `catalog/dependencies.lock.json` pins upstream archives, redirects, versions, archive hashes, normalized entrypoint hashes, sources, and licenses. Runtime installation uses a unique `.part` download, verifies SHA-256, rejects traversal, links, and reparse points, normalizes under `temp/package-installs`, verifies the result, and moves it into place atomically. A failed install rolls back only paths it created.

Server modules receive `.portable-developer-module.json`; tools receive `.portable-developer-tool.json`; Apache/PHP native runtime data is recorded separately. An EXE with the expected name is never sufficient for readiness.

Verified fixed-target packages can repair an incomplete installation through a staged transactional replacement. Successful upgrades remove only older verified catalog-managed browser/driver versions. Download archives and reproducible package caches are deleted after successful use and can be cleared from Settings without touching user data.

## Projects and tools

Web projects are stored in `instances/default/config/web-projects.json`. The legacy default project root remains `instances/default/www`, while its Apache document root is the safe `www/public` subdirectory; migration creates only a missing starter page and never overwrites project files. New projects live under `instances/default/projects/<id>` with an optional web root and host `<id>.localhost`. Apache generates transient virtual-host configuration under `temp/`, binds to `127.0.0.1`, uses `Require local`, and never edits the Windows hosts file.

Composer runs through verified PHP and keeps `composer.json`/`vendor` in the active project. Python uses a verified explicit interpreter and installs packages into `instances/default/python/packages` with isolated home, site, config, and cache settings. An atomic portable Python requirements registry distinguishes user-selected roots from transitive packages and limits cleanup to the managed reachable graph. Both use an argument-list process runner without a system shell.

The terminal parses a small allowlist of PHP, Composer, Python, filesystem, and service commands. It rejects shell operators and keeps its logical working directory under the active project. This is an accidental-damage boundary, not an OS sandbox. The file manager enforces the same project root, rejects link/reparse escapes, and opens files with the Windows association when available or the verified portable editor as a fallback. Directory listings are sorted and paged before reaching WPF; editable paths are canonicalized against the active root and large UI collections use bounded virtualized viewports.

## UI operation model

Long-running runtime, package, storage, and filesystem work executes away from the WPF dispatcher. One application-wide operation state blocks conflicting input and reports progress while page-specific cards retain contextual detail. Dynamic collections own their scroll boundary; the application shell does not grow with package, project, session, database, listener, or directory counts.

Concrete WPF colors live only in `Assets/Theme.xaml` under semantic brush names. Pages, dialogs, item templates, icons, and view models consume those resources rather than defining their own palette. The active tools project is shared by Composer, the terminal, and the file manager, but server lifecycle state does not lock that selection; only an operation whose working directory would change mid-flight may delay it.

## Ports, PHP, and database

`state/port-settings.json` is the single source for Apache, PHP FastCGI, MariaDB, and Selenium ports. Changes require all services to be stopped, distinct non-privileged values, a read-only listener snapshot, and an actual localhost bind test. The application never terminates or reconfigures unrelated listeners.

Validated PHP settings live in `instances/<id>/config/php-settings.json`. A generated `php.ini` is rebuilt under `temp/generated` for the current drive. Optional `php-custom.ini` is a bounded advanced override. Saving while the web stack is running triggers a controlled restart.

MariaDB initializes its data directory transactionally, creates the local `portable_dev` database, and binds to localhost. The development-default `root` account has no password unless the user sets one. Credentials are passed through a short-lived defaults file, never process arguments or logs. phpMyAdmin uses cookie authentication and stores no database password.

## Selenium

Selenium uses verified Selenium Server and app-managed OpenJDK. A usable browser environment is always a catalog-matched pair: Chrome for Testing plus ChromeDriver, or Firefox plus geckodriver. System browsers and profiles are not scanned or consumed.

Profile enrollment and editing use a temporary managed-browser directory. The resulting normalized master is stored read-only under `profiles/selenium/<id>/master` with a manifest of file hashes. Editing is transactional and preserves the stable profile ID. Every Selenium session receives a writable copy under the bounded runtime workspace; cloud sync is disabled and the copy is removed after the session. Startup reconciliation cleans stale owned processes and transient session residue without touching immutable masters.

Firefox normalization conservatively removes only reproducible caches, crash reports, telemetry queues, and thumbnails. Authentication, extensions, site storage, bookmarks, history, password data, sync metadata, security state, codecs, and Safe Browsing data remain.

Cookie vaults are independent from profiles. Imported cookies are normalized and encrypted locally with AES-256-GCM. Java decrypts them in memory for session creation. Optional downloads are redirected to the active project's persistent `seldownloads` folder.

## Release forms

`Publish-Online-Windows.ps1` creates the public self-contained base ZIP and checksum, including only the application, catalogs, policies/notices, and verified app-local Visual C++ support. `Publish-Windows.ps1` creates a full offline aggregate after fetching and validating every catalog dependency. Release outputs never overwrite an existing target and retention keeps the two newest safe outputs.
