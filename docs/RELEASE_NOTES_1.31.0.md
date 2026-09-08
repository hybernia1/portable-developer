# Portable Developer 1.31.0

This unsigned release completes the post-1.30 native Fluent polishing pass. It makes the workspace quieter and more consistent, moves short-lived results into transient notifications, and improves the most frequently used project, package, scheduler, database, and Selenium surfaces without changing the portable runtime boundary.

## Fluent workspace consistency

- Shared section, form, dialog, runtime-header, and icon components now give feature pages one coherent visual grammar while retaining native Windows controls, system UI typography, light/dark and high-contrast behavior, and the user-selected accent color.
- Projects, Files, Modules, Ports, PHP, Terminal, Guides, Scheduler, package managers, databases, and Selenium use clearer adaptive card layouts with less repeated explanatory text and more predictable action placement.
- The sidebar matches the native window surface, keeps its footer pinned, and uses a quieter scrollbar aligned with the workspace divider.
- Non-interactive scroll containers no longer retain a distracting focus outline; interactive buttons, inputs, selectors, and navigation keep their normal keyboard focus behavior.
- The public repository screenshots now show the current English Projects, Modules, and Files experience using an isolated nonsensitive sample workspace.

## Focused actions and feedback

- Selenium profile creation, cookie-vault import, and database creation now open focused native dialogs instead of permanently occupying page space.
- Optional package, profile, and vault explanations move behind accessible information actions, while validation, warnings, progress, and failures remain visible at the point of action.
- One dismissible transient notification host now reports short completed actions such as copied identifiers. MariaDB password validation focuses and highlights the responsible field without placing durable feedback in the server header.
- Package install and removal buttons keep stable captions; the existing operation strip is the single source of progress and detail.

## Selenium, packages, and scheduling

- Selenium browser packages and immutable profile masters use a shared browser-led catalog style with recognizable product marks, separated version or storage metadata, and aligned actions.
- The duplicate managed-browser inventory was removed, the Hub action is hidden while Selenium is stopped, and the section navigator no longer draws a stray focus frame after browser installation.
- Composer, npm, and Python inventories are lazy and page-cached. Reopening an already loaded page is immediate, while project changes, package mutations, explicit refreshes, and failed initial loads still invalidate the cache correctly.
- Scheduled tasks and history now share the same icon-first identity layout. History retains command type and target, and its search no longer repeats self-evident labels or help copy.

## Reliability

- Fixed crashes when opening Scheduler with an existing task, opening a populated Selenium profile catalog, and reacting to live Windows light/dark or accent changes.
- Fixed valid Python, npm, and Composer package requests being rejected because nested form values were not reaching the package action.
- Avoided repeatedly hashing unchanged portable tool entrypoints during a single application run while preserving immediate invalidation when a runtime file changes.

## Verification and upgrade

The release passed locked restore, formatting verification, dependency-catalog validation, a zero-warning Release build, all 429 automated tests, and single-executable metadata and layout validation. The tag workflow rebuilds the self-contained Windows x64 executable from public source and publishes its SHA-256, SPDX 2.2 SBOM, and build-provenance attestation.

Download `PortableDeveloper-win-x64-1.31.0.exe` and `PortableDeveloper-win-x64-1.31.0.exe.sha256` from the release. Verify the executable with `Get-FileHash`, explicitly exit the previous Portable Developer instance from its notification-area menu, back up important portable data, replace the old executable, and start the new one. Existing projects, profiles, downloads, databases, settings, scheduled-task definitions and history, and other user data are retained.

This release is not code-signed. Windows Smart App Control or SmartScreen may block it; do not disable Windows security to run it. See the [code-signing policy](CODE_SIGNING_POLICY.md).
