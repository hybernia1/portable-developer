# Privacy

Portable Developer does not send project data to its maintainers. It contains no telemetry, analytics, advertising SDK, automatic crash upload, or automatic update check. Configuration, databases, logs, temporary files, process state, profiles, and cookie vaults remain under the portable application root.

## User-initiated network activity

Network activity can occur only as a direct result of a user action or code in a user project:

- local Apache, MariaDB, PHP FastCGI, and Selenium listeners bind to configured local ports;
- the module manager downloads exact catalog-pinned archives from their upstream publishers after an install action;
- Composer and pip contact their package registries and package sources during package operations;
- opening a project, phpMyAdmin, or Selenium Grid passes a local URL to the default browser;
- Selenium automation and user code contact destinations selected by that code;
- a Selenium session using a cookie vault visits listed origins so WebDriver can insert cookies.

Upstream servers receive normal connection metadata such as an IP address. Their own privacy policies apply. Exact base-package hosts are visible in `catalog/dependencies.lock.json`; the application cannot extend this list remotely.

Relevant upstream privacy notices include [Microsoft](https://privacy.microsoft.com/privacystatement), [Mozilla](https://www.mozilla.org/privacy/websites/), [Google](https://policies.google.com/privacy), [GitHub](https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement), [Python](https://www.python.org/privacy/), [Apache](https://privacy.apache.org/policies/privacy-policy-public.html), [PHP](https://www.php.net/privacy.php), and [MariaDB Foundation](https://mariadb.org/privacy-policy/). Composer, pip, and user-selected websites or packages may involve additional independent services chosen by the user.

## Cookie vaults and browser profiles

Cookie imports are processed locally. Only cookie name, value, domain, path, expiry, `httpOnly`, `secure`, and `sameSite` are retained. Invalid, expired, duplicate, and extension-specific fields are removed. Values are encrypted with AES-256-GCM using an automatically generated 256-bit key in `state/selenium-cookie-vault.key`; names, domains, counts, and import time remain readable for the UI.

The key travels with the portable folder. Encryption protects a separately copied vault and detects tampering, but it does not protect against theft of the complete folder or access by the same Windows account. Cookie exports, `profiles/`, and `state/` may contain authentication secrets and must never be published or committed.

Managed browser masters may contain active sessions, saved credentials, bookmarks, extensions, and account data. Portable Developer does not display or send that content. Selenium uses a disposable copy with cloud sync disabled; the immutable master remains sensitive. Project `seldownloads` files persist until the user removes them.

## Diagnostics

Logs stay in `logs/`. Cookie values, cookie names, and vault encryption keys are not written to application logs. Before sharing a log, review paths, project names, and output produced by user code.

Material changes to this policy will be recorded in [CHANGELOG.md](CHANGELOG.md). Questions may be opened in [GitHub Issues](https://github.com/hybernia1/portable-developer/issues).
