# Portable Developer 1.1.0

Portable Developer 1.1.0 focuses on clarity, responsiveness, and predictable project-scale workflows while preserving the portable, verified-runtime model introduced in 1.0.0.

## Clearer and more consistent interface

- A refined neutral dark theme improves text contrast and uses one semantic color system throughout pages, dialogs, controls, title bars, tabs, dropdowns, and scrollbars.
- The new server-and-terminal application identity is used by the executable, Windows chrome, dialogs, and the in-app title bar.
- Sidebar groups are collapsible and use lightweight icons, while module cards use recognizable visual cues and a single unambiguous installed state.
- Potentially large project, database, port, package, Selenium, and file collections now remain inside bounded virtualized viewports instead of expanding the entire application page.
- Long-running module, cache, Composer, and Python operations share a centralized responsive progress overlay that prevents accidental duplicate actions.

## Project and file workflows

- Fresh and migrated default projects use `www/public` as their Apache document root and receive a safe starter `index.php` without overwriting existing content.
- Apache project actions follow a stable hierarchy, and the terminal exposes the same active-project selector as the file manager.
- The file manager supports editable project-relative paths, natural sorting, file-type icons, 25/50/100-item pagination, stable navigation, and bounded directory rendering.
- Project selection remains available while Apache, MariaDB, or Selenium is running; only an active Composer or terminal operation briefly protects the shared context from changes.

## Composer and Python dependency management

- Composer and Python show packages explicitly requested by the user as the primary list and keep transitive dependencies in a separate bounded section.
- Installing an already-present transitive package promotes it to a direct project requirement instead of duplicating it.
- Python maintains an atomic portable direct-requirements registry and removes only dependencies no longer reachable from another direct package.
- Composer package-not-found failures now return a concise actionable suggestion instead of duplicating raw command usage across the page.

## Runtime and performance improvements

- Runtime downloads report transferred and expected byte counts, including the current component in multi-part packages.
- Successful installed-entrypoint SHA-256 results are reused only within the current process while file identity metadata remains unchanged. Every application start performs a fresh full integrity check, file changes invalidate the cache, and failed checks are never cached.
- Shared runtime components are evaluated once per package inventory refresh, reducing measured startup reads from approximately 637 MB to 157 MB on the maintained test environment.
- Hidden progress indicators stop their indeterminate animation state when idle, and long extraction, package, cache, and file operations stay off the WPF dispatcher.
- Cache management is more compact, disables empty actions, and offers one explicit clear-all operation for disposable data only.

## Download and verification

Download `PortableDeveloper-win-x64-1.1.0.zip` and verify it with `PortableDeveloper-win-x64-1.1.0.zip.sha256`. Extract the complete ZIP to a writable folder or external drive and run `PortableDeveloper.exe`.

This build is self-contained but **not digitally signed yet**. Windows Smart App Control, SmartScreen, or Defender reputation checks may block it. Do not disable Windows security solely to run Portable Developer. Verify the checksum and review the public source and GitHub Actions release workflow.
