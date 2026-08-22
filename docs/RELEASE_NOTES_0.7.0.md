# Portable Developer 0.7.0

Toto vydání sjednocuje vzhled aplikace, zpřehledňuje každodenní práci a zásadně rozšiřuje Selenium bez vnuceného výchozího prohlížeče.

## Selenium

- Selenium Server a portable OpenJDK se instalují bez WebDriveru.
- Na samostatné kartě lze explicitně stáhnout hashově ověřený Microsoft Edge WebDriver, ChromeDriver nebo geckodriver.
- Vlastní `msedgedriver.exe`, `chromedriver.exe` a `geckodriver.exe` pod `drivers/custom/` zůstávají podporované.
- Bez alespoň jednoho načteného driveru aplikace spuštění Selenium nenabídne.
- Nová karta Profily importuje Edge, Chrome nebo Firefox profil jako read-only master.
- Relace s capability `portable:profile=<id>` vždy dostane vlastní pracovní kopii. Kopie se uklidí při chybě startu, standardním ukončení, zániku relace i při příštím startu aplikace po pádu.
- Profily ani drivery nemění systémový `PATH`, registr nebo instalaci hostitelského prohlížeče.

Chrome a Edge vyžadují driver odpovídající verzi nainstalovaného prohlížeče. Verze je proto v katalogu i UI vždy viditelná; aplikace netvrdí, že jeden driver funguje univerzálně.

## Uživatelské rozhraní

- PHP, Apache, databáze, Selenium a Porty používají jednotné záložky a stejné odsazení.
- Selecty včetně rozbalených položek používají konzistentní tmavý vzhled.
- Potvrzení smazání, odebrání knihovny či projektu a ukončení Selenium relace používají vlastní tmavý dialog s bezpečnou výchozí volbou Zrušit.
- Kořen release zůstává přehledný: hlavní EXE je snadno dohledatelné a aplikační závislosti jsou organizované podle publish pravidel.

## Composer, Python a terminál

- Instalace a odebírání Composer a Python knihoven zobrazuje společný průběh operace a čitelný konečný stav.
- Portable terminál podporuje interní `mkdir` bez spuštění `cmd.exe` nebo PowerShellu.
- `help` a `help <command>` se generují ze skutečně povolených příkazů, takže nápověda odpovídá implementaci.

## Ověření

- Self-contained Windows x64 build nevyžaduje nainstalovaný .NET runtime.
- Katalogové archivy a výsledné binárky se přijmou pouze při shodě připnutého SHA-256.
- Release build a 109 automatických testů prošly bez varování.
- Selenium 4.47.0 bylo reálně spuštěno s vlastním profile-node rozšířením a ověřeným per-session profile lifecycle.

Upozornění: vlastní `PortableDeveloper.exe` zatím není digitálně podepsaný. Windows Smart App Control nebo SmartScreen jej proto mohou blokovat. Projekt nedoporučuje vypínat ochranu Windows; veřejné podepisování přes SignPath Foundation je nadále připravené.

## Code signing policy

Pravidla, odpovědné role a ověřitelný původ sestavení popisuje veřejná [Code signing policy](https://github.com/hybernia1/portable-developer/blob/main/docs/CODE_SIGNING_POLICY.md). **Free code signing provided by [SignPath.io](https://signpath.io/), certificate by [SignPath Foundation](https://signpath.org/)** se bude vztahovat až na vydání, která budou výslovně označena jako podepsaná.
