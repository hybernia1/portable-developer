# Functional audit for 0.7.0

Průběžné funkční požadavky pro další verzi. V této fázi jde o návrh a vymezení bezpečnostních hranic, ne o implementaci.

## FUN-001: Bezpečné interní filesystem příkazy v portable terminálu

### Současný stav

Terminál používá explicitní whitelist a nevolá `cmd.exe` ani PowerShell. Aktuálně podporuje:

- `help`, `clear`, `pwd`;
- `ls` / `dir`, `cd`;
- `service status|start|stop|restart`;
- přibalené `php`, `composer`, `python`.

Blokované zůstávají absolutní cesty, opuštění aktivního projektového workspace, reparse pointy, pipes, redirection a shell chaining.

### Požadavek pro 0.7.0

- Přidat `mkdir <relative-directory>` jako interní příkaz implementovaný přímo přes .NET filesystem API.
- Příkaz smí pracovat pouze uvnitř aktuálního projektu a má zachovat současnou ochranu cest a reparse pointů.
- Relativní cesta může obsahovat bezpečné vnořené segmenty a mezery při použití existujícího uvozovkového parseru.
- Prázdná cesta, absolutní cesta, pokus o `..` mimo workspace, kolize se souborem a průchod přes odkaz musí skončit čitelnou chybou bez částečné změny mimo povolený prostor.
- Výsledek má jasně oznámit vytvořenou projektovou cestu; pracovní adresář terminálu se nemění.

### Kandidáti na další bezpečné příkazy

Před implementací samostatně potvrdit přesnou sadu. Vhodní kandidáti:

- `tree [relative-directory]` s limitem hloubky a počtu položek;
- `cat <relative-file>` pouze pro omezeně velké textové soubory;
- `stat <relative-path>` pro typ, velikost a čas změny;
- `which <php|composer|python>` pro zobrazení portable runtime a verze;
- `help <command>` pro detailní syntaxi konkrétního příkazu.

V první bezpečné sadě nepřidávat `rm`, `del`, `rmdir`, přepisující `copy/move`, libovolné spouštění EXE, wildcard mazání ani shellové operátory.

### Help a architektura pro budoucí headless režim

- Metadata příkazů mají tvořit společný registr: název, aliasy, usage, popis, kategorie a handler.
- Obecný `help` se má generovat z registru, aby se seznam příkazů nerozcházel s implementací.
- Parser, validace workspace, handlery a výsledkové modely musí zůstat nezávislé na WPF.
- GUI terminál a budoucí headless/CLI vstup mají volat stejnou aplikační službu.
- Řízení služeb nemá být dokončováno event handlerem v `MainWindow`; pro headless režim bude potřeba sdílený aplikační orchestrátor služeb.
- Textový výstup je vhodný pro člověka, ale headless vrstva má do budoucna umět převést strukturovaný výsledek také na stabilní návratový kód a případně JSON.

### Minimální testy

- vytvoření jedné a vnořené relativní složky;
- název s mezerou v uvozovkách;
- opakované vytvoření a kolize se souborem;
- zamítnutí absolutní cesty a úniku přes `..`;
- zamítnutí reparse pointu v kterémkoli existujícím segmentu;
- help obsahuje `mkdir` a odpovídá registru;
- žádný filesystem handler nespustí externí proces.

Stav: implementováno pro potvrzený rozsah 0.7.0. `mkdir` běží pouze přes .NET filesystem API, zachovává ochranu workspace a nespouští externí proces. Metadata whitelistu vystavuje aplikační služba a generuje z nich `help` i `help <command>`. Kandidáti `tree`, `cat`, `stat` a `which` zůstávají vědomě neimplementovaní do samostatného potvrzení rozsahu.
