# Komponenty třetích stran

Licence `GPL-3.0-or-later` v kořenovém souboru [LICENSE](LICENSE) se vztahuje na vlastní zdrojový kód Portable Developeru. Offline distribuce navíc sdružuje samostatné programy a runtime třetích stran. Ty nejsou přelicencovány a nadále se řídí vlastními licencemi a autorskými právy.

| Komponenta | Verze v 0.4.0 | Licence | Zdroj |
|---|---:|---|---|
| Apache HTTP Server (Windows build Apache Lounge) | 2.4.66 | Apache-2.0; build může obsahovat další oznámení | [Apache HTTP Server](https://httpd.apache.org/) / [Apache Lounge](https://www.apachelounge.com/) |
| PHP | 8.4.12 | PHP-3.01 | [PHP](https://www.php.net/) |
| MariaDB Server | 12.3.2 | GPL-2.0-only; jednotlivé knihovny mohou mít další licence | [MariaDB](https://mariadb.org/) |
| Selenium Server | 4.47.0 | Apache-2.0 | [Selenium](https://github.com/SeleniumHQ/selenium) |
| geckodriver | 0.37.1 | MPL-2.0 | [Mozilla geckodriver](https://github.com/mozilla/geckodriver) |
| Microsoft Build of OpenJDK | 25.0.3 | GPL-2.0-only WITH Classpath-exception-2.0 a licence přibalených částí | [Microsoft OpenJDK](https://www.microsoft.com/openjdk) |
| Composer | 2.10.2 | MIT | [Composer](https://github.com/composer/composer) |
| Python | 3.13.0 | PSF-2.0 a licence přibalených částí | [Python](https://www.python.org/) |
| pip | 24.2 | MIT | [pip](https://github.com/pypa/pip) |
| Notepad++ | 8.9.2 | GPL-3.0-or-later | [Notepad++](https://github.com/notepad-plus-plus/notepad-plus-plus) |
| phpMyAdmin | 5.2.3 | GPL-2.0-only a licence Composer závislostí | [phpMyAdmin](https://github.com/phpmyadmin/phpmyadmin) |
| Microsoft Visual C++ Redistributable DLL | verze hostitelského buildu | Microsoft redistribuční podmínky; nejde o součást GPL kódu | [Microsoft](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist) |

Konkrétní zdroj, verze a kontrolní součty release vstupů jsou v `catalog/modules.json`, ve skriptu `scripts/Bundle-OfflineDependencies.ps1` a ve výsledném `bundle-manifest.json`. Distribuce musí zachovat licenční a NOTICE soubory dodané jednotlivými komponentami. Před veřejným vydáním nového binárního balíku je nutné zkontrolovat jeho úplný licenční inventář; samotný zdrojový repozitář stažené binárky neobsahuje.
