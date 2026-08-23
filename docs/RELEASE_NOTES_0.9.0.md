# Portable Developer 0.9.0

Portable Developer 0.9.0 makes Selenium deterministic and portable: browsers, drivers, authenticated profiles, cookie vaults, downloads, and temporary session data now remain under the application root.

## Highlights

- Install complete, version-matched Firefox + geckodriver or Chrome for Testing + ChromeDriver environments from the Modules page. System browsers and their profiles are not used.
- Enroll an immutable signed-in master profile in an app-managed browser. Every WebDriver session receives a disposable copy and browser synchronization is disabled in that copy.
- Import a standard JSON cookie export into an automatically encrypted vault and select it through the `portable:vault` capability without storing readable temporary cookies.
- Allow Selenium downloads per server configuration. Files are saved into the active project's persistent `seldownloads` directory and remain available after the session ends.
- Use the unified dark window chrome, selects, scrollbars, progress states, confirmations, and maximized layout across the application.

## Reliability and storage

- Selenium session responses now accept both numeric and string duration values, fixing the crash previously seen when opening the running Sessions tab.
- Apache, PHP, MariaDB, and Selenium process trees are attached to Windows Job Objects. Windows terminates them if the application crashes before normal shutdown, so an invisible orphan no longer keeps a configured port occupied.
- Temporary Selenium profile copies are removed after session termination and again during application startup recovery.
- Runtime JSONL logs rotate at 10 MiB, keep 14 days, and are capped at 100 MiB in total.
- Verified package archives are retained in a 512 MiB LRU cache; stale partial downloads and excess archives are removed after package operations.

User projects, databases, immutable master profiles, cookie vaults, and files in `seldownloads` are intentional portable data and are never removed by these automatic cleanup policies.

## Security and portability

All optional modules are downloaded only after an explicit user action. The application accepts only cataloged HTTPS sources and verifies pinned SHA-256 values before installation. It does not install Windows services or modify the system `PATH`, registry, firewall, browser profiles, or unrelated processes.

Cookie vault encryption protects against accidental plaintext exposure, but its key is intentionally stored inside the same portable folder. Use an encrypted drive or container if the entire folder requires protection from theft.

## Download

The release provides a self-contained Windows 10/11 x64 ZIP and a separate SHA-256 checksum. Extract the complete directory and run `PortableDeveloper.exe`; no system .NET or Python installation is required.

This build is not digitally signed yet. Windows Smart App Control, SmartScreen, or Defender reputation checks may block it. Do not disable Windows security solely to run Portable Developer. Verify the checksum and review the public source and release workflow instead.
