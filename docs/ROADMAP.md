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
- [ ] MariaDB start/stop controller a databázový health check.
- [ ] Selenium start/stop controller, port a čitelná diagnostika WebDriveru.

## M4 — PHP nástroje a nastavení

- [x] Přibalený Composer 2.9.4.
- [x] Čeština/angličtina a portable uložení volby.
- [ ] UI pro bezpečné PHP volby, rozšíření a Composer příkazy.
- [ ] Správa projektů a virtual hosts bez zápisu do systémového hosts souboru.

## M5 — Kvalita vydání

- [x] Offline self-contained publish skript s připnutými hashi.
- [x] Dashboard bez runtime download/import kroků.
- [ ] Test na čistém Windows účtu a z USB/exFAT/NTFS disku.
- [ ] Kompletní inventář licencí a právní kontrola redistribuce třetích stran.
- [ ] Verzionovaný release proces, release archiv a jeho SHA-256.
