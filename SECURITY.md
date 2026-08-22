# Bezpečnost

## Podporované verze

Bezpečnostní opravy dostává nejnovější vydaná řada. Projekt je zatím v rané fázi vývoje a starší sestavy nemusí být zpětně opravovány.

| Verze | Podpora |
|---|---|
| 0.4.x | ano |
| < 0.4 | ne |

## Nahlášení zranitelnosti

Citlivý bezpečnostní problém neposílej do veřejného issue. Použij soukromé hlášení přes [GitHub Security Advisories](https://github.com/hybernia1/portable-developer/security/advisories/new). Uveď dotčenou verzi, reprodukční kroky, očekávaný dopad a případný návrh opravy.

Běžné chyby bez bezpečnostního dopadu patří do [GitHub Issues](https://github.com/hybernia1/portable-developer/issues).

## Hranice bezpečnostního modelu

Portable Developer izoluje vlastní konfiguraci a data do svého adresáře, ale není operačním systémem vynucený sandbox. PHP, Python, Composer balíčky, Selenium testy a další uživatelem spuštěný kód běží s běžnými oprávněními aktuálního uživatele Windows. Spouštěj pouze důvěryhodný kód a knihovny.
