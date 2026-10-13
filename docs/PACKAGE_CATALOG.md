# Download package catalog

`catalog/modules.json` allowlists exact server entrypoints. `catalog/dependencies.lock.json` pins upstream archives, versions, SHA-256 values, normalized entrypoints, sources, and licenses. Both ship with the release; the application accepts neither arbitrary URLs nor a remotely replaced catalog.

## Current runtime set

| Component | Version | Normalized entrypoint |
|---|---:|---|
| Apache | 2.4.68 | `bin/httpd.exe` |
| PHP | 8.4.12 | `php-cgi.exe` |
| MariaDB | 12.3.2 | `bin/mariadbd.exe` |
| Selenium Server | 4.47.0 | `selenium-server.jar` |
| Microsoft OpenJDK | 25.0.3 | `bin/java.exe` |
| Composer | 2.10.2 | `composer.phar` |
| Node.js | 24.19.0 | `node.exe` |
| Python | 3.13.0 | `python.exe` |
| Notepad++ | 8.9.2 | `notepad++.exe` |
| phpMyAdmin | 5.2.3 | release root |
| Chrome for Testing + ChromeDriver | 152.0.7977.54 | `chrome.exe` / `chromedriver.exe` |
| Firefox + geckodriver | 154.0 / 0.37.1 | `firefox.exe` / `geckodriver.exe` |

## Runtime installation

1. A user selects a logical package in the application.
2. The downloader uses only allowlisted HTTPS sources and validates redirect destinations.
3. Sources are tried in catalog order, with the next source attempted immediately after a failure and transient failures retried in bounded rounds.
4. A unique `.part` file becomes cache content only after its archive SHA-256 matches. Mirrors must serve byte-identical content under the same pinned digest.
5. Extraction under `temp/package-installs/<guid>` rejects traversal, symbolic links, and reparse points.
6. The normalized entrypoint is checked against its own SHA-256 and package-specific authenticity rules.
7. Metadata is written and the staged directory is moved into place atomically.
8. Successful installation deletes the source archive; a failure rolls back only newly created paths.

Apache keeps Apache Lounge as its canonical source and uses the project's versioned GitHub runtime-asset release as a resilience mirror. The mirrored archive is unchanged, publishes the same pinned SHA-256 and embedded license notices, and does not weaken archive or installed-entrypoint verification.

Firefox installers additionally require the expected valid Mozilla Authenticode signature and are run only in extraction mode. App-local Visual C++ DLLs are extracted without installation from an exact signed Microsoft redistributable. phpMyAdmin validates release markers and dependency metadata.

The catalog can be validated without downloads:

```powershell
.\scripts\Fetch-Dependencies.ps1 -ValidateCatalogOnly
```
