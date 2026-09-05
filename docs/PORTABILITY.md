# Portability contract

Portable Developer stores all persistent application data under its own extracted root unless a user explicitly chooses a project input file or asks Windows to open a file/URL. Persistent application paths are relative to that root. The self-contained .NET host may extract immutable native framework files into its transient per-user bundle cache when starting the single executable; Portable Developer stores no user data or configuration there.

The application must not:

- install Windows services, drivers, scheduled tasks, or system runtimes;
- modify the system or user `PATH`, registry, file associations, hosts file, or firewall;
- copy runtime files into Windows directories;
- use a host-installed PHP, Python, Java, browser, WebDriver, database, or web server;
- persist an absolute drive letter or Windows profile path as application configuration.

It may:

- inspect local TCP listeners without changing them;
- start verified portable processes with explicit paths and a controlled environment;
- generate run-specific absolute paths only in transient configuration under `temp/`;
- download pinned catalog artifacts after an explicit user action;
- run trusted project code, which retains normal Windows-user permissions and is not sandboxed;
- schedule explicitly configured project scripts while Portable Developer itself is running;
- open local URLs and files through an explicit user action.

The project scheduler is application-local. It stores only project IDs and relative script paths, uses verified portable runtimes, and runs while the application process is present, including while its main window is hidden in the Windows notification area. Closing the window does not create an autostart entry or a Windows background service. Explicit Exit stops the scheduler and owned processes; missed runs are skipped rather than replayed after a drive was disconnected or the application was stopped.

Moving the complete folder between writable drives must require no reinstall. Transient configuration is regenerated for the new location. User data that must survive includes `instances/`, `profiles/`, `state/`, and project `seldownloads`; caches and `temp/` are disposable.

The single-executable online package initially contains only the downloaded EXE. Its embedded seed contains only application-owned catalogs, resources, notices, and the verified app-local Visual C++ runtime. First launch stages and verifies those files below `temp/bootstrap`, installs them beside the executable, creates the standard portable data roots, and records completion in `state/portable-seed.json`. Repeated startups repair changed app-owned seed files but never replace or remove `instances/`, `profiles/`, `downloads/`, or other user content. Initialization fails closed when the destination is not writable, a target path is a reparse point, or seed validation fails.
