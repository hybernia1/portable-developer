# Portable Developer 0.6.0

První hotová binární verze s malým self-contained základem pro Windows x64 a správcem modulů přímo v aplikaci.

- Apache/PHP, MariaDB, Selenium, Composer, Python, Notepad++ a phpMyAdmin se instalují pouze po výslovné akci uživatele.
- Každý archiv pochází z připnutého HTTPS zdroje a před rozbalením musí odpovídat SHA-256 v katalogu vydání.
- Levé menu je rozdělené na prostředí, servery, vývoj a aplikaci. Stránka modulu se zobrazí až po jeho úspěšné instalaci a ověření.
- Aplikace je self-contained; .NET ani systémový Python nejsou potřeba.
- Portable VC++ runtime je přibalený pouze jako několik lokálních DLL a neinstaluje se do Windows.

Upozornění: vlastní `PortableDeveloper.exe` zatím není digitálně podepsaný. Windows Smart App Control nebo SmartScreen jej proto mohou blokovat. Projekt nedoporučuje kvůli spuštění vypínat ochranu Windows; veřejné podepisování přes SignPath Foundation připravujeme.

## Code signing policy

Pravidla, odpovědné role a ověřitelný původ sestavení popisuje veřejná [Code signing policy](https://github.com/hybernia1/portable-developer/blob/main/docs/CODE_SIGNING_POLICY.md). **Free code signing provided by [SignPath.io](https://signpath.io/), certificate by [SignPath Foundation](https://signpath.org/)** se bude vztahovat až na budoucí vydání, která budou výslovně označena jako podepsaná.
