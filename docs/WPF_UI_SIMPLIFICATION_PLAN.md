# WPF UI simplification plan

Status: accepted for incremental implementation on 2026-09-07.

## Objective

Make the WPF presentation layer small, predictable, and inexpensive to maintain without changing the application's product direction or introducing another UI framework.

The finished interface should use the built-in .NET WPF Fluent theme and standard controls, keep one authoritative state for navigation and operations, and isolate each feature page from the application shell. Visual novelty is not a goal. Reliability, clarity, Windows behavior, and removal of duplicated presentation logic are the priorities.

## Non-goals

- Do not add a third-party UI toolkit, navigation framework, dependency-injection container, or design system.
- Do not replace standard WPF control templates, window chrome, scrollbars, focus states, or accessibility behavior.
- Do not perform a big-bang rewrite or require pure MVVM for view-only behavior.
- Do not redesign service, process, storage, package, or project contracts while moving their presentation.
- Do not support a mobile layout. The supported minimum window remains a deliberate desktop constraint.
- Do not mix feature additions with structural page extraction.

## Current baseline

The current source already provides the correct visual foundation:

- `Application.ThemeMode` is `System`.
- Windows owns light, dark, high-contrast, accent, native title-bar, and standard-control states.
- Navigation has one typed page/section state and no longer uses nested `TabControl` indices.
- Long-running work renders progress in its owning page rather than a duplicate shell overlay.
- Reusable layout primitives are centralized in `Assets/WorkspaceStyles.xaml`.

The remaining debt is primarily structural:

| Area | Current baseline |
| --- | ---: |
| `MainWindow.xaml` | 2,124 lines |
| Primary pages | 15 |
| Section visibility roots | 20 |
| Named elements in `MainWindow.xaml` | 81 |
| Direct XAML event attributes | 96 |
| Direct assignments to `InstallationStatusText.Text` | 143 |
| `DashboardViewModel.cs` | 1,318 lines |
| `UiText.cs` | 1,838 lines |

These numbers are diagnostic, not quality targets by themselves. Completion is defined by ownership and behavior rather than an arbitrary line limit.

## Design rules

### Platform first

Use the built-in Fluent resources and native controls. A custom style is allowed only for a reusable application concept that WPF does not provide, or for behavior such as virtualization. A style targeting a standard control must inherit its implicit framework style.

### One owner for every state

- The shell owns the current route and globally selected project.
- A feature page owns its inputs, validation, transient result, collection selection, and progress presentation.
- Application services own portable data, processes, filesystem operations, and business rules.
- The application operation coordinator owns conflict exclusion only; it does not own user-facing progress text.

No page may write into another page's controls or into a generic shell status label.

### Pragmatic presentation separation

View models hold page data, state transitions, validation, and asynchronous actions. Code-behind remains acceptable for focus, selection, drag-and-drop, keyboard routing, dialog presentation, and other behavior coupled to WPF controls.

Do not introduce commands merely to move a one-line view concern out of code-behind. Do move feature logic out of `MainWindow` when it needs a service or mutates page state.

### One active content view

The shell eventually presents the current page through one `ContentControl`. Standard WPF data templates map page presentation models to page views. Page models remain alive where state must survive navigation; inactive visual trees do not need to remain attached merely to retain data.

Secondary sections remain part of the same typed route. The shell header derives its page title and optional section navigator from that authoritative route; it does not repeat the route as a breadcrumb.

### Local and durable feedback

Use four explicit feedback forms:

1. field-level validation beside the relevant input;
2. a page-owned inline result for a completed action or recoverable error;
3. one page-owned progress presentation for a running operation;
4. a modal dialog only for confirmation or a genuinely blocking decision.

The legacy `InstallationStatusText` shell label is removed after every caller has a local destination. Toasts, transparent busy overlays, and duplicate progress surfaces are outside this plan.

### Intentional layout values

Fixed icon sizes, minimum touch targets, readable form widths, and the desktop minimum window size are valid constraints. Repeated numbers are not converted into resources merely to eliminate literals.

A value becomes a shared style or layout policy when it represents a repeated semantic concept. Page-specific spacing may remain local. Concrete colors remain centralized and ordinary controls continue to use system theme brushes.

## Target structure

```text
MainWindow
|- WorkspaceShellViewModel
|  |- current WorkspaceRoute
|  |- navigation entries
|  `- active project context
|- AppSidebar
|- WorkspaceHeader
`- ContentControl
   `- current feature PageView
      |- feature PageViewModel
      `- application service contracts
```

Suggested source layout:

```text
src/PortableDeveloper.App/
|- Shell/
|  |- WorkspaceRoute.cs
|  |- WorkspaceShellViewModel.cs
|  `- WorkspaceLayoutMode.cs
|- Views/
|  |- Projects/
|  |- Services/
|  |- Selenium/
|  |- Packages/
|  |- Scheduler/
|  |- Terminal/
|  |- Files/
|  |- Guides/
|  `- Settings/
`- ViewModels/
   `- feature-scoped presentation models
```

The exact directories may be adjusted during the first vertical slice. The ownership boundaries must not be weakened for a preferred folder name.

## Responsive contract

The application remains a desktop tool with a supported minimum window of `900 x 620`. It needs two content modes, not a general-purpose responsive framework:

- **Wide:** related master/detail or form/result surfaces may be displayed side by side.
- **Compact:** those surfaces stack vertically or show the selected detail below the master list.

The mode is derived from available workspace width by one shell-owned policy. The initial transition target is approximately 760 device-independent pixels of content width and must be calibrated through manual tests. Individual pages must not invent their own unrelated breakpoints.

Layout rules:

- The shell owns its viewport and sidebar overflow.
- Each page has at most one outer vertical scroll boundary.
- Potentially unbounded dynamic collections own a finite, virtualized inner viewport.
- A whole page must not acquire horizontal scrolling to preserve a two-column layout.
- Headers and action groups wrap or stack in compact mode.
- Dynamic localized text must not depend on fixed row heights.
- Do not use WPF `DataGrid`; bounded collections use labeled adaptive rows, and list selection exists only where the workflow requires it.

## Navigation and operation policy

Navigation and project-context mutation are different actions and should not be coupled accidentally.

- The WPF dispatcher must remain responsive during every operation.
- An operation disables only conflicting actions and context changes required for correctness.
- Page navigation may remain temporarily locked while an operation must stay visible, but this is an explicit page policy rather than a side effect of a global overlay.
- When a page can safely continue work in the background, its presentation model retains progress across navigation and the shell does not duplicate that progress.
- The active-project selector is disabled only when changing the working directory would invalidate an active operation.

The current conservative route lock can remain until a migrated page proves that navigation-away behavior is safe. Any broader policy change requires an update to the UI operation architecture decision.

## Implementation stages

Every stage must leave the application buildable and must preserve the current user-visible behavior unless the stage explicitly removes a documented inconsistency.

### Stage 0 — Plan and baseline

- [x] Record the accepted direction and current structural measurements.
- [x] Preserve the system Fluent and typed navigation decisions.
- [x] Define non-goals, ownership rules, responsive behavior, and completion criteria.
- [x] Run and record the complete pre-refactor build and test baseline immediately before implementation begins.

Exit condition: the plan is linked from the documentation index and no production code has changed.

### Stage 1 — Presentation guardrails

- [x] Replace markup tests that depend on exact control counts with semantic assertions where necessary.
- [x] Retain hard guardrails against concrete colors, custom standard-control templates, any WPF data grid, duplicate operation overlays, and direct process ownership in UI code.
- [x] Add an STA smoke test that constructs application resources and extracted views, catching missing resources and invalid bindings where practical.
- [x] Add pure tests for page/section route transitions and the shared wide/compact layout policy.

Exit condition: structural extraction can proceed without weakening theme, navigation, or portability protection.

### Stage 2 — Package-manager vertical slice

- [x] Extract one reusable package-manager view for Composer, Node.js, and Python.
- [x] Reuse the existing `PackageManagerPageViewModel` state instead of retaining three copied XAML layouts.
- [x] Route install, removal, refresh, and package input through a typed package-manager identity rather than control-name switches.
- [x] Keep the operation presentation inside the reusable view.
- [x] Preserve direct/transitive dependency semantics and all safety checks in application services.
- [x] Validate Czech and English text, empty state, long package names, progress, success, failure, and removal.

Exit condition: the three pages share one authoritative markup and behavior path with no visual regression.

### Stage 3 — Thin shell presentation model

- [x] Introduce a shell-scoped presentation model containing route, navigation entries, active project, and shell availability only.
- [x] Move service-specific status decisions out of `AppSidebar` triggers and into data supplied by navigation entries.
- [x] Make `AppSidebar` a passive renderer with no Apache, MariaDB, Selenium, or ancestor-window knowledge.
- [x] Keep `WorkspaceHeader` limited to section navigation, one page title, and project-context presentation.
- [x] Remove blanket shell disabling where a narrower conflict rule is sufficient.

Exit condition: sidebar and header can be understood and tested without reading feature page code.

### Stage 4 — Real page hosting

- [x] Introduce one shell `ContentControl` and standard data-template mapping from the current page model to its view.
- [x] Retain the existing typed `NavigationPage` and `NavigationSection` route; do not add a navigation package.
- [x] Establish navigation activation/deactivation hooks only where a page requires refresh or cleanup.
- [x] Keep state that must survive navigation in presentation models, not in attached inactive visual trees.
- [x] Remove a page's old `Visibility` root immediately after its extracted view becomes authoritative.

Recommended extraction order:

1. Modules and Apache;
2. Ports and Settings;
3. PHP and Databases;
4. Projects;
5. Scheduler and Guides;
6. Terminal and Files;
7. Selenium.

Exit condition: `MainWindow.xaml` contains shell composition but no feature-page markup.

### Stage 5 — Page-owned feedback

This work proceeds alongside Stage 4 so each extracted page is complete before moving to the next one.

- [x] Give every feature page a local result/error state.
- [x] Replace its direct `InstallationStatusText.Text` assignments.
- [x] Keep validation adjacent to the relevant input.
- [x] Keep long-operation progress in exactly one owner view.
- [x] Remove `InstallationStatusText` after its final caller is migrated.
- [x] Verify that navigation cannot display a stale result from another feature.

Exit condition: no feature writes user-facing state into a named shell control.

### Stage 6 — Adaptive page layouts

- [x] Implement one shared wide/compact layout mode without replacing native control templates.
- [x] Convert repeated two-column feature layouts as each page is extracted.
- [x] Stack the workspace header controls in compact mode when required.
- [x] Remove nested or competing scroll boundaries.
- [ ] Validate the minimum, default, and maximized window at 100%, 150%, and 200% scaling.
- [ ] Validate Czech and English with long project, package, profile, vault, and file names.

Exit condition: every supported page remains usable without clipping at the documented minimum window size.

### Stage 7 — Presentation-model and localization cleanup

- [x] Split the current dashboard model by feature after its consumers have moved to feature views.
  - [x] Move Files paging and entries plus Scheduler tasks and history into their page models.
  - [x] Move TCP listener projection and PHP extension selection into their page models, using narrow runtime snapshots instead of dashboard forwarding.
  - [x] Move database rows and Selenium environments, sessions, profiles, vaults, and browser choices into their page models.
  - [x] Move project projection, templates, registration candidates, row types, and inspected selection into the Projects page while sharing one collection with the shell selector.
  - [x] Move Apache active-project presentation and Settings port/application identity into their page models, removing the final direct feature-page dependencies on the dashboard.
- [x] Retain a small root composition model rather than replacing one monolith with a generic service locator.
- [x] Split `UiText` into feature-scoped partial files or equivalent compile-time resources while preserving runtime Czech/English switching.
- [x] Remove obsolete converters, resources, named elements, handlers, and compatibility code after proving they have no consumers.
- [x] Update architecture decisions to describe the final shell, page-hosting, feedback, and operation policies.

Exit condition: adding or changing one page does not require editing `MainWindow.xaml`, unrelated feature handlers, or one monolithic localization file.

### Stage 8 — Release hardening

- [x] Run formatting, locked restore, Release build, and the complete test suite.
- [x] Publish the self-contained single executable through the standard repository script.
- [x] Verify startup from a clean portable root and upgrade against representative existing portable state.
- [ ] Manually test Windows 10 and 11 where available, system light/dark, high contrast, keyboard navigation, DPI scaling, empty and populated collections, long operations, cancellation, and close-to-tray behavior.
- [x] Record user-visible changes in `CHANGELOG.md`, architecture changes in `docs/DECISIONS.md`, and completed stages in `docs/WORKLOG.md`.

Exit condition: the verified executable preserves portable behavior and the UI simplification has no known functional regression.

## Feature migration tracker

The tracker records completed ownership, not merely the existence of a new file.

| Feature | Independent view | Local feedback | Adaptive layout | View/behavior tests |
| --- | :---: | :---: | :---: | :---: |
| Projects | [x] | [x] | [x] | [x] |
| Modules | [x] | [x] | [x] | [x] |
| PHP | [x] | [x] | [x] | [x] |
| Apache | [x] | [x] | [x] | [x] |
| Databases | [x] | [x] | [x] | [x] |
| Selenium | [x] | [x] | [x] | [x] |
| Ports | [x] | [x] | [x] | [x] |
| Composer / Node.js / Python | [x] | [x] | [x] | [x] |
| Scheduler | [x] | [x] | [x] | [x] |
| Terminal | [x] | [x] | [x] | [x] |
| Files | [x] | [x] | [x] | [x] |
| Guides | [x] | [x] | [x] | [x] |
| Settings | [x] | [x] | [x] | [x] |

## Verification checklist for every stage

- The solution restores with the locked dependency graph.
- Release build and relevant tests pass.
- No new system dependency, Windows service, registry entry, system `PATH`, host profile storage, or absolute persisted path is introduced.
- No UI code starts or owns an external process directly.
- No new concrete UI color exists outside the allowed brand/theme resource boundary.
- Standard controls retain their implicit Fluent style, keyboard behavior, and automation semantics.
- Czech and English text have defaults and remain switchable at runtime.
- Long-running work stays off the WPF dispatcher and has cancellation or a bounded completion path where the underlying operation permits it.
- The stage does not leave duplicate hidden markup or parallel legacy behavior behind.

## Definition of done

The plan is complete when:

- `MainWindow` contains only native window lifecycle and shell composition;
- one content host displays one authoritative feature view for the current route;
- every feature owns its view state, validation, result, and progress presentation;
- sidebar and header contain no feature-specific service logic;
- the generic `InstallationStatusText` channel no longer exists;
- all pages follow the shared wide/compact policy and remain usable at the supported minimum window;
- system light, dark, high-contrast, native chrome, keyboard, and accessibility behavior remain owned by WPF;
- no third-party UI framework or copied platform control template has been introduced;
- relevant automated tests and the self-contained release verification pass;
- architecture, worklog, changelog, and release documentation describe the resulting implementation.

## Progress record

### 2026-09-07

- Accepted optimization of the existing built-in WPF Fluent interface instead of adopting a third-party UI toolkit.
- Recorded the current structural baseline, target ownership model, responsive contract, staged migration, feature tracker, and completion criteria.
- Verified locked restore, formatting, and the Release build with zero warnings; all 301 automated tests passed.
- Left production code unchanged so implementation can begin from this verified baseline.
- Completed Stage 1 by replacing exact XAML control-count assertions with location-independent semantic guardrails, loading application resources and shell controls in an STA smoke test, and extracting tested navigation and wide/compact layout policies.
- Retargeted the UI-aware test assembly to `net10.0-windows`; the Release build completed with zero warnings and all 320 tests passed.
- Completed Stage 2 by replacing three copied package-manager layouts and their control-name-specific handlers with one reusable view, one typed manager identity, and one operation path for Composer, Node.js, and Python.
- Reduced `MainWindow.xaml` from 2,124 to 1,736 physical lines. The shared view loads through the STA smoke test, package identity/inventory/progress behavior has focused coverage, and all 325 tests pass.
- Completed Stage 3 by introducing `WorkspaceShellViewModel` as the owner of the typed route, section navigation, active-project identity, navigation metadata, service indicators, and operation-derived shell availability.
- Reduced `AppSidebar` and `WorkspaceHeader` to passive shell renderers. They no longer bind through the window to Apache, MariaDB, Selenium, or the global operation object, and individual controls now consume explicit shell capabilities.
- Added focused shell-model and structural coverage; formatting and the Release build complete with zero warnings and all 329 tests pass.
- Began Stage 4 with the Modules and Apache slice. One `WorkspacePageHost` now renders typed page models through implicit WPF data templates, while the two legacy visibility roots were deleted from `MainWindow.xaml` immediately.
- Added authoritative `ModulesPageView` and `ApachePageView` controls with narrow routed actions back to the existing application-service handlers. Their models retain runtime-card and Apache state across navigation without retaining inactive visual trees.
- Moved runtime installation progress, failure, and success entirely into the owning package card instead of also writing the generic shell status. `MainWindow.xaml` is down to 1,652 physical lines; the Release build has zero warnings and all 332 tests pass.
- Continued Stage 4 with Ports and Settings. Both routes now use the shared typed page host and their old visibility roots and named shell controls were removed immediately.
- Moved editable port values, per-port validation, listener presentation, editor preference, storage measurements, cache action availability, and local result text into page-scoped models. Host probing and persistence remain in the existing application-service handlers.
- Added pure port-status precedence tests plus page-host, local-feedback, implicit-template, and WPF construction checks. `MainWindow.xaml` is down to 1,399 physical lines; the Release build has zero warnings and all 337 tests pass.
- Continued Stage 4 with PHP and Databases. Their typed page models and implicit templates now own the routes, and both legacy visibility roots were removed from `MainWindow.xaml`.
- Moved PHP form values, extension selection, validation, save/reset/custom-INI results, MariaDB lifecycle and bootstrap feedback, database creation/catalog state, password-change results, and phpMyAdmin feedback into their owning pages. Password values remain transient inside the database view and are explicitly cleared only after a successful change.
- Kept configuration generation, persistence, runtime restart, database services, process supervision, and browser launching on their existing service-backed paths. `MainWindow.xaml` is down to 1,189 physical lines; the Release build has zero warnings and all 339 tests pass.
- Continued Stage 4 with Projects. The project browser, creation form, registration form, restart prompt, and local results now live in one persistent `ProjectsPageViewModel` rendered by `ProjectsPageView`; the legacy shell root and its four named form controls were removed.
- Preserved the distinction between inspecting a project and activating it. Row actions carry an explicit project ID and action kind, while catalog validation, transactional template creation, registration, web configuration, capability detection, filesystem launching, and project-context activation stay on their existing service-backed paths. `MainWindow.xaml` is down to 984 physical lines; the Release build has zero warnings and all 341 tests pass.
- Continued Stage 4 with Scheduler and Guides. Scheduler collections and local results now live behind `SchedulerPageViewModel`; row requests carry a stable task ID and explicit run, edit, or delete intent while dialogs and scheduler execution stay in the existing service-backed handlers.
- Moved guide category, search query, filtered article collection, selected article, and loaded bounded Markdown into a persistent `GuidesPageViewModel`. The view alone constructs and releases the WPF `FlowDocument`, so navigation preserves guide state without retaining an inactive visual tree or putting WPF document objects in the model.
- Removed both legacy visibility roots and their named controls from `MainWindow.xaml`, which is down to 719 physical lines. The Release build has zero warnings and all 343 tests pass.
- Continued Stage 4 with Terminal and Files. Terminal prompt text, protected output boundary, command history, busy/session input state, truncation, and focus requests now survive navigation in `TerminalPageViewModel`; its view alone owns caret, selection, keyboard, and scrolling behavior.
- Moved the file-manager visual tree, path input, sort labels, page-size selection, collection presentation, and local results into `FilesPageView` and `FilesPageViewModel`. Existing selection, rename, context-menu, keyboard, and drag/drop handlers are reached through a view-owned WPF interaction adapter while portable path validation, clipboard boundaries, conflict handling, background filesystem work, and file launching retain their existing service paths.
- Removed both legacy shell roots and all terminal/file named elements from `MainWindow.xaml`, which is down to 460 physical lines. The Release build has zero warnings and all 346 tests pass.
- Completed Stage 4 with Selenium. Settings, browser choice, profile and cookie-vault inputs, selected cookie file, progress, and results now live in `SeleniumPageViewModel`; explicit action requests retain the existing process, profile-store, vault-store, driver, and session service paths.
- Routed the three shared package-manager presentations through the same typed host, removed the final visibility-switched feature roots, and consolidated refresh-on-entry work in one explicit page-activation method. `MainWindow.xaml` now contains only shell composition at 92 physical lines; all 347 tests pass.
- Completed Stage 5 by deleting the generic shell status channel and its obsolete localized strings. Every feature result now stays in its owning page or operation card; active-project feedback is the sole shell-scoped exception and renders directly beside the selector it describes.
- Made Apache restart feedback require an explicit page sink, removed the redundant language-change acknowledgement, and added a whole-source guardrail preventing `InstallationStatusText` from returning. `MainWindow.xaml` is down to 85 physical lines; the debug build has zero warnings and all 349 tests pass.
- Began Stage 6 with one centrally derived wide/compact mode inherited by the workspace visual tree and one reusable two-child panel that changes only layout, leaving native Fluent control templates and interaction behavior intact.
- Converted repeated headers, forms, master/detail regions, and package, project, database, Selenium, guide, scheduler, and settings splits to the shared layout contract. At the minimum window, two-column regions stack and page-level horizontal scrolling remains disabled.
- Bounded the dynamic project list so it owns a useful inner vertical viewport instead of competing with the page scrollbar. Added WPF arrange checks for both modes and compact-width measurement of every main adaptive surface.
- Continued Stage 6 validation against a running Release build. Port and Selenium section navigation, localized data-grid headers, route-derived page titles, bounded vertical scrolling, and the non-tabular master-profile action group—including Copy ID—remain exposed through native UI Automation.
- Added an explicit overflow contract for long project, package, profile, vault, and file identities: bounded one-line values use ellipsis plus the full native tooltip, while descriptions and paths wrap. Formatting and the Release test run pass with all 355 tests. Multi-DPI and complete Czech/English visual validation remain open before Stage 6 is complete.
- Began Stage 7 by moving workspace entries, paging metadata, scheduled tasks, and scheduler history out of `DashboardViewModel` and into their authoritative Files and Scheduler page models. Their handlers now update the owning page directly; the root no longer republishes those collections or forwards their change notifications.
- Added focused page-state tests and source-boundary guardrails. `DashboardViewModel` dropped from 1,288 to 1,212 lines and all 358 tests pass; the remaining feature state and localization split are still open.
- Continued Stage 7 with Ports and PHP. Listener rows and localized counts now belong only to `PortsPageViewModel`, while extension selection belongs only to `PhpPageViewModel`; both models observe the shell route directly and receive a narrow runtime snapshot instead of subscribing to every dashboard change.
- Removed the duplicate root collections, listener reprojection, port-status aliases, and feature mutation methods. `DashboardViewModel` is down to 1,166 lines; focused state and source-boundary coverage brings the suite to 361 passing tests.
- Continued Stage 7 with Databases and Selenium. Database rows and all Selenium environment, browser-choice, session, profile, and cookie-vault projections now live with their page models, including localization-aware reprojection from retained application snapshots.
- The dashboard retains only service lifecycle and the ready-environment count required to gate Selenium startup; page runtime state crosses through explicit immutable records. Feature row types moved out of the root file, which is down to 1,004 lines, and all 364 tests pass.
- Continued Stage 7 with Projects. Its page model now owns the raw catalog snapshot, capability projection, templates, registration candidates, inspected selection, and project row types while the shell selector consumes that same authoritative observable collection.
- Runtime readiness and Apache restart presentation cross through an explicit project runtime snapshot; catalog, filesystem, capability detection, active-context mutation, and web configuration remain service-owned. `DashboardViewModel` is down to 826 lines and all 366 tests pass.
- Completed the feature split with Apache and Settings. Apache now owns its active web-project snapshot and derived document root, while Settings owns its port snapshot and immutable application version; neither page observes or forwards the dashboard.
- Removed the obsolete web-project presentation wrapper and the root's public service-card collection, moving the shared service-card record into its own file. No feature page references `DashboardViewModel`; the root is down to 785 lines and all 369 tests pass.
- Extracted verified module/runtime inventory and Apache, MariaDB, and Selenium lifecycle presentation into the concrete `WorkspaceRuntimeCoordinator`. It owns no page, view, process controller, or generic service resolution; the root applies its state to explicit feature snapshots.
- `DashboardViewModel` now contains only shell/page construction, typed route selection, language/runtime refresh entry points, and runtime-to-page composition. It is down to 249 lines, the obsolete UI-level PHP preflight dependency is gone, and all 371 tests pass.
- Split the 1,832-line localization contract into one 113-line language-state/navigation core and feature-scoped partial files for guides, modules, projects, terminal, ports, files, packages, PHP, Selenium, databases, settings, services, and scheduling. Public bindings and call sites remain unchanged.
- Added runtime Czech/English switching coverage across multiple feature partials and a structural size guard. The largest localization file is 286 lines and all 373 tests pass.
- Audited XAML resources, named elements, private fields and handlers, converters, page adapters, and the full `UiText` contract by source reachability. Removed seven unused vector icons, seven unused `x:Name` declarations, and 44 obsolete localized members; active converters and the Files view interaction adapter remain because they have concrete consumers.
- Added source-level guards that reject declaration-only keyed resources, named elements, and localization members before they can accumulate again. Locked restore and formatting verification pass; the Release build has zero warnings and all 375 tests pass.
- Completed the Stage 7 architecture audit by removing all feature-view event attributes from `MainWindow.xaml`. The 55-line shell now contains one passive current-page host and no feature view namespace; a focused `MainWindow.PageRouting` partial registers bubbled typed actions and leaves their implementations in the existing feature partials.
- Recorded the final shell, typed page-host, page-local feedback, state-only operation coordination, runtime composition, localization, and action-routing boundaries in ADR-069 through ADR-072 and `docs/ARCHITECTURE.md`. The Stage 7 exit condition is satisfied; locked restore and formatting verification pass, the Release build has zero warnings, and all 375 tests pass.
- Began Stage 8 with locked restore, formatting verification, dependency-catalog validation, a zero-warning Release build, and all 375 tests. The standard online publish path produced the unsigned 71,402,831-byte single EXE, matching checksum, and SPDX 2.2 SBOM under `artifacts/publish`; the executable SHA-256 is `93c2c00da9ac3b56ec8f1bc380861346369c07ab14b656de8439fcb1a523e994`.
- Verified a 12-second first start from an isolated clean portable root. The process stayed alive, materialized the verified seed and all portable roots, wrote one successful startup log entry with no errors, and retained the published executable hash.
- Simulated an upgrade in that isolated root by damaging an application-owned catalog and adding representative project, profile, download, and custom-state files. The next start repaired the catalog from the embedded verified seed, preserved all four user files byte-for-byte, and produced no application or Windows runtime error. Native visual automation exposed no Windows application surface in this session, so the Windows/theme/DPI/keyboard/manual-operation matrix remains explicitly open.
- Corrected preview regressions found during manual validation: shared package-manager localization now resolves through its host model, rebuilding the project collection reapplies the active identity, and project switching no longer waits for serial package inventories.
- Replaced the package `DataGrid` with a non-selectable card list that keeps name, description, version, and removal together. Added regression coverage for labels, startup project selection, context reset, the lazy inventory boundary, and list presentation; the remaining cross-version/theme/DPI manual matrix stays open.
- Formatting verification and the zero-warning Release build pass with all 379 tests. A new single-file artifact passed isolated startup smoke testing; the remaining manual visual matrix is still intentionally open.
- Removed redundant success text below the active-project selector while retaining the same channel for errors. Converted the database catalog to non-selectable cards with direct phpMyAdmin management and confirmed, service-validated deletion; `portable_dev` is protected in both UI and application service. The Release build remains warning-free and all 382 tests pass.
- Published and copied the updated single-file preview with SHA-256 `bbc432599797dc9335d1d1150b6b41e12680683e530c5af3771770c3a1d84423`.
- Removed the redundant breadcrumb from the workspace header and deleted its unused presentation/localization state. The typed page/section route remains authoritative through one page title and the optional shared section navigator.
- Replaced the irregular wrapping module-card cloud with three semantic catalog groups and one reusable adaptive row template. Runtime grouping follows the authoritative module collection and preserves existing install actions and operation state; the zero-warning Release build and all 383 tests pass.
- Published the grouped-module preview and copied it to `E:\portabledev\PortableDeveloper-module-catalog-preview.exe`; source and destination SHA-256 match: `0b43a1b132a35879585f8e987625a40ceb2821f1b704106ec05ac7b18eda4cc9`. The established preview filename remained untouched because that executable was running.
- Replaced the active scheduled-task `DataGrid` with a bounded non-selectable catalog. Every adaptive row keeps task identity, target, explicitly labeled type/plan/status, run timing, and all three actions together. Formatting verification and the zero-warning Release build pass with all 383 tests.
- Published the scheduler-catalog preview and copied it to `E:\portabledev\PortableDeveloper-scheduler-catalog-preview.exe`; source and destination SHA-256 match: `74a4d04a95568d790931b56da8ea5aa8084c57d09aa670aed8bfc059f091e005`.
- Removed the final data grids from scheduler history, TCP listeners, and Selenium sessions and deleted the now-unreachable global grid style. Scheduler history is filterable across visible metadata and captured output; details open in a resizable modal, while confirmed individual or project-wide deletion remains scoped through the portable scheduler history store. The zero-warning Release build and all 385 tests pass.
- Published the no-data-grid preview and copied it to `E:\portabledev\PortableDeveloper-no-datagrid-preview.exe`; source and destination SHA-256 match: `472d1ff04be30ac09fa104d30e1a9d687e54d28bea7fca6d32b20b84540dbf25`.
- Prepared version 1.30.0 for release after a final dependency-catalog audit, locked restore, formatting check, zero-warning Release build, all 385 tests, and successful single-file metadata, checksum, and SBOM generation.
