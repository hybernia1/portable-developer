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
- open local URLs and files through an explicit user action.

Moving the complete folder between writable drives must require no reinstall. Transient configuration is regenerated for the new location. User data that must survive includes `instances/`, `profiles/`, `state/`, and project `seldownloads`; caches and `temp/` are disposable.

The experimental single-executable online package initially contains only `PortableDeveloper.exe`. Its embedded seed contains only application-owned catalogs, resources, notices, and the verified app-local Visual C++ runtime. First launch stages and verifies those files below `temp/bootstrap`, installs them beside the executable, creates the standard portable data roots, and records completion in `state/portable-seed.json`. Repeated startup repairs changed app-owned seed files but never replaces or removes `instances/`, `profiles/`, `downloads/`, or other user content. Initialization fails closed when the destination is not writable, a target path is a reparse point, or seed validation fails.
