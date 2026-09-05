# Changelog

The format follows [Keep a Changelog](https://keepachangelog.com/) and dates use ISO 8601.

## [1.24.2] - 2026-09-05

### Changed

- Release packaging no longer depends on WiX to recover app-local Microsoft Visual C++ runtime libraries from the pinned redistributable.

### Fixed

- Native runtime extraction remains compatible with Windows PowerShell 5.1 as used by the GitHub release runner.

### Security

- Native runtime packaging now uses a bounded CAB extraction path under the portable repository root and continues only when the source EXE and every selected x64 DLL match their pinned hashes, expected versions, and Microsoft signatures.

## [1.24.1] - 2026-08-25

### Fixed

- Interactive Node.js and Vite terminal sessions now use a Windows Job Object, so Ctrl+C, normal shutdown, and a forced application exit terminate every owned child process and release its listening ports.

## [1.24.0] - 2026-08-25

### Added

- Added an optional verified Node.js 24.19.0 runtime with its bundled npm CLI.
- Added a Node.js package page for active web projects: it creates and maintains project-local `package.json`, `package-lock.json`, and `node_modules`, separates direct and transitive dependencies, and supports explicit install, refresh, and removal.
- Added portable npm cache management in Settings.
- Added terminal support for verified `node` and project-local `npm run <script>` commands, including Vite development servers with streamed output and Ctrl+C process-tree cleanup.

### Security

- npm uses the verified `node.exe` directly with portable cache/config locations, non-interactive audit/funding settings, and disabled package lifecycle scripts.

## [1.23.0] - 2026-08-25

### Changed

- Apache and PHP are now independent runtime packages. Apache is the only user-controllable web service; its start/stop action owns the required PHP FastCGI worker.
- The PHP page no longer presents a web-service restart action. Saving PHP settings restarts Apache only when it is running.
- PHP now appears under Development rather than Servers in the sidebar.

### Fixed

- Runtime-package cards now ignore delayed download-progress updates after an installation has completed, so a successful installation cannot remain visually stuck on "Downloading".

## [1.22.1] - 2026-08-24

### Added

- The portable terminal now has bounded project-local `find`, `grep`, `tree`, and non-overwriting `write` commands.

### Fixed

- Direct `python -m pip` and `python -m ensurepip` commands are blocked in the terminal so package changes cannot bypass the managed portable Python package store.

### Changed

- The terminal now retains 250,000 visible characters and 400,000 pending output characters, with a clear notice when older output is trimmed.

## [1.22.0] - 2026-08-24

### Added

- Public security-model, SignPath integration, and documentation-index pages clarify trust boundaries, eligible signing scope, and the future trusted-build flow.
- Release builds now generate an SPDX 2.2 SBOM and GitHub build-provenance attestation.
- CodeQL and dependency-review workflows add continuous source and dependency security checks.

### Changed

- Windows releases now keep the root clean: one `PortableDeveloper.exe` plus organized `catalog`, `resources`, `runtime`, and `docs` folders. Native framework DLLs are bundled into the executable, and release validation rejects unexpected root files.
- Selenium's module badge now shows only its version, matching the other single-product modules.
- The project file manager now distinguishes HTML/XML, executable, text/Markdown, JSON/YAML, image, archive, database, configuration, and source files with shared vector icons.
- Python files and Java archives now receive their corresponding file-manager types and icons.
- Text files, Word-compatible documents, PDFs, and Excel-compatible spreadsheets now receive dedicated file-manager types and icons; CSV is treated as a spreadsheet.
- Module cards and installed-technology navigation now use transparent technology marks, while neutral system icons remain for application actions.
- Runtime downloads, Composer, and Python package operations now present the same operation status, detail line, and progress treatment. Package operations identify the requested package while it is being processed.
- Sidebar group labels are passive section headings instead of collapsible controls that resemble navigation items.
- GitHub Actions are pinned to full commit SHAs, the .NET SDK and NuGet graph are locked, release tags must resolve to `main`, and manifests record the source revision.
- The repository landing page now includes an actual application screenshot, quick start, host-system boundary, security links, and explicit Code signing policy section.

## [1.2.1] - 2026-08-23

### Fixed

- One-shot portable commands without standard input no longer fail while preparing Python, running Composer, or initializing MariaDB after the UTF-8 terminal changes.

## [1.2.0] - 2026-08-23

### Added

- A built-in offline Guides page renders separate Czech and English Markdown as tagged chapters with current local ports and copyable Python, PHP, Selenium profile, cookie-vault, download, and MariaDB examples.
- The quick start explicitly identifies `selenium` and `php-webdriver/webdriver` as project dependencies that must be added through the application's package pages.
- The restricted terminal adds safe project-local `cat`, `touch`, `cp`, `mv`, `rm`, `rmdir`, and `echo` commands without exposing a system shell or recursive deletion.

### Changed

- Bundled Python, PHP, and Composer commands now stream output while running, accept line-oriented input directly in the terminal, and can be stopped with Ctrl+C.

### Fixed

- Portable process streams consistently use UTF-8 without a byte-order mark, preserving Czech and other Unicode text in both output and the first interactive input line.
- Interactive scripts no longer make the WPF terminal appear frozen while waiting for input or producing incremental output.

## [1.1.0] - 2026-08-23

### Added

- The project file manager now has editable project-relative navigation, natural column sorting, lightweight file-type icons, bounded 25/50/100-item pages, and stable pagination controls.
- Runtime downloads report transferred and expected sizes, including the current component within multi-component packages.
- A centralized responsive operation overlay prevents duplicate interaction during runtime, package, and cache operations.

### Changed

- Installed runtime entrypoints are fully verified once per application process and reused only while their path, expected digest, size, and timestamps remain unchanged; package inventory refreshes each shared component once.
- The application theme uses clearer neutral dark surfaces, navigation icons, and collapsible sidebar groups.
- Module cards use visual type cues, compact safety guidance, and a single unambiguous installed state.
- Composer and Python show direct requirements as the primary package list and keep transitive dependencies in a bounded expandable view.
- Apache project actions use a stable hierarchy, while project, database, port, Selenium, package, and file collections own bounded virtualized viewports.
- The terminal now exposes the same active-project selector as the file manager.
- Cache management is compact, disables empty cleanup actions, and supports one explicit clear-all action.
- All WPF colors now come from one semantic theme palette; buttons, tabs, cache rows, Selenium master cards, and title bars shared by the main window and dialogs use a quieter, consistent visual hierarchy.
- The application now uses a purpose-built server-and-terminal identity across the executable, Windows window chrome, dialogs, and the in-app title-bar thumbnail.

### Fixed

- Idle hidden progress indicators no longer retain indeterminate animation state after their operation finishes.
- Fresh and migrated default projects use `www/public` as the Apache document root and receive a safe local starter `index.php` without overwriting existing files.
- Runtime extraction and package-manager work no longer resume on the WPF UI thread after asynchronous waits.
- Composer not-found failures no longer duplicate raw command usage across the page and preserve a concise package suggestion.
- Existing transitive Composer or Python packages are promoted to direct requirements without being presented as duplicate installations.
- Python records direct requirements atomically and preserves dependencies still reachable from another direct package.
- Primary text remains readable in file, port, Composer, Python, dependency, and Selenium profile lists instead of inheriting the native dark-on-dark control foreground.
- The active tools project can be selected while Apache, MariaDB, or Selenium is running; only an in-flight Composer or terminal operation temporarily guards the shared context.

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
