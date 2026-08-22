# UI audit for 0.7.0

Průběžné poznámky z vizuální kontroly vydané verze 0.6.0 spuštěné z `E:\PortableDeveloper-win-x64-0.6.0`.

V této fázi jde pouze o záznam pozorování. Opravy se provedou společně pro další verzi.

## Projektový standard

- Další verze má používat jeden konzistentní layout napříč celou aplikací, ne sadu lokálních oprav jednotlivých stránek.
- Sdílené komponenty mají definovat alespoň stránkový nadpis, informační kartu, záložky, obsahovou kartu, formulářové řádky, akční tlačítka a aplikační dialogy.
- Rozestupy, výšky, typografie, barvy, stavy focus/hover/disabled a chování při změně jazyka mají pocházet ze společných stylů a tokenů.

## Nálezy

### UI-001: Nejednotná svislá mezera pod přepínacími kartami

- Stránka **Porty**: mezi spodní hranou přepínačů `Porty aplikace / Obsazené porty` a následující obsahovou kartou je přibližně 16 px.
- Stránka **PHP**: mezi spodní hranou přepínačů `Nastavení / Rozšíření` a následující obsahovou kartou jsou přibližně 3 px.
- Levé zarovnání přepínačů je na obou stránkách prakticky stejné; viditelný rozdíl je především ve svislém odsazení navazujícího obsahu.
- Požadovaný směr: zavést jeden sdílený standard komponenty záložek, včetně horního a spodního odsazení, výšky a návaznosti na obsahovou kartu.

Stav: implementováno. `SectionTabControlStyle` nyní vlastní hlavičku i jednotnou 14px mezeru; lokální horní mezery obsahu byly odstraněny.

### UI-004: Instalace Composer/Python knihoven nemá lokální ukazatel průběhu

- Po spuštění instalace se formulář pouze deaktivuje a uživatel čeká na konečný výsledek.
- Průběžný text se zapisuje do globálního stavového řádku u spodního okraje aplikace, mimo místo, kde uživatel operaci zahájil. Na velké stránce je snadno přehlédnutelný.
- `PackageManagerPageViewModel` aktuálně nabízí pouze `IsBusy` a `Status`; nemá fázi operace, režim průběhu, procenta, čas ani lokální detail.
- Downloader modulů už používá `RuntimePackageInstallProgress` a progress bar přímo v kartě. Composer a Python mají být vizuálně sjednocené se stejným systémem dlouhých operací.

Požadovaný směr:

- vytvořit sdílenou komponentu `OperationProgress`, použitelnou pro moduly, Composer, Python a později další dlouhé operace;
- podporovat determinate režim s procenty a indeterminate režim bez falešných procent;
- u Composeru/pip zobrazit nejméně fáze `Připravuji`, `Řeším závislosti`, `Stahuji`, `Instaluji`, `Obnovuji přehled` a konečný výsledek, pokud je lze spolehlivě odvodit;
- průběh zobrazit přímo pod akcí instalace, změnit text tlačítka na probíhající stav a ponechat jasně deaktivované konfliktní akce;
- případný živý výstup zobrazit v omezeném, rozbalitelném detailu a nepoužívat jej jako jediný ukazatel stavu;
- stejný model použít také pro odebrání a ruční obnovení seznamu balíčků;
- přidat přístupný textový stav, aby informace nebyla sdělena pouze animací nebo barvou.

Composer ani pip neposkytují vždy stabilní procentuální průběh. Dokud nebude k dispozici důvěryhodný údaj, použít indeterminate animaci a skutečné fáze místo simulovaných procent.

Architektonický dopad:

- rozšířit rozhraní projektových package managerů o `IProgress<ProjectPackageOperationProgress>` nebo ekvivalentní stream událostí;
- pokud má být vidět živý výstup procesu, doplnit bezpečné průběžné čtení stdout/stderr do command runneru místo čekání pouze na konečný `PortableCommandResult`;
- prezentační model průběhu držet společný pro Composer i Python, nikoli duplikovaný v event handlerech WPF.

Stav: implementováno. Composer a Python používají společný `ProjectPackageOperationProgress`, lokální indeterminate/determinate panel a přístupný text fáze i konečného výsledku. Živý výstup procesu zůstává samostatným budoucím rozšířením.

### UI-002: Mazání používá nativní Windows MessageBox mimo vzhled aplikace

- Potvrzení odstranění souboru se zobrazuje jako světlý systémový dialog nad tmavou aplikací.
- Dialog nepřebírá aplikační barvy, typografii, rozestupy, ikony ani styl tlačítek.
- Aktuální akce používají obecné popisky `Ano / Ne`; pro destruktivní operaci nejsou tak srozumitelné jako konkrétní `Smazat / Zrušit`.
- Český text je v kontrolované relaci čitelný, ale lokalizace a formulace dialogu nemají být závislé na systémovém MessageBoxu. Mají používat stejný lokalizační zdroj jako zbytek aplikace.
- Požadovaný směr: společná aplikační modal/dialog komponenta vizuálně shodná s formulářem `Nový soubor` a použitelná také pro novou složku, potvrzení, upozornění a chyby.
- Destruktivní varianta má mít jasný název položky, přesně popsaný rozsah operace, výraznou destruktivní akci, bezpečnou výchozí volbu `Zrušit`, podporu Escape a správné řízení focusu.
- Nativní MessageBox nepoužívat pro běžné uživatelské workflow uvnitř aplikace.
- Zdrojový audit potvrzuje čtyři přímá použití `MessageBox.Show` v `MainWindow.xaml.cs`; oprava proto musí pokrýt celý projekt, ne pouze správce souborů.
- Existující `NamePromptDialog` pro nový soubor, novou složku a přejmenování je vhodný vizuální základ. Má se zobecnit na sdílenou aplikační dialogovou komponentu místo vytvoření dalšího jednorázového okna.

Stav: implementováno. Všechna čtyři původní použití `MessageBox.Show` byla nahrazena `ConfirmationDialog`; existující `NamePromptDialog` zároveň používá stejné sdílené styly. Při kontrole nebyl `index.php` odstraněn.

### UI-003: Selecty používají výchozí světlý WPF vzhled

- Jazykový selector v sidebaru a projektové selecty na stránkách Composer a Soubory mají bílé pozadí, které vizuálně nepatří do tmavého rozhraní.
- Problém se týká zavřeného pole i rozbaleného popupu a položek; nestačí změnit pouze `Background` hlavního `ComboBox` prvku.
- Zdrojový audit potvrzuje tři `ComboBox` prvky v `MainWindow.xaml` a žádný společný `ComboBox` styl v `App.xaml`.
- Požadovaný směr: vytvořit jeden sdílený tmavý styl pro `ComboBox`, jeho toggle/šipku, popup, scrollbar a `ComboBoxItem`.
- Styl musí sjednotit normální, hover, selected, focus, open a disabled stav, výšku, vnitřní odsazení, border, focus indicator a kontrast textu.
- Jazykový a projektový selector mají používat stejnou komponentu; případné rozdíly ve šířce patří do layoutu stránky, ne do samostatných vizuálních stylů.

Stav: implementováno. Všechny tři selecty používají `AppComboBoxStyle`; vizuální smoke ověřil zavřený stav, tmavý popup i selected položku.
