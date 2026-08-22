# Code signing policy

## Stav

Veřejné podepisování zatím není aktivní. Od verze 0.6.0 projekt vydává hotové binární releasy i bez podpisu, ale každý takový ZIP, release manifest i poznámky jej musí jasně označit jako nepodepsaný. Windows Smart App Control nebo SmartScreen jej proto mohou zablokovat. Projekt nebude uživatelům doporučovat vypnutí této ochrany. Cílem zůstává podepisovat budoucí release po schválení projektu SignPath Foundation.

**Free code signing provided by [SignPath.io](https://signpath.io/), certificate by [SignPath Foundation](https://signpath.org/).** Tato věta popisuje zamýšlené podepisování budoucích vydání; verze označené jako nepodepsané tento podpis nemají.

## Tým a odpovědnosti

Projekt je v současnosti spravovaný jednou osobou. Role jsou veřejné a při rozšíření týmu budou aktualizované před udělením přístupu k podpisu.

- autor, committer a reviewer: [@hybernia1](https://github.com/hybernia1);
- approver každé žádosti o podpis: [@hybernia1](https://github.com/hybernia1);
- vlastnictví signing-sensitive souborů vynucuje [CODEOWNERS](../.github/CODEOWNERS).

Příspěvky lidí bez přímého commit oprávnění musí projít pull requestem, veřejným CI a review commitera. Změny release workflow, build skriptů, katalogů, signing policy a závislostí jsou signing-sensitive a vyžadují zvláštní kontrolu původu, oprávnění a dopadu. Účty s přístupem k repozitáři nebo SignPath musí používat vícefaktorové ověření. Každá žádost o podpis vyžaduje ruční approval; samotné vytvoření tagu nesmí podpis automaticky schválit.

## Rozsah podpisu

Projektový certifikát smí podepisovat pouze binárky vytvořené ze zdrojů tohoto repozitáře, zejména `PortableDeveloper.exe`. Nesmí se jím přepodepisovat Apache, PHP, MariaDB, Selenium, Java, Python, Notepad++, phpMyAdmin, WebDriver ani jiné komponenty třetích stran. Ty si zachovávají podpis, hash a licenci svého vydavatele.

## Ověřitelný původ

- repozitář: <https://github.com/hybernia1/portable-developer>;
- vlastník a současný autor releasu: [@hybernia1](https://github.com/hybernia1);
- každý podpis musí navazovat na veřejný commit nebo tag;
- sestavení používá připnuté .NET SDK z `global.json` a veřejný CI workflow;
- vstupy online i offline balíku musí mít zaznamenaný zdroj, verzi, licenci a SHA-256;
- podepsání releasu vyžaduje ruční schválení oprávněnou osobou a vícefaktorové ověření účtu.

Tagové workflow sestaví self-contained aplikaci z veřejného commitu, vytvoří ZIP a SHA-256 a publikuje je jako GitHub Release. Nepodepsaný stav nesmí být skryt ani zaměněn za důvěryhodnost certifikátu. Serverové moduly nejsou součástí malého online ZIPu; uživatel je stahuje z katalogově připnutých upstream zdrojů. Plný offline balík může zůstat neveřejný, dokud nebude dokončena samostatná právní kontrola redistribuce všech jeho komponent.

## Soukromí

Aplikace bez výslovné uživatelské akce nic nepřenáší autorům projektu. Úplný popis síťových funkcí je v [zásadách soukromí](../PRIVACY.md).

This program will not transfer any information to other networked systems unless specifically requested by the user or the person installing or operating it. Přesné uživatelské síťové akce a dotčené upstream služby popisují [zásady soukromí](../PRIVACY.md).

## Portable instalace a odebrání

Aplikace neinstaluje Windows služby, nemění systémový `PATH`, registr ani firewall. Všechny moduly se stahují až po explicitním kliknutí a ukládají se pod kořen portable složky. Odebrání spočívá v zastavení služeb, zavření aplikace a smazání její složky; uživatel musí předem zazálohovat vlastní `instances/`, pokud je chce zachovat.
