# Architektonická rozhodnutí

Rozhodnutí mají trvalé identifikátory. Nové významné rozhodnutí přidej na konec souboru a neměň historii; případné zrušení vyjádři novým záznamem.

## ADR-001 — C# / .NET 8 / WPF pro desktopovou aplikaci

- Stav: přijato
- Datum: 2026-08-21

**Kontext:** Cílové prostředí je Windows a koncový uživatel nemusí mít Python ani .NET.

**Rozhodnutí:** Aplikace bude psaná v C# jako WPF aplikace cílená na .NET 8 a publikovaná self-contained pro `win-x64`.

**Důsledky:** Distribuce je samostatná složka s vyšší velikostí. Vývojový počítač potřebuje .NET SDK 8. Python + PyInstaller není základní architekturou, ale lze jej použít pro oddělené vývojové nástroje.

## ADR-002 — Bez Dockeru a bez systémových služeb

- Stav: přijato
- Datum: 2026-08-21

**Kontext:** Projekt má fungovat z USB či libovolné složky bez instalace a zásahů do Windows.

**Rozhodnutí:** Apache, PHP, databáze, Selenium a JRE poběží jako dítě hlavní aplikace z přenosných modulů. Docker, instalátory a Windows služby nejsou součástí první verze.

**Důsledky:** Je nutné vlastnit správu procesů, konfiguraci a životní cyklus serverů.

## ADR-003 — Relativní cesty a data uvnitř kořene aplikace

- Stav: přijato
- Datum: 2026-08-21

**Rozhodnutí:** Konfigurace je relativní vůči kořeni Portable Developeru. Runtime data patří do `instances/`, sdílená data do definovaných podadresářů kořene.

**Důsledky:** Přesun složky mezi disky je podporovaný scénář a musí být testován.

## ADR-004 — Přechod z .NET 8 na .NET 10

- Stav: přijato; nahrazuje cílovou verzi z ADR-001
- Datum: 2026-08-21

**Kontext:** Vývojové prostředí obsahuje .NET SDK 10.0.400, nikoliv SDK 8. V projektu se zatím nenachází produkční implementace závislá na .NET 8.

**Rozhodnutí:** Aplikace cílí na .NET 10 a přesná verze SDK je připnutá v `global.json`.

**Důsledky:** Lokální build je ihned reprodukovatelný se současným SDK. Distribuce i nadále bude self-contained, takže koncový uživatel .NET instalovat nemusí.

## ADR-005 — Normalizovaný layout ručně vložených modulů

- Stav: přijato
- Datum: 2026-08-21

**Kontext:** ZIP archivy Apache, PHP a dalších komponent mají rozdílnou vnitřní strukturu. Runtime ani konfigurace nemají záviset na konkrétním archivu.

**Rozhodnutí:** Každý modul používá tvar `modules/<druh>/<verze>/` s pevně definovaným vstupním souborem. Například Apache používá `bin/httpd.exe` a PHP `php-cgi.exe`. Složky typu junction a symbolické odkazy se ignorují.

**Důsledky:** Ruční vložení je předvídatelné a budoucí instalátor může každý archiv normalizovat do stejného tvaru. Pouhá detekce modulu nezakládá oprávnění jej spustit; to vyžaduje budoucí katalog s ověřením hashe.

## ADR-006 — Absolutní serverové cesty jen v dočasně generované konfiguraci

- Stav: přijato
- Datum: 2026-08-21

**Kontext:** Apache pro své `ServerRoot`, logy a PID soubory vyžaduje praktické absolutní cesty, zatímco přenosná konfigurace nesmí obsahovat písmeno aktuálního disku.

**Rozhodnutí:** Trvalé nastavení Apache/PHP obsahuje jen relativní cesty. Konfigurační soubory specifické pro běh se vytvářejí pod `temp/generated/` z aktuálního kořene aplikace a před každým startem se nahradí.

**Důsledky:** Přesun celé složky nevyžaduje úpravu uživatelské konfigurace. Dočasné soubory mohou obsahovat absolutní cesty pouze uvnitř aktuálního kořene aplikace.

## ADR-007 — Lokální katalog a instalace přes ověřený archiv

- Stav: přijato
- Datum: 2026-08-21

**Kontext:** Aplikace musí umět stáhnout komponenty, ale nesmí důvěřovat libovolné URL ani rozbalit škodlivý ZIP mimo vlastní složku.

**Rozhodnutí:** Katalog je přibalený v `catalog/modules.json`. Každý balíček uvádí HTTPS zdroj, verzi, licenci, očekávaný vstupní soubor, kořen archivu a SHA-256 celého ZIPu. Instalátor nejdřív ověří archiv, rozbalí jej do `temp/install/` a až potom přesune do `modules/`.

**Důsledky:** Vydání aplikace musí aktualizovat katalog společně s ověřenými hashi. Vzdálené aktualizace katalogu nejsou podporované, dokud nezavedeme podpisový řetězec.

## ADR-008 — App-local nativní runtime místo instalace do Windows

- Stav: přijato
- Datum: 2026-08-21

**Kontext:** Oficiální Windows build PHP potřebuje Microsoft Visual C++ runtime. Systémový redistributable by zapsal závislost mimo portable složku a vyžadoval by jiný životní cyklus než aplikace.

**Rozhodnutí:** PHP se považuje za připravené pouze tehdy, když jsou `vcruntime140.dll` a `vcruntime140_1.dll` vedle `php-cgi.exe` ve složce modulu. Dashboard provede pouze čtecí preflight; budoucí balíček runtime bude instalován app-local přes lokální hashovaný katalog.

**Důsledky:** Aplikace nespouští `vc_redist*.exe`, nemění systém a může přesně oznámit chybějící soubory. Do doby licenčního a hashového schválení runtime balíčku se PHP po samotné instalaci z katalogu nespouští.

## ADR-009 — Portable volba jazyka rozhraní

- Stav: přijato
- Datum: 2026-08-21

**Kontext:** Uživatel musí volit mezi češtinou a angličtinou, ale portable aplikace nesmí pro toto nastavení používat registr ani profil Windows.

**Rozhodnutí:** Jazyk dashboardu se uchová v `state/settings.json` pod kořenem aplikace. Výchozí jazyk je čeština; změna se okamžitě projeví v UI a zapíše provozní událost do logu.

**Důsledky:** Nastavení se přenese se složkou aplikace. Neplatný či chybějící soubor bezpečně vrací výchozí češtinu.

## ADR-010 — Apache/PHP start pouze přes ověřený stack controller

- Stav: přijato
- Datum: 2026-08-21

**Kontext:** WPF nesmí řídit `Process` přímo a ručně vložená binárka nemá být automaticky považována za důvěryhodnou.

**Rozhodnutí:** Jediný controller řídí pořadí PHP FastCGI → Apache, volné porty, transientní konfiguraci, TCP health check a rollback. Spuštění vyžaduje záznam instalace shodný s přibaleným katalogem a kompletní app-local PHP runtime. Při zavření aplikace controller nejprve zastaví Apache, poté PHP.

**Důsledky:** UI pouze zobrazuje snapshot a volá aplikační rozhraní. Neověřené či jen ručně vložené moduly se nespouštějí; katalogové moduly navíc čekají na kompletní app-local runtime.

## ADR-011 — Apache Lounge jako zdroj Windows httpd

- Stav: přijato
- Datum: 2026-08-21

**Kontext:** Apache Software Foundation vydává httpd jako zdrojový kód a Windows binárky odkazuje na třetí strany. Potřebujeme reprodukovatelný x64 ZIP bez instalátoru a služby.

**Rozhodnutí:** Katalog používá Apache Lounge httpd 2.4.68 VS18 x64. URL a SHA-256 odpovídají Microsoft WinGet manifestu; SHA-256 byl navíc vypočten z lokálně staženého archivu. Archiv se normalizuje z kořene `Apache24/` a zachovává přiložené `LICENSE.txt` a `NOTICE.txt`.

**Důsledky:** Apache je třetí stranou sestavená binární distribuce a musí být při každé aktualizaci znovu ověřena. Protože ZIP neobsahuje importovaný `VCRUNTIME140.dll`, spuštění blokuje app-local runtime preflight; DLL lze dodat ověřeným importem podle ADR-012.

## ADR-012 — Uživatelský import podepsaného Visual C++ runtime

- Stav: přijato
- Datum: 2026-08-21

**Kontext:** Apache i PHP potřebují Visual C++ runtime. Spuštění `vc_redist.exe` by provedlo systémovou instalaci a automatické šíření DLL projektem vyžaduje oprávnění podle licenčních podmínek Microsoftu.

**Rozhodnutí:** Aplikace umožní import retail x64 DLL z uživatelem zvolené složky. Před app-local kopírováním ověří WinTrust řetězec, Microsoft signer, x64 PE a minimální verzi 14.50; k cílovému modulu zapíše SHA-256 metadata. Zdrojovou absolutní cestu neukládá a žádný externí program nespouští.

**Důsledky:** Portable hranice zůstává zachovaná a projekt nešíří neprověřené Microsoft DLL. Distributor musí dodat licencovaný zdroj runtime nebo připravit výslednou složku pomocí importéru; odpovědnost za bezpečnostní aktualizace app-local souborů zůstává na distributorovi.

## ADR-013 — MariaDB jako ZIP modul s transakční inicializací

- Stav: přijato
- Datum: 2026-08-22

**Kontext:** MariaDB musí běžet z portable složky bez MSI a služby Windows. Její inicializační nástroj vytváří datové soubory i `my.ini` s absolutními cestami a při přerušení může zanechat neúplný adresář.

**Rozhodnutí:** Katalog používá oficiální MariaDB 12.3.2 x64 ZIP a SHA-256 z REST API MariaDB Foundation. `mariadb-install-db.exe` se spouští přes obecný jednorázový command runner bez parametru služby, s timeoutem a daty ve staging složce. Po úspěchu se odstraní nepřenosný `my.ini`, data se atomicky přesunou do instance a náhodné root heslo se uloží pod `instances/<id>/state/`.

**Důsledky:** Neúplná inicializace nepřepíše existující data a nepřidá službu, registr ani systémovou konfiguraci. Start controller musí před každým během vytvořit nový transientní `my.ini` z aktuálního kořene aplikace. Soubor s heslem je tajemství a nesmí se dostat do Gitu ani logů.

## ADR-014 — Offline distribuce se všemi runtime komponentami

- Stav: přijato; nahrazuje runtime download z ADR-007 a uživatelský import z ADR-012
- Datum: 2026-08-22

**Kontext:** Download různých vendor archivů je křehký a uživatelský import Visual C++ DLL zhoršuje první spuštění. Cílem je intuitivní složka, která funguje bez instalace a bez sítě hned po rozbalení.

**Rozhodnutí:** Release obsahuje přesně připnuté verze Apache, PHP, MariaDB, Selenium, JRE a Composeru. Release skript je sestaví z předem připravených zdrojů, ověří SHA-256 serverových vstupních souborů a přidá podepsané app-local Microsoft VC++ DLL. Spuštěná aplikace nenabízí downloader ani import runtime a pouze ověřuje integritu přibaleného obsahu.

**Důsledky:** Výstup je větší, ale na cílovém počítači nevyžaduje síť, Python, .NET, Javu ani VC++ instalátor. Distributor odpovídá za aktualizace, kontrolní součty a licenční oprávnění všech přibalených komponent. ADR-007 zůstává historickým popisem původního směru; jeho runtime download workflow se dále nepoužívá. ADR-012 je nahrazené a importní UI bylo odstraněno.
