# Worklog

This is a concise chronological record of significant engineering work. Git history remains the detailed source.

## 2026-08-23 — 1.0 release preparation

- Converted every maintained project document, policy, architecture record, historical audit, and release note to an English source of truth.
- Removed the duplicate localized README and obsolete next-release draft, normalized ADR numbering, and aligned application/publish metadata to 1.0.0.
- Prepared a full source, catalog, test, portable-package, checksum, and public workflow verification for the 1.0 tag.

## 2026-08-23 — Conservative Firefox master pruning

- Compared a real 206 MiB Firefox master with a 5 MiB Chromium master and identified reproducible cache/diagnostic roots as the main difference.
- Excluded Firefox `cache2`, `startupCache`, shader cache, crash reports, telemetry queues, and thumbnails while retaining authentication, extensions, site storage, Sync, history, security state, Safe Browsing, codecs, and unknown databases.
- Added regression coverage proving disposable roots are omitted and representative persistent state remains.

## 2026-08-23 — Managed Firefox update and repair

- Diagnosed an unverified Firefox runtime that had silently self-updated because shared JSON options changed the case of Mozilla policy keys.
- Preserved exact vendor property names, pinned official Firefox 154.0, verified Mozilla signatures and hashes, and retained current geckodriver 0.37.1.
- Added transactional repair of an occupied but unverified fixed runtime target, rollback recovery, and cleanup of superseded verified browser/driver versions.

## 2026-08-23 — Editable Selenium masters and copyable IDs

- Added early profile-name validation, explicit editing through writable app-managed drafts, transactional resealing, stable IDs, and interrupted-swap recovery.
- Added copy-ID actions to profile and cookie-vault cards.
- Kept damaged masters non-editable and ensured failed edits leave the original verified master unchanged.

## 2026-08-23 — Cache policy and Firefox lifecycle

- Deleted verified install archives after success, disabled pip download cache, and added measured/explicit cleanup for runtime, Composer, and pip caches.
- Protected modules, drivers, projects, databases, profiles, vaults, and Selenium downloads from cleanup.
- Distinguished stale Firefox lock files from active OS locks, observed the detached browser lifetime, waited for profile database flushes, and cleaned abandoned enrollment drafts at startup.

## 2026-08-23 — 0.9 release hardening

- Accepted numeric and string Selenium GraphQL session durations.
- Attached supervised server trees to Windows Job Objects with kill-on-close behavior.
- Added 10 MiB rotating JSONL segments, 14-day/100 MiB log budgets, bounded temporary package state, and startup cleanup of stale Selenium session copies.

## 2026-08-23 — Cookie vault, window, and profile fixes

- Fixed deletion of read-only vault files with fail-closed handling of unexpected directories.
- Fixed a WPF dialog style crash during profile deletion, prevented worker-thread UI access, and moved large profile sealing off the UI thread.
- Corrected maximized work-area bounds, Snap Layout handling, dark scrollbars, and session parsing crashes.

## 2026-08-23 — Project Selenium downloads and encrypted cookie vault

- Added persistent per-project `seldownloads`, explicit download permission, browser preference enforcement, Chromium CDP enforcement, and Apache denial of the download directory.
- Added normalized JSON cookie import, AES-256-GCM storage, automatic portable key creation, and in-memory Java decryption through `portable:vault`.
- Removed manual vault passwords after the initial UI model proved unnecessary and error-prone.

## 2026-08-23 — App-managed Selenium browsers

- Replaced host browser/driver discovery with complete catalog-matched Chrome for Testing/ChromeDriver and Firefox/geckodriver environments.
- Removed host-profile import after Chromium App-Bound cookies proved non-portable.
- Added enrollment in managed browsers, immutable hashed masters, disposable session copies, disabled cloud sync, and browser-specific lifecycle handling.

## 2026-08-23 — UI optimization package

- Unified app-wide page/tab spacing, dark selectors and popups, themed dialogs, window chrome, scrollbars, maximized layout, and operation progress.
- Added single-instance activation, safe Windows file association opening with portable editor fallback, and safe filesystem commands in the restricted terminal.

## 2026-08-22 — Public release and trust foundation

- Adopted GPL-3.0-or-later, public governance, contribution and security policies, CODEOWNERS, Code of Conduct, privacy disclosure, and Windows reputation guidance.
- Created public CI and tag-based release workflows with source-built self-contained ZIPs and SHA-256 files.
- Defined the SignPath approval model while continuing to label unsigned releases honestly.

## 2026-08-22 — Online bootstrap and module manager

- Replaced dependence on a local Laragon tree with exact upstream HTTPS sources, archive/entrypoint hashes, safe extraction, and portable staging.
- Added explicit in-app installation, conditional navigation, source metadata, app-local Visual C++ support, and repair-safe package boundaries.

## 2026-08-22 — Projects, ports, and independent services

- Added Apache project catalog, `.localhost` virtual hosts, `.htaccess`, per-project roots, and project-scoped Composer state.
- Added a central port manager that observes but never changes unrelated listeners.
- Split web, database, and Selenium lifecycles while retaining the Apache/PHP dependency and explicit phpMyAdmin prerequisites.

## 2026-08-22 — Developer tools and application shell

- Added Composer and Python package management, structured PHP settings, advanced `php-custom.ini`, portable Notepad++, restricted terminal, and project-rooted file manager.
- Added Czech/English localization, detailed service pages, shared tabs, database overview/creation, optional root password, and phpMyAdmin.

## 2026-08-21 to 2026-08-22 — Core implementation

- Established the .NET 10/WPF layered solution, relative-path resolver, process supervisor, command runner, health checks, JSONL logging, and tests.
- Implemented verified module inventory, generated Apache/PHP FastCGI configuration, controlled lifecycle rollback, MariaDB transactional initialization, Selenium Grid/session management, and self-contained Windows publication.
- Established the documentation, changelog, decision log, and commit conventions used by the project.
