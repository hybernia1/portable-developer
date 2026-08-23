# Bezpečnost

## Podporované verze

Bezpečnostní opravy dostává nejnovější vydaná řada. Projekt je zatím v rané fázi vývoje a starší sestavy nemusí být zpětně opravovány.

| Verze | Podpora |
|---|---|
| 0.8.x | ano |
| 0.9.x | vývojová větev |
| < 0.8 | ne |

## Nahlášení zranitelnosti

Citlivý bezpečnostní problém neposílej do veřejného issue. Použij soukromé hlášení přes [GitHub Security Advisories](https://github.com/hybernia1/portable-developer/security/advisories/new). Uveď dotčenou verzi, reprodukční kroky, očekávaný dopad a případný návrh opravy.

Běžné chyby bez bezpečnostního dopadu patří do [GitHub Issues](https://github.com/hybernia1/portable-developer/issues).

## Hranice bezpečnostního modelu

Portable Developer izoluje vlastní konfiguraci a data do svého adresáře, ale není operačním systémem vynucený sandbox. PHP, Python, Composer balíčky, Selenium testy a další uživatelem spuštěný kód běží s běžnými oprávněními aktuálního uživatele Windows. Spouštěj pouze důvěryhodný kód a knihovny.

Runtime downloader nepřijímá libovolnou URL. Důvěra je ukotvená v katalogu konkrétní verze aplikace, povoleném HTTPS zdroji a připnutém SHA-256. Podezření na kompromitovaný upstream archiv, nesprávný hash, únik při rozbalování nebo možnost zápisu přes reparse point oznam jako bezpečnostní problém.

Cookie vault používá AES-256-GCM a automatický klíč uložený uvnitř portable `state/`. Nevytváří čitelný dočasný soubor, ale nechrání před útočníkem, který získá celou portable složku nebo přístup ke stejnému Windows účtu. Exporty cookies jsou autentizační tajemství. Nepřikládej je k veřejným issues, logům ani testovacím fixture a po podezření na únik příslušné relace na cílové službě odhlaš nebo odvolej.

Stejně citlivé jsou Selenium browser mastery pod `profiles/`: mohou obsahovat živé relace a uložené přihlašovací údaje. Pracovní kopie mají cloudovou synchronizaci vypnutou a po relaci se odstraňují, nejde však o ochranu při odcizení celé aplikace. Projektový `seldownloads` je trvalý; se staženými soubory zacházej jako s běžnými nedůvěryhodnými soubory a nespouštěj je bez ověření původu.
