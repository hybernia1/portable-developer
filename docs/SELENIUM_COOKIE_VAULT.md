# Selenium cookie vault

Cookie vault přenáší pouze uživatelem vybrané cookies, nikoli celý browser profil. Je nezávislý na Firefoxu i Chrome for Testing.

## Import

1. V browseru exportuj cookies pouze pro web, který smíš automatizovat.
2. V aplikaci otevři `Selenium > Profily > Cookie vault`.
3. Zvol JSON soubor, název a klikni na **Importovat**. Heslo ani odemykání nejsou potřeba.
4. Po importu zabezpeč nebo odstraň původní nešifrovaný export; aplikace jej nemění.

Podporovaný vstup je JSON pole nebo objekt s polem `cookies`. Z každé položky se zachová pouze:

- `name`, `value` a `domain`;
- `path`, případně výchozí `/`;
- `expires`, `expiry`, `expirationDate` nebo `expiration` jako Unix timestamp;
- `httpOnly`, `secure` a podporovaná hodnota `sameSite`.

Pole rozšíření jako `id`, `storeId`, `hostOnly` nebo libovolná další metadata se zahodí. Prošlé a neplatné cookies se neuloží, duplicity řeší poslední položka exportu.

## Použití v Selenium

Selenium skript předá ID vaultu jako namespaced capability:

```python
from selenium import webdriver

options = webdriver.ChromeOptions()
options.set_capability("portable:vault", "ID_Z_APLIKACE")

driver = webdriver.Remote(
    command_executor="http://127.0.0.1:4444",
    options=options,
)
```

Stejnou capability lze použít s `FirefoxOptions`. Je možné ji zkombinovat s `portable:profile`, běžné použití vaultu však začíná v čistém dočasném profilu.

Při vzniku relace Node vault autentizuje a rozšifruje v paměti, navštíví jednotlivé domény a vloží cookies do jejich správného původu ještě před vrácením relace klientovi. Pokud vault chybí, je poškozený nebo browser cookie odmítne, nová relace se uzavře a klient dostane chybu.

## Bezpečnost a přenositelnost

Vault na disku používá AES-256-GCM a automatický 256bitový klíč uložený pod `state/selenium-cookie-vault.key`. Čitelný dočasný payload nevzniká. Domény, název vaultu, počet položek a čas importu jsou v šifrované obálce viditelné pro UI.

Klíč cestuje spolu s portable složkou. Toto řešení nevyžaduje heslo a chrání samostatně zkopírovaný soubor vaultu i integritu dat, nechrání však při odcizení celé složky nebo před jiným procesem stejného Windows účtu. Pro silnou ochranu celé přenosné instalace použij šifrovaný disk či kontejner.

Cílový web může relaci svázat s IP adresou, zařízením, User-Agentem nebo dalším browserovým stavem. Přenos přihlášení proto nelze zaručit pro každou službu.
