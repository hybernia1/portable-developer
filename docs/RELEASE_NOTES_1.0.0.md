# Portable Developer 1.0.0

Portable Developer 1.0.0 is the first stable milestone: a transparent, portable Windows development environment with verified on-demand runtimes, independent local services, managed browser automation, project tools, storage controls, and a fully English public documentation set.

## Stable portable environment

- Run the self-contained application from a writable folder or external drive without installing .NET, Python, Java, Windows services, or system runtimes.
- Install only the capabilities you need: Apache/PHP, MariaDB, Selenium/OpenJDK, Composer, Python, Notepad++, phpMyAdmin, Chrome for Testing, or Firefox.
- Every catalog package uses an allowlisted HTTPS source, pinned archive and entrypoint SHA-256, safe extraction, and transactional install or repair.
- Pages appear only when their required modules are verified. Apache/PHP, MariaDB, and Selenium support appropriate independent combinations.

## Selenium profiles, cookies, and downloads

- Use exact app-managed Chrome/ChromeDriver or Firefox/geckodriver pairs instead of unknown host browsers.
- Enroll an authenticated immutable master, use a disposable copy for every session, and intentionally edit/reseal the master later without changing its ID.
- Copy long profile and cookie-vault IDs directly from the UI.
- Import normalized cookie JSON into a passwordless AES-256-GCM vault and inject it in memory through `portable:vault`.
- Store optional automation downloads in the active project's persistent `seldownloads` directory.
- Clean up transient sessions, stale owned processes, enrollment drafts, and browser cache residue while preserving masters, vaults, projects, and downloads.

## Storage and reliability

- Successful module installs no longer retain duplicate archives, pip uses no download cache, and Settings reports or clears reproducible runtime, Composer, and pip caches separately.
- Firefox 154.0 and geckodriver 0.37.1 are pinned with Mozilla signature validation. Exact enterprise-policy casing prevents unmanaged browser updates.
- Failed fixed-target installations can be repaired atomically with rollback.
- Firefox masters omit reproducible cache and diagnostics but retain authentication, extensions, site data, Sync, history, security state, Safe Browsing, and media components.
- Managed server trees use Windows Job Objects, logs rotate within a fixed budget, and startup reconciles stale portable runtime residue.

## Documentation and openness

All maintained documentation, privacy/security policy, architecture decisions, worklog, contributor guidance, changelog, and release notes now have one English source of truth. The application UI remains available in Czech and English. The project remains GPL-3.0-or-later with no CLA or copyright assignment.

## Download and verification

Download `PortableDeveloper-win-x64-1.0.0.zip` and verify it with `PortableDeveloper-win-x64-1.0.0.zip.sha256`. Extract the complete ZIP and run `PortableDeveloper.exe`.

This build is self-contained but **not digitally signed yet**. Windows Smart App Control, SmartScreen, or Defender reputation checks may block it. Do not disable Windows security solely to run Portable Developer. Verify the checksum and review the public source and GitHub Actions release workflow.
