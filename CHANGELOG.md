# Changelog

The format follows [Keep a Changelog](https://keepachangelog.com/) and dates use ISO 8601.

## [1.0.0] - 2026-08-23

### Added

- A storage overview separates disposable runtime-package, Composer, and pip caches from protected runtimes and user data, with explicit per-cache cleanup.
- Verified Selenium master profiles can be intentionally edited through an isolated writable draft and atomically resealed without changing their stable ID.
- Profile and cookie-vault cards provide copy-ID actions for automation code.
- English is now the canonical language for all maintained project documentation, policies, architecture records, release notes, and contributor guidance.

### Changed

- Successfully installed runtime archives are deleted; pip operations use no download cache.
- Managed Firefox is pinned to official Firefox 154.0 with geckodriver 0.37.1. A successful newer managed browser/driver install removes superseded verified version directories.
- Firefox masters exclude reproducible cache, startup cache, shader cache, thumbnails, crash reports, and queued telemetry while preserving authentication, extensions, site storage, Sync, history, security state, Safe Browsing data, and media components.
- Application, assembly, file, publish, and HTTP product versions are 1.0.0.

### Fixed

- Firefox enrollment follows the real browser lifetime, waits for SQLite files to flush, and distinguishes an active OS lock from a stale `parent.lock` file.
- Profile names are validated before a managed browser opens.
- Firefox enterprise policy property names retain Mozilla's required casing, preventing an unmanaged browser update.
- Reinstalling a catalog package can atomically repair an existing component that fails verification and roll back safely on failure.

## [0.9.0] - 2026-08-23

### Added

- App-managed Firefox/geckodriver and Chrome for Testing/ChromeDriver environments.
- Persistent per-project `seldownloads`, opt-in Selenium downloads, encrypted cookie vaults, immutable authenticated profile masters, crash-safe Windows Job Object process ownership, rotating logs, and bounded package cache.

### Changed

- Selenium stopped using system browsers and host profiles. Enrollment moved entirely into app-managed browsers, working copies disabled cloud sync, and profiles/cookies/session residue remained inside the portable root.
- Debug and Release builds received separate single-instance identities.

### Fixed

- Cookie-vault deletion, maximized work-area sizing, dark scrollbars, profile removal dialogs, worker-thread UI access, Snap Layout handling, large-profile sealing, download-policy enforcement, GraphQL session-duration parsing, and orphaned server cleanup.

## [0.8.0] - 2026-08-22

### Added

- Catalog-managed Chrome for Testing, local profile inventory, profile manifests, shared custom window chrome, and single-instance activation.

### Changed

- Unified dark selectors and module navigation; file opening began respecting safe Windows associations with portable editor fallback; Selenium required an explicit compatible browser/driver pair and disposable profile copy.

### Fixed

- Removed the navigation validation border after module refresh and kept damaged profiles visible for removal while blocking their use.

## [0.7.0] - 2026-08-22

### Added

- Public governance, security/reporting guidance, signing roles, uninstall documentation, Code of Conduct, and issue templates.
- Central WPF styles for tabs, dialogs, selectors, scrollbars, progress, and window controls.
- Safe terminal filesystem commands and reusable command registry.

### Fixed

- Composer/Python progress, inconsistent page spacing, native deletion dialogs, and several Selenium enrollment and UI-state issues.

## [0.6.0] - 2026-08-22

### Added

- Small online bootstrap release with explicit catalog-driven module downloads, conditional navigation, public tag workflow, ZIP, and SHA-256 checksum.

### Security

- Downloads became restricted to bundled HTTPS sources and pinned hashes with safe staging and no system installation.
- Unsigned binary status and Windows reputation limitations became explicit public policy.

## [0.5.0] - 2026-08-22

### Added

- Multiple Apache projects, `.localhost` virtual hosts, per-project roots, `.htaccess`, and project-scoped Composer state.

### Security

- Apache remained localhost-only and stopped following links outside project roots.

## [0.4.0] - 2026-08-22

### Added

- Central port manager with listener diagnostics and collision checks.

### Changed

- Service controllers consumed one validated port configuration and never modified unrelated processes.

## [0.3.0] - 2026-08-22

### Added

- Independent web, database, and Selenium lifecycles; contextual restart actions; shared tab layout; improved file-manager controls.

### Changed

- Web-stack start/stop remained dashboard-only while detail pages exposed relevant restart/configuration actions.

## [0.2.3] - 2026-08-22

### Added

- Two-release artifact retention and safer Composer dependency removal.

### Removed

- External Double Commander integration in favor of the built-in project file manager.

## [0.2.2] - 2026-08-22

### Changed

- Published output used a clean root with one primary EXE and framework/runtime files under internal folders where supported.

## [0.2.1] - 2026-08-22

### Added

- Direct console interaction and an early external file-manager experiment.

### Security

- Terminal navigation and file operations remained within the active project.

## [0.2.0] - 2026-08-22

### Added

- Restricted portable terminal, project file manager, portable editor integration, and manual PHP override.

## [0.1.0] - 2026-08-22

### Added

- Initial self-contained WPF application, Czech/English UI, Apache/PHP, MariaDB, Selenium, Composer, Python, phpMyAdmin, server status, generated configuration, verified module inventory, and portable process/logging foundation.

### Changed

- The stack control became a stateful toggle and service detail pages shared controller state.

### Fixed

- Composer removal parsing, language satellite output, WPF startup resources, database initialization, and native runtime preflight.
