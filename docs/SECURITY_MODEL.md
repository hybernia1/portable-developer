# Security model

## Purpose

Portable Developer is a local Windows development environment. It owns processes and files below one portable root while avoiding operating-system installation and configuration changes. Its isolation protects portability and reduces accidental interference with the host; it is not an operating-system sandbox and does not make untrusted project code safe.

## Trust boundaries

| Boundary | Guarantee |
|---|---|
| Application root | Managed configuration, projects, databases, logs, caches, modules, profiles, and temporary data resolve below the portable root. Reparse-point and traversal escapes are rejected at managed file boundaries. |
| Host configuration | The application does not install services or drivers and does not modify system `PATH`, registry, file associations, hosts, or firewall rules. |
| Local services | Apache, PHP FastCGI, MariaDB, and Selenium bind to `127.0.0.1`. The port manager observes unrelated listeners but never stops or reconfigures them. |
| Process execution | Managed services have explicit binaries, working directories, environment variables, logs, health checks, and termination ownership. Project tools do not inherit a general system toolchain. |
| Package installation | Runtime URLs are not user supplied. Archives must match the bundled allowlist, HTTPS-origin policy, archive SHA-256, normalized entrypoint SHA-256, and package-specific validation before atomic installation. |
| User code | PHP, Python, Composer packages, browser automation, and project files run with the current Windows user's permissions and must be treated as trusted code selected by the user. |

## Network behavior

The application contains no telemetry, analytics, advertising SDK, automatic update check, or automatic crash upload. Outbound connections occur only after an explicit module/package action, opening a user-selected destination, or executing user project code. The complete disclosure is maintained in the [privacy policy](../PRIVACY.md).

Local listeners use loopback only:

- Apache serves local projects and phpMyAdmin;
- PHP FastCGI accepts requests from the local Apache instance;
- MariaDB accepts local database clients;
- Selenium accepts local WebDriver clients.

Portable Developer does not expose these services to the LAN and does not create firewall exceptions.
Windows can still display its own firewall consent prompt when the app-managed Java executable first opens Selenium's loopback listener. Portable Developer warns before the first start that the exception is unnecessary and the user can cancel the Windows prompt.

## Browser profiles and cookie vaults

Portable Developer does not scan, import, or extract credentials from host-installed browsers. A browser master is created or edited only in an app-managed Chrome for Testing or Firefox environment opened by the user. Cloud sync is disabled by application policy. Each Selenium session receives a disposable copy; the immutable master remains local and sensitive.

Cookie vaults accept only a file explicitly imported by the user. Cookie records are normalized and encrypted locally with AES-256-GCM. Cookie values, encryption keys, saved passwords, and profile contents are not logged or transmitted to maintainers. Possession of the complete portable folder includes possession of its local encryption key, so the folder itself must be protected and never attached to issues.

## Downloaded and third-party content

The base release contains the project-owned application and app-local Microsoft Visual C++ support. Optional Apache, PHP, MariaDB, Selenium, Java, browsers, drivers, Python, Composer, editor, and phpMyAdmin components retain their upstream licenses and signatures or unsigned state. Composer, pip, Selenium downloads, and user projects can introduce arbitrary third-party content selected by the user; Portable Developer does not claim that content is safe.

Only `PortableDeveloper.exe` is eligible for the project code-signing certificate. The release artifact configuration must not apply that signature to third-party executables or libraries.

## Explicit non-goals

Portable Developer does not:

- provide privilege isolation, virtualization, a container boundary, or malware analysis;
- identify or exploit host vulnerabilities;
- bypass Windows security, browser security, authentication, or network policy;
- collect host-browser sessions or credentials;
- make arbitrary Composer, pip, project, or downloaded content trustworthy;
- protect secrets after an attacker obtains the entire portable folder or controls the current Windows account.

## Reporting and removal

Report suspected vulnerabilities through [GitHub private vulnerability reporting](https://github.com/hybernia1/portable-developer/security/advisories/new). To remove the product, stop its services, close the application, back up any wanted project data, and delete the portable folder. No installed service, registry entry, firewall rule, or system `PATH` change remains.
