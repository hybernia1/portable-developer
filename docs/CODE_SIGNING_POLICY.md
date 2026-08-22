# Code signing policy

## Stav

Veřejné podepisování zatím není aktivní. Od verze 0.6.0 projekt vydává hotové binární releasy i bez podpisu, ale každý takový ZIP, release manifest i poznámky jej musí jasně označit jako nepodepsaný. Windows Smart App Control nebo SmartScreen jej proto mohou zablokovat. Projekt nebude uživatelům doporučovat vypnutí této ochrany. Cílem zůstává podepisovat budoucí release po schválení projektu SignPath Foundation.

Po schválení bude na projektu uvedeno: **Free code signing provided by SignPath.io, certificate by SignPath Foundation**.

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
