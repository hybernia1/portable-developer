# Portable Developer 1.30.0

This unsigned release completes a broad native WPF interface cleanup. It adopts the system Fluent theme, replaces duplicated and permanently attached page markup with one typed navigation host, and makes responsive behavior, project context, package operations, database actions, and scheduler history substantially clearer.

## Native Fluent interface and navigation

- Windows now supplies light, dark, high-contrast, accent, native title-bar, caption-button, and snap behavior through the built-in .NET WPF Fluent theme.
- One typed page-and-section route replaces seven independent tab groups and the former nested Selenium navigation.
- The shell contains one active page host; inactive feature views no longer remain permanently attached or duplicate state.
- Breadcrumbs, the generic window status line, custom standard-control templates, the modal download overlay, and all WPF `DataGrid` presentation were removed.
- Shared adaptive layouts stack cleanly at narrow widths while bounded collections keep their own vertical scrolling.

## Clearer feature pages

- Composer, npm, and Python share one localized package-manager view with project-local inventory and explicit install, refresh, and removal actions.
- Modules are organized into web/database, development, and browser-automation groups with consistent identity, version, state, progress, and action rows.
- Package inventories, databases, TCP listeners, Selenium sessions, profiles, vaults, scheduled tasks, and task history use non-selectable labeled rows instead of accidental table selection.
- The active-project selector reliably restores startup selection and changes project immediately; package inventories refresh only on page entry or explicit request.
- Database rows can open their schema in phpMyAdmin or delete user databases after confirmation. The application-owned `portable_dev` database remains protected in both UI and service code.

## Scheduler history

- Task history can be filtered by task name, time, duration, trigger, result, or captured output.
- Captured output opens in a separate resizable details window instead of expanding inside the page.
- Individual history records and the active project's complete history can be deleted after confirmation.
- History deletion is scoped by both project and stable record ID and remains owned by the atomic portable JSON history store.

## Architecture and safety

- Feature page models now own their presentation collections and feedback rather than forwarding the root dashboard state.
- A bounded runtime coordinator supplies verified module and service presentation without owning views, processes, downloads, or filesystem operations.
- All runtime and package operations remain behind the existing services, use project-relative or portable-root-relative paths, and preserve the established verified download catalog and SHA-256 checks.
- Portable Developer still installs no Windows service or driver and does not modify the system `PATH`, registry, file associations, hosts file, or firewall.

## Verification and upgrade

The release passed locked restore, formatting verification, dependency-catalog checks, a zero-warning Release build, 385 automated tests, single-executable metadata and layout validation, and isolated portable startup and upgrade checks.

Download `PortableDeveloper-win-x64-1.30.0.exe` and `PortableDeveloper-win-x64-1.30.0.exe.sha256` from the release. Verify the executable with `Get-FileHash`, explicitly exit the previous Portable Developer instance from its notification-area menu, back up important portable data, replace the old executable, and start the new one. Existing projects, profiles, downloads, databases, settings, scheduled-task definitions and history, and other user data are retained.

This release is not code-signed. Windows Smart App Control or SmartScreen may block it; do not disable Windows security to run it. See the [code-signing policy](CODE_SIGNING_POLICY.md).
