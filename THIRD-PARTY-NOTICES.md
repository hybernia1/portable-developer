# Third-party components

The root [GPL-3.0-or-later license](LICENSE) covers Portable Developer's own source code. The online base includes app-local Microsoft Visual C++ runtime files; users install other independent tools and runtimes from their publishers through the verified catalog. An optional offline package may aggregate them. Third-party components are not relicensed and retain their own copyright and license terms.

Only the project-owned `PortableDeveloper.exe` is eligible for the Portable Developer code-signing certificate. Third-party executables and libraries listed here retain their upstream signatures or unsigned state and must not be signed with the project certificate.

Transparent technology marks used in the interface are sourced from [Simple Icons](https://github.com/simple-icons/simple-icons). The catalog is CC0-1.0, while the marks remain subject to their owners' trademark and any individual licensing conditions. They identify installed technologies only and are not used as Portable Developer branding.

| Component | Version in 1.28.0 catalog | License | Source |
|---|---:|---|---|
| Apache HTTP Server (Apache Lounge Windows build) | 2.4.68 | Apache-2.0; build may contain additional notices | [Apache](https://httpd.apache.org/) / [Apache Lounge](https://www.apachelounge.com/) |
| PHP | 8.4.12 | PHP-3.01 | [PHP](https://www.php.net/) |
| Node.js | 24.19.0 | MIT | [Node.js](https://nodejs.org/) |
| MariaDB Server | 12.3.2 | GPL-2.0-only; individual libraries may differ | [MariaDB](https://mariadb.org/) |
| Selenium Server | 4.47.0 | Apache-2.0 | [Selenium](https://github.com/SeleniumHQ/selenium) |
| Mozilla Firefox | 154.0 | MPL-2.0 and bundled-component licenses | [Mozilla Firefox](https://www.mozilla.org/firefox/) |
| geckodriver | 0.37.1 | MPL-2.0 | [geckodriver](https://github.com/mozilla/geckodriver) |
| Chrome for Testing | 152.0.7977.54 | BSD-3-Clause and bundled-component licenses | [Chromium](https://www.chromium.org/) |
| ChromeDriver | 152.0.7977.54 | BSD-3-Clause | [Chrome for Testing](https://googlechromelabs.github.io/chrome-for-testing/) |
| Microsoft Build of OpenJDK | 25.0.3 | GPL-2.0-only WITH Classpath-exception-2.0 and bundled-component licenses | [Microsoft OpenJDK](https://www.microsoft.com/openjdk) |
| Composer | 2.10.2 | MIT | [Composer](https://github.com/composer/composer) |
| Python | 3.13.0 | PSF-2.0 and bundled-component licenses | [Python](https://www.python.org/) |
| pip | 24.2 | MIT | [pip](https://github.com/pypa/pip) |
| Notepad++ | 8.9.2 | GPL-3.0-or-later | [Notepad++](https://github.com/notepad-plus-plus/notepad-plus-plus) |
| phpMyAdmin | 5.2.3 | GPL-2.0-only and Composer dependency licenses | [phpMyAdmin](https://github.com/phpmyadmin/phpmyadmin) |
| Microsoft Visual C++ Redistributable DLLs | 14.51.36247.0 | Microsoft redistribution terms; not GPL-covered project code | [Microsoft](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist) |

Exact sources, versions, and checksums are recorded in `catalog/dependencies.lock.json` and `catalog/modules.json`. Packaging must retain license and NOTICE files supplied with each component. A public full offline bundle requires a separate complete license review; downloaded binaries are not committed to this source repository.
