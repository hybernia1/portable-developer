# Komponenty třetích stran

Licence `GPL-3.0-or-later` v kořenovém souboru [LICENSE](LICENSE) se vztahuje na vlastní zdrojový kód Portable Developeru. Online základ obsahuje app-local Microsoft VC++ runtime; ostatní samostatné programy a runtime třetích stran doplní uživatel ze zdrojů jejich vydavatelů. Volitelná offline distribuce je sdružuje předem. Tyto komponenty nejsou přelicencovány a nadále se řídí vlastními licencemi a autorskými právy.

| Komponenta | Verze v katalogu 0.9.0 | Licence | Zdroj |
|---|---:|---|---|
| Apache HTTP Server (Windows build Apache Lounge) | 2.4.68 | Apache-2.0; build může obsahovat další oznámení | [Apache HTTP Server](https://httpd.apache.org/) / [Apache Lounge](https://www.apachelounge.com/) |
| PHP | 8.4.12 | PHP-3.01 | [PHP](https://www.php.net/) |
| MariaDB Server | 12.3.2 | GPL-2.0-only; jednotlivé knihovny mohou mít další licence | [MariaDB](https://mariadb.org/) |
| Selenium Server | 4.47.0 | Apache-2.0 | [Selenium](https://github.com/SeleniumHQ/selenium) |
| Mozilla Firefox | 142.0 | MPL-2.0 a licence přibalených částí | [Mozilla Firefox](https://www.mozilla.org/firefox/) |
| geckodriver | 0.37.1 | MPL-2.0 | [Mozilla geckodriver](https://github.com/mozilla/geckodriver) |
| Chrome for Testing | 152.0.7977.54 | BSD-3-Clause a licence přibalených částí | [Chromium](https://www.chromium.org/) |
| ChromeDriver | 152.0.7977.54 | BSD-3-Clause | [Chrome for Testing](https://googlechromelabs.github.io/chrome-for-testing/) |
| Microsoft Build of OpenJDK | 25.0.3 | GPL-2.0-only WITH Classpath-exception-2.0 a licence přibalených částí | [Microsoft OpenJDK](https://www.microsoft.com/openjdk) |
| Composer | 2.10.2 | MIT | [Composer](https://github.com/composer/composer) |
| Python | 3.13.0 | PSF-2.0 a licence přibalených částí | [Python](https://www.python.org/) |
| pip | 24.2 | MIT | [pip](https://github.com/pypa/pip) |
| Notepad++ | 8.9.2 | GPL-3.0-or-later | [Notepad++](https://github.com/notepad-plus-plus/notepad-plus-plus) |
| phpMyAdmin | 5.2.3 | GPL-2.0-only a licence Composer závislostí | [phpMyAdmin](https://github.com/phpmyadmin/phpmyadmin) |
| Microsoft Visual C++ Redistributable DLL | 14.51.36247.0 | Microsoft redistribuční podmínky; nejde o součást GPL kódu | [Microsoft](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist) |

Konkrétní zdroj, verze a kontrolní součty vstupů jsou v `catalog/dependencies.lock.json` a `catalog/modules.json`; offline normalizaci popisuje `scripts/Bundle-OfflineDependencies.ps1`. Distribuce musí zachovat licenční a NOTICE soubory dodané jednotlivými komponentami. Před veřejným vydáním plného offline balíku je nutné zkontrolovat jeho úplný licenční inventář; samotný zdrojový repozitář stažené binárky neobsahuje.
