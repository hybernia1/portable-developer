# Roadmapa

## M0 — Pravidla a návrh

- [x] Dokumentace, pracovní konvence a portable hranice.
- [x] C# / .NET 10 / WPF a seznam modulů první verze.

## M1 — Procesní jádro

- [x] Portable resolver cest.
- [x] Process supervisor, command runner, JSONL logování a TCP health check.
- [x] Automatické testy hranic a procesních operací.

## M2 — Apache a PHP

- [x] Inventář a hashové ověření modulů.
- [x] Generovaná Apache/PHP FastCGI konfigurace.
- [x] Řízený start/stop s rollbackem a health checkem.
- [x] Přibalený Apache 2.4.66, PHP 8.4.12 a app-local VC++ runtime.

## M3 — MariaDB a Selenium

- [x] Přibalená MariaDB 12.3.2 a transakční inicializace dat.
- [x] Přibalený Selenium Server 4.47.0 a Microsoft OpenJDK 25.0.3.
- [x] MariaDB start/stop controller, automatický první start a databázový health check.
- [x] Selenium start/stop controller, port a čitelná diagnostika WebDriveru.
- [x] Přibalený Firefox driver a načítání uživatelských Firefox, Chrome a Edge driverů.
- [x] Nastavení limitů Gridu, přehled relací, Hub a ukončení relace z UI.

## M4 — PHP nástroje a nastavení

- [x] Přibalený Composer 2.10.2 a samostatná správa projektových Composer balíčků.
- [x] Přibalený čistý Python 3.13.0 s pip a samostatná správa projektových knihoven.
- [x] Čeština/angličtina a portable uložení volby.
- [x] Navigační shell a samostatné detailní stránky serverových komponent.
- [x] UI pro bezpečné PHP volby a přibalená rozšíření.
- [ ] Obecný portable terminál nad explicitně vybraným runtime a pracovním adresářem.
- [ ] Správa projektů a virtual hosts bez zápisu do systémového hosts souboru.
- [x] Přehled velikostí a vytváření lokálních databází přes účet `root`.
- [x] Volitelné root heslo a lokální phpMyAdmin s cookie přihlášením.

## M5 — Kvalita vydání

- [x] Offline self-contained publish skript s připnutými hashi.
- [x] Dashboard bez runtime download/import kroků.
- [x] Jednotný stavový ovladač webového stacku a kontextové akce přímo v kartách služeb.
- [ ] Test na čistém Windows účtu a z USB/exFAT/NTFS disku.
- [ ] Kompletní inventář licencí a právní kontrola redistribuce třetích stran.
- [ ] Verzionovaný release proces, release archiv a jeho SHA-256.
