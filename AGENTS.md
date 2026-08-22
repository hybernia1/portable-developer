# Pravidla pro lidi i agenty

## Priorita projektu

Portable Developer musí zůstat skutečně přenositelný. Každá změna má přednostně chránit izolaci od hostitelského Windows systému, čitelnost kódu a snadnou diagnostiku.

## Povinná pravidla

1. Neinstalovat Windows služby, ovladače ani systémové závislosti.
2. Neměnit systémový `PATH`, registr, asociace souborů ani firewall bez výslovného rozhodnutí vlastníka projektu.
3. Všechny cesty ukládat relativně vůči kořenu aplikace; nikdy nefixovat písmeno disku ani uživatelský profil.
4. Data databáze, konfigurace serverů, cache, dočasné soubory a logy směrovat do složky instance či kořene aplikace.
5. Každý externí proces musí mít jasného vlastníka, pracovní adresář, přesměrovaný výstup, kontrolu stavu a bezpečné ukončení.
6. Nespouštět stažené binárky bez ověření očekávaného SHA-256 a záznamu zdroje/verze.
7. Nepřidávat tajemství, hesla, API klíče, databázová data ani stažené binárky do Gitu.
8. Změny architektury zapisovat do `docs/DECISIONS.md`; uživatelsky viditelné změny do `CHANGELOG.md`; významné průběžné kroky do `docs/WORKLOG.md`.
9. Před předáním ověřit relevantní testy či build. Pokud to není možné, přesně uvést proč.
10. Runtime downloader smí pracovat pouze po výslovné uživatelské akci, jen s přibaleným verzovaným katalogem, povolenými HTTPS zdroji a připnutým SHA-256. Instalace musí proběhnout přes portable staging a nesmí instalovat systémový runtime ani přijmout libovolnou URL.

## Práce v repozitáři

- Nejprve si přečti dokumentaci související se změnou.
- Drž moduly malé a nezávislé; UI nesmí přímo řídit procesy bez servisní vrstvy.
- Logy musí být užitečné pro člověka a nesmějí obsahovat citlivé údaje.
- Nové nastavení musí mít výchozí hodnotu, validaci a lokalizovaný popis.
- Preferuj malé, tematicky čisté commity podle `docs/COMMITS.md`.

## Stav vývoje

Projekt používá .NET SDK 10.0.400, připnutý v `global.json`. Samotná cílová aplikace nebude vyžadovat instalovaný .NET runtime: bude publikována jako self-contained Windows build.
