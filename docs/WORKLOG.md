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

## 2026-08-22 — Automatická MariaDB a správa databází

- Ruční příprava MariaDB byla nahrazena automatickým bootstrapem při prvním spuštění; nová instance používá lokální `root` bez hesla, odstraní pouze čerstvě vygenerované schéma `test` a automaticky vytvoří `portable_dev`.
- Přidán samostatný MariaDB lifecycle controller s transientním `my.ini`, vazbou na `127.0.0.1:3307`, TCP health checkem a normálním shutdownem přes přibalený `mariadb-admin.exe`.
- Databázová stránka zobrazuje připojení, výchozí databázi, uživatelská schémata a orientační velikost dat plus indexů. Nové názvy přijímá jen v bezpečném ASCII formátu a databáze vytváří s `utf8mb4`.
- Automatické testy byly rozšířeny na 34/34: prázdné heslo, SQL validaci, bezpečné odstranění čerstvého schématu `test`, parsování přehledu a localhost-only konfiguraci serveru.
- Na čistém self-contained výstupu `PortableDeveloper-offline-win-x64-database-ready` proběhla skutečná inicializace MariaDB 12.3.2, ověření připojení bez hesla a existence `portable_dev`. Vizuální kontrola databázové stránky prošla a zavření aplikace provedlo normální shutdown; port 3307 poté nebyl dostupný.

## 2026-08-22 — Volitelné root heslo a phpMyAdmin

- Databázová stránka dostala dvojité heslové pole pro nastavení či změnu root hesla; výchozí nová instance zůstává bez hesla.
- MariaDB klientské operace používají krátkodobý defaults soubor, změna hesla jde přes standardní vstup a log obsahuje pouze informaci, že heslo bylo nastaveno.
- Offline balíček obsahuje phpMyAdmin 5.2.3 bez setup adresáře a vlastní konfigurace ze zdrojového prostředí. Runtime konfigurace používá cookie login, lokální MariaDB port a náhodný secret instance bez databázového hesla.
- Reálný smoke test MariaDB 12.3.2 ověřil nastavení hesla, přihlášení novým heslem, návrat na prázdné heslo a normální shutdown.
- Reálný Apache/PHP test odhalil dvě Windows FastCGI odlišnosti: backend potřebuje koncové lomítko a `SCRIPT_FILENAME` nesmí začínat `/C:/`. Po opravě vrací `/phpmyadmin/` HTTP 200 a přihlašovací HTML.
- Release build má 37/37 úspěšných automatických testů; nové testy kontrolují SQL předaný přes stdin, nepřítomnost hesla v argumentech a logu, aktualizaci portable state a bezpečnou phpMyAdmin konfiguraci.
- Čistý výstup `PortableDeveloper-offline-win-x64-phpmyadmin-ready-v2` neobsahuje runtime data. ZIP má 339,4 MiB a SHA-256 `f0d2309bc0c9b0a8f7ff23bcb2efe567015723be06d5949ae636fe1bfd543075`.

## 2026-08-22 — Portable Selenium Grid a správa relací

- Přidán lifecycle controller pro Selenium Standalone Grid: používá pouze ověřený Selenium JAR, přibalené JRE, localhost port, readiness endpoint a vlastněný procesní strom.
- Nastavení portu, maximálního počtu relací a limitu neaktivity má validované výchozí hodnoty a atomické portable uložení v `state/selenium-settings.json`.
- Offline release nově ověřuje a přibaluje geckodriver 0.37.1 pro Windows x64; jeho EXE se při načtení kontroluje podle `drivers/bundled/drivers.json`.
- Inventář načítá i uživatelem vložené Firefox, Chrome a Edge drivery z `drivers/custom/`, ignoruje reparse points a do transientního TOML zapisuje explicitní cesty. Selenium Manager a automatické stahování jsou vypnuté.
- Detailní stránka Selenium dostala karty nastavení a běžících relací, proklik do Hubu, ruční obnovení a potvrzované ukončení relace přes standardní WebDriver endpoint.
- Reálný self-contained smoke test ověřil start Gridu 4.47.0, stav `ready`, dva Firefox sloty s explicitním geckodriverem, GraphQL přehled bez relací a korektní stop bez zbylého Java procesu nebo portu 4444.
- Kontrola prvního release ZIPu odhalila staré access/error logy ve zdrojovém Apache stromu z Laragonu; balicí skript je nyní po kopírování explicitně odstraňuje společně s případným PID souborem.
- Release build i kontrola formátování prošly bez varování; automatické testy jsou zelené 49/49.
- Čistý výstup `PortableDeveloper-offline-win-x64-selenium-ready-v2` neobsahuje runtime data, Apache provozní logy ani lokální build cesty. ZIP má 340,6 MiB a SHA-256 `22d4e75eb00d297dcb752ce31ef1679aac7d056e709249d9acf26d94c0581991`.

## 2026-08-22 — Composer a Python knihovny

- Přidán ověřovaný inventář portable nástrojů a samostatné aplikační služby pro Composer a Python. Příkazy používají explicitní argumenty bez shellu, portable pracovní adresář, timeout, přesměrovaný výstup a ukončení procesního stromu.
- Composer stránka zobrazuje nainstalované i přímé závislosti projektu `instances/default/www`, podporuje validovaný název a omezení verze, potvrzované odebrání a příklad `php-webdriver/webdriver`. Pluginy a instalační skripty jsou pro UI operace vypnuté.
- Python stránka spravuje knihovny pod `instances/default/python/packages` pomocí `pip --target`; systémový profil, globální pip konfigurace a základní runtime se nepoužívají. Příkladem pro Selenium je balíček `selenium`.
- Offline release používá Composer 2.10.2 s oficiálním SHA-256 místo zranitelné řady 2.9.x a přibaluje Python 3.13.0. Zdrojové `Scripts` a `site-packages` se nekopírují, pip 24.2 se připraví offline přes `ensurepip`.
- Balicí smoke test ověřil verze, metadata nástrojů a čistý Python obsahující pouze pip. První pokus odhalil dlouhou cestu v lokálních `site-packages`; selektivní kopírování tuto závislost na build profilu odstranilo.
- Vizuální kontrola stránek Composer a Python ověřila rozložení, prázdné stavy a deaktivované akce při chybějícím runtime. Kontrola zároveň odhalila a opravila sdílenou spodní hlášku, aby se stav obou správců balíčků nepřepisoval.
- Release build a 56/56 automatických testů prošly bez varování; testy pokrývají integritu nástroje, bezpečné argumenty, parsování přehledu a portable cesty obou správců.
- Kontrola nedotčeného release odhalila Laragon `php.ini` s cestou `E:\laragon` a nepotřebné aplikační i MariaDB `.pdb`; balení je nyní odstraňuje a PHP za běhu dál používá pouze generovanou portable konfiguraci.
- Reálný self-contained výstup ověřil všechny serverové moduly, automatickou databázi `portable_dev`, Composer 2.10.2, Python 3.13.0 a korektní shutdown bez zbylých procesů či portů. Instalační akce balíčků nebyly při vizuální kontrole spuštěny, aby test nestahoval cizí kód.
- Nedotčený výstup `PortableDeveloper-offline-win-x64-composer-python-final` má 929,2 MiB, neobsahuje runtime data, PDB ani lokální textové cesty. ZIP má 347,0 MiB a SHA-256 `ff71c717e38f58e48cf54cbad1f18a3572d20649fec9759d646af9e154c15b68`.

## 2026-08-22 — Bezpečný editor PHP nastavení

- PHP stránka dostala skutečný editor `memory_limit`, upload/POST limitu, `max_execution_time`, `max_input_vars`, zobrazení vývojových chyb a přibalených rozšíření.
- Nastavení se validuje a atomicky ukládá do `instances/default/config/php-settings.json`; runtime `php.ini` se z něj při každém startu znovu vytvoří pod `temp/generated/` s aktuálními portable cestami.
- Rozšíření používají pevný allowlist a generátor ověřuje existenci odpovídající DLL. `mbstring`, `mysqli`, `openssl` a `zip` zůstávají povinné; výchozí profil přidává `curl`, `fileinfo`, `gd`, `intl` a `pdo_mysql`.
- Vizuální kontrola potvrdila, že celý editor je čitelný bez skrytých ovladačů a stavový řádek zůstává viditelný. Test neplatného rozsahu odhalil technickou anglickou výjimku; UI ji nyní nahrazuje lokalizovaným přehledem povolených hodnot.
- Reálný self-contained smoke test uložil `memory_limit = 384M`, zapnul `sockets`, spustil Apache/PHP a přes přibalené PHP 8.4.12 ověřil hodnoty i načtení `sockets`, `mysqli`, `curl` a `intl`. Následný stop nezanechal procesy ani porty 8080, 9000, 3307 či 4444.
- Release build prošel bez varování, formátování je čisté a automatické testy jsou zelené 61/61.
- Čistý výstup `PortableDeveloper-offline-win-x64-php-settings-final` neobsahuje runtime data, PDB ani lokální textové cesty. ZIP má 347,1 MiB a SHA-256 `ad4408cec8824302a675c6670129e38d1b89478fc7af89d8c43f86737273607b`.
