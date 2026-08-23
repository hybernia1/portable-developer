# Portability contract

Portable Developer may write only under its own extracted root unless a user explicitly chooses a project input file or asks Windows to open a file/URL. Persistent application paths are relative to that root.

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
