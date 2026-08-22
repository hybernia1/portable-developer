# Code signing policy

## Stav

Veřejné podepisování zatím není aktivní. Sestava 0.4.0 je nepodepsaná a Windows Smart App Control ji proto může zablokovat. Projekt nebude uživatelům doporučovat vypnutí této ochrany. Cílem je podepisovat veřejné release po schválení projektu SignPath Foundation.

Po schválení bude na projektu uvedeno: **Free code signing provided by SignPath.io, certificate by SignPath Foundation**.

## Rozsah podpisu

Projektový certifikát smí podepisovat pouze binárky vytvořené ze zdrojů tohoto repozitáře, zejména `PortableDeveloper.exe`. Nesmí se jím přepodepisovat Apache, PHP, MariaDB, Selenium, Java, Python, Notepad++, phpMyAdmin, WebDriver ani jiné komponenty třetích stran. Ty si zachovávají podpis, hash a licenci svého vydavatele.

## Ověřitelný původ

- repozitář: <https://github.com/hybernia1/portable-developer>;
- vlastník a současný autor releasu: [@hybernia1](https://github.com/hybernia1);
- každý podpis musí navazovat na veřejný commit nebo tag;
- sestavení používá připnuté .NET SDK z `global.json` a veřejný CI workflow;
- vstupy offline balíku musí mít zaznamenaný zdroj, verzi, licenci a SHA-256;
- podepsání releasu vyžaduje ruční schválení oprávněnou osobou a vícefaktorové ověření účtu.

Dokud není veřejný build offline závislostí plně automatizovaný a licenčně zkontrolovaný, CI ověřuje pouze zdrojový kód aplikace. Nepodepsané lokální balíky nejsou oficiální veřejné releasy.

## Soukromí

Aplikace bez výslovné uživatelské akce nic nepřenáší autorům projektu. Úplný popis síťových funkcí je v [zásadách soukromí](../PRIVACY.md).
