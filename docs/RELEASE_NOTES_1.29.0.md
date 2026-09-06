# Portable Developer 1.29.0

This unsigned release removes redundant interface noise, makes database administration clearer without optional phpMyAdmin, and consolidates the established dark interface into reusable application components.

## Cleaner package and runtime pages

- Composer, Node.js, and Python package pages hide the operation card after a successful inventory refresh while keeping active work, failures, and explicit package-operation results visible.
- Uninstalled modules rely on their download-and-install action instead of repeating a permanent ready-to-download status.
- The sidebar, PHP settings, and runtime headers no longer repeat product, portability, or verified-version information that is already clear from context.

## Clearer database administration

- A database can be created whenever MariaDB is installed; phpMyAdmin is no longer required for the create-database form to exist.
- Database creation remains enabled only while MariaDB is running.
- The phpMyAdmin card is hidden when that optional module is absent, so the page no longer presents an unavailable tool as if it were ready.

## Consistent application shell

- The accepted dark appearance and existing navigation remain unchanged.
- Sidebar and active-project header markup now use reusable controls.
- Shared workspace, file-manager, and guide styles have one authoritative resource path instead of page-local alternatives.
- Large main-window behavior is separated into focused partial classes without changing service ownership, portable storage, or project data formats.

## Safety and upgrade

Portable Developer remains self-contained and does not install Windows services or modify the system `PATH`, registry, file associations, hosts file, or firewall. Optional modules still require an explicit user action and are fetched only from the pinned HTTPS catalog with SHA-256 verification.

The release passed locked restore, formatting verification, a Release build with zero warnings, 301 automated tests, dependency-catalog validation, release metadata and layout checks, and portable startup from a separate `E:` installation.

Download `PortableDeveloper-win-x64-1.29.0.exe` and `PortableDeveloper-win-x64-1.29.0.exe.sha256` from the release. Verify the executable with `Get-FileHash`, explicitly exit the previous Portable Developer instance from its notification-area menu, back up important portable data, replace the old executable, and start the new one. Existing projects, profiles, downloads, databases, settings, scheduled-task definitions and history, and other user data are retained.

This release is not code-signed. Windows Smart App Control or SmartScreen may block it; do not disable Windows security to run it. See the [code-signing policy](CODE_SIGNING_POLICY.md).
