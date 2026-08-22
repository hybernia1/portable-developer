using System.ComponentModel;
using System.Runtime.CompilerServices;
using PortableDeveloper.Application.Packages;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Application.Selenium;
using PortableDeveloper.Application.Settings;
using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.App.ViewModels;

public sealed class UiText : INotifyPropertyChanged
{
    private readonly IApplicationSettingsStore _settingsStore;
    private ApplicationLanguage _currentLanguage;

    public UiText(IApplicationSettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
        _currentLanguage = settingsStore.Load().Language;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ApplicationLanguage CurrentLanguage => _currentLanguage;

    public string ApplicationTitle => "Portable Developer";

    public string Subtitle => IsCzech ? "Přenosné lokální vývojové prostředí" : "Portable local development environment";

    public string NavigationLabel(NavigationPage page) => page switch
    {
        NavigationPage.Dashboard => IsCzech ? "Přehled" : "Dashboard",
        NavigationPage.Modules => IsCzech ? "Moduly" : "Modules",
        NavigationPage.Php => "PHP",
        NavigationPage.Apache => "Apache",
        NavigationPage.Databases => IsCzech ? "Databáze" : "Databases",
        NavigationPage.Selenium => "Selenium",
        NavigationPage.Ports => IsCzech ? "Porty" : "Ports",
        NavigationPage.Composer => "Composer",
        NavigationPage.Python => "Python",
        NavigationPage.Terminal => IsCzech ? "Terminál" : "Terminal",
        NavigationPage.Files => IsCzech ? "Soubory" : "Files",
        NavigationPage.Tools => IsCzech ? "Nástroje" : "Tools",
        NavigationPage.Settings => IsCzech ? "Nastavení" : "Settings",
        _ => page.ToString()
    };

    public string PageTitle(NavigationPage page) => page switch
    {
        NavigationPage.Dashboard => IsCzech ? "Přehled prostředí" : "Environment overview",
        NavigationPage.Modules => IsCzech ? "Správce modulů" : "Module manager",
        NavigationPage.Php => IsCzech ? "PHP runtime" : "PHP runtime",
        NavigationPage.Apache => IsCzech ? "Apache server" : "Apache server",
        NavigationPage.Databases => IsCzech ? "Databáze" : "Databases",
        NavigationPage.Selenium => "Selenium Server",
        NavigationPage.Ports => IsCzech ? "Správce portů" : "Port manager",
        NavigationPage.Composer => IsCzech ? "Composer balíčky" : "Composer packages",
        NavigationPage.Python => IsCzech ? "Python balíčky" : "Python packages",
        NavigationPage.Terminal => IsCzech ? "Portable terminál" : "Portable terminal",
        NavigationPage.Files => IsCzech ? "Soubory projektu" : "Project files",
        NavigationPage.Tools => IsCzech ? "Portable nástroje" : "Portable tools",
        NavigationPage.Settings => IsCzech ? "Nastavení aplikace" : "Application settings",
        _ => page.ToString()
    };

    public string NavigationGroup(int groupOrder) => groupOrder switch
    {
        0 => IsCzech ? "PROSTŘEDÍ" : "ENVIRONMENT",
        1 => IsCzech ? "SERVERY" : "SERVERS",
        2 => IsCzech ? "VÝVOJ" : "DEVELOPMENT",
        _ => IsCzech ? "APLIKACE" : "APPLICATION"
    };

    public string ModulesIntroduction => IsCzech
        ? "Nainstalujte jen části prostředí, které skutečně používáte. Aplikace přijme pouze HTTPS soubory z přibaleného verzovaného katalogu a před rozbalením ověří jejich SHA-256."
        : "Install only the parts of the environment you use. The application accepts only HTTPS files from its bundled versioned catalog and verifies their SHA-256 before extraction.";

    public string ModulesPortableNotice => IsCzech
        ? "Moduly zůstávají uvnitř této složky. Aplikace neinstaluje Windows služby, nemění systémový PATH ani registr."
        : "Modules remain inside this folder. The application does not install Windows services or change the system PATH or registry.";

    public string NoModulesDashboard => IsCzech
        ? "Zatím není nainstalovaná žádná serverová část. Otevřete Moduly a vyberte si pouze prostředí, které potřebujete."
        : "No server component is installed yet. Open Modules and choose only the environment you need.";

    public string RuntimePackageName(RuntimePackageKind kind) => kind switch
    {
        RuntimePackageKind.WebStack => IsCzech ? "Webový stack" : "Web stack",
        RuntimePackageKind.Database => IsCzech ? "Databáze" : "Database",
        RuntimePackageKind.Selenium => "Selenium",
        RuntimePackageKind.Composer => "Composer",
        RuntimePackageKind.Python => "Python",
        RuntimePackageKind.Editor => IsCzech ? "Editor" : "Editor",
        RuntimePackageKind.PhpMyAdmin => "phpMyAdmin",
        RuntimePackageKind.SeleniumEdgeDriver => "Microsoft Edge WebDriver",
        RuntimePackageKind.SeleniumChromeDriver => "ChromeDriver",
        RuntimePackageKind.SeleniumFirefoxDriver => "geckodriver",
        _ => kind.ToString()
    };

    public string RuntimePackageDescription(RuntimePackageKind kind) => kind switch
    {
        RuntimePackageKind.WebStack => IsCzech ? "Apache a PHP pro lokální webové projekty." : "Apache and PHP for local web projects.",
        RuntimePackageKind.Database => IsCzech ? "Přenosný MariaDB server a lokální databáze." : "Portable MariaDB server and local databases.",
        RuntimePackageKind.Selenium => IsCzech ? "Selenium Server a vlastní portable Java runtime; ovladač prohlížeče si vyberete zvlášť." : "Selenium Server and its portable Java runtime; choose a browser driver separately.",
        RuntimePackageKind.Composer => IsCzech ? "Správa PHP knihoven; chybějící PHP se doplní automaticky." : "PHP dependency management; missing PHP is added automatically.",
        RuntimePackageKind.Python => IsCzech ? "Přenosný Python s projektovou správou knihoven." : "Portable Python with project package management.",
        RuntimePackageKind.Editor => IsCzech ? "Lehký portable Notepad++ propojený se správcem souborů." : "Lightweight portable Notepad++ integrated with the file manager.",
        RuntimePackageKind.PhpMyAdmin => IsCzech ? "Webová správa databází včetně potřebného webového stacku a MariaDB." : "Web database administration including the required web stack and MariaDB.",
        RuntimePackageKind.SeleniumEdgeDriver => IsCzech ? "Ověřený driver pro konkrétní vydání Microsoft Edge. Verze musí odpovídat buildu prohlížeče." : "Verified driver for a specific Microsoft Edge release. Its version must match the browser build.",
        RuntimePackageKind.SeleniumChromeDriver => IsCzech ? "Ověřený ChromeDriver z oficiálního katalogu Chrome for Testing." : "Verified ChromeDriver from the official Chrome for Testing catalog.",
        RuntimePackageKind.SeleniumFirefoxDriver => IsCzech ? "Ověřený geckodriver pro Mozilla Firefox." : "Verified geckodriver for Mozilla Firefox.",
        _ => string.Empty
    };

    public string DownloadAndInstall => IsCzech ? "Stáhnout a nainstalovat" : "Download and install";

    public string Installed => IsCzech ? "Nainstalováno" : "Installed";

    public string PackageInstalledAndVerified => IsCzech ? "Nainstalováno a ověřeno" : "Installed and verified";

    public string PackageMissingComponents => IsCzech ? "Připraveno ke stažení" : "Ready to download";

    public string PackageInstallProgress(RuntimePackageInstallProgress progress) => progress.Stage switch
    {
        RuntimePackageInstallStage.Preparing => IsCzech ? "Připravuji instalaci…" : "Preparing installation…",
        RuntimePackageInstallStage.Downloading => IsCzech ? $"Stahuji {progress.ComponentName}…" : $"Downloading {progress.ComponentName}…",
        RuntimePackageInstallStage.Verifying => IsCzech ? $"Ověřuji {progress.ComponentName}…" : $"Verifying {progress.ComponentName}…",
        RuntimePackageInstallStage.Extracting => IsCzech ? $"Rozbaluji {progress.ComponentName}…" : $"Extracting {progress.ComponentName}…",
        RuntimePackageInstallStage.Installing => IsCzech ? "Dokončuji portable instalaci…" : "Finishing portable installation…",
        RuntimePackageInstallStage.Completed => PackageInstalledAndVerified,
        _ => PackageMissingComponents
    };

    public string PackageInstallFailed(string detail) => IsCzech
        ? $"Instalace modulu selhala: {detail}"
        : $"Module installation failed: {detail}";

    public string PackageInstallSucceeded(string name) => IsCzech
        ? $"Modul {name} je nainstalovaný a připravený."
        : $"Module {name} is installed and ready.";

    public string WebStack => IsCzech ? "WEBOVÝ STACK" : "WEB STACK";

    public string TechnicalDetails => IsCzech ? "Technické informace" : "Technical information";

    public string ServiceControl => IsCzech ? "Ovládání služby" : "Service control";

    public string CurrentConfiguration => IsCzech ? "Aktuální konfigurace" : "Current configuration";

    public string PlannedConfiguration => IsCzech ? "Připravovaná konfigurace" : "Planned configuration";

    public string Planned => IsCzech ? "Plánováno" : "Planned";

    public string PackageRuntime => IsCzech ? "Portable runtime" : "Portable runtime";

    public string ProjectDirectory => IsCzech ? "Složka projektu" : "Project directory";

    public string TerminalHelp => IsCzech
        ? "Pište přímo do konzole a potvrďte Enterem; šipky nahoru a dolů procházejí historii. Omezený shell používá jen přibalené PHP, Composer a Python, nevolá cmd.exe ani PowerShell a zůstává uvnitř webového projektu. Nápovědu zobrazí příkaz help."
        : "Type directly in the console and press Enter; Up and Down browse command history. The restricted shell uses only bundled PHP, Composer, and Python, does not invoke cmd.exe or PowerShell, and stays inside the web project. Type help for commands.";

    public string RunCommand => IsCzech ? "Spustit" : "Run";

    public string ClearTerminal => IsCzech ? "Vyčistit" : "Clear";

    public string OverviewTab => IsCzech ? "Přehled" : "Overview";

    public string SettingsTab => IsCzech ? "Nastavení" : "Settings";

    public string ExtensionsTab => IsCzech ? "Rozšíření" : "Extensions";

    public string AdvancedTab => IsCzech ? "Pokročilé" : "Advanced";

    public string DatabasesTab => IsCzech ? "Databáze" : "Databases";

    public string AccessTab => IsCzech ? "Připojení a účet" : "Connection and account";

    public string AdministrationTab => IsCzech ? "Webová správa" : "Web administration";

    public string ApplicationPortsTab => IsCzech ? "Porty aplikace" : "Application ports";

    public string ListeningPortsTab => IsCzech ? "Obsazené porty" : "Listening ports";

    public string CentralPortManager => IsCzech ? "Centrální nastavení portů" : "Central port settings";

    public string PortManagerHelp => IsCzech
        ? "Porty 1024–65535 jsou společné pro všechny části aplikace. Uložení je možné pouze při zastavených službách a jen tehdy, když vybrané porty nepoužívá jiný proces."
        : "Ports 1024–65535 are shared by all application components. They can only be saved while services are stopped and when no other process is using the selected ports.";

    public string PortReadOnlyNotice => IsCzech
        ? "Seznam je pouze čtecí snímek TCP listenerů ve Windows. Portable Developer cizí procesy nezastavuje, nemění jejich konfiguraci ani neuvolňuje jejich porty."
        : "This is a read-only snapshot of TCP listeners in Windows. Portable Developer never stops external processes, changes their configuration, or releases their ports.";

    public string ApacheHttpPort => "Apache HTTP";

    public string PhpFastCgiPortLabel => "PHP FastCGI";

    public string MariaDbPortLabel => "MariaDB";

    public string SeleniumPortLabel => "Selenium";

    public string PortAvailable => IsCzech ? "Volný" : "Available";

    public string PortOccupied => IsCzech ? "Obsazený jiným procesem" : "Occupied by another process";

    public string PortInvalid => IsCzech ? "Neplatný port" : "Invalid port";

    public string PortDuplicate => IsCzech ? "Duplicitní port aplikace" : "Duplicate application port";

    public string PortUsedByApplication => IsCzech ? "Používá Portable Developer" : "Used by Portable Developer";

    public string PortSettingsReady => IsCzech
        ? "Služby jsou zastavené; porty lze upravit."
        : "Services are stopped; ports can be edited.";

    public string PortSettingsRequireStoppedServices => IsCzech
        ? "Před změnou portů zastavte Apache/PHP, MariaDB i Selenium."
        : "Stop Apache/PHP, MariaDB, and Selenium before changing ports.";

    public string RefreshPortList => IsCzech ? "Obnovit obsazené porty" : "Refresh occupied ports";

    public string SavePorts => IsCzech ? "Uložit porty" : "Save ports";

    public string PortsSaved => IsCzech ? "Porty byly uloženy." : "Ports were saved.";

    public string PortsInvalid => IsCzech
        ? "Zadejte čtyři různé porty v rozsahu 1024–65535."
        : "Enter four different ports in the 1024–65535 range.";

    public string PortsOccupied(IEnumerable<int> ports) => IsCzech
        ? $"Nelze uložit: porty {string.Join(", ", ports)} již používá jiný proces."
        : $"Cannot save: ports {string.Join(", ", ports)} are already used by another process.";

    public string PortScanFailed(string detail) => IsCzech
        ? $"Obsazené porty se nepodařilo načíst: {detail}"
        : $"Occupied ports could not be loaded: {detail}";

    public string TcpListenerCount(int count) => IsCzech ? $"TCP listenery: {count}" : $"TCP listeners: {count}";

    public string TcpListenerEndpoint(string address, int port) => $"{address}:{port}";

    public string LocalAddress => IsCzech ? "Lokální adresa" : "Local address";

    public string PortStatus => IsCzech ? "Stav portu" : "Port status";

    public string ManagedOnPortsPage => IsCzech
        ? "Port Selenium se spravuje centrálně na stránce Porty."
        : "The Selenium port is managed centrally on the Ports page.";

    public string TerminalCommand => IsCzech ? "Terminálová konzole" : "Terminal console";

    public string FileManagerHelp => IsCzech
        ? "Soubory aktuálního webového projektu. Dvojklik otevře složku nebo soubor v Notepad++."
        : "Files of the active web project. Double-click opens a folder or a file in Notepad++.";

    public string CurrentFolder => IsCzech ? "Aktuální složka" : "Current folder";

    public string Up => IsCzech ? "Nahoru" : "Up";

    public string Back => IsCzech ? "Zpět" : "Back";

    public string RefreshFiles => IsCzech ? "Obnovit" : "Refresh";

    public string NewItemName => IsCzech ? "Název nové položky" : "New item name";

    public string NewFile => IsCzech ? "Nový soubor" : "New file";

    public string NewFolder => IsCzech ? "Nová složka" : "New folder";

    public string Folder => IsCzech ? "Složka" : "Folder";

    public string File => IsCzech ? "Soubor" : "File";

    public string Open => IsCzech ? "Otevřít" : "Open";

    public string Edit => IsCzech ? "Upravit" : "Edit";

    public string Rename => IsCzech ? "Přejmenovat" : "Rename";

    public string Delete => IsCzech ? "Smazat" : "Delete";

    public string EmptyFolder => IsCzech ? "Složka je prázdná." : "The folder is empty.";

    public string Name => IsCzech ? "Název" : "Name";

    public string Size => IsCzech ? "Velikost" : "Size";

    public string Modified => IsCzech ? "Změněno" : "Modified";

    public string Actions => IsCzech ? "Akce" : "Actions";

    public string CreateFileTitle => IsCzech ? "Nový soubor" : "New file";

    public string CreateFolderTitle => IsCzech ? "Nová složka" : "New folder";

    public string RenameItemTitle => IsCzech ? "Přejmenovat položku" : "Rename item";

    public string EnterFileName => IsCzech ? "Zadejte název nového souboru." : "Enter the new file name.";

    public string EnterFolderName => IsCzech ? "Zadejte název nové složky." : "Enter the new folder name.";

    public string EnterNewName => IsCzech ? "Zadejte nový název položky." : "Enter the new item name.";

    public string Confirm => IsCzech ? "Potvrdit" : "Confirm";

    public string Cancel => IsCzech ? "Zrušit" : "Cancel";

    public string WorkspaceItemNameRequired => IsCzech
        ? "Nejdříve zadejte platný název souboru nebo složky."
        : "Enter a valid file or directory name first.";

    public string RenameItemQuestion(string name) => IsCzech
        ? $"Zadejte nový název položky {name} do pole nahoře a potvrďte přejmenování."
        : $"Enter a new name for {name} in the field above and confirm rename.";

    public string DeleteItemQuestion(string name) => IsCzech
        ? $"Opravdu smazat {name}? U neprázdné složky se smaže celý její obsah."
        : $"Delete {name}? A non-empty folder and all its contents will be removed.";

    public string DeleteItemTitle => IsCzech ? "Smazání položky projektu" : "Delete project item";

    public string WorkspaceOperationFailed(string detail) => IsCzech
        ? $"Operace se souborem selhala: {detail}"
        : $"File operation failed: {detail}";

    public string OpenProjectDirectory => IsCzech ? "Otevřít projekt" : "Open project";

    public string PortableEditor => "Notepad++";

    public string PortableEditorHelp => IsCzech
        ? "Lehký editor běží přímo z portable složky. Neukládá nastavení do profilu Windows a neobsahuje automatický updater."
        : "The lightweight editor runs directly from the portable folder. It does not store settings in the Windows profile and has no automatic updater.";

    public string StartEditor => IsCzech ? "Spustit editor" : "Start editor";

    public string EditCustomPhpIni => IsCzech ? "Upravit vlastní php.ini" : "Edit custom php.ini";

    public string CustomPhpIni => IsCzech ? "Vlastní PHP konfigurace" : "Custom PHP configuration";

    public string CustomPhpIniHelp => IsCzech
        ? "Soubor se připojí za bezpečně generovaný php.ini při každém startu. Ruční direktivy mohou přepsat hodnoty z formuláře a použijí se až po restartu webového stacku."
        : "This file is appended after the safely generated php.ini on every start. Manual directives can override form values and take effect after restarting the web stack.";

    public string EditorStarted => IsCzech ? "Portable editor byl spuštěn." : "The portable editor was started.";

    public string VerifiedPortableEditor(string version) => IsCzech
        ? $"Ověřený portable editor {version}."
        : $"Verified portable editor {version}.";

    public string EditorStartFailed(string detail) => IsCzech
        ? $"Portable editor se nepodařilo spustit: {detail}"
        : $"The portable editor could not be started: {detail}";

    public string InstalledPackages => IsCzech ? "Nainstalované knihovny" : "Installed packages";

    public string NoInstalledPackages => IsCzech
        ? "V tomto projektu zatím nejsou nainstalované žádné knihovny."
        : "No libraries are installed in this project yet.";

    public string AddPackage => IsCzech ? "Přidat knihovnu" : "Add package";

    public string PackageName => IsCzech ? "Název balíčku" : "Package name";

    public string VersionConstraint => IsCzech ? "Verze / omezení (volitelné)" : "Version / constraint (optional)";

    public string InstallPackage => IsCzech ? "Nainstalovat" : "Install";

    public string RemovePackage => IsCzech ? "Odebrat" : "Remove";

    public string DirectDependency => IsCzech ? "Přímá závislost" : "Direct dependency";

    public string TransitiveDependency => IsCzech ? "Závislost jiné knihovny" : "Transitive dependency";

    public string ComposerHelp => IsCzech
        ? "Balíčky se instalují do vendor aktuálního projektu. Každý projekt má vlastní composer.json a závislosti."
        : "Packages are installed into the active project's vendor directory. Every project has its own composer.json and dependencies.";

    public string ComposerPackageExample => "php-webdriver/webdriver";

    public string ComposerConstraintExample => IsCzech ? "např. ^1.15" : "e.g. ^1.15";

    public string PythonHelp => IsCzech
        ? "Projektové balíčky se instalují do instances/default/python/packages. Základní Python ani systémový profil Windows se nemění."
        : "Project packages are installed into instances/default/python/packages. The base Python runtime and Windows user profile are not modified.";

    public string PythonPackageExample => IsCzech ? "např. selenium" : "e.g. selenium";

    public string PythonConstraintExample => IsCzech ? "např. ==4.35.0" : "e.g. ==4.35.0";

    public string PackageNetworkNotice => IsCzech
        ? "Instalace a odebrání jsou explicitní uživatelské akce. Mohou používat internet a spouštět instalační logiku balíčku; vybírejte jen důvěryhodné knihovny."
        : "Install and remove are explicit user actions. They may use the internet and execute package installation logic; choose trusted libraries only.";

    public string RefreshPackages => IsCzech ? "Obnovit přehled" : "Refresh packages";

    public string LoadingPackages => IsCzech ? "Načítám nainstalované knihovny…" : "Loading installed packages…";

    public string InstallingPackage => IsCzech ? "Instaluji knihovnu…" : "Installing package…";

    public string RemovingPackage => IsCzech ? "Odebírám knihovnu…" : "Removing package…";

    public string PackageOperationProgress(ProjectPackageOperationProgress progress) =>
        (progress.Operation, progress.Phase) switch
        {
            (_, ProjectPackageOperationPhase.Preparing) =>
                IsCzech ? "Připravuji operaci s knihovnou…" : "Preparing package operation…",
            (ProjectPackageOperationKind.Install, ProjectPackageOperationPhase.RunningPackageManager) =>
                IsCzech ? "Řeším závislosti a instaluji knihovnu…" : "Resolving dependencies and installing package…",
            (ProjectPackageOperationKind.Remove, ProjectPackageOperationPhase.RunningPackageManager) =>
                IsCzech ? "Odebírám knihovnu a upravuji závislosti…" : "Removing package and updating dependencies…",
            (_, ProjectPackageOperationPhase.RefreshingInventory) => LoadingPackages,
            (ProjectPackageOperationKind.Refresh, ProjectPackageOperationPhase.Completed) =>
                IsCzech ? "Přehled knihoven je aktuální." : "Package inventory is up to date.",
            (_, ProjectPackageOperationPhase.Completed) =>
                IsCzech ? "Operace správce balíčků byla dokončena." : "Package manager operation completed.",
            _ => IsCzech ? "Probíhá operace s knihovnou…" : "Package operation in progress…"
        };

    public string PackageListFailed(string detail) => IsCzech
        ? $"Přehled knihoven se nepodařilo načíst: {detail}"
        : $"The package list could not be loaded: {detail}";

    public string PackageOperationFailed(string detail) => IsCzech
        ? $"Operace s knihovnou selhala: {detail}"
        : $"The package operation failed: {detail}";

    public string PackageInstalled(string name) => IsCzech
        ? $"Knihovna {name} byla nainstalována."
        : $"Package {name} was installed.";

    public string PackageRemoved(string name) => IsCzech
        ? $"Knihovna {name} byla odebrána."
        : $"Package {name} was removed.";

    public string RemovePackageQuestion(string name) => IsCzech
        ? $"Opravdu odebrat knihovnu {name} z tohoto projektu?"
        : $"Remove package {name} from this project?";

    public string RemovePackageTitle => IsCzech ? "Odebrání knihovny" : "Remove package";

    public string PhpSettings => IsCzech ? "Nastavení php.ini" : "php.ini settings";

    public string PhpSettingsHelp => IsCzech
        ? "Hodnoty se ukládají k portable instanci. php.ini se z nich znovu vytvoří při každém startu webového stacku."
        : "Values are stored with the portable instance. php.ini is regenerated from them whenever the web stack starts.";

    public string MemoryLimit => "memory_limit (MB)";

    public string UploadLimit => "upload_max_filesize (MB)";

    public string PostLimit => "post_max_size (MB)";

    public string ExecutionTime => IsCzech ? "max_execution_time (sekundy)" : "max_execution_time (seconds)";

    public string MaximumInputVariables => "max_input_vars";

    public string DisplayErrors => IsCzech ? "Zobrazovat chyby ve výstupu" : "Display errors in output";

    public string DisplayErrorsHelp => IsCzech
        ? "Chyby se vždy zapisují do instances/default/logs/php-error.log. Zobrazení ve stránce je vhodné jen pro lokální vývoj."
        : "Errors are always logged to instances/default/logs/php-error.log. Displaying them in the page is suitable only for local development.";

    public string PhpExtensions => IsCzech ? "PHP rozšíření" : "PHP extensions";

    public string PhpExtensionsHelp => IsCzech
        ? "Zapnout lze pouze rozšíření přibalená v ověřeném PHP modulu. Nedostupné volby zůstanou vypnuté."
        : "Only extensions bundled with the verified PHP module can be enabled. Unavailable options remain disabled.";

    public string RequiredPhpExtensions => IsCzech
        ? "Povinná rozšíření mbstring, mysqli, openssl a zip jsou vždy aktivní."
        : "Required extensions mbstring, mysqli, openssl, and zip are always enabled.";

    public string SavePhpSettings => IsCzech ? "Uložit PHP nastavení" : "Save PHP settings";

    public string SaveAndRestartPhp => IsCzech ? "Uložit a restartovat Apache/PHP" : "Save and restart Apache/PHP";

    public string ResetDefaults => IsCzech ? "Výchozí hodnoty" : "Default values";

    public string PhpSettingsInvalid => IsCzech
        ? "Zkontrolujte rozsahy: paměť 32–8192 MB, upload 1–2048 MB, POST 1–4096 MB, timeout 0–3600 s a max_input_vars 100–100000. POST limit nesmí být menší než upload."
        : "Check the ranges: memory 32–8192 MB, upload 1–2048 MB, POST 1–4096 MB, timeout 0–3600 s, and max_input_vars 100–100000. The POST limit cannot be smaller than the upload limit.";

    public string PhpSettingsSaved(ManagedProcessState stackState) => stackState == ManagedProcessState.Running
        ? IsCzech
            ? "PHP nastavení bylo uloženo a webová služba byla restartována."
            : "PHP settings were saved and the web service was restarted."
        : IsCzech
            ? "PHP nastavení bylo uloženo a použije se při příštím startu webového stacku."
            : "PHP settings were saved and will be used the next time the web stack starts.";

    public string PhpSettingsSaveFailed(string detail) => IsCzech
        ? $"PHP nastavení se nepodařilo uložit: {detail}"
        : $"PHP settings could not be saved: {detail}";

    public string PhpDefaultsPrepared => IsCzech
        ? "Výchozí hodnoty jsou připravené ve formuláři. Potvrďte je tlačítkem Uložit PHP nastavení."
        : "Default values are ready in the form. Confirm them with Save PHP settings.";

    public string WebProjectsTab => IsCzech ? "Webové projekty" : "Web projects";

    public string AddWebProject => IsCzech ? "Přidat webový projekt" : "Add web project";

    public string WebProjectsHelp => IsCzech
        ? "Projekt dostane vlastní kořen, Composer vendor a adresu projekt.localhost. Apache použije zadanou podsložku jako document root a nezapisuje do Windows hosts."
        : "The project gets its own root, Composer vendor, and project.localhost address. Apache uses the selected subdirectory as document root and does not modify Windows hosts.";

    public string ProjectName => IsCzech ? "Název projektu" : "Project name";

    public string WebRootInsideProject => IsCzech ? "Web root uvnitř projektu" : "Web root inside project";

    public string WebRootExample => IsCzech
        ? "Doporučeno: public. Tečka (.) zpřístupní celý projekt."
        : "Recommended: public. A dot (.) exposes the whole project.";

    public string CreateProject => IsCzech ? "Vytvořit projekt" : "Create project";

    public string ConfiguredWebProjects => IsCzech ? "Nakonfigurované projekty" : "Configured projects";

    public string ActiveProject => IsCzech ? "Aktivní projekt nástrojů" : "Active tools project";

    public string UseProject => IsCzech ? "Použít" : "Use";

    public string ActiveProjectBadge => IsCzech ? "Aktivní" : "Active";

    public string DefaultProjectName => IsCzech ? "Výchozí" : "Default";

    public string Enabled => IsCzech ? "povoleno" : "enabled";

    public string Disabled => IsCzech ? "vypnuto" : "disabled";

    public string EnableHtaccess => IsCzech ? "Povolit .htaccess" : "Enable .htaccess";

    public string DisableHtaccess => IsCzech ? "Vypnout .htaccess" : "Disable .htaccess";

    public string EnableInApache => IsCzech ? "Zapnout v Apache" : "Enable in Apache";

    public string DisableInApache => IsCzech ? "Vypnout v Apache" : "Disable in Apache";

    public string RemoveProject => IsCzech ? "Odebrat projekt" : "Remove project";

    public string ApacheConfigurationSaved => IsCzech
        ? "Konfigurace Apache byla uložena."
        : "Apache configuration was saved.";

    public string ProjectSelected(string name) => IsCzech
        ? $"Aktivní projekt: {name}."
        : $"Active project: {name}.";

    public string ProjectCreated(string name) => IsCzech
        ? $"Projekt {name} byl vytvořen a konfigurace Apache aktualizována."
        : $"Project {name} was created and the Apache configuration was updated.";

    public string ProjectRemoved(string name) => IsCzech
        ? $"Projekt {name} byl odebrán z konfigurace; jeho soubory zůstaly zachované."
        : $"Project {name} was removed from the configuration; its files were preserved.";

    public string ProjectOperationFailed(string detail) => IsCzech
        ? $"Operace s projektem selhala: {detail}"
        : $"Project operation failed: {detail}";

    public string ProjectChangeBusy => IsCzech
        ? "Projekt nelze přepnout, vytvořit ani odebrat během běžící operace Composeru nebo terminálu."
        : "A project cannot be selected, created, or removed while a Composer or terminal operation is running.";

    public string RemoveProjectQuestion(string name) => IsCzech
        ? $"Odebrat projekt {name} z Apache a seznamu projektů? Soubory na disku se nesmažou."
        : $"Remove project {name} from Apache and the project list? Files on disk will not be deleted.";

    public string DatabaseManagementPlan => IsCzech
        ? "Po doplnění MariaDB controlleru zde půjde vytvářet a mazat lokální databáze. První verze používá pouze účet root."
        : "After the MariaDB controller is added, this page will create and remove local databases. The first version uses only the root account.";

    public string SeleniumSettings => IsCzech ? "Nastavení serveru" : "Server settings";

    public string MaximumSessions => IsCzech ? "Maximum souběžných relací" : "Maximum concurrent sessions";

    public string SessionTimeout => IsCzech ? "Limit neaktivity relace (sekundy)" : "Session inactivity timeout (seconds)";

    public string SessionTimeoutHelp => IsCzech
        ? "Relace bez WebDriver příkazu po tuto dobu bude Selenium automaticky ukončena. Rozsah 30–86400 sekund."
        : "Selenium automatically terminates a session with no WebDriver command for this period. Range: 30–86400 seconds.";

    public string SaveSeleniumSettings => IsCzech ? "Uložit nastavení" : "Save settings";

    public string SeleniumSettingsSaved => IsCzech
        ? "Nastavení Selenium bylo uloženo a použije se při příštím startu."
        : "Selenium settings were saved and will be used on the next start.";

    public string SeleniumSettingsInvalid => IsCzech
        ? "Zadejte port 1024–65535, 1–32 relací a timeout 30–86400 sekund."
        : "Enter port 1024–65535, 1–32 sessions, and a timeout of 30–86400 seconds.";

    public string SeleniumDrivers => IsCzech ? "Ovladače prohlížečů" : "Browser drivers";

    public string SeleniumDriversHelp => IsCzech
        ? "Selenium se instaluje bez driveru. Níže stáhněte ověřený Edge, Chrome nebo Firefox driver. Vlastní geckodriver.exe, chromedriver.exe či msedgedriver.exe lze dál vložit do drivers/custom. Pro Chrome a Edge musí verze driveru odpovídat verzi prohlížeče."
        : "Selenium installs without a driver. Download a verified Edge, Chrome, or Firefox driver below. You can still add a custom geckodriver.exe, chromedriver.exe, or msedgedriver.exe under drivers/custom. Chrome and Edge driver versions must match the browser version.";

    public string SeleniumDriverCatalog => IsCzech ? "Katalog driverů" : "Driver catalog";

    public string InstalledSeleniumDrivers => IsCzech ? "Aktivní drivery" : "Active drivers";

    public string SeleniumProfiles => IsCzech ? "Profily" : "Profiles";

    public string SeleniumProfileMasters => IsCzech ? "Master profily" : "Master profiles";

    public string SeleniumProfilesHelp => IsCzech
        ? "Importovaný master zůstává pouze ke čtení. Relace s capability portable:profile dostane vlastní pracovní kopii, která se po ukončení smaže. Před importem zavřete prohlížeč používající zdrojový profil."
        : "An imported master remains read-only. A session using the portable:profile capability receives its own working copy, which is removed when the session ends. Close the browser using the source profile before importing it.";

    public string ProfileName => IsCzech ? "Název profilu" : "Profile name";

    public string ProfileSource => IsCzech ? "Zdrojová složka profilu" : "Profile source directory";

    public string SelectProfileFolder => IsCzech ? "Vybrat složku" : "Select folder";

    public string ImportProfile => IsCzech ? "Importovat master" : "Import master";

    public string NoSeleniumProfiles => IsCzech ? "Zatím není importovaný žádný master profil." : "No master profile has been imported yet.";

    public string SeleniumProfileCount(int count) => IsCzech ? $"Master profily: {count}" : $"Master profiles: {count}";

    public string SeleniumProfileBrowserLabel(SeleniumProfileBrowser browser) => browser switch
    {
        SeleniumProfileBrowser.Edge => "Microsoft Edge",
        SeleniumProfileBrowser.Chrome => "Google Chrome",
        SeleniumProfileBrowser.Firefox => "Mozilla Firefox",
        _ => browser.ToString()
    };

    public string SeleniumProfileImported(string name) => IsCzech ? $"Profil {name} byl bezpečně importován." : $"Profile {name} was imported safely.";

    public string SeleniumProfileImportFailed(string detail) => IsCzech ? $"Import profilu selhal: {detail}" : $"Profile import failed: {detail}";

    public string RemoveSeleniumProfileTitle => IsCzech ? "Odebrání master profilu" : "Remove master profile";

    public string RemoveSeleniumProfileQuestion(string name) => IsCzech
        ? $"Opravdu odebrat master profil {name}? Zdrojový profil mimo aplikaci zůstane beze změny."
        : $"Remove master profile {name}? The original profile outside the application will remain unchanged.";

    public string SeleniumProfileRemoved => IsCzech ? "Master profil byl odebrán." : "The master profile was removed.";

    public string OpenDriversFolder => IsCzech ? "Otevřít složku driverů" : "Open drivers folder";

    public string ReloadDrivers => IsCzech ? "Načíst drivery" : "Reload drivers";

    public string VerifiedBundledDriver => IsCzech ? "Přibalený a ověřený" : "Bundled and verified";

    public string CustomDriver => IsCzech ? "Vlastní – bez ověření hashe" : "Custom — hash not verified";

    public string SeleniumDriverCount(int count) => IsCzech ? $"Načtené drivery: {count}" : $"Loaded drivers: {count}";

    public string SeleniumSessions => IsCzech ? "Běžící relace" : "Running sessions";

    public string SeleniumSessionCount(int count, int maximum) => IsCzech
        ? $"Aktivní relace: {count} / {maximum}"
        : $"Active sessions: {count} / {maximum}";

    public string NoSeleniumSessions => IsCzech ? "Momentálně neběží žádná relace." : "No sessions are currently running.";

    public string OpenSeleniumHub => IsCzech ? "Otevřít Hub" : "Open Hub";

    public string TerminateSession => IsCzech ? "Ukončit relaci" : "Terminate session";

    public string TerminateSessionQuestion => IsCzech
        ? "Opravdu ukončit vybranou Selenium relaci? Prohlížeč a jeho rozpracovaný stav se zavřou."
        : "Terminate the selected Selenium session? Its browser and in-progress state will be closed.";

    public string TerminateSessionTitle => IsCzech ? "Ukončení Selenium relace" : "Terminate Selenium session";

    public string TerminatingSession => IsCzech ? "Ukončuji Selenium relaci…" : "Terminating Selenium session…";

    public string SeleniumSessionTerminated => IsCzech ? "Selenium relace byla ukončena." : "The Selenium session was terminated.";

    public string SeleniumSessionsFailed(string detail) => IsCzech
        ? $"Relace Selenium se nepodařilo načíst: {detail}"
        : $"Selenium sessions could not be loaded: {detail}";

    public string SeleniumOperationFailed(string detail) => IsCzech
        ? $"Operace Selenium selhala: {detail}"
        : $"Selenium operation failed: {detail}";

    public string Browser => IsCzech ? "Prohlížeč" : "Browser";

    public string Platform => IsCzech ? "Platforma" : "Platform";

    public string Started => IsCzech ? "Spuštěno" : "Started";

    public string Duration => IsCzech ? "Doba běhu" : "Duration";

    public string ConnectionDetails => IsCzech ? "Připojení" : "Connection";

    public string Host => "Host";

    public string Port => IsCzech ? "Port" : "Port";

    public string User => IsCzech ? "Uživatel" : "User";

    public string Password => IsCzech ? "Heslo" : "Password";

    public string NoPassword => IsCzech ? "bez hesla" : "no password";

    public string RootPasswordSecurity => IsCzech ? "Zabezpečení účtu root" : "Root account security";

    public string NewPassword => IsCzech ? "Nové heslo" : "New password";

    public string ConfirmPassword => IsCzech ? "Potvrzení hesla" : "Confirm password";

    public string SetPassword => IsCzech ? "Nastavit heslo" : "Set password";

    public string ChangePassword => IsCzech ? "Změnit heslo" : "Change password";

    public string PasswordConfigured => IsCzech ? "Heslo je nastavené" : "Password is configured";

    public string NoPasswordConfigured => IsCzech ? "Výchozí stav: bez hesla" : "Default state: no password";

    public string PasswordMismatch => IsCzech ? "Zadaná hesla se neshodují." : "The entered passwords do not match.";

    public string PasswordChanging => IsCzech ? "Měním heslo účtu root…" : "Changing the root password…";

    public string PasswordChanged => IsCzech
        ? "Heslo účtu root bylo změněno a portable připojení bylo aktualizováno."
        : "The root password was changed and the portable connection was updated.";

    public string PasswordChangeFailed(string detail) => IsCzech
        ? $"Heslo se nepodařilo změnit: {detail}"
        : $"The password could not be changed: {detail}";

    public string PasswordGuidance => IsCzech
        ? "Použijte alespoň 8 znaků. Heslo se nezobrazuje v UI, argumentech procesů ani logu."
        : "Use at least 8 characters. The password is never shown in the UI, process arguments, or logs.";

    public string PhpMyAdminDescription => IsCzech
        ? "Webová správa databází přes lokální Apache a PHP. Přihlaste se jako root aktuálním heslem."
        : "Web database administration through local Apache and PHP. Sign in as root with the current password.";

    public string OpenPhpMyAdmin => IsCzech ? "Otevřít phpMyAdmin" : "Open phpMyAdmin";

    public string OpeningPhpMyAdmin => IsCzech ? "Otevírám phpMyAdmin…" : "Opening phpMyAdmin…";

    public string PhpMyAdminReady => IsCzech
        ? "Web i MariaDB běží. phpMyAdmin je připravený."
        : "The web service and MariaDB are running. phpMyAdmin is ready.";

    public string PhpMyAdminNeedsWeb => IsCzech
        ? "Nejprve spusťte webový stack na stránce Přehled."
        : "Start the web stack on the Dashboard first.";

    public string PhpMyAdminNeedsDatabase => IsCzech
        ? "Nejprve spusťte MariaDB."
        : "Start MariaDB first.";

    public string PhpMyAdminNeedsBoth => IsCzech
        ? "phpMyAdmin vyžaduje spuštěný webový stack i MariaDB."
        : "phpMyAdmin requires both the web stack and MariaDB to be running.";

    public string Version => IsCzech ? "Verze" : "Version";

    public string BinaryStatus => IsCzech ? "Stav komponenty" : "Component status";

    public string PhpIni => "php.ini";

    public string DocumentRoot => "Document root";

    public string LocalOnly => IsCzech ? "Pouze lokální vývoj" : "Local development only";

    public string RootAccountNote => IsCzech
        ? "Výchozí účet root je bez hesla a dostupný pouze na 127.0.0.1. Vlastní heslo můžete nastavit níže; tato instance není určená pro produkci."
        : "The root account has no password by default and is only available at 127.0.0.1. You can set a password below; this instance is not intended for production.";

    public string CreateDatabase => IsCzech ? "Vytvořit databázi" : "Create database";

    public string NewDatabaseName => IsCzech ? "Název nové databáze" : "New database name";

    public string DatabaseOverview => IsCzech ? "Přehled databází" : "Database overview";

    public string ApproximateSize => IsCzech ? "Orientační velikost" : "Approximate size";

    public string Refresh => IsCzech ? "Obnovit" : "Refresh";

    public string DefaultDatabase => IsCzech ? "Výchozí databáze" : "Default database";

    public string DatabaseCount(int count) => IsCzech ? $"Databáze: {count}" : $"Databases: {count}";

    public string CreatingDatabase => IsCzech ? "Vytvářím databázi…" : "Creating database…";

    public string DatabaseCreated(string name) => IsCzech
        ? $"Databáze {name} byla vytvořena."
        : $"Database {name} was created.";

    public string DatabaseCreateFailed(string detail) => IsCzech
        ? $"Databázi se nepodařilo vytvořit: {detail}"
        : $"The database could not be created: {detail}";

    public string DatabaseOverviewFailed(string detail) => IsCzech
        ? $"Přehled databází se nepodařilo načíst: {detail}"
        : $"The database overview could not be loaded: {detail}";

    public string MariaDbReady => IsCzech
        ? "MariaDB je připravená a výchozí databáze portable_dev je dostupná."
        : "MariaDB is ready and the default portable_dev database is available.";

    public string MariaDbPreparedStopped => IsCzech
        ? "MariaDB a výchozí databáze portable_dev jsou připravené. Server zůstává zastavený, dokud jej ručně nespustíte."
        : "MariaDB and the default portable_dev database are ready. The server remains stopped until you start it.";

    public string MariaDbStarting => IsCzech ? "Spouštím MariaDB…" : "Starting MariaDB…";

    public string MariaDbStopping => IsCzech ? "Zastavuji MariaDB…" : "Stopping MariaDB…";

    public string MariaDbAction(ManagedProcessState state) => state switch
    {
        ManagedProcessState.Running => IsCzech ? "Zastavit MariaDB" : "Stop MariaDB",
        ManagedProcessState.Starting => IsCzech ? "Spouštím…" : "Starting…",
        ManagedProcessState.Stopping => IsCzech ? "Zastavuji…" : "Stopping…",
        ManagedProcessState.Failed => IsCzech ? "Zkusit znovu" : "Try again",
        _ => IsCzech ? "Spustit MariaDB" : "Start MariaDB"
    };

    public string MariaDbRuntimeDetail(string version, ManagedProcessState state, int port) => state switch
    {
        ManagedProcessState.Running => RunningModule(version, port),
        ManagedProcessState.Starting => IsCzech ? "Server se spouští pouze na localhostu." : "The server is starting on localhost only.",
        ManagedProcessState.Stopping => IsCzech ? "Server se bezpečně ukončuje." : "The server is shutting down safely.",
        _ => VerifiedModule(version)
    };

    public string SeleniumAction(ManagedProcessState state) => state switch
    {
        ManagedProcessState.Running => IsCzech ? "Zastavit Selenium" : "Stop Selenium",
        ManagedProcessState.Starting => IsCzech ? "Spouštím…" : "Starting…",
        ManagedProcessState.Stopping => IsCzech ? "Zastavuji…" : "Stopping…",
        ManagedProcessState.Failed => IsCzech ? "Zkusit znovu" : "Try again",
        _ => IsCzech ? "Spustit Selenium" : "Start Selenium"
    };

    public string SeleniumRuntimeDetail(string version, ManagedProcessState state, int port, int driverCount) => state switch
    {
        ManagedProcessState.Running => IsCzech
            ? $"Verze {version} naslouchá na portu {port}; načtené drivery: {driverCount}."
            : $"Version {version} is listening on port {port}; loaded drivers: {driverCount}.",
        ManagedProcessState.Starting => IsCzech ? "Spouštím lokální Standalone Grid…" : "Starting the local Standalone Grid…",
        ManagedProcessState.Stopping => IsCzech ? "Ukončuji Grid a jeho relace…" : "Stopping the Grid and its sessions…",
        _ when driverCount == 0 => IsCzech
            ? $"Verze {version} je ověřená. Před spuštěním stáhněte na kartě Ovladače alespoň jeden kompatibilní driver."
            : $"Version {version} is verified. Download at least one compatible driver on the Drivers tab before starting.",
        _ => IsCzech
            ? $"Verze {version} je ověřená; načtené drivery: {driverCount}."
            : $"Version {version} is verified; loaded drivers: {driverCount}."
    };

    public string Language => IsCzech ? "Jazyk rozhraní" : "Interface language";

    public string PortableStorage => IsCzech ? "Portable úložiště" : "Portable storage";

    public string PortableBoundaryNote => IsCzech
        ? "Všechna nastavení, data, logy a dočasné soubory zůstávají uvnitř této složky."
        : "All settings, data, logs, and temporary files stay inside this folder.";

    public string ApplicationRoot => IsCzech ? "Kořen aplikace" : "Application root";

    public string ApplicationVersion => IsCzech ? "Verze aplikace" : "Application version";

    public string PrepareMariaDb => IsCzech ? "Připravit databázi" : "Prepare database";

    public string PreparingMariaDb => IsCzech ? "Připravuji…" : "Preparing…";

    public string InitializingMariaDb => IsCzech
        ? "Připravuji datový adresář MariaDB…"
        : "Preparing the MariaDB data directory…";

    public string MariaDbInitialized => IsCzech
        ? "MariaDB je připravená. Přihlašovací údaje jsou uložené v privátní složce instance."
        : "MariaDB is ready. Credentials are stored in the instance's private folder.";

    public string MariaDbAlreadyInitialized => IsCzech
        ? "MariaDB už je v této instanci připravená."
        : "MariaDB is already prepared in this instance.";

    public string MariaDbInitializationFailed(string detail) => IsCzech
        ? $"Příprava MariaDB selhala: {detail}"
        : $"MariaDB preparation failed: {detail}";

    public string StackAction(ManagedProcessState state) => state switch
    {
        ManagedProcessState.Running => IsCzech ? "Zastavit webový stack" : "Stop web stack",
        ManagedProcessState.Starting => IsCzech ? "Spouštím…" : "Starting…",
        ManagedProcessState.Stopping => IsCzech ? "Zastavuji…" : "Stopping…",
        ManagedProcessState.Failed => IsCzech ? "Zkusit znovu" : "Try again",
        _ => IsCzech ? "Spustit webový stack" : "Start web stack"
    };

    public string RestartWebService => IsCzech ? "Restartovat Apache/PHP" : "Restart Apache/PHP";

    public string RestartingWebService => IsCzech ? "Restartuji Apache a PHP…" : "Restarting Apache and PHP…";

    public string WebServiceRestarted => IsCzech ? "Apache a PHP byly restartovány." : "Apache and PHP were restarted.";

    public string StackStatus(ManagedProcessState state) => state switch
    {
        ManagedProcessState.Stopped => IsCzech ? "Zastaveno" : "Stopped",
        ManagedProcessState.Starting => IsCzech ? "Spouští se" : "Starting",
        ManagedProcessState.Running => IsCzech ? "Běží" : "Running",
        ManagedProcessState.Stopping => IsCzech ? "Zastavuje se" : "Stopping",
        ManagedProcessState.Failed => IsCzech ? "Spuštění selhalo" : "Startup failed",
        _ => state.ToString()
    };

    public string StackSummary(ManagedProcessState state, string errorDetail, int apachePort) => state switch
    {
        ManagedProcessState.Stopped => IsCzech
            ? "Apache a PHP jsou připravené ke spuštění."
            : "Apache and PHP are ready to start.",
        ManagedProcessState.Starting => IsCzech
            ? "Spouštím PHP FastCGI a potom Apache."
            : "Starting PHP FastCGI and then Apache.",
        ManagedProcessState.Running => IsCzech
            ? $"Web je dostupný na http://127.0.0.1:{apachePort}."
            : $"The web server is available at http://127.0.0.1:{apachePort}.",
        ManagedProcessState.Stopping => IsCzech
            ? "Ukončuji Apache a PHP."
            : "Stopping Apache and PHP.",
        ManagedProcessState.Failed => errorDetail,
        _ => string.Empty
    };

    public string InitialStatus => IsCzech
        ? "Offline komponenty byly zkontrolovány."
        : "Offline components have been verified.";

    public string LanguageChanged => IsCzech ? "Jazyk aplikace byl změněn." : "Application language was changed.";

    public string OperationCanceled => IsCzech ? "Operace byla zrušena." : "The operation was cancelled.";

    public string ServiceDescription(string key) => key switch
    {
        "apache" => IsCzech ? "Webový server" : "Web server",
        "php" => IsCzech ? "PHP FastCGI runtime" : "PHP FastCGI runtime",
        "mariadb" => IsCzech ? "Lokální databáze" : "Local database",
        "selenium" => IsCzech ? "WebDriver server" : "WebDriver server",
        _ => throw new ArgumentOutOfRangeException(nameof(key))
    };

    public string ModuleNotFound => IsCzech ? "Komponenta není nainstalovaná nebo ověřená." : "The component is not installed or verified.";

    public string WaitingRuntime => IsCzech ? "Chybí runtime" : "Runtime missing";

    public string RuntimeMissing(IEnumerable<string> missingFiles) => IsCzech
        ? $"Chybí app-local runtime: {string.Join(", ", missingFiles)}."
        : $"Missing app-local runtime: {string.Join(", ", missingFiles)}.";

    public string VerifiedModule(string version) => IsCzech
        ? $"Verze {version} je ověřená a připravená."
        : $"Version {version} is verified and ready.";

    public string RunningModule(string version, int port) => IsCzech
        ? $"Verze {version} naslouchá na portu {port}."
        : $"Version {version} is listening on port {port}.";

    public string MariaDbNeedsPreparation(string version) => IsCzech
        ? $"Verze {version} je přibalená; před prvním spuštěním je potřeba vytvořit databázová data."
        : $"Version {version} is bundled; its data directory must be prepared before first use.";

    public string MariaDbInstanceReady(string version) => IsCzech
        ? $"Verze {version} má připravený datový adresář. Ovládání serveru doplníme v dalším kroku."
        : $"Version {version} has a prepared data directory. Server controls are the next step.";

    public string MariaDbInstanceIncomplete => IsCzech
        ? "Datový adresář nebo přihlašovací údaje jsou neúplné. Existující soubory zůstaly beze změny."
        : "The data directory or credentials are incomplete. Existing files were left unchanged.";

    public string ControlNotAvailable(string version) => IsCzech
        ? $"Verze {version} je přibalená. Ovládání serveru ještě není zapojené."
        : $"Version {version} is bundled. Server controls are not connected yet.";

    public string NotInstalled => IsCzech ? "Chybí" : "Missing";

    public string VerificationFailed => IsCzech ? "Chyba integrity" : "Integrity error";

    public string Running => IsCzech ? "Běží" : "Running";

    public string Starting => IsCzech ? "Spouští se" : "Starting";

    public string Stopping => IsCzech ? "Zastavuje se" : "Stopping";

    public string Stopped => IsCzech ? "Zastaveno" : "Stopped";

    public string Failed => IsCzech ? "Chyba" : "Failed";

    public string Bundled => IsCzech ? "Přibaleno" : "Bundled";

    public string Initialized => IsCzech ? "Inicializováno" : "Initialized";

    public string NeedsSetup => IsCzech ? "Vyžaduje přípravu" : "Setup required";

    public string NeedsAttention => IsCzech ? "Vyžaduje kontrolu" : "Needs attention";

    public void SetLanguage(ApplicationLanguage language)
    {
        if (_currentLanguage == language)
        {
            return;
        }

        _currentLanguage = language;
        _settingsStore.Save(new ApplicationSettings(language));
        OnPropertyChanged(string.Empty);
    }

    private bool IsCzech => _currentLanguage == ApplicationLanguage.Czech;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
