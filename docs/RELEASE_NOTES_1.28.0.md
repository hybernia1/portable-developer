# Portable Developer 1.28.0

This unsigned feature release adds portable project task scheduling, makes background operation explicit and resilient, and replaces the monolithic built-in guide with a focused article catalog.

## Portable scheduled tasks

- Projects can schedule relative PHP, Python, and Node.js scripts or named `npm run` tasks.
- Tasks support interval, daily, weekly, and application-start schedules, plus an immediate manual run.
- The scheduler shows enabled state, next run, current status, and a bounded execution history with captured output and timeout handling.
- Tasks remain fully portable: no Windows service or Windows Task Scheduler entry is created.

## Safer background operation

- Closing the main window now keeps Portable Developer running in the Windows notification area so scheduled work and managed services are not stopped accidentally.
- The tray icon restores the window and provides an explicitly confirmed Exit action that safely stops owned tasks, services, and processes.
- Apache, MariaDB, and Selenium navigation entries show a compact red or green status dot based on their actual process state.
- Dark application dialogs now have a consistent visible border and remain distinct from the content behind them.

## Focused offline guides

- The original Czech and English guide content is split into nine articles across four categories without adding network-backed content.
- Categories, clickable tags, and full-text search make articles easier to find.
- Only the selected article is rendered, avoiding the continuously growing monolithic document.
- Fenced code blocks retain and display their language and keep the existing copy action, establishing a clean foundation for a future reviewed snippet library.

## Safety and upgrade

Scheduled processes use only registered portable projects and verified bundled runtimes. Script paths must stay relative to the project root, reparse-point escapes are rejected, no host shell is used, output is bounded, and common secret assignments are masked before history is persisted.

The release passed locked restore, formatting verification, Release build with zero warnings, 299 automated tests, dependency-catalog validation, release metadata and layout checks, and portable preview startup from a separate `E:` installation.

Download `PortableDeveloper-win-x64-1.28.0.exe` and `PortableDeveloper-win-x64-1.28.0.exe.sha256` from the release. Verify the executable with `Get-FileHash`, explicitly exit the previous Portable Developer instance from its notification-area menu, back up important portable data, replace the old executable, and start the new one. The first start refreshes only application-owned seed files and retains projects, profiles, downloads, databases, settings, scheduled-task definitions and history, and other user data.

This release is not code-signed. Windows Smart App Control or SmartScreen may block it; do not disable Windows security to run it. See the [code-signing policy](CODE_SIGNING_POLICY.md).
