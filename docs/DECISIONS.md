# Architecture decision records

All records were accepted on 2026-08-21 through 2026-08-23 unless marked superseded. Numbers reflect decision order; the 1.0 documentation pass removed a historical numbering collision without changing the decisions.

## ADR-001 — C# and WPF for the desktop application

Use C# with WPF for a native Windows control surface and a layered solution. This provides direct process and filesystem integration without requiring a browser shell.

## ADR-002 — No Docker or system services

Run portable child processes, never Docker containers or installed Windows services. Removal must remain equivalent to deleting the folder.

## ADR-003 — Relative persistent paths

Resolve paths from the executable root and persist only relative paths. Projects, databases, configuration, profiles, logs, cache, and temporary data remain inside that root.

## ADR-004 — Move from .NET 8 to .NET 10

Use the pinned .NET 10 SDK and publish self-contained. The target host does not need a system .NET runtime.

## ADR-005 — Normalized versioned module layout

Store each runtime at `modules/<kind>/<version>` and verify explicit entrypoints. Never discover runtime executables through the host `PATH`.

## ADR-006 — Absolute server paths only in transient configuration

Generate current-drive absolute paths under `temp/` at service start. Persistent configuration remains relocatable.

## ADR-007 — Local catalog and verified archives

Package trust comes from the release's bundled catalog, allowlisted HTTPS source, archive SHA-256, normalized entrypoint SHA-256, safe extraction, and recorded provenance.

## ADR-008 — App-local native runtime

Extract exact verified Visual C++ DLLs beside Apache/PHP rather than installing a redistributable or copying into Windows directories.

## ADR-009 — Portable UI language preference

Support Czech and English, storing the choice under portable state rather than the Windows profile.

## ADR-010 — Apache-owned PHP FastCGI worker

Apache is the only user-controllable web service. Its controller starts the required verified PHP FastCGI worker before Apache, stops it afterwards, verifies health, and rolls back partial startup. PHP remains an independently installable runtime and configuration surface; the UI never presents it as a service to start or stop.

## ADR-011 — Apache Lounge Windows build

Use the exact Apache Lounge build pinned by the catalog while retaining Apache and build notices.

## ADR-012 — User-imported VC runtime (superseded by ADR-030/032)

The first design allowed explicit import from a signed Microsoft redistributable. The online catalog/publish process later replaced this manual flow with verified app-local extraction.

## ADR-013 — MariaDB ZIP module and transactional initialization

Use a portable MariaDB archive, initialize a staging data directory, and commit it only after successful bootstrap.

## ADR-014 — Full offline distribution (supplemented by ADR-032)

Support a complete offline aggregate for controlled distribution. The smaller online base later became the public default.

## ADR-015 — Detail pages share service state

Dashboard and detail pages observe the same controllers and status models; UI duplication must not create independent process state.

## ADR-016 — Local MariaDB with a default database

Initialize localhost-only MariaDB and `portable_dev` automatically when the database module first becomes usable, then leave it stopped until explicit use.

## ADR-017 — Optional root password and phpMyAdmin

Use a passwordless development-default root account with an optional user-set password. Pass credentials through a short-lived defaults file. phpMyAdmin uses local cookie authentication and stores no database password.

## ADR-018 — Explicit portable WebDrivers and local Selenium session control (superseded by ADR-041)

Keep drivers under the portable root and expose Grid status/session termination in the UI. Later decisions tightened this to complete app-managed browser/driver pairs.

## ADR-019 — Project packages without a system shell

Run Composer through verified PHP and pip through verified Python using argument lists, controlled working directories, timeouts, captured output, and portable homes/caches.

## ADR-020 — Structured PHP settings

Persist a validated settings model and generate `php.ini`; do not edit vendor defaults as the primary configuration mechanism.

## ADR-021 — Portable editor and explicit advanced PHP override

Offer verified portable Notepad++ and a bounded `php-custom.ini` appended after generated settings. Advanced directives are an explicit user responsibility.

## ADR-022 — Restricted terminal and project file manager

Provide useful project operations without exposing `cmd.exe` or PowerShell. Enforce the active project root, safe path normalization, and a command allowlist.

## ADR-023 — Direct console and external file manager experiment (partly superseded by ADR-025)

Use a direct console input surface. The external file-manager experiment was later removed because it weakened integration and portability.

## ADR-024 — Single-file application with bundled native framework support

Publish the application and required native WPF/framework libraries as one executable, keeping the release root free of framework DLLs. The .NET host may extract immutable native framework support into its transient per-user bundle cache; persistent application data, module data, configuration, logs, and temporary work remain under the portable root.

## ADR-025 — Built-in project file manager

Replace Double Commander with a small WPF file manager that can enforce project-root and reparse-point boundaries and integrate safe file opening.

## ADR-026 — Keep two newest release artifacts

Release cleanup may remove older verified build outputs but must keep the two newest and never delete a directory containing a running release process.

## ADR-027 — Independent services and explicit UI dependencies

Apache, MariaDB, and Selenium can run independently. UI actions such as phpMyAdmin remain disabled with clear prerequisites rather than silently starting services.

## ADR-028 — Central port manager without interference

Validate distinct non-privileged ports through read-only listener inspection and localhost bind tests. Never stop, reconfigure, or bypass an unrelated host process.

## ADR-029 — Copyleft license and separated signing

License project code under GPL-3.0-or-later without copyright assignment. A future project certificate may sign only project-owned binaries, never third-party runtimes.

## ADR-030 — Reproducible online bootstrap

Build public releases from exact upstream sources and hashes without Laragon, `System32`, or private caches as trust inputs. Public CI must recreate the portable base.

## ADR-031 — Project catalog and `.localhost` virtual hosts

Keep the existing default root, create new managed project roots, generate localhost virtual hosts without editing `hosts`, and keep Composer dependencies per project.

## ADR-032 — Optional runtime packages in the application

Ship a small self-contained base. Install Apache, PHP, Database, Selenium, Composer, Python, Editor, phpMyAdmin, and browser packages only after explicit user actions. Hide unavailable capabilities.

## ADR-033 — Immutable Selenium master profiles

Seal verified masters read-only with a per-file hash manifest. Every session uses a disposable writable copy and never writes back to the master.

## ADR-034 — Complete unsigned binary releases

Publish usable ZIPs before signing approval, but identify them as unsigned in notes and manifests and never recommend disabling Windows protection.

## ADR-035 — Shared WPF theme and structured operation progress

Centralize tabs, dropdowns, scrollbars, dialogs, window chrome, spacing, and busy/progress patterns in application resources.

## ADR-036 — Safe command registry for GUI and future headless use

Keep terminal parsing, validation, help, and execution outside the WPF console. Filesystem commands use application services and project-root boundaries.

## ADR-037 — Single instance, shared WindowChrome, and system associations

Allow one Release instance and activate it through a named pipe; keep Debug separate. Use one custom title-bar implementation. Open safe files through Windows associations with verified editor fallback.

## ADR-038 — Verified browser environments and master profiles (superseded in part by ADR-041)

Represent Selenium readiness as a compatible browser and driver with an explicit browser binary. Validate profile manifests and copy masters per session.

## ADR-039 — Local host-profile inventory (superseded by ADR-041)

An intermediate design read minimal metadata from standard host profile locations after explicit user intent. It never read credentials or modified sources, but portability limits led to removal.

## ADR-040 — Chromium App-Bound cookies are not portable

Do not disable browser security policy, extract protected cookies, or run automation against the writable host profile. A copied modern Chromium profile cannot promise authenticated portability.

## ADR-041 — Selenium uses only app-managed browsers

Register only exact catalog pairs: Chrome for Testing/ChromeDriver or Firefox/geckodriver. Do not scan system browsers, accept arbitrary drivers, or import host profiles. Enrollment occurs inside a managed browser.

## ADR-042 — Portable cookie vault separate from browser profiles

Normalize bounded cookie JSON and encrypt values with AES-256-GCM. Keep the portable key under `state/`, decrypt only in memory for session creation, and document that theft of the complete folder is outside this protection.

## ADR-043 — Project Selenium downloads and one-way working profiles

Store downloads in persistent `<project>/seldownloads` only after opt-in, enforce the path server-side, deny Apache access, and disable browser cloud sync in disposable session copies.

## ADR-044 — Crash-safe process ownership and bounded residue

Assign supervised server trees to kill-on-close Windows Job Objects. Rotate/budget logs and bound reproducible runtime cache while never cleaning projects, databases, masters, vaults, or downloads.

## ADR-045 — English canonical documentation for 1.0

Use English as the source of truth for maintained documentation, policies, architecture, contributor guidance, changelog, and release notes. Czech remains a supported application UI language.

## ADR-046 — Disposable caches separated from user data

Delete successful install archives, disable pip cache, report runtime/Composer/pip caches separately, and allow only explicit reparse-safe cleanup of fixed cache roots while operations are idle.

## ADR-047 — Transactional editing of immutable Selenium masters

Edit a verified master only through a writable draft in its matching managed browser. Normalize and verify the result, atomically swap with recovery backup, preserve identity, and leave the original untouched on failure.

## ADR-048 — Atomic repair of failed verified runtimes

Treat an existing unverified fixed runtime target as repairable reproducible data. Stage and verify its replacement first, back up the old target on the same volume, commit atomically, and restore on failure. User-data roots are never eligible.

## ADR-049 — Conservative Firefox master pruning

Remove only proven reproducible Firefox cache and diagnostic roots. Preserve authentication, passwords, extensions, storage, Sync, history, security/Safe Browsing state, media components, preferences, permissions, and unknown data. Existing masters change only after explicit edit/reseal.

## ADR-050 — Bounded UI collections and explicit dependency roots

Every potentially unbounded UI collection owns a finite virtualized viewport instead of expanding the page shell. The project file manager additionally pages stable, naturally sorted directory results and canonicalizes editable paths inside the active project. Composer derives direct roots from `composer.json`; Python maintains an atomic portable direct-requirements registry and removes only managed packages no longer reachable from another root. Long operations run away from the WPF dispatcher and publish one shared application operation state.

## ADR-051 — Semantic theme resources and operation-scoped project locking

Keep all concrete WPF colors in the central theme dictionary and reference semantic brushes from pages, dialogs, controls, and icons. A regression test rejects concrete colors elsewhere. Keep buttons neutral by default and reserve status colors for information rather than broad action surfaces. Apache, MariaDB, and Selenium lifecycle state never locks the shared tools-project selector; only an active Composer or terminal operation can delay a context change.

## ADR-052 — Process-local installed-file integrity cache

Perform a full installed-entrypoint SHA-256 verification on every application start. Within that process, reuse only successful results while the canonical path, expected digest, length, creation time, and last-write time remain unchanged; never persist results or cache failures. Evaluate each shared runtime component only once per package inventory refresh.

## ADR-053 — Versioned offline Markdown guides

Keep practical user documentation in separate embedded Czech and English Markdown files instead of expanding the UI translation class. Render a deliberately small, non-interactive Markdown subset without a web engine or third-party parser, including chapter lists and bounded visual tags. Substitute only known local-port tokens at display time, keep code blocks selectable and copyable, and never fetch or execute guide content.

## ADR-054 — Interactive terminal without a host shell

Run allowlisted bundled PHP, Composer, and Python tools through an application-owned asynchronous session with UTF-8 streams, incremental output, line-oriented input, timeout, and process-tree termination. Batch display updates on the WPF dispatcher and bound retained console text. Keep filesystem commands as explicit project-rooted operations; do not add a PTY, `cmd.exe`, PowerShell, arbitrary executables, redirects, pipes, shell chaining, recursive deletion, or host `PATH` inheritance.

## ADR-055 — Recognizable technology marks and shared operation presentation

Use transparent, properly attributed technology marks for installed runtimes and their catalog cards. Keep neutral vector icons for application navigation and actions. Render runtime downloads and project package operations from the same operation-state vocabulary: status, optional contextual detail, indeterminate state, and percentage. Page-specific context may mirror the global operation but must never contradict or be less specific than it.

## ADR-056 — Verified Node.js and project-local npm

Install Node.js only from a pinned official `nodejs.org` ZIP and verify both the archive and normalized `node.exe`. Run the bundled npm CLI through that verified executable with an argument list, in the active web project, and keep npm cache and configuration below the portable root. Package changes are explicit UI operations; disable audit/funding prompts and lifecycle scripts so third-party install hooks cannot execute implicitly. Do not expose global npm installation or write to the host user profile.

## ADR-057 — Owned Node.js terminal sessions and explicit npm scripts

Expose the verified Node.js runtime in the portable terminal and allow only `npm run <script>` for project scripts, including Vite's development server. The terminal starts it through the verified `node.exe` and bundled npm CLI with portable npm state, captures UTF-8 output, and owns Ctrl+C plus process-tree termination. Attach each interactive terminal process to a dedicated kill-on-close Windows Job Object so forced application exit cannot leave its children or listening ports behind. Keep dependency installation, removal, and arbitrary npm subcommands on the dedicated Node.js package page; an explicitly named project script is the narrow execution boundary needed for local development servers.

## ADR-058 — Native CAB extraction without WiX

Remove WiX from the release toolchain because Portable Developer only needs to recover app-local Microsoft Visual C++ runtime files from a pinned redistributable, not author Windows installers. A shared bounded PowerShell helper validates the complete Microsoft EXE by SHA-256 and Authenticode, identifies structurally valid embedded CAB segments, expands them with the explicit Windows `System32\expand.exe`, and accepts exactly one hash-matched, version-matched, Microsoft-signed x64 payload for every catalog entry. Keep all extraction under the repository `temp` root, own and time-limit the external process, reject reparse points and excessive nesting or size, and fail closed if Microsoft changes the bundle layout. Do not execute the redistributable or obtain runtime files from the host Visual Studio installation.

## ADR-059 — Embedded first-launch portable seed

Publish the online base as a single executable by compiling a compressed, versioned seed into the self-contained application. On every start, before catalogs or logging are opened, validate the bounded seed entry set, relative paths, lengths, and SHA-256 digests; stage it below the portable root and atomically install only app-owned catalogs, resources, notices, and Visual C++ runtime files. Write the state marker last so an interrupted initialization is safely repeatable. Create but never replace or remove user-data roots such as `instances`, `profiles`, `downloads`, and `state`. Keep the full offline aggregate as an expanded package. The immutable .NET host may still use its documented per-user native bundle cache before managed startup.

## ADR-060 — Independent Apache, explicit editor choice, and literal terminal operators

Allow verified Apache to run without PHP. Generated static-only configuration must deny `.php` files and omit phpMyAdmin; verified PHP remains an automatically owned FastCGI worker when available. Persist a choice between portable Notepad++ for text/source files when available and Windows file associations. Treat markup and shell-operator characters in the portable terminal as literal argument text and provide explicit project-rooted overwrite/append commands without introducing a host shell. Keep Selenium loopback-only and explain the unnecessary Windows firewall exception before first start rather than changing firewall state. This partly supersedes ADR-021, ADR-022, ADR-043, and ADR-054.

## ADR-061 — General project context with optional capabilities

Replace the web-project-owned tools context with one application-owned general project catalog and active-project context. Treat Web, PHP, Python, Node.js, and browser automation as detected or explicitly configured capabilities rather than permanent project types; creation templates only create initial files. Keep runtimes and services shared, keep Apache web settings optional, migrate the existing catalog non-destructively, and retain only portable relative project paths. Configure web roots, Apache enablement, and `.htaccess` from Projects; persist changes immediately, but require an explicit controlled restart before a running Apache instance adopts them. Keep the Apache page focused on service status and effective configuration. Implement the change in the staged order and against the acceptance criteria in `docs/PROJECT_MANAGEMENT_PLAN.md`; Python dependency-storage changes require a separate later decision.

## ADR-062 — Direct navigation instead of an environment dashboard

Remove the environment overview because it exposed only controllable services while implying a complete inventory and duplicated actions owned by detail pages. Open Projects by default and keep project, module, port, and service information on their authoritative pages. A future start page is justified only for cross-cutting problems that cannot be represented accurately in those pages. This supersedes the dashboard UI portion of ADR-015; service controllers and status models remain shared.

## ADR-063 — Task-owned tools and categorized application settings

Do not expose a generic Tools page when its actions already belong to task-specific workspaces. Keep custom PHP configuration on the PHP page, concrete file opening in the project file manager, and editor selection in Settings. Organize application settings with the shared horizontal tab pattern: General for preferences, Storage for cache and protected-data visibility, and About for read-only technical details. The portable editor remains a verified shared runtime, but it is launched only in the context of a concrete file rather than as an empty standalone tool.

## ADR-064 — Project-scoped file clipboard

Implement keyboard file-manager copy, cut, and paste as an application-owned clipboard scoped to the active project rather than silently consuming arbitrary host clipboard paths. Resolve every source and destination through the workspace boundary, reject reparse points and directory self-descendants, perform moves directly, and run recursive copies away from the WPF dispatcher. Clear the clipboard when project context changes. Support host interoperability only through an explicit pointer drag: export the validated current selection with Windows `FileDrop`, and accept a bounded set of explicitly dropped host files or link-free directory trees into the current or directly targeted project directory. An internal drag is identified with an application-owned data format and moves its selection; a same-directory or self-target drop is a no-op. Host-to-project drops copy. Resolve name collisions explicitly as overwrite, renamed copy, skip, or queue cancellation; an apply-to-remaining choice may cover later conflicts in the same operation, and directory overwrite means a recursive merge rather than wholesale destination deletion.

## ADR-065 — Project-scoped in-process task scheduler

Run explicitly configured PHP, Python, Node.js, and `npm run` tasks only while Portable Developer is open. Persist definitions by stable project ID with relative script paths under `instances/default/config`; never create a Windows service or scheduled task, invoke a host shell, inherit the host `PATH`, or accept an arbitrary executable. Resolve the registered project and verified portable runtime for every run, capture the project before launch, limit each task to one concurrent run and the scheduler to two, honor a validated timeout, and cancel owned work during application shutdown. Skip missed runs instead of replaying them. Keep at most 200 portable run records with bounded output and conservative credential masking; application diagnostics record only task IDs, outcomes, and exit codes. Removing a project from the catalog preserves its files and history but disables its scheduled definitions.

## ADR-066 — Explicit tray-owned application exit

Treat the main-window close affordance as Hide to notification area, not process shutdown, because the application owns long-lived services and an in-process scheduler whose accidental termination is destructive to the user's current development environment. Keep one visible tray icon for the running process; double-click, Open, and second-instance activation restore the main window. Expose process shutdown only as an explicitly confirmed Exit action in the tray menu, then cancel scheduled work and stop every owned service and process tree through the existing lifecycle. Do not add Windows startup registration, a service, or any non-portable host setting. Let Windows logoff and shutdown bypass close-to-tray so the application does not obstruct session termination.
