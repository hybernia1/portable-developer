# Implementační plán verze 0.8.0

> Plán byl 2026-08-22 převeden do implementace verze 0.8.0. Uživatelský souhrn je v
> `RELEASE_NOTES_0.8.0.md`; tento soubor zůstává jako audit původního zadání a trade-offů.

## Aktuálně sesbírané změny

### 1. Červený validační rámeček navigace po instalaci modulu

- Po úspěšné instalaci se volá obnova dostupnosti modulů a znovu se sestavuje levé menu.
- `NavigationItems.Clear()` dočasně zruší výběr `ListBoxu`. Jeho `SelectedValue` je
  obousměrně navázaný na nenulovatelnou stránku, takže WPF pravděpodobně vytvoří
  validační chybu a výchozí červený `Validation.ErrorTemplate` kolem menu.
- Oprava má odstranit neplatný mezistav, zachovat vybranou stránku, pokud je stále
  dostupná, a explicitně zapsat uživatelský výběr stránky do view modelu.
- Doplnit regresní test obnovy navigace po instalaci nebo změně dostupnosti modulu.

### 2. Systémové asociace souborů a volitelný portable editor

- Akce **Otevřít** má respektovat výchozí aplikaci nastavenou ve Windows.
- Obrázky, PDF, textové a další běžné typy se mají otevřít v uživatelem zvolené aplikaci.
- Pokud asociace neexistuje, nabídnout systémovou volbu aplikace; portable Notepad++
  ponechat jako volitelný fallback, nikoliv povinnou závislost.
- Úprava `php.ini` a dalších konfigurací musí fungovat i bez staženého portable editoru.
- Zachovat ochranu kořene projektu. Rizikové spustitelné typy (`.exe`, `.com`, `.scr`,
  `.msi`, `.bat`, `.cmd`, `.ps1`, `.reg`, `.lnk`, `.url`) se z file manageru nesmí
  bez explicitního bezpečnostního rozhodnutí automaticky spouštět.
- Rozlišit význam akcí **Otevřít** (systémová asociace) a případné **Upravit**
  (preferovaný editor nebo bezpečný fallback).

### 3. Globální styl všech selectů

- Selenium profil aktuálně používá výchozí světlý WPF `ComboBox`, protože mu chybí
  explicitní `AppComboBoxStyle`.
- Tmavý vzhled nastavit centrálně jako implicitní app-wide styl pro `ComboBox` i
  `ComboBoxItem`, aby jej nebylo nutné přidávat ke každému prvku ručně.
- Projít všechny selecty v hlavním okně i aplikačních dialozích a ověřit popup,
  hover, focus, disabled stav, scrollbar a klávesové ovládání.
- Doplnit kontrolu, která zabrání návratu nestylovaného selectu v dalších stránkách.

### 4. Pouze jedna spuštěná instance aplikace

- Ve stejné interaktivní Windows relaci nesmí současně běžet dvě instance Portable
  Developer, ani když byly spuštěny ze dvou různých portable složek.
- Použít uživatelsky omezený pojmenovaný mutex (`Local`, nikoliv privilegovaný
  globální objekt) a malý aktivační kanál, ideálně named pipe.
- Druhé spuštění nemá jen tiše skončit: má požádat první instanci o obnovení z
  minimalizace, přesun do popředí a aktivaci hlavního okna.
- Při běžném i chybovém ukončení korektně uvolnit prostředky; opuštěný mutex nesmí
  po pádu bránit dalšímu startu.
- Koordinaci oddělit od WPF startupu tak, aby šla jednotkově otestovat.

### 5. Vlastní jednotná horní lišta všech aplikačních oken

- Hlavní okno, potvrzovací dialog a dialog pro zadání názvu mají používat jeden
  centrální title-bar vzhled odpovídající tmavému UI.
- Implementovat společný styl nebo znovupoužitelný title-bar prvek nad WPF
  `WindowChrome`; nekopírovat samostatné šablony do každého dialogu.
- Hlavní okno: minimalizace, maximalizace/obnovení a zavření.
- Aplikační dialogy: podle účelu pouze relevantní tlačítka, typicky zavření bez
  maximalizace a minimalizace.
- Zachovat nativní chování Windows: tažení okna, dvojklik pro maximalizaci, resize,
  Snap Layouts, systémové menu, `Alt+F4`, správné chování na více monitorech a DPI.
- Native Windows dialogy pro výběr souboru nebo složky zůstanou systémové; centrální
  styl se vztahuje na okna vlastněná aplikací.
- Doplnit přístupné popisky a focus stavy ovládacích tlačítek.

### 6. Selenium browser prostředí a validace master profilů

#### Co validujeme nyní

- Master se ukládá do `profiles/selenium/<profile-id>/master` a metadata leží vedle
  něj. Pracovní kopie relací vznikají v `temp/selenium-profiles/<session-token>`.
- Import používá staging, hlídá ID, název, existenci zdroje, únik z portable kořene,
  reparse pointy, symbolické odkazy a speciální soubory.
- Master soubory se označí read-only. Selenium nikdy nedostane master přímo: pro
  každou relaci vytvoří zapisovatelnou kopii a po ukončení ji odstraní.
- Node kontroluje shodu deklarované rodiny browseru s capability `browserName`.

#### Co zatím chybí

- Browser vybírá uživatel ručně; obsah složky se s vybranou rodinou sémanticky
  neporovnává.
- Nerozlišujeme Chromium user-data root od konkrétního profilu `Default` / `Profile 1`.
  Aktuální předání celé importované složky jako `--user-data-dir` proto nemusí načíst
  očekávaný Chromium profil.
- Neověřujeme browser binárku, její verzi ani kompatibilitu s driverem.
- Import živého profilu může narazit na locky nebo vytvořit nekonzistentní SQLite data.
- Chybí limity velikosti, počtu souborů, průběh importu, zrušení, filtrace cache a
  kontrolní manifest integrity masteru.
- Nelze garantovat přenositelnost přihlašovacích údajů uložených browserem; část může
  být šifrovaná pro konkrétní Windows účet nebo počítač.

#### Doporučený model browser prostředí

- V UI pracovat s celým **browser prostředím**, ne s osamoceným driverem. Jedna karta
  ukáže browser binárku, browser verzi, driver verzi, zdroj a výsledek compatibility
  preflightu.
- Podporovat tři explicitní zdroje:
  1. **Portable Chrome for Testing + odpovídající ChromeDriver** – doporučené,
     deterministické prostředí, stažené do složky aplikace jako verzovaný pár.
  2. **Systémový Edge, Chrome nebo Firefox** – aplikace browser pouze detekuje a nic
     v systému nemění; odpovídající driver se řeší podle zjištěné verze.
  3. **Vlastní browser binárka** – pokročilá volba s explicitně zvolenou cestou a
     preflightem `--version` a testovací relace.
- Microsoft Edge na Windows nenabízet jako automaticky instalovaný portable browser.
  Oficiální automatická instalace používá MSI a vyžaduje administrátorská oprávnění;
  Edge proto podporovat jako systémový nebo uživatelem zadaný browser.
- Pro hostitelské browsery zvážit oficiální Selenium Manager jako resolver driveru.
  Musí mít cache přesměrovanou do portable kořene (`SE_CACHE_PATH`), vypnutou
  telemetrii (`SE_AVOID_STATS=true`) a stahovat pouze po transparentní uživatelské
  akci. Je potřeba výslovně rozhodnout trade-off mezi aktuálním SHA-pinned katalogem
  a živým vendor/Selenium Manager resolution.
- Vygenerovaný Grid stereotype má obsahovat absolutní cestu registrované browser
  binárky i driveru; čistý PATH aplikace zůstane zachovaný.
- Když není dostupné žádné kompatibilní browser prostředí, Selenium modul může zůstat
  nainstalovaný, ale spuštění serveru bude zakázané s konkrétní nabídkou: stáhnout
  portable Chrome for Testing, použít nalezený systémový browser nebo vybrat vlastní.

#### Doporučený životní cyklus master profilu

- Primární cesta má být **Vytvořit čistý master profil**:
  1. uživatel vybere konkrétní registrované browser prostředí,
  2. aplikace vytvoří dočasný zapisovatelný profil,
  3. spustí browser pro přihlášení a konfiguraci,
  4. po jeho zavření odstraní locky a volatilní cache, profil ověří a zapečetí jako
     read-only master.
- **Import existujícího profilu** ponechat jako pokročilou možnost. Import wizard má
  sám rozpoznat browser a u Chromium nabídnout nalezené profily (`Default`,
  `Profile 1`, ...), místo ručního selectu bez validace.
- Chromium master normalizovat jako user-data root s `Local State` a vybraným profilem
  pod stabilním názvem; Node musí předávat `--user-data-dir` i `--profile-directory`.
- Firefox master zůstane přímo profile root a používá `-profile <session-copy>`.
- Při importu kontrolovat browser-specific sentinel soubory, aktivní locky, maximální
  velikost a počet souborů, délky cest a volné místo. Operace musí mít průběh a možnost
  zrušení.
- Vynechat pouze bezpečně definované volatilní položky (crash data, cache, locky), ne
  cookies, rozšíření nebo uživatelské nastavení bez vědomí uživatele.
- Uložit manifest se schématem, rodinou browseru, verzí použitou při vytvoření,
  rozložením profilu, počtem souborů, velikostí a kontrolním hashem. Master ověřit před
  každým použitím.
- Nad jednorázovou kopií provést volitelný smoke test. V UI rozlišit stavy **Ověřený**,
  **Neotestovaný**, **Nekompatibilní**, **Poškozený** a **Browser není dostupný**.

#### Zdroje k rozhodnutí

- Selenium Manager a portable cache: <https://www.selenium.dev/documentation/selenium_manager/>
- Selenium Grid browser/driver configuration: <https://www.selenium.dev/documentation/grid/configuration/cli_options/>
- Chrome for Testing jako verzovaný browser + driver pár:
  <https://developer.chrome.com/docs/automation-and-testing>
- Požadavek na shodu Edge a EdgeDriver:
  <https://learn.microsoft.com/en-us/microsoft-edge/webdriver/>

## Předběžný rozsah vydání

- Rozsah byl potvrzen jako `0.8.0`.
- Zdroj, testy a publish metadata jsou připravené pro veřejný tag `v0.8.0` a automatický
  GitHub Release se self-contained Windows x64 ZIPem a SHA-256 součtem.

## Vědomě odložené rozšíření

- Pokročilé jednorázové přidání libovolné browser binárky mimo portable kořen. Verze 0.8.0
  používá ověřený portable Chrome nebo bezpečně detekované standardní instalace Windows a
  neukládá absolutní hostitelské cesty do přenosného stavu.
- Zrušitelný asynchronní import s detailním průběhem pro velmi velké existující profily.
  Současný import má bezpečnostní limit 25 000 souborů / 2 GiB a je určen hlavně pro
  normalizovaný čistý master; plnohodnotný background workflow vyžaduje samostatný UI stav.
- Volitelná automatická testovací WebDriver relace po vytvoření masteru. Integrita a layout
  se nyní ověřují před použitím, ale skutečný browser smoke zůstává explicitním dalším krokem.
