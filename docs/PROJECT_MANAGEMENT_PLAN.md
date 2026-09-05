# Central project management plan

Status: accepted for staged implementation. This document is the living implementation checklist for replacing the web-project-centered UI with a general project context. Check an item only after its code, tests, documentation, and migration behavior are complete.

## Objective

Make a project the common workspace used by files, the terminal, package tools, browser automation, and optional Apache hosting. A project is not permanently classified as Web, Python, Node.js, or Selenium. Those labels are templates and detected capabilities, so the same project can change focus without conversion or recreation.

The design must preserve the portable contract:

- every persisted path is relative to the Portable Developer root;
- managed projects remain below the instance root;
- runtimes, browsers, servers, caches, and application state remain shared below the portable root;
- selecting a template or detecting a technology never downloads a runtime or executes project code;
- external folders are copied into the portable root instead of being retained as absolute references;
- project removal does not delete source files unless the user chooses a separate, explicit destructive action;
- links and reparse points never provide an escape from a managed project root.

## Current baseline

The existing implementation already provides much of the required behavior, but its ownership is inverted:

- `IWebProjectCatalog` owns both Apache web configuration and the active tools project.
- `JsonWebProjectCatalog` persists `instances/default/config/web-projects.json` and creates a PHP starter page for every project.
- Composer, npm, the terminal, and the file manager read `IWebProjectCatalog.ActiveProject`.
- project selection is repeated independently on several pages.
- Apache configuration consumes every enabled web project and generates `.localhost` virtual hosts.
- Python runtime and its managed package directory are currently shared by the installation rather than selected per project.
- several infrastructure services have convenience constructors that create their own catalog instance, so project context is not yet represented by one application-owned service.

The migration will reuse the safe roots, IDs, existing project files, and Apache virtual-host behavior. It will not introduce a second competing project system.

## Chosen model

### Project identity

A project is a registered, managed directory plus optional feature-specific settings. Its identity does not change when files or technologies change.

Persisted project data contains only information that cannot be reliably inferred:

- stable ID;
- display name;
- portable root-relative directory;
- optional web configuration.

Template choice, detected technologies, runtime readiness, package inventories, and generated URLs are not persisted as project identity.

### Two scopes

Portable Developer must name the two scopes consistently in the UI:

**Shared environment**

- Apache, PHP, MariaDB, Python, Node.js, Composer, Selenium Server, OpenJDK, browsers, drivers, and editor;
- download catalogs and verified runtime installations;
- bounded application-owned caches and ports;
- service lifecycle and health.

**Active project**

- source files and project folders;
- terminal working directory;
- `composer.json` and `vendor`;
- `package.json`, lock files, and `node_modules`;
- Selenium scripts and persistent `seldownloads`;
- optional Apache web root and `.htaccess` policy.

The current shared Python package store remains unchanged during the project-catalog migration. Project-local Python isolation is a later, separate architecture decision because it affects dependency deduplication, import precedence, migration, and cleanup. Technology detection may read `requirements.txt` or `pyproject.toml`, but must not imply that those dependencies are already installed.

### Capabilities, not types

Capabilities are computed snapshots. They guide the UI but never mutate the project automatically.

| Capability | Evidence | User-facing action |
| --- | --- | --- |
| Web | persisted web configuration, `public/`, or an HTML/PHP entry point | Enable/configure Apache hosting |
| PHP | `composer.json` or PHP files | Open PHP/Composer tools; install a missing shared runtime explicitly |
| Node.js | `package.json` or common JS/TS files | Open Node.js packages or terminal commands |
| Python | `pyproject.toml`, `requirements.txt`, or Python files | Open Python packages or terminal commands |
| Browser automation | common Selenium files/imports, or project download settings | Open Selenium guidance and shared server controls |

Detection must be bounded, synchronous over filenames where possible, and free of code execution. Deep content inspection is limited to small UTF-8 manifest/source files and tolerant of malformed input. An absent signal means “not detected,” not “unsupported.” All tools remain available to every project when their shared runtime is ready.

## User experience

### Global project context

Add a compact project selector to the application shell, visible on every page where horizontal space allows. It shows the active project name and opens the Projects page for management. Individual package, terminal, and file pages stop rendering their own independent selectors after the global selector is established.

Changing the active project:

1. validates the selected registered project and its managed root;
2. refuses the change while a project-scoped package operation or interactive terminal session is active, with a concrete explanation;
3. persists the selection atomically;
4. raises one project-context change notification;
5. refreshes project-scoped pages and clears directory navigation/history that belongs to the previous project;
6. does not start, stop, or restart global services.

Apache may serve multiple enabled projects simultaneously, so switching the active project does not change Apache configuration. It only changes which project the tools operate on.

### Projects page

Add `NavigationPage.Projects` near the top of the Environment group. The page has three bounded areas:

1. **Project list** — name, relative path, active marker, detected capability chips, and web status.
2. **Project overview** — actions and readiness for the selected project.
3. **Create or add project** — template, name, and safe source choice.

Primary actions:

- Activate project.
- Open files.
- Open terminal.
- Open project directory.
- Configure or disable web hosting.
- Rename the display name.
- Unregister the project while retaining its files.

Deleting project files is deliberately separate from unregistering. If later added, it must display the exact portable-relative target, require explicit confirmation at action time, reject the default compatibility project, reject reparse points, stop conflicting project-scoped work, and never recurse outside the validated managed project root.

### Creation templates

Templates create initial files only and are not stored as a permanent project type.

| Template | Initial content | Automatic external action |
| --- | --- | --- |
| Empty | project root only | none |
| Web | `public/index.html` and enabled web configuration | none |
| Python | `main.py` and an optional empty `requirements.txt` | none |
| Browser automation | a small example script and README explaining the shared Selenium endpoint | none |
| Node.js | minimal `package.json` and source entry point | none |

Templates never install modules or dependencies. After creation the UI may show explicit installation actions for missing shared runtimes or project dependencies.

The Web template and later web-capability enablement use one shared static `index.html` starter. The file is created only when web support is enabled and `index.html` is absent; existing content is never replaced and PHP remains an optional shared runtime.

The legacy default project remains registered at `instances/default/www`, keeps its existing files and web behavior, and cannot be unregistered in the first implementation. New project roots remain `instances/default/projects/<id>`.

### Adding existing work

Support two safe flows:

- **Register portable folder:** register an existing real directory directly below `instances/default/projects` after validating its ID, containment, and absence of reparse points.
- **Import host folder:** after explicit user selection, copy it into a new managed project root. Never persist the host path. Reject links/reparse points and path escapes, report skipped or refused content, use staging plus atomic final placement, and leave the source untouched.

Host-folder import is scheduled after the core catalog migration because it requires separate progress, cancellation, collision, size, and cleanup behavior.

### Web configuration

Web hosting is optional project configuration rather than project identity:

- `web = null` means the project is not configured for Apache;
- an enabled web configuration is included in generated virtual hosts;
- a disabled configuration retains its chosen web root and `.htaccess` preference without being served;
- the hostname remains computed from the project ID and is never persisted;
- document roots remain inside the project root;
- Apache continues to bind to loopback and never edits the hosts file.

Enabling web hosting asks for `.` or a relative directory such as `public`. For a missing directory, the UI states exactly what will be created. It does not create or replace an application entry point unless the user selected a creation template that includes one.

Changing web configuration while Apache is running presents a predictable “Save and restart Apache” action. Cancellation leaves both persisted and generated configuration unchanged. A failed restart reports the failure and retains recoverable configuration state.

## Persistence format

Introduce a versioned catalog at `instances/default/config/projects.json`:

```json
{
  "schemaVersion": 2,
  "activeProjectId": "default",
  "projects": [
    {
      "id": "default",
      "name": "Default",
      "rootRelativePath": "instances/default/www",
      "web": {
        "isEnabled": true,
        "rootRelativePath": "public",
        "allowHtaccess": true
      }
    },
    {
      "id": "automation-lab",
      "name": "Automation Lab",
      "rootRelativePath": "instances/default/projects/automation-lab",
      "web": null
    }
  ]
}
```

Rules:

- `schemaVersion` is required after migration.
- IDs use the existing lowercase ASCII slug rules and remain stable when the display name changes.
- roots are normalized root-relative paths and must match the managed default/project layout.
- web roots are relative to their project and may not contain empty, `.` (except as the complete value), or `..` segments.
- unknown future fields are tolerated by older readers only when doing so cannot weaken validation.
- writes use a same-directory staged file and atomic replacement; a malformed catalog fails to the last valid backup or the safe legacy default rather than partially loading records.

Do not place runtime readiness, detected capabilities, absolute paths, package lists, credentials, service PIDs, or generated hostnames in this file.

## Application architecture

Replace the web-specific context with these responsibilities:

```text
IProjectCatalog
  owns registered projects and atomic persistence

IProjectContext
  owns the active project and emits one change notification

IProjectTemplateService
  stages and creates initial project content without downloads

IProjectCapabilityDetector
  returns a bounded read-only capability snapshot

IProjectWebConfigurationService
  validates optional web settings and coordinates Apache regeneration
```

Domain records should be immutable. Infrastructure validates physical paths and performs filesystem work. WPF view models consume application abstractions and must not construct catalogs or control processes directly.

Only the composition root creates the catalog/context implementation. Remove convenience constructors that silently create `JsonWebProjectCatalog`; all consumers receive the same injected `IProjectContext`. This prevents stale active-project snapshots and makes project switching observable and testable.

Expected consumer changes:

- `WorkspaceFileManager` resolves its root from `IProjectContext.ActiveProject`.
- `PortableTerminalService` captures the active project when a session starts and retains that root until the session ends.
- Composer and npm managers resolve project roots from the context at operation start.
- Selenium downloads resolve the active project at session creation and retain that path for the session.
- Apache receives only projects with enabled web configuration; it does not own active-project selection.
- dashboard and package view models subscribe to a single application-level refresh path instead of manually duplicating selector logic.

## Operation and failure rules

- Global service lifecycle never locks project selection by itself.
- Project-scoped package operations and terminal sessions lock selection for their duration.
- File operations resolve and validate the active root for every operation; they do not retain an obsolete mutable root.
- A capability scan is canceled and restarted after a project-context change.
- Failed project creation removes only its own staging directory after validating that directory; it never removes a pre-existing destination.
- Failed registration or migration does not change the active project.
- Unregistering an active non-default project activates the default project first and emits one context-change event.
- Removing a catalog entry leaves its directory and all project data untouched.
- Logs use project IDs and portable-relative paths, never host source paths, package secrets, file contents, or credentials.

## Migration from `web-projects.json`

Migration is automatic, idempotent, and non-destructive:

1. If a valid `projects.json` exists, load it and do not read legacy state for replacement.
2. Otherwise read and normalize `web-projects.json` with the existing validation rules.
3. Convert every valid `WebProject` into a general project with matching ID, name, root, active state, and web settings.
4. Preserve the default project at `instances/default/www` and all non-default roots under `instances/default/projects/<id>`.
5. Write `projects.json.part`, reread and validate it, then atomically place `projects.json`.
6. Leave `web-projects.json` untouched for rollback during at least one released compatibility cycle.
7. Record a non-sensitive migration result in the application log.
8. Never create starter pages while merely loading or migrating the catalog.

The new implementation must be able to start with legacy state containing disabled web projects, custom web roots, `.htaccess` choices, missing directories, malformed records, a missing active project, or duplicate IDs. Invalid records are reported and skipped; no existing project directory is deleted or overwritten.

Rollback during the compatibility cycle consists of running an older executable, which still sees the untouched legacy catalog. Changes made only through the new catalog will not be backported automatically, so the release notes must state this boundary before legacy writes are retired.

## Implementation stages

Each stage should be a focused commit and leave the application buildable and testable.

### Stage 0 — Contract and fixtures

- [x] Add generic immutable project and optional web-settings records.
- [x] Add catalog/context/capability/template abstractions in the Application layer.
- [x] Add representative legacy and versioned JSON fixtures.
- [x] Add validation tests for IDs, relative roots, web roots, duplicates, malformed state, and reparse points.
- [x] Keep production consumers on the existing catalog until the migration implementation passes independently.

Acceptance: the new contract compiles, has no WPF or process dependency, and tests describe all persistence invariants.

### Stage 1 — Versioned catalog and migration

- [x] Implement `JsonProjectCatalog` with atomic persistence and the migration algorithm above.
- [x] Stop creating starter files as a side effect of catalog construction.
- [x] Preserve all valid current projects and active selection in migration tests.
- [x] Keep the legacy file untouched.
- [x] Add corruption, interrupted-write, missing-directory, and repeated-migration coverage.

Acceptance: a copy of real current catalog shapes migrates repeatedly without changing or deleting project files.

### Stage 2 — One active project context

- [x] Implement one application-owned `IProjectContext` instance and change notification.
- [x] Inject it into the file manager, terminal, Composer, npm, and Selenium project-download resolution.
- [x] Remove constructors that instantiate their own project catalog.
- [x] Centralize project-switch blocking around active project-scoped operations.
- [x] Verify that switching cannot redirect an already running command or Selenium session.

Acceptance: every project-scoped consumer resolves the same active project, and one switch produces one coherent refresh.

### Stage 3 — Projects page and global selector

- [x] Add the Projects navigation page, bounded list, overview, and global selector.
- [x] Add Czech and English text for every new setting, state, validation, and failure.
- [x] Add activate, rename, open files, open terminal, and unregister-without-deletion actions.
- [x] Remove duplicated selectors from Composer, Node.js, terminal, and file pages after keyboard and screen-reader behavior is verified.
- [x] Make empty state, missing directory, invalid registration, and blocked switching understandable to a new user.

Acceptance: a user can always see which project is active and which scope an action affects.

### Stage 4 — Creation and detection

- [x] Implement staged project creation and the five templates.
- [x] Implement bounded, read-only capability detection with cancellation.
- [x] Show capability and missing-runtime states without automatic downloads.
- [x] Register safe existing directories already under the managed projects root.
- [x] Ensure template failure cannot leave a catalog record pointing to partial content.

Acceptance: template choice does not constrain later features, and adding files changes detected capabilities without changing project identity.

### Stage 5 — Optional web configuration

- [x] Move Apache-specific fields behind optional project web settings.
- [x] Add enable, configure, disable, and `.htaccess` actions to the Projects page.
- [x] Keep all enabled projects in generated `.localhost` virtual hosts.
- [x] Add explicit save/restart behavior when Apache is running.
- [x] Ensure static-only Apache blocks PHP source and phpMyAdmin remains unavailable without PHP.
- [x] Retire the project-management tab from the Apache page; keep Apache status and configuration there.

Acceptance: a non-web project can become web-enabled and later be disabled without moving or rewriting its source files.

### Stage 6 — Host-folder import

- [ ] Add explicit source-folder selection followed by copy into a new staging root.
- [ ] Define and enforce item-count, total-size, individual-file-size, path-length, cancellation, and collision handling.
- [ ] Reject links/reparse points and special device paths.
- [ ] Show progress and a complete result summary.
- [ ] Register the project only after the final root is complete and validated.

Acceptance: no absolute source path is persisted and cancellation leaves the source untouched and no active partial project.

### Stage 7 — Compatibility cleanup and Python decision

- [ ] After at least one compatibility release, decide when legacy catalog reading can be removed.
- [ ] Update architecture, guides, screenshots, release notes, and recovery documentation.
- [ ] Evaluate shared-only Python packages versus a shared base with optional project-local overlays in a separate ADR.
- [ ] Do not change Python dependency storage merely as a side effect of the project-catalog migration.

Acceptance: the central project system has shipped and migration evidence exists before compatibility code is removed or Python storage changes.

## Verification matrix

Automated tests must cover at least:

- fresh state, legacy migration, repeated migration, and corrupt/partial state;
- default and non-default roots, unicode display names, stable IDs, collisions, and rename;
- root, web-root, traversal, reparse-point, and malformed JSON rejection;
- creation rollback and unregister-without-delete;
- active context propagation to files, terminal, Composer, npm, and Selenium downloads;
- selection blocking during a package operation and interactive terminal session;
- selection remaining available while Apache, MariaDB, or Selenium is merely running;
- capability detection for empty, Web, PHP, Python, Node.js, and automation examples;
- capability detection limits and malformed files;
- Apache output for zero additional projects, enabled/disabled projects, custom roots, PHP present, and PHP absent;
- Czech and English UI values and accessible names;
- bounded lists, keyboard navigation, and refresh after a context change.

Manual testing on a fresh portable root must verify:

1. automatic migration of the default and existing web projects;
2. project creation from every template;
3. changing an Empty or Python project into an Apache-served project;
4. disabling web hosting without losing files;
5. switching the active project from every project-scoped page;
6. clear blocking while a terminal command is running;
7. Apache start/HTTP/stop with PHP installed and without PHP installed;
8. Selenium downloads staying in the project captured by the session;
9. removable-drive relocation without stale absolute paths;
10. application shutdown leaving no managed process or listener behind.

## Definition of done

The project-management work is complete only when:

- Projects is a first-class navigation page and the active project is globally visible.
- Apache no longer owns generic project creation or selection.
- projects can gain or lose web support without changing identity or moving files.
- all project-scoped tools use the same injected context.
- legacy state migrates automatically without changing project files.
- no runtime or dependency is downloaded because of detection, template selection, migration, or project activation.
- all stored paths remain portable and all filesystem boundaries reject escapes and reparse points.
- all new UI has validated defaults and Czech/English text.
- architecture, decisions, guides, changelog, work log, and release notes match the shipped behavior.
- formatting, automated tests, clean-root tests, and the manual verification matrix pass.

## Progress record

When continuing this work, update the stage checkboxes and append a dated note here with the commit, tests run, and any deliberate deviation. Do not mark a stage complete while one of its acceptance conditions remains unmet.

- 2026-09-05: architecture and staged migration plan accepted; implementation not started.
- 2026-09-05: Stage 0 completed with the generic contracts, logical and physical root validation, legacy/v2 fixtures, and 226 passing tests. Production consumers still use `IWebProjectCatalog`.
- 2026-09-05: Stage 1 completed with `JsonProjectCatalog`, validated atomic writes and backups, non-destructive legacy migration, recovery from corrupt/partial state, missing-directory tolerance, and 235 passing tests. Production wiring remains scheduled for Stage 2.
- 2026-09-05: Stage 2 completed with one injected `ProjectContext`, centralized switch blocking, and all project-scoped file, terminal, Composer, npm, and Selenium-download resolution routed through it. A temporary legacy adapter preserves the current Apache project UI until Stage 3; 239 tests pass.
- 2026-09-05: Stage 3 completed with the always-visible global selector, bounded Projects page, localized project states and management actions, removal of per-tool selectors, and a production switch to the versioned catalog. The WPF accessibility tree and live activation/files flows were verified on `E:\portabledev`; 248 tests pass.
- 2026-09-05: Stage 4 completed with atomic staged creation, five non-executing templates, bounded capability detection, shared-runtime readiness guidance, and content-preserving registration of existing managed folders. Live create/detect/unregister/register flows were verified on `E:\portabledev`; 263 tests pass.
- 2026-09-05: Stage 5 completed with optional contained web-root configuration, reversible Apache enablement and `.htaccess` settings, an explicit restart prompt for running Apache, and removal of project management from the Apache page. A follow-up simplified the Projects page into horizontal workflow tabs and one selectable master-detail view, with all web fields consolidated into one dialog. Live enable/disable and non-web-to-web flows were verified on `E:\portabledev`; 268 tests pass.
