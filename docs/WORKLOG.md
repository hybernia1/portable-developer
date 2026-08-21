# Pracovní záznam

Tento soubor je stručný chronologický deník významné práce. Není náhradou detailní historie Git commitů.

## 2026-08-21 — Založení projektového rámce

- Vytvořena dokumentace pro přenosnou Windows aplikaci spravující lokální servery.
- Přijaty architektonické hranice: C# / .NET 8 / WPF, self-contained distribuce, žádné Windows služby ani Docker.
- Zavedena pravidla pro změny, rozhodnutí, changelog a commity.
- Zjištěno: pracovní prostředí má .NET runtime, ale nemá .NET SDK; založení a ověření WPF řešení čeká na SDK.

## 2026-08-21 — Procesní jádro a WPF kostra

- Ověřeno a připnuto .NET SDK 10.0.400.
- Založeno řešení `PortableDeveloper.slnx` s projekty App, Domain, Application, Infrastructure a Tests.
- Přidán resolver cest chránící před opuštěním kořene aplikace a základní supervisor podřízených procesů.
- Přidán počáteční WPF dashboard se stavy připravených modulů.
- Přidány testy portable resolveru; build a test jsou součástí následujícího ověřovacího kroku.
- První build odhalil kolizi názvu projektu `Application` s WPF třídou `Application`; opraveno explicitním odkazem na `System.Windows.Application`.

## 2026-08-21 — Runtime logování a health check

- Přidán JSON Lines logger ukládající UTC události do `logs/` uvnitř kořene aplikace.
- Supervisor nyní zachycuje standardní výstup i chybový výstup podřízeného procesu a zapisuje události startu, ukončení a selhání.
- Přidán TCP health check s konfigurovatelným timeoutem.
- Přidány testy logování a pozitivního i negativního stavu TCP portu.

## 2026-08-21 — Inventář ručně vložených modulů

- Definován normalizovaný layout `modules/<druh>/<verze>/` a dokumentace pro ruční vložení komponent.
- Přidán bezpečný file inventory, který ignoruje junctions a symbolické odkazy a vybírá nejvyšší číselnou verzi.
- Dashboard nyní zobrazuje skutečně nalezené moduly a lze jej obnovit bez restartu aplikace.
- Detekovaný modul nelze zatím spustit: před spuštěním musí M4 přidat katalog a ověření SHA-256.

## 2026-08-21 — Generování Apache/PHP konfigurace

- Přidána relativní konfigurace instance Apache/PHP a generátor runtime souborů do `temp/generated/`.
- Apache konfigurace používá pouze `127.0.0.1`, neprivilegovaný port a FastCGI backend PHP na vlastním lokálním portu.
- PHP logy, session a dočasná data jsou směrované pod kořen portable instance.
- Přidán test, že konfigurace vzniká pouze uvnitř kořene a odmítá cestu opouštějící aplikaci.

## 2026-08-21 — Katalog a ověřená instalace modulů

- Přidán lokální JSON katalog s validací schématu, HTTPS zdrojů, cest a SHA-256.
- Přidán instalační postup: download do `downloads/`, hash kontrola, bezpečný ZIP staging a normalizovaný přesun do `modules/`.
- Archiv s nesprávným hashem, traversal cestami nebo symbolickými odkazy se odmítne.
- Přidány integrační testy simulovaného HTTP stažení; katalog je zatím prázdný, dokud nejsou schválené konkrétní zdroje a hashe.
- Při izolovaném buildu vznikly dočasné `artifacts/` podsložky ve zdrojových projektech; přidáno globální vyloučení `artifacts/**` z MSBuild itemů, aby se build výstupy nikdy nekompilovaly jako zdrojový kód.

## 2026-08-21 — První instalovatelný modul v dashboardu

- Do lokálního katalogu přidáno oficiální PHP 8.5.9 NTS x64 včetně publikovaného SHA-256.
- Dashboard zobrazuje katalogové balíčky a instaluje je přes ověřený instalační proces.
- Ověřen Release build WPF aplikace a testovací projekt.
- Zaznamenáno omezení: PHP pro Windows vyžaduje Visual C++ runtime; portable app-local řešení bude předcházet spuštění PHP.

## 2026-08-21 — Kontrola app-local PHP runtime

- Přidán preflight pro `php-cgi.exe`, `vcruntime140.dll` a `vcruntime140_1.dll` uvnitř nainstalovaného PHP modulu.
- Dashboard zobrazuje konkrétní chybějící soubory a stav `Čeká na runtime`; kontrola nic nestahuje ani nemění ve Windows.
- Přidány automatické testy úplného i neúplného app-local runtime a ADR-008 s pravidly pro budoucí hashovaný runtime balíček.

## 2026-08-21 — Portable self-contained výstup

- Přidán PowerShell skript pro publikaci WPF aplikace jako self-contained `win-x64` do `artifacts/publish/`.
- Výstup zůstává složkový, aby vedle EXE nesl katalog i vlastní portable data a šel beze změny zkopírovat na USB.

## 2026-08-21 — Lokalizace dashboardu

- Přidán přepínač češtiny a angličtiny v UI; přepíná viditelné stavy, popisy a instalační akce bez restartu.
- Jazyk se ukládá a čte z `state/settings.json` uvnitř portable kořene; chybějící či neplatné nastavení vrací výchozí češtinu.
- Přidány testy portable úložiště nastavení a ADR-009.

## 2026-08-21 — Apache/PHP FastCGI lifecycle

- Přidán samostatný stack controller: připravuje konfiguraci, startuje PHP před Apache, ověřuje lokální TCP porty a při selhání provede rollback.
- Start vyžaduje ověřený záznam instalace shodný s přibaleným katalogem a kompletní PHP app-local runtime; ručně vložené moduly se nespouštějí.
- Dashboard obsahuje start/stop ovládání a při zavření aplikace bezpečně ukončí spravované podprocesy.

## 2026-08-21 — Ověřený Apache katalog

- Apache Lounge httpd 2.4.68 VS18 x64 přidán do lokálního katalogu; SHA-256 souhlasí s Microsoft WinGet manifestem i lokálně staženým ZIPem.
- Ověřen layout `Apache24/bin/httpd.exe`, přiložené licence a absence Visual C++ runtime v archivu.
- Přidán Apache app-local runtime preflight pro `bin/vcruntime140.dll`; bez něj dashboard i controller start bezpečně blokují.
- Instalační kontrola již nepovažuje ručně vytvořenou složku se správně pojmenovaným EXE za ověřenou instalaci.

## 2026-08-21 — Portable import Visual C++ runtime

- Dashboard dostal výběr zdrojové složky a import app-local runtime DLL k nainstalovanému Apache a PHP.
- Importér ověřuje WinTrust, Microsoft signer, x64 PE a minimální verzi 14.50, pracuje přes staging a ukládá SHA-256 metadata bez absolutní zdrojové cesty.
- Přidány testy úspěšného importu, odmítnutí nedůvěryhodného vstupu a integrační ověření Authenticode na Windows Microsoft DLL.
- Integrační test odhalil chybný první návrh WinTrust marshallingu; P/Invoke byl nahrazen explicitními unmanaged strukturami a následně test úspěšně prošel.

## 2026-08-21 — Oprava startu WPF UI

- V publikované aplikaci bylo diagnostikováno zablokované UI vlákno při synchronním čekání na asynchronní zápis startovního logu.
- Interní awaity JSONL loggeru již nezachycují WPF synchronizační kontext, takže start i ukončení mohou log bezpečně dokončit.
- Přidán regresní test simulující synchronní volání loggeru na vlákně se synchronizačním kontextem.

## 2026-08-22 — Katalog a inicializace MariaDB

- Z REST API MariaDB Foundation vybrán oficiální Windows x64 ZIP 12.3.2; publikovaný SHA-256 byl potvrzen nad lokálně staženým archivem.
- Skutečný `mariadb-install-db.exe` byl ověřen pomocí `--help` a smoke testu s vlastním datovým adresářem, portem, heslem a konfigurační šablonou; nebyla vytvořena Windows služba.
- Přidán sdílený verifier katalogových instalací a obecný runner krátkých portable příkazů s timeoutem a přesměrovaným výstupem.
- Inicializace pracuje ve staging složce, zachová existující data, odstraňuje absolutní `my.ini` a ukládá náhodné root heslo pouze do state složky instance.
- Dashboard dostal lokalizovanou akci **Inicializovat MariaDB**; automatické testy pokrývají ověření modulu, úspěch, rollback a ochranu existujících dat.
- Vizuální kontrola publikovaného WPF buildu odhalila oříznutou třetí položku katalogu; katalog proto dostal vlastní svislé rolování.

## 2026-08-22 — Přechod na kompletní offline distribuci

- Odstraněn download katalog, instalační workflow a tlačítko pro import Visual C++ runtime.
- Přidán offline release skript, který z `E:\laragon\bin` přebírá Apache 2.4.66, PHP 8.4.12, Microsoft OpenJDK 25.0.3 a Composer 2.9.4; MariaDB 12.3.2 a Selenium 4.47.0 přidává z ověřené lokální cache.
- App-local VC++ DLL se při balení ověřují pomocí Authenticode, Microsoft signeru a minimální verze 14.50; runtime metadata obsahují SHA-256, nikoli absolutní zdrojovou cestu.
- Katalog byl změněn z archivních hashů na SHA-256 skutečných vstupních souborů a verifier kontroluje obsah při zobrazení i před řízeným spuštěním.
- Dashboard zobrazuje všechny čtyři serverové moduly jako **Připraveno** a už nevyžaduje žádnou uživatelskou přípravu runtime.
- Vytvořen a ověřen výstup `artifacts/publish/PortableDeveloper-offline-win-x64/` o velikosti přibližně 844,5 MiB; přímo z něj prošly verze Apache, PHP, MariaDB, Javy, Selenium a Composeru.
- Release build: 29/29 automatických testů úspěšných a WPF dashboard vizuálně ověřen v češtině.
- End-to-end UI test odhalil, že Apache 2.4.66 má Windows MPM staticky vestavěné a neobsahuje `modules/mod_mpm_winnt.so`; generátor byl opraven a dostal regresní kontrolu.
- Opravená `PortableDeveloper-offline-win-x64-v2` sestava následně přes UI úspěšně spustila Apache/PHP, prošla TCP health checkem na portu 8080 a oba procesy korektně zastavila.
- Čistý distribuční výstup `PortableDeveloper-offline-win-x64-ready` neobsahuje runtime data ani lokální build cesty; ZIP má 328,3 MiB a SHA-256 `33fb96af52afa69cc1b143b969ca6201ad45a12ad18d0c638781c7b648c88077`.

## 2026-08-22 — První UX revize dashboardu

- Dvojice tlačítek Start/Stop byla nahrazena jedním stavovým ovladačem, který mění popisek, barvu i dostupnost podle životního cyklu Apache/PHP.
- Odstraněno ruční obnovení neměnného offline inventáře; technický kořen aplikace byl přesunut do sbalených informací.
- Karty Apache/PHP nyní zobrazují skutečný stav procesu a porty místo zavádějícího obecného „Připraveno“.
- Akce přípravy MariaDB byla přesunuta přímo do její karty. Stav dat se kontroluje při startu, během operace i po jejím dokončení; po úspěchu tlačítko zmizí.
- Selenium je označené jako přibalené, ale bez zapojeného ovládání, aby UI neslibovalo neexistující funkci.
- Přidán vlastní styl tlačítek, který zachovává čitelnost během hoveru, startu a deaktivovaného stavu.
- Vizuálně a funkčně ověřeno: start i stop webového stacku jedním ovladačem a skutečná příprava MariaDB v samostatném UX buildu.
- Finální publish s delším názvem odhalil limit 260 znaků v `Expand-Archive`; MariaDB staging byl přesunut do ověřené krátké dočasné cesty s GUID a bezpečnostní kontrolou cíle před úklidem.

## 2026-08-22 — Navigace a detailní stránky služeb

- Přidána boční navigace s položkami Přehled, PHP, Apache, Databáze, Selenium a Nastavení.
- Všechny stránky používají jeden view model; provozní stav a ovládání Apache/PHP se okamžitě synchronizují napříč přehledem i detailními stránkami.
- PHP a Apache zobrazují aktuální verzi, port a umístění generované konfigurace; budoucí bezpečné editory jsou označené jako plánované.
- Databázová stránka obsahuje přípravu MariaDB, host `127.0.0.1`, port `3307` a lokální účet `root`. Heslo zůstává mimo dashboard a logy.
- Selenium a Nastavení dostaly vlastní stránky s aktuálním stavem, porty a portable kořenem aplikace.
- Vizuální kontrola prošla stránkami Přehled, PHP, Databáze a Nastavení; následně byl opraven oříznutý název sidebaru a rozložení čtyř dashboard karet.
