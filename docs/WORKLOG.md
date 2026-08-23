# Pracovní záznam

Tento soubor je stručný chronologický deník významné práce. Není náhradou detailní historie Git commitů.

## 2026-08-23 — 0.9.0 release hardening

- A live Selenium GraphQL response exposed `sessionDurationMillis` as a string and crashed the Sessions page. The parser now accepts both numeric representations and safely defaults malformed optional values; two regression tests reproduce the vendor response and unexpected schema types.
- All supervised Apache, PHP, MariaDB, and Selenium processes are now assigned to Windows Job Objects with kill-on-close semantics. A real child-process integration test verifies successful job assignment and complete tree termination.
- Portable JSONL logging now rotates 10 MiB segments, retains 14 days, and enforces a 100 MiB total budget. The verified runtime package cache uses a 512 MiB LRU budget and removes stale partial downloads after package operations.
- The release documentation and GitHub release notes for 0.9.0 are written in English. Canonical project documentation will be converted to English before the 1.0 release.
- The updated Release test suite passes 135/135 tests before final publish verification.

## 2026-08-23 — Hotfix cookie vaultu, maximalizace a scrollbarů

- Živě reprodukované odebrání cookie vaultu selhalo, protože servis pouze odstranil read-only atribut `vault.json`, ale samotný soubor před nerekurzivním odstraněním adresáře nesmazal. Oprava nejprve validuje všechny položky, odmítne odkaz či podsložku bez částečné změny a teprve potom odstraní běžné soubory a prázdný vault adresář.
- Dva regresní testy pokrývají úspěšné odebrání read-only vaultu i bezpečné odmítnutí neočekávané podsložky se zachováním původního šifrovaného souboru.
- Sdílený `WindowChrome` nyní zpracovává `WM_GETMINMAXINFO` podle work area nejbližšího monitoru. Vizuální smoke na 1920×1080 ověřil maximalizované okno 1920×1032, které končí nad 48px Windows taskbarem.
- App-wide tmavá šablona scrollbarů pokrývá vertikální i horizontální orientaci, skryté page oblasti a hover jezdce. Živá kontrola normálního okna ověřila levé menu bez světlého nativního pásu a systémových šipek.
- Aktualizovaný self-contained hotfix byl nasazen do odděleného `E:\PortableDeveloper-win-x64-0.9.0-test-20260823`; existující profily, cookie vault a moduly nebyly při nasazení změněné. Release build a formátování prošly bez varování a automatická sada skončila 130/130 úspěšnými testy.

## 2026-08-23 — Pád při odebírání Selenium master profilu

- Živý test sestavení 0.9.0 na `E:\` reprodukoval úplné ukončení aplikace před samotným mazáním profilu. JSONL log určil příčinu v lazy BAML načtení potvrzovacího dialogu: WPF nedokázalo vyhodnotit `Setter.Property` odvozeného window stylu.
- Konfigurace `WindowChrome` je nyní aplikována z jednoho C# helperu a oba app dialogy mají vlastní základní vlastnosti okna přímo v XAML; nepoužívaný odvozený styl byl odstraněn. `ConfirmationDialog.Show` navíc při případné budoucí XAML chybě selže bezpečně, zaloguje diagnostiku a neprovede požadovanou změnu.
- První nasazený hotfix ověřil fail-closed ochranu: aplikace nespadla a profil zůstal zachovaný. Druhý hotfix v témže odděleném testovacím kořeni otevřel správně lokalizovaný tmavý potvrzovací dialog a ponechal aplikaci stabilně spuštěnou.
- Po explicitním potvrzení byl profil `apokalypsa` přes opravené UI skutečně odebrán. Karta se okamžitě obnovila, profilová složka zanikla, druhý master i cookie vault zůstaly zachované a běžící proces aplikace pokračoval bez nové neošetřené výjimky.
- Po původním tvrdém pádu zůstaly osiřelé procesy Apache/PHP/Selenium z přesného testovacího kořene. Byly ukončeny pouze podle ověřené absolutní cesty; vydání 0.8.0 ani procesy mimo testovací složku nebyly zasažené. Samostatně je potřeba prověřit garantovaný úklid podřízených procesů při fatálním pádu a dokončení procesu po zavření posledního okna.

## 2026-08-23 — Projektové Selenium downloady a plynulé zapečetění profilu

- Každý Default i nově vytvořený webový projekt automaticky dostává trvalou složku `seldownloads`. Složka je viditelná ve správci souborů, není vázaná na účet nebo relaci a Apache k ní generuje `Require all denied` i u legacy Default document rootu.
- Přibylo portable nastavení „Povolit stahování souborů“, ve výchozím stavu vypnuté. Při startu Selenium se bezpečně odvodí relativní cesta aktivního projektu; projekt za běhu serveru nelze přepnout.
- Vlastní Java Node přepisuje download preferences Chrome/Edge/Firefox. Chromium navíc dostává CDP policy `allow` s jediným projektovým adresářem nebo `deny`; pozdější klientský pokus o změnu `setDownloadBehavior` Node odmítne.
- Zakládání master profilu už nežádá libovolnou webovou URL a otevírá správu browser účtu. Po zavření browseru probíhá kopírování, normalizace, manifest a úklid přes `Task.Run`, zatímco UI zůstává responzivní a ukazuje čekání, zpracování a dokončení. Diagnostický log měří pouze časy browseru, zapečetění a úklidu, nikoli název či obsah profilu.
- Pracovní kopie Chromium mají `--disable-sync`; Firefox dostane vypnutí Firefox Account Sync pouze do dočasné kopie. Read-only master zůstává nezměněný.
- Reálný Chrome for Testing 152 + Selenium 4.47.0 smoke na odděleném portu ověřil povolený download do projektového `seldownloads`, zachování souboru po DELETE relace, úplné zablokování při vypnutém nastavení a HTTP 403 při klientském pokusu změnit `Browser.setDownloadBehavior`.
- Samostatný nepodepsaný testovací build byl nasazen do `E:\PortableDeveloper-win-x64-0.9.0-test-20260823` bez přepsání 0.8.0. Převzal aktuální portable moduly, projektová data, jeden master profil a jeden cookie vault z vývojového Release runtime; staré logy, cache stahování a `temp` se nepřenášely. EXE 0.9.0 se po startu udrželo spuštěné.
- WPF Debug build, přímá kompilace Java Node proti Selenium Serveru 4.47.0 a všech 128 automatických testů prošly bez chyby.

## 2026-08-23 — Šifrovaný Selenium cookie vault

- Přibyl samostatný cookie vault nezávislý na browser master profilech. Běžný JSON export se zpracuje pouze v paměti, normalizuje na WebDriver pole a vyřadí prošlé, neplatné a duplicitní cookies i metadata rozšíření.
- Payload chrání AES-256-GCM; aplikace automaticky vytvoří 256bitový klíč pod portable `state/`. Klíč, názvy ani hodnoty cookies se nelogují a UI nevyžaduje heslo ani ruční odemykání. Složky `profiles/` a `state/` jsou mimo Git.
- UI nabízí samostatnou kartu vaultů, import souboru a potvrzované odstranění. Živá kontrola odhalila, že import četl WPF textbox z pracovního vlákna; název se nyní zachytí na UI vlákně před spuštěním práce.
- Capability `portable:vault=<id>` funguje i bez master profilu. Vlastní Selenium Node před vrácením nové relace navštíví jednotlivé domény, vloží normalizované cookies a při chybě relaci uzavře.
- Java Node načítá šifrovanou obálku i portable klíč a payload rozšifruje pouze v paměti při vytváření relace. Čitelný dočasný soubor nevzniká; Java zdroj byl přeložen proti přesnému Selenium Serveru 4.47.0.
- Izolovaný end-to-end test původní integrace nad Chrome for Testing 152 a Selenium 4.47.0 vytvořil čistou headless relaci pouze s `portable:vault`, nalezl vloženou neškodnou cookie pro `example.com` a relaci korektně odstranil. Nová bezheslová varianta je krytá automatickými kryptografickými testy a opakovanou kompilací Java Node.
- Release sestavení, formátování a validace katalogu prošly bez chyb; automatická sada skončila 128/128 úspěšnými testy.

## 2026-08-23 — Spravované Selenium browsery pro 0.9.0

- Selenium přestalo skenovat systémový Edge, Chrome a Firefox i jejich uživatelské profily. Z instalačního UI zmizely samostatné drivery a vlastní `drivers/custom`; připravené prostředí nyní vzniká pouze z přesné katalogové dvojice.
- Do lock katalogu přibyl oficiální Mozilla Firefox 142.0 pro Windows x64 a geckodriver 0.37.1 jako jeden logický balíček. Firefox instalátor má připnutý SHA-256 i očekávaného vydavatele, používá pouze oficiální `/ExtractDir` a výsledný `firefox.exe` se znovu ověří hashem.
- Spravovaný Firefox dostává `distribution/policies.json` s vypnutou vlastní aktualizací, default-browser agentem, studiemi a telemetrií. Grid i enrollment mají vypnuté Mozilla crash reporty a nepoužívají systémový `PATH`.
- Profilový formulář už neumí vybrat hostitelskou složku. Vytvoří nový profil pouze pod `temp/selenium-profile-creation`, dovolí bezpečnou HTTP/HTTPS startovní adresu a po zavření browseru jej zapečetí jako read-only master s manifestem.
- Servisní rozhraní profilového úložiště přijme pouze relativní draft z vyhrazeného portable prostoru; libovolnou absolutní nebo hostitelskou cestu odmítne ještě před kopírováním.
- Při technickém ověření Mozilla EXE došlo kvůli ztracenému PowerShell argumentu k nechtěné instalaci Firefoxu 142 do `Program Files`. Instalace nebyla spuštěna, byla ihned odstraněna podepsaným Mozilla odinstalátorem a následná kontrola nepotvrdila programovou složku, uninstall záznam, službu ani zástupce. Samotný `/ExtractDir` byl poté ověřen v izolované dočasné cestě.

## 2026-08-23 — Opravy UI a importu browser profilů pro 0.8.1

- Živá kontrola vydání 0.8.0 reprodukovala nefunkční maximalizaci: `HTMAXBUTTON` zachoval Windows Snap Layout, ale non-client kliknutí nedošlo do běžného WPF handleru. Sdílená lišta nyní bezpečně obslouží down/up zprávu a přepne stav jen jednou.
- Profilové akce byly oddělené od `SeleniumSettingsEnabled`; import, vytvoření a odebrání masteru mohou proběhnout i při běžícím Selenium serveru.
- Přidán čtecí inventář standardních místních Edge, Chrome a Firefox profilů. Chromium `Local State` poskytne pouze zobrazovaný název, účetní identifikátory se nečtou do UI a inventář se inicializuje jen pro nainstalovaný Selenium modul.
- Import a čistý master mají oddělené formuláře. UI předvyplní nalezený profil i název, rozpozná běžící browser a místo šedého tlačítka zobrazí konkrétní pokyn k jeho zavření.
- Debug build používá jiný single-instance identifikátor než veřejný Release build. Izolovaný smoke vedle běžící verze na `E:\` ověřil maximalizaci/obnovení i nalezení Edge profilu bez spuštění importu uživatelských dat.
- Finální Release build prošel s nulou chyb a varování, kontrola formátování byla čistá a automatická sada skončila 124/124 úspěšnými testy. Veřejná verze 0.8.0 spuštěná z `E:\` zůstala nedotčená.
- Živá kontrola po zavření Chrome našla profily `Nikola (Default)` a `Nicole (Profile 1)`. Současně odhalila technický `System Profile`; inventář byl zpřesněn na browserem vytvářené uživatelské adresáře `Default` a `Profile N` a regresní test pokrývá i vyloučení `Guest Profile`.
- Pro skutečný test přihlášeného Edge profilu vznikl standard-library Python scénář. Záměrně nečte obsah schránky ani nevypisuje cookies, účty nebo úplné URL; ověřuje pouze WebDriver spojení, cílový hostname, přihlášený stav a bezpečný vznik i zánik pracovní kopie.
- První reálné ověření odhalilo, že Python `pathlib` bez extended-length prefixu považoval dlouhé Chromium CacheStorage cesty za chybějící, zatímco C# i Java manifest správně ověřily. Smoke validátor proto pro profilová data používá `\\?\` cesty a zůstává použitelný i z hlubšího portable kořene.
- End-to-end Edge 151 test ověřil master o 542 souborech / 34 238 528 bajtech, vznik pracovní kopie, kompatibilní EdgeDriver, standardní DELETE, nulový pozůstatek kopie a nezměněný master. Přihlášení na Seznam se nepřeneslo: všech šest platných cílových cookies mělo App-Bound `v20` formát a Chromium s nestandardním `--user-data-dir` skončilo na `login.seznam.cz` v headless i headed režimu. Grid, Edge i testovací aplikace byly ukončené; port 4444 zůstal volný.

## 2026-08-22 — Implementace optimalizačního plánu 0.8.0

- Sjednocen implicitní tmavý styl selectů, dynamická navigace a vlastní `WindowChrome` horní lišta hlavního okna i aplikačních dialogů.
- Přidána jedna uživatelská instance s aktivační named pipe a bezpečné otevírání souborů přes asociace Windows s volitelným portable editorem.
- Selenium nově skládá browser prostředí z ověřené browser binárky a kompatibilního driveru; systémové browsery pouze detekuje.
- Do připnutého katalogu přidán Chrome for Testing 152.0.7977.54 a balíček se shodným ChromeDriverem.
- Master profily mají normalizované Chromium/Firefox rozložení, read-only data, limity, hashovaný manifest a kontrolu integrity před relací v C# i Java Node rozšíření.
- Přidáno vytvoření čistého masteru v portable `temp/`; aplikace po zavření browseru profil zapečetí a pracovní kopii bezpečně uklidí.
- Verze zvýšena na 0.8.0 a připravena k vydání přes tag `v0.8.0`, který sestaví veřejný self-contained ZIP a SHA-256 součet.

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

## 2026-08-22 — Verze 0.1.0 a kosmetické opravy

- Aplikace dostala explicitní assembly, file a informational verzi 0.1.0, zobrazení verze v sidebaru i nastavení a vlastní Windows ikonu.
- Composer přehled nyní bezpečně přijímá i kořenové `[]`, které může zůstat v `composer.json` po odebrání poslední přímé závislosti; regresní test pokrývá původní výjimku i argumenty příkazu `composer remove`.
- Publish omezuje satelitní prostředky na `en;cs`, takže vedle neutrální angličtiny nevytváří nepoužívané jazykové složky .NET/WPF.
- Vizuální smoke test ověřil ikonu v titulku, verzi v sidebaru i nastavení a pouze dvě volby jazyka: češtinu a angličtinu.
- Formátování i release build prošly bez varování a automatické testy jsou zelené 63/63.
- Čistý rozbalený výstup `PortableDeveloper-offline-win-x64-0.1.0-final` má verzi souboru 0.1.0.0, produktovou verzi 0.1.0, jen kořenovou jazykovou složku `cs` a neobsahuje runtime stav ani PDB soubory.
- Na přání vlastníka se pro tuto sérii kosmetických a bugfix změn nevytváří nový offline ZIP.

## 2026-08-22 — Portable editor a ruční PHP konfigurace

- Offline tooling nyní obsahuje hashově ověřený Notepad++ 8.9.2 v minimálním portable režimu. Balení z Laragonu přebírá jen editor, syntax data, českou lokalizaci a `doLocalConf.xml`; updater, pluginy, session, zálohy a uživatelská konfigurace se nekopírují.
- Přibyla stránka Nástroje se stavem a verzí editoru. Notepad++ lze spustit samostatně nebo jím z PHP stránky otevřít `instances/default/config/php-custom.ini`.
- Ruční PHP direktivy se po kontrole reparse pointu, nulových znaků a limitu 256 KiB připojují za generovaný `php.ini`. UI upozorňuje, že mohou přepsat formulářové hodnoty a projeví se až po restartu stacku.
- Spouštěcí služba používá ověřenou relativní cestu, `ArgumentList`, vlastní pracovní adresář a portable dočasné složky bez shellu. Česká aplikace předává Notepad++ přepínač `-Lcs`; angličtina zůstává jeho výchozí lokalizací.
- Automatické testy pokrývají inventář editoru, bezpečné spuštění, vytvoření vlastního INI, připojení override i odmítnutí příliš velkého souboru. Formátování, release build a 67/67 testů jsou zelené.
- Vizuální smoke test ověřil stránku Tools/Nástroje v obou jazycích a skutečné otevření `php-custom.ini` v českém Notepad++.
- Čistý rozbalený výstup `PortableDeveloper-offline-win-x64-editor-final` má 932,9 MiB, neobsahuje runtime data ani PDB a přidává pouze 11,2 MiB editoru se správnou verzí, hashem a metadaty. Nový ZIP se nevytvářel.

## 2026-08-22 — Portable Developer 0.2.0: terminál a soubory

- Přidány samostatné stránky Terminál a Soubory do české i anglické navigace. Verze assembly, souboru a produktu byla zvýšena na 0.2.0.
- Terminál používá vlastní parser bez `cmd.exe` a PowerShellu, odmítá roury, přesměrování a řetězení a spouští pouze explicitní přibalené PHP, Composer a Python s čistým portable `PATH`. Příkaz `service` sdílí stávající lifecycle controllery webového stacku, MariaDB a Selenium.
- Správce souborů je uzamčený na `instances/default/www`, podporuje vytvoření souboru a složky, přejmenování, potvrzované mazání a otevření v přibaleném Notepad++. Kořen projektu, únikové cesty a reparse pointy jsou blokované.
- Dokumentace otevřeně rozlišuje ochranu správce souborů od OS sandboxu: spuštěný důvěryhodný PHP nebo Python kód má běžná oprávnění uživatele.
- Release build prošel bez varování a automatické testy jsou zelené 73/73. Nový ZIP se podle současného release workflow nevytváří; vydává se čistá rozbalená složka.
- Vizuální smoke test lokálního buildu potvrdil verzi 0.2.0, kompletní navigaci, čitelné stránky Terminál a Soubory a viditelné upozornění na hranici Windows sandboxu. Chování parseru a souborových operací ověřují automatické testy.
- Čistý rozbalený výstup `PortableDeveloper-offline-win-x64-0.2.0-final` má 933,0 MiB, verzi souboru 0.2.0.0, produktovou verzi 0.2.0, neobsahuje PDB ani lokální build cesty a zachovává všechny připnuté serverové a nástrojové verze. Samostatný release ZIP se nevytvářel.

## 2026-08-22 — Portable Developer 0.2.1: přímá konzole a Double Commander

- Terminál byl převeden na jedinou konzolovou plochu: prompt, vstup i výstup jsou spolu, Enter příkaz spustí a šipky nahoru/dolů procházejí historii. Samostatné vstupní pole a tlačítko Spustit byly odstraněny.
- Vlastní CRUD správce souborů, jeho aplikační kontrakt, view model a destruktivní backend byly odstraněny. Interní bezpečné `ls` a `cd` zůstávají přímo v omezeném terminálovém servisu.
- Vybrán Double Commander 1.2.8 x64 pod licencí GPL-2.0. Oficiální portable archiv i `doublecmd.exe` mají připnuté SHA-256 a nástroj používá společný ověřovaný runtime inventář.
- Spouštěcí servis otevírá oba panely ve `instances/default/www`, posílá `TEMP`/`TMP` pod kořen aplikace, ukládá konfiguraci do `state/doublecmd` a F4 propojuje s aktuálním Notepad++ přes procesní `%PORTABLE_DEVELOPER_EDITOR%` bez trvalé absolutní cesty.
- UI otevřeně upozorňuje, že externí plnohodnotný správce může z výchozí složky přejít jinam a není sandboxem aplikace.
- Release build, kontrola PowerShell syntaxe i formátování prošly a automatické testy jsou zelené 72/72. Nový test ověřuje přesné argumenty panelů, portable config, lokalizaci a propojení editoru bez shellu.
- Vizuální smoke test publikované aplikace ověřil verzi 0.2.1, jedinou terminálovou konzoli s promptem a stránku Soubory se stavem Double Commanderu 1.2.8 i viditelným upozorněním. Externí správce nebyl při UI kontrole spuštěn; spouštěcí konfiguraci pokrývá izolovaný automatický test.
- Čistý rozbalený výstup `PortableDeveloper-offline-win-x64-0.2.1-final-clean` má 977,2 MiB, verzi souboru 0.2.1.0 a produktu 0.2.1, neobsahuje PDB, runtime data ani lokální build cesty. Double Commander EXE odpovídá připnutému SHA-256 a jeho distribuční strom obsahuje licenční soubory v `doc/`. ZIP se nevytvářel.

## 2026-08-22 — Portable Developer 0.2.2: čistý single-file root

- Hlavní WPF aplikace se nyní publikuje jako `PortableDeveloper.exe` se spravovanými .NET a projektovými knihovnami uvnitř bundle. Pět nativních WPF DLL zůstává vedle EXE a `IncludeNativeLibrariesForSelfExtract=false` zabraňuje runtime extrakci do `%TEMP%`.
- Verze assembly, souboru a produktu byla zvýšena na 0.2.2. Root namespace zůstal explicitně `PortableDeveloper.App`, takže přejmenování výstupu nemění existující typy ani XAML namespace.
- Balicí skript nově odstraňuje všechny zdrojové varianty `php.ini*`. Kontrola odhalila a vyloučila lokální Laragon zálohu s absolutní cestou; čistý výstup již neobsahuje cestu uživatelského profilu ani build zdroje v textové konfiguraci.
- MariaDB controller test používá dočasný volný port, takže nekoliduje se současně spuštěnou portable instancí na portu 3307. Automatické testy jsou zelené 72/72.
- Aplikační smoke publish vytvořil hlavní WPF okno, zůstal stabilně spuštěný a po požadavku na zavření skončil s exit code 0. Samostatné version checky s exit code 0 prošly pro Apache 2.4.66, PHP 8.4.12, MariaDB 12.3.2, OpenJDK 25.0.3, Selenium JAR, Composer 2.10.2 a Python 3.13.0.
- Čistý rozbalený výstup `PortableDeveloper-offline-win-x64-0.2.2-final-clean` má 970,7 MiB a 13 006 souborů. Jeho root obsahuje jen 11 položek: čtyři distribuční složky, manifest, `PortableDeveloper.exe` a pět nativních DLL. EXE má verzi souboru 0.2.2.0 a produktu 0.2.2; výstup neobsahuje PDB, runtime data, staré `PortableDeveloper.App*` soubory ani zdrojové `php.ini*`. ZIP se nevytvářel.

## 2026-08-22 — Portable Developer 0.2.3: retence, Composer a interní soubory

- Z `artifacts/publish` bylo po ověření přesných cílů trvale odstraněno 47 starých release položek a uvolněno 35,96 GiB. Nový `Cleanup-Releases.ps1` i běžný publish ponechávají dva nejnovější release adresáře a navíc chrání sestavu, ze které právě běží proces.
- Skutečný Composer příkaz odebral `php-webdriver/webdriver` správně včetně již nepotřebných nepřímých závislostí. Chyba vznikla až při následném načtení přehledu: Composer pro prázdný projekt vrátil kořenové `[]`, zatímco parser očekával objekt s vlastností `installed`. Parser nyní podporuje oba platné tvary a regresní test pokrývá prázdný výsledek po odebrání posledního balíčku.
- Double Commander, jeho spouštěcí servis, metadata, konfigurace a distribuční modul byly odstraněny. Stránka Soubory používá lehký interní správce omezený na `instances/default/www`, umí navigaci, vytvoření, přejmenování, potvrzené smazání a otevření souboru v Notepad++.
- Vizuální smoke test verze 0.2.3 ověřil českou stránku Soubory a skutečné vytvoření `index.php` v izolované testovací instanci. Současně odhalil nesrozumitelný technický stav po prázdném názvu; UI nyní používá lokalizovanou výzvu a po úspěšné operaci starou chybu vyčistí.
- Čistý rozbalený výstup `PortableDeveloper-offline-win-x64-0.2.3-final-clean` má 926,38 MiB, verzi souboru 0.2.3.0 a produktu 0.2.3. Kořen má 11 položek a balíček neobsahuje PDB, Double Commander, zdrojové `php.ini*` ani lokální cesty uživatelského profilu či build zdroje. ZIP se nevytvářel.

## 2026-08-22 — Portable Developer 0.3.0: nezávislé služby a jednotné UI

- Start/stop technického celku Apache/PHP je pouze na Přehledu. Běžící web lze restartovat z dashboardové karty i detailů Apache a PHP; uložení PHP konfigurace za běhu používá stejný stop/start postup a nové `php.ini` se tak uplatní ihned.
- MariaDB a Selenium se ovládají nezávisle. První bootstrap MariaDB nadále bezpečně vytvoří `portable_dev`, ale server po dokončení zastaví; další spuštění aplikace jej automaticky nezapíná.
- Dostupnost phpMyAdminu řídí explicitní `ServiceDependencyPolicy`. UI rozlišuje chybějící web, chybějící MariaDB nebo obě služby a odkaz nic skrytě nespouští. Čtyři nové regresní případy pokrývají všechny stavy závislostí.
- PHP, Apache, MariaDB a Selenium používají jeden vzhled záložek. Databázová stránka je rozdělena na Přehled, Webovou správu a Databáze; dlouhý svislý sled panelů byl odstraněn.
- Terminál nyní vyplňuje celou pracovní plochu bez horní informační lišty; `clear` a `cls` zůstávají součástí parseru. Správce souborů má jedinou integrovanou lištu, skutečnou historii Zpět, dialogy pro vytvoření a přejmenování, dvojklik a vlastní lokální sadu WPF vektorových ikon.
- Vizuální smoke test izolovaného self-contained buildu ověřil dashboard 0.3.0, databázové i PHP záložky, stav závislostí phpMyAdminu, konzoli přes celou stránku, správce souborů a dialog nového souboru. Dialog byl zrušen bez změny dat.
- Release build, formátování a 78/78 automatických testů prošly bez varování. Čistý offline výstup `PortableDeveloper-offline-win-x64-0.3.0-final-clean` má 926,39 MiB, verzi souboru 0.3.0.0 a produktu 0.3.0, pouze 11 položek v kořeni a neobsahuje runtime data, PDB, zdrojové `php.ini*` ani lokální build cesty. ZIP se nevytvářel.

## 2026-08-22 — Portable Developer 0.4.0: centrální správce portů

- Přibyla samostatná stránka Porty pro Apache HTTP, PHP FastCGI, MariaDB a Selenium. Čtyři jedinečné porty v rozsahu 1024–65535 se atomicky ukládají do `state/port-settings.json` a všechny controllery z nich vytvářejí skutečnou runtime konfiguraci.
- Přehled čte aktivní TCP listenery Windows bez zásahů do hostitelského systému. Živá kontrola formuláře rozlišuje volný, obsazený, duplicitní a aplikací vlastněný port a navíc zkouší skutečný localhost bind; uložení zůstává blokované, dokud nejsou všechny služby zastavené.
- Starší samostatné nastavení portu Selenium se použije jako migrační fallback, ale další změny portu probíhají už jen centrálně. Každý controller si port před startem nadále znovu ověřuje kvůli možné změně stavu mezi kontrolou a spuštěním.
- Automatické testy jsou zelené 87/87 a formátování i release build proběhly bez varování. Vizuální smoke test ověřil českou stránku Porty, verzi 0.4.0, výchozí hodnoty a okamžité označení obou řádků při duplicitě `8080`.
- První smoke test odhalil chybný výchozí režim WPF bindingu uvnitř `Run`, který ukončil aplikaci před zobrazením okna; explicitní `Mode=OneWay` start opravil a opakovaný test již prošel. Finální čistý výstup `PortableDeveloper-offline-win-x64-0.4.0-final-clean` má 926,36 MiB, 11 položek v kořeni, verzi souboru 0.4.0.0 a produktu 0.4.0 a neobsahuje PDB ani runtime data. Retence ponechala také 0.3.0; ZIP se nevytvářel.

## 2026-08-22 — Otevření projektu a příprava důvěryhodných releasů

- Historie repozitáře byla před zveřejněním zkontrolována na velké binárky, tajné klíče a citlivé údaje. Obsahuje pouze zdrojové soubory a commit používá GitHub `noreply` adresu.
- Vlastní kód byl licencován pod `GPL-3.0-or-later`; přibyly zásady soukromí, bezpečnostní reporting, pravidla přispívání, inventář licencí třetích stran a transparentní politika podepisování.
- GitHub Actions na Windows ověřuje restore, formátování, Release build a testy. Dependabot měsíčně kontroluje NuGet a workflow závislosti.
- Publish nově přidává do kořene distribuce licenci, zásady soukromí a třetí strany. Oficiální binární release zůstává odložený, dokud nebude dokončen licenční audit všech přibalených souborů a podpis vlastního EXE; Windows ochranu nebudeme obcházet.

## 2026-08-22 — Online bootstrap bez Laragonu a systémových DLL

- Přidán úzký `catalog/dependencies.lock.json` pro všech jedenáct skutečných release vstupů. Nejde o runtime marketplace; obsahuje přesné verze, varianty, HTTPS zdroje, SHA-256 a licenční odkazy jen pro současný offline balík.
- `Fetch-Dependencies.ps1` stahuje přes dočasné soubory do ignorované `downloads/dependencies`, používá allowlist hostů, tři pokusy a fail-closed kontrolu SHA-256. Režimy `-VerifyOnly` a `Publish-Windows.ps1 -OfflineDependencies` zaručují sestavení bez sítě pouze z ověřené cache.
- Veřejná CI kontroluje dependency lock bez stahování téměř gigabajtového release: validuje schéma, unikátní ID, bezpečné názvy, SHA-256 a allowlist HTTPS zdrojů.
- Laragon a `System32` byly odstraněny ze vstupů publish procesu. PHP, OpenJDK, CPython NuGet runtime, Notepad++, phpMyAdmin a všechny servery se rozbalují přímo z upstream archivů; Python binárka z NuGetu přesně odpovídá dříve používanému runtime a nadále offline vytváří pip 24.2 přes `ensurepip`.
- Nedostupný archiv Apache 2.4.66 byl nahrazen aktuálním přesným Apache Lounge buildem 2.4.68-260617 VS18. Archiv i výsledný `httpd.exe` mají připnutý SHA-256 a skutečný version check vrací Apache 2.4.68.
- Microsoft VC++ Redistributable 14.51.36247 se stahuje jako přesný podepsaný bundle. WiX 6.0.2 z něj bez instalace vyjme x64 CAB; sedm app-local DLL se přijme jen při shodě přesného hashe, verze a podpisu Microsoft Corporation.
- Z oficiálního Microsoft OpenJDK archivu se do release kopíruje jen runtime obraz, konfigurace a licence. Vývojové `jmods`, hlavičky, manuály a `src.zip` Selenium nepotřebuje a v portable balíku se neukládají.
- Čistý online-bootstrap release vznikl také v režimu `-OfflineDependencies`, má 895,88 MiB a 8 402 souborů. Všechny archivy prošly opakovanou kontrolou cache, runtime version checky odpovídají locku, zmenšená Java úspěšně načetla Selenium Server JAR, aplikační smoke skončil standardním zavřením s exit code 0 a automatické testy jsou zelené 87/87.

## 2026-08-22 — Portable Developer 0.5.0: Apache projekty

- Apache stránka nyní vytváří a spravuje více webových projektů. Default bezpečně zachovává `instances/default/www`; nové projekty používají `instances/default/projects/<id>/public` a adresu `<id>.localhost` bez zápisu do Windows `hosts`.
- Generátor načítá `mod_rewrite`, zapíná `.htaccess` přes `AccessFileName` a per-project `AllowOverride`, vytváří name-based virtual hosty a používá pouze `127.0.0.1` s `Require local`.
- Composer pracuje v kořeni zvoleného projektu, takže každý web má vlastní `composer.json` a `vendor`; standardní `public` document root závislosti nezveřejňuje. Stejný aktivní projekt sleduje terminál i interní správce souborů.
- Projektový katalog je atomický, validuje ID i relativní web root, odmítá reparse pointy a při odebrání registrace zachová všechny soubory. Vytvoření, výběr, změny Apache a odebrání se zapisují do JSONL aplikačního logu bez citlivých dat.
- Debug i Release kompilace proběhly bez varování a 94/94 automatických testů pokrývá migraci Default, zachování souborů, oddělený Composer/správce souborů a generování virtual hostů. Nový release balík ani ZIP se v tomto kroku nevytváří.

## 2026-08-22 — Verze 0.6.0, runtime moduly a binární release

- Starý zákaz runtime downloaderu byl na výslovné rozhodnutí vlastníka nahrazen verzovaným správcem balíčků. Aplikace nabízí Web, Databázi, Selenium, Composer, Python, Editor a phpMyAdmin a stahuje pouze komponenty z lokálního dependency locku.
- Downloader kontroluje allowlist HTTPS včetně finálního redirectu, používá tři pokusy, `.part` cache, SHA-256 archivu, bezpečné ZIP rozbalení bez traversal/odkazů/reparse pointů, normalizovaný vstupní hash, atomický přesun a rollback nových cílů.
- Navigace je seskupená na Prostředí, Servery, Vývoj a Aplikaci. Čistý první start neukazuje Apache, PHP, databáze, Selenium, Composer, Python ani editor; odpovídající stránka se objeví až po ověřené instalaci.
- `Publish-Online-Windows.ps1` vytvořil self-contained základ bez serverových modulů, ZIP o velikosti přibližně 54 MiB a SHA-256. Základ obsahuje jen aplikaci, katalogy, právní dokumenty a app-local VC++ DLL vyjmuté z hashově i podpisově ověřeného Microsoft balíku.
- První čistý smoke odhalil WPF TwoWay vazbu na read-only průběh. Vazba byla změněna na OneWay, přidáno nouzové logování UI výjimek a opakovaný první start ověřil verzi 0.6.0, prázdný dashboard, skupiny menu i všech sedm instalačních karet.
- Přidán tagový release workflow s ZIPem a checksumem. Politika nově dovoluje hotové nepodepsané binární releasy s povinným transparentním upozorněním; plán SignPath zůstává.
- Izolované integrační testy runtime manageru ověřují stažení, opakování po síťové chybě, hash, rozbalení, metadata, atomickou registraci MariaDB, skutečný přibalený lock katalog a úklid stagingu. Celá sada je zelená 99/99.

## 2026-08-22 — Příprava SignPath Foundation a Windows reputace

- Lokální Defender Operational log neobsahoval pro Portable Developer událost 1116/1117 ani aktivní hrozbu; oficiální EXE je nepodepsané. Dosavadní blokace proto odpovídá SmartScreen/Smart App Control reputaci, nikoli doložené antivirové detekci.
- Připraven byl přesný oficiální EXE z GitHub Release 0.6.0 pro Microsoft analýzu; jeho SHA-256 je `eee3ab344c72a9532a5f10df27f2ca93a6e9b9fc848dbd44a2b3d625241e598e`.
- Repozitář dostal anglický přehled, governance, signing role, uninstall postup, community šablony, přesná produktová metadata a checklist podle aktuálních podmínek SignPath Foundation.
- Audit odhalil, že lokálně obsazený port 4444 destabilizoval jeden Selenium test; test nově používá dočasný volný port a celá sada znovu prochází 99/99.
- První UI smoke odhalil výchozí TwoWay režim WPF `SelectedValue` nad read-only ID projektu, který ukončil aplikaci při startu. Oba selektory nyní používají explicitní `Mode=OneWay`; opakovaný start byl stabilní.
- Vizuální scénář v izolovaném Debug výstupu vytvořil `Smoke Projekt`, zobrazil `smoke-projekt.localhost`, samostatný `public`, lokalizované stavy a bezpečně zablokované akce Default. Composer ukázal kořen `projects/smoke-projekt`, správce souborů pouze jeho obsah a terminál prompt `smoke-projekt:/>`.

## 2026-08-22 — Optimalizační balík pro 0.7.0

- Vznikl společný `Theme.xaml` pro tmavé ComboBoxy, popup položky, scrollbar a dialogové formuláře. Jedna šablona TabControlu nyní určuje 14px návaznost obsahu na záložky na PHP, Apache, databázi, Selenium i Porty.
- Čtyři nativní MessageBox potvrzení nahradil tmavý `ConfirmationDialog` s konkrétním názvem destruktivní akce, Escape podporou a bezpečným výchozím fokusem na Zrušit. `NamePromptDialog` používá stejná vstupní a tlačítková pravidla.
- Composer a Python dostaly společný typovaný průběh pro přípravu, běh správce závislostí, obnovu inventáře a dokončení. UI používá indeterminate animaci během práce a ponechá viditelný lokalizovaný konečný výsledek.
- Portable terminál přidal interní `mkdir` bez shellu. Příkaz respektuje aktivní webový projekt, relativní cesty a zákaz reparse pointů; `help` a `help <command>` se generují ze sdíleného aplikačního registru.
- Přibylo sedm regresních testů terminálu a dva testy fází package manageru. Debug build, formátovací kontrola a celá sada 106/106 testů prošly bez varování.
- Vizuální smoke izolované Debug aplikace ověřil tmavý zavřený select, popup a selected položku i sjednocený vstupní dialog. První smoke odhalil černý selected text uvnitř ComboBox šablony; explicitní dědění `Foreground` jej opravilo. Testovací instance byla ukončena přesným filtrem cesty bez zásahu do vydání běžícího z `E:\`.

## 2026-08-22 — Selenium driver katalog a immutable profily

- Selenium Server + OpenJDK už neinstalují výchozí geckodriver. Samostatná karta nabízí explicitní hashově připnuté EdgeDriver 151.0.4129.101, ChromeDriver 152.0.7977.54 a geckodriver 0.37.1; vlastní drivery zůstaly podporované.
- Driver instalace sdílí zabezpečený runtime downloader, každý EXE ověří druhým SHA-256 a atomicky znovu sestaví společný manifest bez ztráty dříve nainstalovaných driverů. Izolovaný integrační test ověřil instalaci ChromeDriveru i jeho načtení inventářem.
- Nová karta Profily importuje browser profil do read-only masteru uvnitř portable kořene, odmítá reparse pointy a při odebrání nikdy nemaže původní zdroj. C# testy ověřily neměnnost masteru, zapisovatelnou pracovní kopii a bezpečný úklid.
- Veřejný Java zdroj `PortableProfileNode` se při prvním Selenium startu sestaví přibaleným `javac` proti přesnému Server JAR. Capability `portable:profile` vytvoří kopii pro konkrétní relaci, doplní Chrome/Edge/Firefox options a uklízí při chybě, DELETE, stopu i zániku relace.
- Reálný smoke spustil Selenium Standalone 4.47.0 s `--ext` na odděleném portu a požadavek skutečně prošel profilovým Node až k ChromeDriveru s vloženým `--user-data-dir`. Driver 152 korektně odmítl hostitelský Chrome 151, což potvrzuje potřebu viditelného verzování a absence falešného univerzálního defaultu; pracovní kopie se po neúspěchu odstranila.
- Release build i 109/109 automatických testů prošly bez varování. Testovací Selenium proces byl ukončen podle přesné cesty/PID a port 55444 zůstal volný; běžící vydání na `E:\` nebylo změněno.
