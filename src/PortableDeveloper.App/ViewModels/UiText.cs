using System.ComponentModel;
using System.Runtime.CompilerServices;
using PortableDeveloper.Application.Packages;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Application.Projects;
using PortableDeveloper.Application.Selenium;
using PortableDeveloper.Application.Scheduling;
using PortableDeveloper.Application.Settings;
using PortableDeveloper.Application.Storage;
using PortableDeveloper.Application.Workspace;
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

    public string NavigationLabel(NavigationPage page) => page switch
    {
        NavigationPage.Projects => IsCzech ? "Projekty" : "Projects",
        NavigationPage.Modules => IsCzech ? "Moduly" : "Modules",
        NavigationPage.Php => "PHP",
        NavigationPage.Apache => "Apache",
        NavigationPage.Databases => IsCzech ? "Databáze" : "Databases",
        NavigationPage.Selenium => "Selenium",
        NavigationPage.Ports => IsCzech ? "Porty" : "Ports",
        NavigationPage.Composer => "Composer",
        NavigationPage.Node => "Node.js",
        NavigationPage.Python => "Python",
        NavigationPage.Scheduler => IsCzech ? "Plánovač" : "Scheduler",
        NavigationPage.Terminal => IsCzech ? "Terminál" : "Terminal",
        NavigationPage.Files => IsCzech ? "Soubory" : "Files",
        NavigationPage.Guides => IsCzech ? "Návody" : "Guides",
        NavigationPage.Settings => IsCzech ? "Nastavení" : "Settings",
        _ => page.ToString()
    };

    public string PageTitle(NavigationPage page) => page switch
    {
        NavigationPage.Projects => IsCzech ? "Správa projektů" : "Project management",
        NavigationPage.Modules => IsCzech ? "Správce modulů" : "Module manager",
        NavigationPage.Php => IsCzech ? "PHP runtime" : "PHP runtime",
        NavigationPage.Apache => IsCzech ? "Apache server" : "Apache server",
        NavigationPage.Databases => IsCzech ? "Databáze" : "Databases",
        NavigationPage.Selenium => "Selenium Server",
        NavigationPage.Ports => IsCzech ? "Správce portů" : "Port manager",
        NavigationPage.Composer => IsCzech ? "Composer balíčky" : "Composer packages",
        NavigationPage.Node => IsCzech ? "Node.js balíčky" : "Node.js packages",
        NavigationPage.Python => IsCzech ? "Python balíčky" : "Python packages",
        NavigationPage.Scheduler => IsCzech ? "Plánovač úloh" : "Task scheduler",
        NavigationPage.Terminal => IsCzech ? "Portable terminál" : "Portable terminal",
        NavigationPage.Files => IsCzech ? "Soubory projektu" : "Project files",
        NavigationPage.Guides => IsCzech ? "Návody a ukázky" : "Guides and examples",
        NavigationPage.Settings => IsCzech ? "Nastavení aplikace" : "Application settings",
        _ => page.ToString()
    };

    public string GuideCategories => IsCzech ? "Kategorie" : "Categories";

    public string GuideSearch => IsCzech ? "Hledat v návodech" : "Search guides";

    public string GuideAllCategories => IsCzech ? "Vše" : "All";

    public string GuideNoArticles => IsCzech
        ? "Tomuto filtru neodpovídá žádný článek."
        : "No articles match this filter.";

    public string GuideSelectArticle => IsCzech
        ? "Vyberte článek ze seznamu."
        : "Select an article from the list.";

    public string GuideArticleCount(int count) => IsCzech
        ? count == 1 ? "1 článek" : $"Články: {count}"
        : count == 1 ? "1 article" : $"{count} articles";

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

    public string RuntimePackageName(RuntimePackageKind kind) => kind switch
    {
        RuntimePackageKind.Apache => "Apache HTTP Server",
        RuntimePackageKind.Php => "PHP",
        RuntimePackageKind.Database => IsCzech ? "Databáze" : "Database",
        RuntimePackageKind.Selenium => "Selenium",
        RuntimePackageKind.Composer => "Composer",
        RuntimePackageKind.Node => "Node.js + npm",
        RuntimePackageKind.Python => "Python",
        RuntimePackageKind.Editor => IsCzech ? "Editor" : "Editor",
        RuntimePackageKind.PhpMyAdmin => "phpMyAdmin",
        RuntimePackageKind.SeleniumChromeEnvironment => "Chrome for Testing + ChromeDriver",
        RuntimePackageKind.SeleniumFirefoxEnvironment => "Mozilla Firefox + geckodriver",
        _ => kind.ToString()
    };

    public string RuntimePackageDescription(RuntimePackageKind kind) => kind switch
    {
        RuntimePackageKind.Apache => IsCzech ? "Lokální webový server pro statické projekty; pokud je nainstalované PHP, připojí se automaticky přes FastCGI." : "Local web server for static projects; when PHP is installed, it is attached automatically through FastCGI.",
        RuntimePackageKind.Php => IsCzech ? "Přenosný PHP runtime pro Apache a Composer." : "Portable PHP runtime for Apache and Composer.",
        RuntimePackageKind.Database => IsCzech ? "Přenosný MariaDB server a lokální databáze." : "Portable MariaDB server and local databases.",
        RuntimePackageKind.Selenium => IsCzech ? "Selenium Server a vlastní portable Java runtime; spravovaný browser si vyberete zvlášť." : "Selenium Server and its portable Java runtime; choose a managed browser separately.",
        RuntimePackageKind.Composer => IsCzech ? "Správa PHP knihoven; chybějící PHP se doplní automaticky." : "PHP dependency management; missing PHP is added automatically.",
        RuntimePackageKind.Node => IsCzech ? "Přenosný Node.js runtime s npm pro projektové JavaScriptové balíčky." : "Portable Node.js runtime with npm for project JavaScript packages.",
        RuntimePackageKind.Python => IsCzech ? "Přenosný Python s projektovou správou knihoven." : "Portable Python with project package management.",
        RuntimePackageKind.Editor => IsCzech ? "Lehký portable Notepad++ propojený se správcem souborů." : "Lightweight portable Notepad++ integrated with the file manager.",
        RuntimePackageKind.PhpMyAdmin => IsCzech ? "Webová správa databází včetně Apache, PHP a MariaDB." : "Web database administration including Apache, PHP, and MariaDB.",
        RuntimePackageKind.SeleniumChromeEnvironment => IsCzech ? "Spravovaný a verzově shodný balíček browseru a driveru pro čisté automatizační relace." : "Managed, version-matched browser and driver bundle for clean automation sessions.",
        RuntimePackageKind.SeleniumFirefoxEnvironment => IsCzech ? "Doporučený spravovaný Firefox a geckodriver; vhodný také pro přenosné přihlašovací profily." : "Recommended managed Firefox and geckodriver; also suitable for portable signed-in profiles.",
        _ => string.Empty
    };

    public string DownloadAndInstall => IsCzech ? "Stáhnout a nainstalovat" : "Download and install";

    public string Installed => IsCzech ? "Nainstalováno" : "Installed";

    public string SeleniumEnvironmentState(SeleniumBrowserEnvironmentState state) => state switch
    {
        SeleniumBrowserEnvironmentState.Ready => IsCzech ? "Připraveno" : "Ready",
        SeleniumBrowserEnvironmentState.DriverMissing => IsCzech ? "Chybí kompatibilní driver" : "Compatible driver missing",
        SeleniumBrowserEnvironmentState.VersionMismatch => IsCzech ? "Nekompatibilní verze" : "Version mismatch",
        SeleniumBrowserEnvironmentState.BrowserUnavailable => IsCzech ? "Prohlížeč není dostupný" : "Browser unavailable",
        _ => state.ToString()
    };

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

    public string PackageDownloadSize(RuntimePackageInstallProgress progress)
    {
        if (progress.Stage != RuntimePackageInstallStage.Downloading || progress.BytesReceived <= 0)
        {
            return string.Empty;
        }

        var current = FormatDownloadBytes(progress.BytesReceived);
        var size = progress.TotalBytes is > 0
            ? $"{current} / {FormatDownloadBytes(progress.TotalBytes.Value)}"
            : current;
        return progress.ComponentCount > 1
            ? $"{size}  ·  {progress.ComponentIndex}/{progress.ComponentCount}"
            : size;
    }

    private static string FormatDownloadBytes(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double value = bytes;
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }

        return $"{value:0.#} {units[unit]}";
    }

    public string PackageInstallFailed(string detail) => IsCzech
        ? $"Instalace modulu selhala: {detail}"
        : $"Module installation failed: {detail}";

    public string PackageInstallSucceeded(string name) => IsCzech
        ? $"Modul {name} je nainstalovaný a připravený."
        : $"Module {name} is installed and ready.";

    public string ApacheServer => "APACHE HTTP SERVER";

    public string TechnicalDetails => IsCzech ? "Technické informace" : "Technical information";

    public string ServiceControl => IsCzech ? "Ovládání služby" : "Service control";

    public string CurrentConfiguration => IsCzech ? "Aktuální konfigurace" : "Current configuration";

    public string PlannedConfiguration => IsCzech ? "Připravovaná konfigurace" : "Planned configuration";

    public string Planned => IsCzech ? "Plánováno" : "Planned";

    public string PackageRuntime => IsCzech ? "Portable runtime" : "Portable runtime";

    public string ProjectDirectory => IsCzech ? "Složka projektu" : "Project directory";

    public string ManageProjects => IsCzech ? "Spravovat" : "Manage";

    public string SharedProjectContext => IsCzech ? "SPOLEČNÝ PRACOVNÍ PROSTOR" : "SHARED WORKSPACE";

    public string ProjectsTab => IsCzech ? "Projekty" : "Projects";

    public string CreateProjectTab => IsCzech ? "Nový projekt" : "New project";

    public string AddProjectTab => IsCzech ? "Přidat projekt" : "Add project";

    public string ProjectCapabilities => IsCzech ? "Schopnosti" : "Capabilities";

    public string ProjectTools => IsCzech ? "Nástroje projektu" : "Project tools";

    public string ProjectManagement => IsCzech ? "Správa projektu" : "Project management";

    public string ProjectRoot => IsCzech ? "Kořen projektu" : "Project root";

    public string WebSupport => IsCzech ? "Webová podpora" : "Web support";

    public string WebEnabled => IsCzech ? "Zapnuto v Apache" : "Enabled in Apache";

    public string WebDisabled => IsCzech ? "Vypnuto v Apache" : "Disabled in Apache";

    public string WebNotConfigured => IsCzech ? "Nenastaveno" : "Not configured";

    public string NotServedByApache => IsCzech ? "Projekt není poskytován přes Apache" : "Project is not served by Apache";

    public string WebNotConfiguredDetail => IsCzech
        ? "Projekt zatím nemá webový kořen."
        : "The project does not have a web root yet.";

    public string WebRootSummary(string root) => IsCzech ? $"Web root: {root}" : $"Web root: {root}";

    public string ConfigureWebProject => IsCzech ? "Nastavit web" : "Configure web";

    public string SaveWebConfiguration => IsCzech ? "Uložit nastavení" : "Save settings";

    public string ServeProjectThroughApache => IsCzech
        ? "Poskytovat projekt přes Apache"
        : "Serve this project through Apache";

    public string AllowHtaccessLabel => IsCzech
        ? "Povolit projektové soubory .htaccess"
        : "Allow project .htaccess files";

    public string HtaccessStatus(bool allowed) => allowed
        ? IsCzech ? ".htaccess je povolen" : ".htaccess is allowed"
        : IsCzech ? ".htaccess je vypnutý" : ".htaccess is disabled";

    public string WebSettingsHelp => IsCzech
        ? "Nastavení nemění zdrojové soubory projektu. Chybějící web root se vytvoří uvnitř projektu; běžící Apache se restartuje až po samostatném potvrzení."
        : "These settings do not rewrite project source files. A missing web root is created inside the project; a running Apache instance restarts only after separate confirmation.";

    public string DefaultProjectWebRequired => IsCzech
        ? "Výchozí localhost musí zůstat v Apache zapnutý."
        : "The default localhost site must remain enabled in Apache.";

    public string ConfigureWebRootPrompt => IsCzech
        ? "Web root uvnitř projektu (například public nebo .):"
        : "Web root inside the project (for example public or .):";

    public string ConfigureWebRootValidation => IsCzech
        ? "Zadejte bezpečnou relativní složku uvnitř projektu."
        : "Enter a safe relative directory inside the project.";

    public string OpenWebUrl => "URL";

    public string ApplyWebConfiguration => IsCzech
        ? "Použít změny a restartovat Apache"
        : "Apply changes and restart Apache";

    public string WebConfigurationRestartPending => IsCzech
        ? "Webové nastavení je uložené. Běžící Apache zatím používá předchozí konfiguraci."
        : "Web settings are saved. The running Apache instance is still using the previous configuration.";

    public string WebConfigurationSavedForNextStart => IsCzech
        ? "Webové nastavení je uložené a použije se při příštím spuštění Apache."
        : "Web settings are saved and will be used the next time Apache starts.";

    public string WebConfigurationApplied => IsCzech
        ? "Webové nastavení bylo použito a Apache restartován."
        : "Web settings were applied and Apache was restarted.";

    public string ProjectDirectoryMissing => IsCzech ? "Složka projektu chybí" : "Project directory is missing";

    public string RenameProject => IsCzech ? "Přejmenovat" : "Rename";

    public string RenameProjectPrompt => IsCzech ? "Nový zobrazovaný název projektu:" : "New project display name:";

    public string RenameProjectValidation => IsCzech ? "Zadejte neprázdný název projektu." : "Enter a non-empty project name.";

    public string ProjectRenamed(string name) => IsCzech ? $"Projekt byl přejmenován na {name}." : $"Project renamed to {name}.";

    public string OpenFiles => IsCzech ? "Soubory" : "Files";

    public string OpenTerminal => IsCzech ? "Terminál" : "Terminal";

    public string UnregisterProject => IsCzech ? "Odebrat ze seznamu" : "Unregister";

    public string UnregisterProjectQuestion(string name) => IsCzech
        ? $"Odebrat projekt {name} ze seznamu? Jeho soubory se nesmažou a naplánované úlohy se vypnou."
        : $"Unregister project {name}? Its files will not be deleted and its scheduled tasks will be disabled.";

    public string ProjectUnregistered(string name) => IsCzech
        ? $"Projekt {name} byl odebrán ze seznamu. Všechny soubory zůstaly zachované."
        : $"Project {name} was unregistered. All files were preserved.";

    public string ProjectDirectoryUnavailable => IsCzech
        ? "Projekt nelze aktivovat, protože jeho složka na disku chybí."
        : "The project cannot be activated because its directory is missing.";

    public string CreateGeneralProject => IsCzech ? "Vytvořit nový projekt" : "Create a new project";

    public string ProjectTemplate => IsCzech ? "Počáteční šablona" : "Initial template";

    public string ProjectTemplateNotice => IsCzech
        ? "Šablona vytvoří jen počáteční soubory. Neurčuje typ projektu a nic nestahuje ani nespouští."
        : "A template only creates initial files. It does not define a project type and downloads or runs nothing.";

    public string ProjectTemplateName(ProjectTemplateKind kind) => kind switch
    {
        ProjectTemplateKind.Empty => IsCzech ? "Prázdný" : "Empty",
        ProjectTemplateKind.Web => "Web",
        ProjectTemplateKind.Python => "Python",
        ProjectTemplateKind.BrowserAutomation => IsCzech ? "Automatizace prohlížeče" : "Browser automation",
        ProjectTemplateKind.NodeJs => "Node.js",
        _ => kind.ToString()
    };

    public string ProjectTemplateDescription(ProjectTemplateKind kind) => kind switch
    {
        ProjectTemplateKind.Empty => IsCzech ? "Pouze prázdná složka projektu." : "Only an empty project directory.",
        ProjectTemplateKind.Web => IsCzech ? "Statická úvodní stránka v public/ a zapnutý Apache web prostor." : "A static starter page in public/ and enabled Apache web space.",
        ProjectTemplateKind.Python => IsCzech ? "main.py a prázdný requirements.txt." : "main.py and an empty requirements.txt.",
        ProjectTemplateKind.BrowserAutomation => IsCzech ? "Malý Selenium příklad a návod ke sdílenému serveru." : "A small Selenium example and shared-server guide.",
        ProjectTemplateKind.NodeJs => IsCzech ? "Minimální package.json a src/index.js." : "A minimal package.json and src/index.js.",
        _ => string.Empty
    };

    public string ProjectCreatedWithoutDownloads(string name) => IsCzech
        ? $"Projekt {name} byl vytvořen a aktivován. Nebyl stažen ani spuštěn žádný runtime."
        : $"Project {name} was created and activated. No runtime was downloaded or executed.";

    public string AddExistingProject => IsCzech ? "Přidat existující portable složku" : "Add an existing portable directory";

    public string ExistingProjectDirectory => IsCzech ? "Nepřiřazená složka" : "Unregistered directory";

    public string ExistingProjectName => IsCzech ? "Zobrazovaný název" : "Display name";

    public string NoExistingProjectDirectories => IsCzech
        ? "Pod instances/default/projects nejsou žádné bezpečné nepřiřazené složky."
        : "There are no safe unregistered directories under instances/default/projects.";

    public string RegisterProject => IsCzech ? "Přidat do projektů" : "Register project";

    public string ProjectRegistered(string name) => IsCzech
        ? $"Existující složka byla přidána jako projekt {name}; její obsah se nezměnil."
        : $"The existing directory was registered as project {name}; its content was not changed.";

    public string DetectedCapabilities => IsCzech ? "Rozpoznáno" : "Detected";

    public string ProjectCapability(ProjectCapabilityKind kind) => kind switch
    {
        ProjectCapabilityKind.Web => "Web",
        ProjectCapabilityKind.Php => "PHP",
        ProjectCapabilityKind.NodeJs => "Node.js",
        ProjectCapabilityKind.Python => "Python",
        ProjectCapabilityKind.BrowserAutomation => IsCzech ? "Automatizace" : "Automation",
        _ => kind.ToString()
    };

    public string NoCapabilitiesDetected => IsCzech ? "Zatím nic nerozpoznáno" : "Nothing detected yet";

    public string CapabilityDetectionHint => IsCzech
        ? "Všechny nástroje lze použít i bez rozpoznané technologie."
        : "All tools remain available even without a detected technology.";

    public string SharedRuntimesReady => IsCzech ? "Potřebné sdílené runtime jsou připravené." : "Required shared runtimes are ready.";

    public string MissingSharedRuntimes(IEnumerable<string> runtimes) => IsCzech
        ? $"Chybí sdílené runtime: {string.Join(", ", runtimes)}. Nainstalujte je ručně v Modulech."
        : $"Missing shared runtimes: {string.Join(", ", runtimes)}. Install them explicitly from Modules.";

    public string TerminalProcessTimedOut => IsCzech
        ? "Proces překročil maximální dobu běhu a byl ukončen."
        : "The process exceeded its maximum runtime and was stopped.";

    public string TerminalProcessExited(int? exitCode) => IsCzech
        ? $"Proces skončil s kódem {exitCode?.ToString() ?? "?"}."
        : $"The process exited with code {exitCode?.ToString() ?? "?"}.";

    public string TerminalOutputTruncated => IsCzech
        ? "… Starší výstup terminálu byl odebrán, aby konzole zůstala svižná."
        : "… Older terminal output was removed to keep the console responsive.";

    public string RunCommand => IsCzech ? "Spustit" : "Run";

    public string ClearTerminal => IsCzech ? "Vyčistit" : "Clear";

    public string OverviewTab => IsCzech ? "Přehled" : "Overview";

    public string SettingsTab => IsCzech ? "Nastavení" : "Settings";

    public string SettingsGeneralTab => IsCzech ? "Obecné" : "General";

    public string SettingsStorageTab => IsCzech ? "Úložiště" : "Storage";

    public string SettingsAboutTab => IsCzech ? "O aplikaci" : "About";

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
        ? "Před změnou portů zastavte Apache, MariaDB i Selenium."
        : "Stop Apache, MariaDB, and Selenium before changing ports.";

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

    public string EditorSelection => IsCzech ? "Editor souborů" : "File editor";

    public string EditorSelectionHelp => IsCzech
        ? "Pro textové a zdrojové soubory můžete upřednostnit ověřený portable Notepad++, nebo vždy použít výchozí aplikaci Windows. Ostatní typy souborů se dál otevírají systémovou aplikací."
        : "For text and source files, prefer the verified portable Notepad++ or always use the Windows default application. Other file types continue to use their system application.";

    public string PreferPortableEditor => IsCzech ? "Portable Notepad++ (pokud je dostupný)" : "Portable Notepad++ (when available)";

    public string UseWindowsDefaultEditor => IsCzech ? "Výchozí aplikace Windows" : "Windows default application";

    public string EditorSelectionSaved => IsCzech ? "Volba editoru byla uložena." : "The editor preference was saved.";

    public string CurrentFolder => IsCzech ? "Aktuální složka" : "Current folder";

    public string Up => IsCzech ? "Nahoru" : "Up";

    public string Back => IsCzech ? "Zpět" : "Back";

    public string RefreshFiles => IsCzech ? "Obnovit" : "Refresh";

    public string NewItemName => IsCzech ? "Název nové položky" : "New item name";

    public string NewFile => IsCzech ? "Nový soubor" : "New file";

    public string NewFolder => IsCzech ? "Nová složka" : "New folder";

    public string Folder => IsCzech ? "Složka" : "Folder";

    public string File => IsCzech ? "Soubor" : "File";

    public string Type => IsCzech ? "Typ" : "Type";

    public string WorkspaceKindLabel(WorkspaceFileKind kind) => kind switch
    {
        WorkspaceFileKind.Folder => Folder,
        WorkspaceFileKind.Php => "PHP",
        WorkspaceFileKind.Python => "Python",
        WorkspaceFileKind.JavaScript => "JavaScript / TypeScript",
        WorkspaceFileKind.StyleSheet => IsCzech ? "Styl" : "Style sheet",
        WorkspaceFileKind.Html => "HTML",
        WorkspaceFileKind.Xml => "XML / SVG",
        WorkspaceFileKind.Json => "JSON",
        WorkspaceFileKind.Yaml => "YAML",
        WorkspaceFileKind.Markdown => "Markdown",
        WorkspaceFileKind.Text => IsCzech ? "Text" : "Text",
        WorkspaceFileKind.Document => IsCzech ? "Dokument" : "Document",
        WorkspaceFileKind.Spreadsheet => IsCzech ? "Tabulka" : "Spreadsheet",
        WorkspaceFileKind.Configuration => IsCzech ? "Konfigurace" : "Configuration",
        WorkspaceFileKind.Image => IsCzech ? "Obrázek" : "Image",
        WorkspaceFileKind.Archive => IsCzech ? "Archiv" : "Archive",
        WorkspaceFileKind.Database => IsCzech ? "Databáze" : "Database",
        WorkspaceFileKind.Executable => IsCzech ? "Spustitelný soubor" : "Executable",
        _ => File
    };

    public string WorkspacePageSummary(int first, int last, int total) => IsCzech
        ? $"{first}–{last} z {total}"
        : $"{first}–{last} of {total}";

    public string WorkspaceAddressHint => IsCzech
        ? "Zadejte cestu uvnitř projektu"
        : "Enter a path inside the project";

    public string FirstPage => IsCzech ? "První stránka" : "First page";

    public string PreviousPage => IsCzech ? "Předchozí stránka" : "Previous page";

    public string NextPage => IsCzech ? "Další stránka" : "Next page";

    public string LastPage => IsCzech ? "Poslední stránka" : "Last page";

    public string Open => IsCzech ? "Otevřít" : "Open";

    public string Edit => IsCzech ? "Upravit" : "Edit";

    public string Copy => IsCzech ? "Kopírovat" : "Copy";

    public string Cut => IsCzech ? "Vyjmout" : "Cut";

    public string Paste => IsCzech ? "Vložit" : "Paste";

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

    public string DeleteItemsQuestion(int count) => IsCzech
        ? $"Opravdu smazat vybrané položky ({count})? U neprázdných složek se smaže celý jejich obsah."
        : $"Delete the selected items ({count})? Non-empty folders and all their contents will be removed.";

    public string DeleteItemTitle => IsCzech ? "Smazání položky projektu" : "Delete project item";

    public string WorkspaceOperationFailed(string detail) => IsCzech
        ? $"Operace se souborem selhala: {detail}"
        : $"File operation failed: {detail}";

    public string WorkspaceCopyNameSuffix => IsCzech ? " - kopie" : " - copy";

    public string WorkspaceItemCopied(string name) => IsCzech
        ? $"{name} je připraven k překopírování."
        : $"{name} is ready to be copied.";

    public string WorkspaceItemsCopied(int count) => IsCzech
        ? $"Vybrané položky jsou připraveny ke kopírování ({count})."
        : $"The selected items are ready to be copied ({count}).";

    public string WorkspaceItemCut(string name) => IsCzech
        ? $"{name} je připraven k přesunutí."
        : $"{name} is ready to be moved.";

    public string WorkspaceItemsCut(int count) => IsCzech
        ? $"Vybrané položky jsou připraveny k přesunutí ({count})."
        : $"The selected items are ready to be moved ({count}).";

    public string WorkspacePasteCompleted => IsCzech ? "Položka byla vložena." : "The item was pasted.";

    public string WorkspaceItemsPasteCompleted(int completed, int total) => IsCzech
        ? $"Vloženo položek: {completed} z {total}."
        : $"Pasted items: {completed} of {total}.";

    public string WorkspaceItemsMoved(int completed, int total) => IsCzech
        ? $"Přesunuto položek: {completed} z {total}."
        : $"Moved items: {completed} of {total}.";

    public string WorkspaceItemsImported(int count) => (IsCzech, count) switch
    {
        (true, 0) => "Nebyla přidána žádná položka.",
        (true, 1) => "Položka byla přidána do projektu.",
        (true, _) => $"Položky byly přidány do projektu ({count}).",
        (false, 0) => "No items were added.",
        (false, 1) => "The item was added to the project.",
        _ => $"Items were added to the project ({count})."
    };

    public string WorkspaceTransferSkipped => IsCzech ? "Položka byla přeskočena." : "The item was skipped.";

    public string WorkspaceConflictTitle => IsCzech ? "Položka již existuje" : "Item already exists";

    public string WorkspaceConflictMessage(WorkspaceConflict conflict) => IsCzech
        ? $"V cíli „{conflict.DestinationRelativePath}“ už existuje položka se stejným názvem. Co udělat s „{conflict.Name}“?"
        : $"The destination “{conflict.DestinationRelativePath}” already contains an item with the same name. What should happen to “{conflict.Name}”?";

    public string Overwrite => IsCzech ? "Přepsat" : "Overwrite";

    public string RenameCopy => IsCzech ? "Přejmenovat kopii" : "Rename copy";

    public string Skip => IsCzech ? "Přeskočit" : "Skip";

    public string ApplyToRemainingConflicts => IsCzech
        ? "Použít tuto volbu pro všechny další kolize"
        : "Use this choice for all remaining conflicts";

    public string WorkspaceClipboardUnavailable => IsCzech
        ? "Položku lze vložit pouze v projektu, ve kterém byla zkopírována nebo vyjmuta."
        : "The item can be pasted only within the project where it was copied or cut.";

    public string WorkspaceDraggedItemUnavailable => IsCzech
        ? "Přetahovaná položka už není součástí aktivního projektu."
        : "The dragged item is no longer inside the active project.";

    public string OpenProjectDirectory => IsCzech ? "Otevřít projekt" : "Open project";

    public string EditCustomPhpIni => IsCzech ? "Upravit vlastní php.ini" : "Edit custom php.ini";

    public string CustomPhpIniHelp => IsCzech
        ? "Soubor se připojí za bezpečně generovaný php.ini při každém startu Apache. Ruční direktivy mohou přepsat hodnoty z formuláře a použijí se po příštím spuštění nebo restartu Apache."
        : "This file is appended after the safely generated php.ini whenever Apache starts. Manual directives can override form values and take effect after Apache starts or restarts.";

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

    public string TransitiveDependencies => IsCzech ? "Použité závislosti" : "Used dependencies";

    public string ComposerHelp => IsCzech
        ? "Balíčky se instalují do vendor aktuálního projektu. Každý projekt má vlastní composer.json a závislosti."
        : "Packages are installed into the active project's vendor directory. Every project has its own composer.json and dependencies.";

    public string ComposerPackageExample => "php-webdriver/webdriver";

    public string ComposerConstraintExample => IsCzech ? "např. ^1.15" : "e.g. ^1.15";

    public string PythonHelp => IsCzech
        ? "Projektové balíčky se instalují do instances/default/python/packages. Základní Python ani systémový profil Windows se nemění."
        : "Project packages are installed into instances/default/python/packages. The base Python runtime and Windows user profile are not modified.";

    public string NodeHelp => IsCzech
        ? "Balíčky npm se instalují do node_modules aktuálního projektu. Každý projekt má vlastní package.json a package-lock.json; instalační skripty balíčků jsou z bezpečnostních důvodů vypnuté."
        : "npm packages are installed into the active project's node_modules. Every project has its own package.json and package-lock.json; package install scripts are disabled for safety.";

    public string NodePackageExample => IsCzech ? "např. lodash" : "e.g. lodash";

    public string NodeConstraintExample => IsCzech ? "např. ^4.17.21" : "e.g. ^4.17.21";

    public string PythonPackageExample => IsCzech ? "např. selenium" : "e.g. selenium";

    public string PythonConstraintExample => IsCzech ? "např. ==4.35.0" : "e.g. ==4.35.0";

    public string PackageNetworkNotice => IsCzech
        ? "Instalace a odebrání jsou explicitní uživatelské akce. Mohou používat internet a spouštět instalační logiku balíčku; vybírejte jen důvěryhodné knihovny."
        : "Install and remove are explicit user actions. They may use the internet and execute package installation logic; choose trusted libraries only.";

    public string RefreshPackages => IsCzech ? "Obnovit přehled" : "Refresh packages";

    public string LoadingPackages => IsCzech ? "Načítám nainstalované knihovny…" : "Loading installed packages…";

    public string InstallingPackage => IsCzech ? "Instaluji knihovnu…" : "Installing package…";

    public string RemovingPackage => IsCzech ? "Odebírám knihovnu…" : "Removing package…";

    public string PackageOperationProgress(ProjectPackageOperationProgress progress)
    {
        var packageName = string.IsNullOrWhiteSpace(progress.PackageName) ? null : progress.PackageName;
        return
        (progress.Operation, progress.Phase) switch
        {
            (_, ProjectPackageOperationPhase.Preparing) =>
                IsCzech
                    ? packageName is null ? "Připravuji operaci s knihovnou…" : $"Připravuji {packageName}…"
                    : packageName is null ? "Preparing package operation…" : $"Preparing {packageName}…",
            (ProjectPackageOperationKind.Install, ProjectPackageOperationPhase.RunningPackageManager) =>
                IsCzech
                    ? packageName is null ? "Řeším závislosti a instaluji knihovnu…" : $"Instaluji {packageName} a jeho závislosti…"
                    : packageName is null ? "Resolving dependencies and installing package…" : $"Installing {packageName} and its dependencies…",
            (ProjectPackageOperationKind.Remove, ProjectPackageOperationPhase.RunningPackageManager) =>
                IsCzech
                    ? packageName is null ? "Odebírám knihovnu a upravuji závislosti…" : $"Odebírám {packageName} a upravuji závislosti…"
                    : packageName is null ? "Removing package and updating dependencies…" : $"Removing {packageName} and updating dependencies…",
            (_, ProjectPackageOperationPhase.RefreshingInventory) => LoadingPackages,
            (ProjectPackageOperationKind.Refresh, ProjectPackageOperationPhase.Completed) =>
                IsCzech ? "Přehled knihoven je aktuální." : "Package inventory is up to date.",
            (_, ProjectPackageOperationPhase.Completed) =>
                IsCzech ? "Operace správce balíčků byla dokončena." : "Package manager operation completed.",
            _ => IsCzech ? "Probíhá operace s knihovnou…" : "Package operation in progress…"
        };
    }

    public string PackageOperationDetail(ProjectPackageOperationProgress progress, string fallbackPackageName = "")
    {
        var packageName = string.IsNullOrWhiteSpace(progress.PackageName)
            ? fallbackPackageName
            : progress.PackageName;

        return string.IsNullOrWhiteSpace(packageName)
            ? string.Empty
            : IsCzech ? $"Knihovna: {packageName}" : $"Package: {packageName}";
    }

    public string PackageListFailed(string detail) => IsCzech
        ? $"Přehled knihoven se nepodařilo načíst: {detail}"
        : $"The package list could not be loaded: {detail}";

    public string PackageOperationFailed(string detail) => IsCzech
        ? $"Operace s knihovnou selhala: {detail}"
        : $"The package operation failed: {detail}";

    public string PackageInstalled(string name) => IsCzech
        ? $"Knihovna {name} byla nainstalována."
        : $"Package {name} was installed.";

    public string PackageOperationSucceeded(string name, PackageOperationOutcome outcome) => outcome switch
    {
        PackageOperationOutcome.PromotedToDirect => IsCzech
            ? $"Knihovna {name} už byla přítomná a nyní je přímým požadavkem projektu."
            : $"Package {name} was already present and is now a direct project requirement.",
        PackageOperationOutcome.AlreadyDirect => IsCzech
            ? $"Knihovna {name} už je přímým požadavkem projektu."
            : $"Package {name} is already a direct project requirement.",
        _ => PackageInstalled(name)
    };

    public string PackageRemoved(string name) => IsCzech
        ? $"Knihovna {name} byla odebrána."
        : $"Package {name} was removed.";

    public string RemovePackageQuestion(string name) => IsCzech
        ? $"Opravdu odebrat knihovnu {name} z tohoto projektu?"
        : $"Remove package {name} from this project?";

    public string RemovePackageTitle => IsCzech ? "Odebrání knihovny" : "Remove package";

    public string PhpSettings => IsCzech ? "Nastavení php.ini" : "php.ini settings";

    public string PhpSettingsHelp => IsCzech
        ? "Hodnoty se ukládají k portable instanci. php.ini se z nich znovu vytvoří při každém startu Apache."
        : "Values are stored with the portable instance. php.ini is regenerated from them whenever Apache starts.";

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

    public string SaveAndRestartPhp => IsCzech ? "Uložit a restartovat Apache" : "Save and restart Apache";

    public string ResetDefaults => IsCzech ? "Výchozí hodnoty" : "Default values";

    public string PhpSettingsInvalid => IsCzech
        ? "Zkontrolujte rozsahy: paměť 32–8192 MB, upload 1–2048 MB, POST 1–4096 MB, timeout 0–3600 s a max_input_vars 100–100000. POST limit nesmí být menší než upload."
        : "Check the ranges: memory 32–8192 MB, upload 1–2048 MB, POST 1–4096 MB, timeout 0–3600 s, and max_input_vars 100–100000. The POST limit cannot be smaller than the upload limit.";

    public string PhpSettingsSaved(ManagedProcessState apacheState) => apacheState == ManagedProcessState.Running
        ? IsCzech
            ? "PHP nastavení bylo uloženo a Apache byl restartován."
            : "PHP settings were saved and Apache was restarted."
        : IsCzech
            ? "PHP nastavení bylo uloženo a použije se při příštím startu Apache."
            : "PHP settings were saved and will be used the next time Apache starts.";

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
        ? "Projekt nelze přepnout, vytvořit ani odebrat během právě běžící operace Composeru nebo terminálu."
        : "A project cannot be selected, created, or removed while a Composer or terminal operation is running.";

    public string RemoveProjectQuestion(string name) => IsCzech
        ? $"Odebrat projekt {name} z Apache a seznamu projektů? Soubory na disku se nesmažou."
        : $"Remove project {name} from Apache and the project list? Files on disk will not be deleted.";

    public string DatabaseManagementPlan => IsCzech
        ? "Po doplnění MariaDB controlleru zde půjde vytvářet a mazat lokální databáze. První verze používá pouze účet root."
        : "After the MariaDB controller is added, this page will create and remove local databases. The first version uses only the root account.";

    public string SeleniumSettings => IsCzech ? "Nastavení serveru" : "Server settings";

    public string SeleniumFirewallNoticeTitle => IsCzech ? "Lokální Selenium a Windows Firewall" : "Local Selenium and Windows Firewall";

    public string SeleniumFirewallNotice => IsCzech
        ? "Selenium i spravované browsery naslouchají pouze na 127.0.0.1. Windows může při prvním startu přesto zobrazit dotaz na povolení Java runtime ve firewallu. Pro Portable Developer není síťová výjimka potřeba — zvolte Zrušit. Aplikace firewall sama nemění. Toto upozornění se zobrazí jen jednou."
        : "Selenium and its managed browsers listen only on 127.0.0.1. Windows may still ask whether to allow the Java runtime through the firewall on first start. Portable Developer does not need a network exception—choose Cancel. The application never changes the firewall itself. This notice is shown only once.";

    public string ContinueSeleniumStart => IsCzech ? "Rozumím, spustit" : "Understood, start";

    public string MaximumSessions => IsCzech ? "Maximum souběžných relací" : "Maximum concurrent sessions";

    public string SessionTimeout => IsCzech ? "Limit neaktivity relace (sekundy)" : "Session inactivity timeout (seconds)";

    public string SessionTimeoutHelp => IsCzech
        ? "Relace bez WebDriver příkazu po tuto dobu bude Selenium automaticky ukončena. Rozsah 30–86400 sekund."
        : "Selenium automatically terminates a session with no WebDriver command for this period. Range: 30–86400 seconds.";

    public string EnableSeleniumDownloads => IsCzech ? "Povolit stahování souborů" : "Allow file downloads";

    public string SeleniumDownloadsHelp => IsCzech
        ? "Povolené soubory se ukládají do složky seldownloads aktivního projektu a zůstávají zachované mezi účty i relacemi. Při vypnutí Selenium stahování zablokuje. Změna se použije při příštím startu serveru."
        : "Allowed files are saved in the active project's seldownloads folder and persist across accounts and sessions. When disabled, Selenium blocks downloads. The change applies on the next server start.";

    public string SaveSeleniumSettings => IsCzech ? "Uložit nastavení" : "Save settings";

    public string SeleniumSettingsSaved => IsCzech
        ? "Nastavení Selenium bylo uloženo a použije se při příštím startu."
        : "Selenium settings were saved and will be used on the next start.";

    public string SeleniumSettingsInvalid => IsCzech
        ? "Zadejte port 1024–65535, 1–32 relací a timeout 30–86400 sekund."
        : "Enter port 1024–65535, 1–32 sessions, and a timeout of 30–86400 seconds.";

    public string SeleniumDrivers => IsCzech ? "Browser prostředí" : "Browser environments";

    public string SeleniumDriversHelp => IsCzech
        ? "Vyberte celý ověřený balíček browseru a odpovídajícího driveru. Selenium systémové prohlížeče ani jejich profily nepoužívá."
        : "Choose a complete verified browser and matching driver bundle. Selenium does not use system browsers or their profiles.";

    public string SeleniumDriverCatalog => IsCzech ? "Katalog browser prostředí" : "Browser environment catalog";

    public string InstalledSeleniumDrivers => IsCzech ? "Spravované browsery" : "Managed browsers";

    public string SeleniumProfiles => IsCzech ? "Profily" : "Profiles";

    public string SeleniumBrowserProfiles => IsCzech ? "Browser profily" : "Browser profiles";

    public string SeleniumCookieVaults => IsCzech ? "Cookie vault" : "Cookie vault";

    public string CookieVaultManagement => IsCzech ? "Spravované cookie vaulty" : "Managed cookie vaults";

    public string CookieVaultHelp => IsCzech
        ? "Import přijme JSON export cookies, ponechá jen údaje potřebné pro Selenium a vyřadí prošlé či neplatné položky. Původní export zůstane beze změny."
        : "Import accepts a JSON cookie export, keeps only fields required by Selenium, and discards expired or invalid items. The original export remains unchanged.";

    public string CookieVaultName => IsCzech ? "Název vaultu" : "Vault name";

    public string CookieExportFile => IsCzech ? "JSON soubor s cookies" : "Cookie JSON file";

    public string ChooseCookieFile => IsCzech ? "Vybrat soubor…" : "Choose file…";

    public string NoCookieFileSelected => IsCzech ? "Není vybraný žádný soubor." : "No file selected.";

    public string CookieVaultAutomaticProtectionHelp => IsCzech
        ? "Aplikace vault automaticky zašifruje klíčem uloženým uvnitř portable složky. Není potřeba žádné heslo ani odemykání."
        : "The app automatically encrypts the vault with a key stored inside the portable folder. No password or unlocking is required.";

    public string ImportCookieVault => IsCzech ? "Importovat" : "Import";

    public string CookieVaultImported(string name, int skipped) => IsCzech
        ? $"Vault {name} byl vytvořen. Vyřazené nebo duplicitní cookies: {skipped}."
        : $"Vault {name} was created. Discarded or duplicate cookies: {skipped}.";

    public string CookieVaultImportFailed(string detail) => IsCzech
        ? $"Import cookie vaultu selhal: {detail}"
        : $"Cookie vault import failed: {detail}";

    public string CookieVaultCount(int count) => IsCzech ? $"Vaulty: {count}" : $"Vaults: {count}";

    public string CookieCount(int count) => IsCzech ? $"Cookies: {count}" : $"Cookies: {count}";

    public string NoCookieDomains => IsCzech ? "Žádné domény" : "No domains";

    public string NoCookieVaults => IsCzech ? "Zatím není vytvořený žádný cookie vault." : "No cookie vault has been created yet.";

    public string CookieVaultReady => IsCzech
        ? "Připraveno — Selenium data rozšifruje pouze při vytváření relace"
        : "Ready — Selenium decrypts the data only while creating a session";

    public string DamagedVault(string detail) => IsCzech ? $"Poškozený vault: {detail}" : $"Damaged vault: {detail}";

    public string RemoveCookieVaultTitle => IsCzech ? "Odstranění cookie vaultu" : "Remove cookie vault";

    public string RemoveCookieVaultQuestion(string name) => IsCzech
        ? $"Opravdu trvale odstranit zašifrovaný vault {name}? Bez zálohy jej nelze obnovit."
        : $"Permanently remove encrypted vault {name}? It cannot be recovered without a backup.";

    public string CookieVaultRemoved => IsCzech ? "Cookie vault byl odstraněn." : "Cookie vault was removed.";

    public string CookieVaultCapabilityHelp => IsCzech
        ? "Do capabilities relace přidejte portable:vault s uvedeným ID. Aplikace vault použije automaticky."
        : "Add portable:vault with the shown ID to session capabilities. The app uses the vault automatically.";

    public string SeleniumProfileMasters => IsCzech ? "Master profily" : "Master profiles";

    public string SeleniumProfileManagement => IsCzech ? "Přihlašovací profily" : "Signed-in profiles";

    public string SeleniumProfilesHelp => IsCzech
        ? "Profil vznikne pouze ve spravovaném browseru aplikace. Přihlaste se, browser zavřete a aplikace uloží neměnný master; každá relace dostane dočasnou kopii, která se po ukončení smaže. Pro přenos přihlášení doporučujeme Firefox."
        : "The profile is created only in an app-managed browser. Sign in, close the browser, and the app seals an immutable master; each session gets a temporary copy that is removed afterwards. Firefox is recommended for portable sign-in state.";

    public string ProfileName => IsCzech ? "Název profilu" : "Profile name";

    public string ProfileNameRequired => IsCzech
        ? "Nejdřív zadejte název profilu (1 až 80 znaků)."
        : "Enter a profile name first (1 to 80 characters).";

    public string BrowserEnvironment => IsCzech ? "Spravovaný prohlížeč" : "Managed browser";

    public string CreateCleanMaster => IsCzech ? "Vytvořit přihlašovací profil" : "Create signed-in profile";

    public string CreateCleanMasterHelp => IsCzech
        ? "Otevře nový dočasný profil uvnitř aplikace. Přihlaste se pouze k webům, které chcete automatizovat, a browser zavřete; profil se ověří a uloží jako neměnný master."
        : "Opens a fresh temporary profile inside the app. Sign in only to sites you want to automate and close the browser; the profile is verified and stored as an immutable master.";

    public string SelectBrowserEnvironment => IsCzech ? "Nejdřív vyberte dostupný prohlížeč." : "Select an available browser first.";

    public string UnsupportedBrowserEnvironment => IsCzech ? "Vybraný typ prohlížeče není podporovaný." : "The selected browser type is not supported.";

    public string ConfigureBrowserAndClose => IsCzech ? "Přihlaste se ve spravovaném prohlížeči a potom jej zavřete…" : "Sign in using the managed browser and then close it…";

    public string SeleniumProfileWaiting => IsCzech
        ? "Prohlížeč běží. Přihlaste účet, který chcete v profilu používat, a potom zavřete všechna jeho okna."
        : "The browser is running. Sign in to the account this profile should use, then close all of its windows.";

    public string SeleniumProfileSealing => IsCzech
        ? "Prohlížeč byl zavřen. Profil se kopíruje, čistí a ověřuje…"
        : "The browser is closed. Copying, cleaning, and verifying the profile…";

    public string SeleniumProfileCleaning => IsCzech
        ? "Dokončuji profil a odstraňuji pracovní soubory…"
        : "Finishing the profile and removing working files…";

    public string BrowserCouldNotStart => IsCzech ? "Prohlížeč se nepodařilo spustit." : "The browser could not be started.";

    public string NoSeleniumProfiles => IsCzech ? "Zatím není vytvořený žádný přihlašovací profil." : "No signed-in profile has been created yet.";

    public string SeleniumProfileCount(int count) => IsCzech ? $"Master profily: {count}" : $"Master profiles: {count}";

    public string VerifiedProfile => IsCzech ? "Ověřený neměnný master" : "Verified immutable master";

    public string ProfileBrowserUnavailable => IsCzech
        ? "Master je ověřený, ale kompatibilní browser prostředí není připravené"
        : "Master is verified, but no compatible browser environment is ready";

    public string DamagedProfile(string detail) => IsCzech ? $"Poškozený: {detail}" : $"Damaged: {detail}";

    public string SeleniumProfileBrowserLabel(SeleniumProfileBrowser browser) => browser switch
    {
        SeleniumProfileBrowser.Edge => "Microsoft Edge",
        SeleniumProfileBrowser.Chrome => "Google Chrome",
        SeleniumProfileBrowser.Firefox => "Mozilla Firefox",
        _ => browser.ToString()
    };

    public string SeleniumProfileCreated(string name) => IsCzech ? $"Profil {name} byl bezpečně vytvořen." : $"Profile {name} was created safely.";

    public string EditSeleniumProfile => IsCzech ? "Upravit profil" : "Edit profile";

    public string EditSeleniumProfileTitle => IsCzech ? "Úprava master profilu" : "Edit master profile";

    public string EditSeleniumProfileQuestion(string name) => IsCzech
        ? $"Otevřít pracovní kopii profilu {name}? Po zavření browseru aplikace kopii ověří a bezpečně jí nahradí současný master. ID profilu zůstane stejné."
        : $"Open a working copy of profile {name}? After the browser closes, the app verifies it and safely replaces the current master. The profile ID stays unchanged.";

    public string SeleniumProfilePreparingEdit => IsCzech
        ? "Připravuji zapisovatelnou pracovní kopii master profilu…"
        : "Preparing a writable working copy of the master profile…";

    public string SeleniumProfileEditing => IsCzech
        ? "Profil je otevřený pro úpravy. Po dokončení zavřete všechna okna browseru."
        : "The profile is open for editing. Close all browser windows when finished.";

    public string SeleniumProfileUpdated(string name) => IsCzech
        ? $"Master profil {name} byl bezpečně aktualizován; jeho ID zůstalo stejné."
        : $"Master profile {name} was safely updated; its ID stayed unchanged.";

    public string SeleniumProfileUpdateFailed(string detail) => IsCzech
        ? $"Úprava profilu selhala: {detail}"
        : $"Profile update failed: {detail}";

    public string CopyId => IsCzech ? "Kopírovat ID" : "Copy ID";

    public string ProfileIdCopied => IsCzech ? "ID profilu bylo zkopírováno." : "Profile ID was copied.";

    public string CookieVaultIdCopied => IsCzech ? "ID cookie vaultu bylo zkopírováno." : "Cookie vault ID was copied.";

    public string CopyIdFailed(string detail) => IsCzech
        ? $"Kopírování ID selhalo: {detail}"
        : $"Copying the ID failed: {detail}";

    public string SeleniumProfileCreateFailed(string detail) => IsCzech ? $"Vytvoření profilu selhalo: {detail}" : $"Profile creation failed: {detail}";

    public string RemoveSeleniumProfileTitle => IsCzech ? "Odebrání master profilu" : "Remove master profile";

    public string RemoveSeleniumProfileQuestion(string name) => IsCzech
        ? $"Opravdu odebrat master profil {name}? Zdrojový profil mimo aplikaci zůstane beze změny."
        : $"Remove master profile {name}? The original profile outside the application will remain unchanged.";

    public string SeleniumProfileRemoved => IsCzech ? "Master profil byl odebrán." : "The master profile was removed.";

    public string ReloadDrivers => IsCzech ? "Obnovit browsery" : "Refresh browsers";

    public string SeleniumDriverCount(int count) => IsCzech ? $"Připravené browsery: {count}" : $"Ready browsers: {count}";

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
        ? "Apache i MariaDB běží. phpMyAdmin je připravený."
        : "Apache and MariaDB are running. phpMyAdmin is ready.";

    public string PhpMyAdminNeedsWeb => IsCzech
        ? "Nejprve spusťte Apache na stránce Apache."
        : "Start Apache on the Apache page first.";

    public string PhpMyAdminNeedsPhp => IsCzech
        ? "Pro phpMyAdmin nejprve nainstalujte PHP. Samotný Apache může dál běžet jako statický server."
        : "Install PHP before using phpMyAdmin. Apache can continue running as a static server without it.";

    public string PhpMyAdminNeedsDatabase => IsCzech
        ? "Nejprve spusťte MariaDB."
        : "Start MariaDB first.";

    public string PhpMyAdminNeedsBoth => IsCzech
        ? "phpMyAdmin vyžaduje spuštěný Apache i MariaDB."
        : "phpMyAdmin requires both Apache and MariaDB to be running.";

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
            ? $"Verze {version} naslouchá na portu {port}; připravené browsery: {driverCount}."
            : $"Version {version} is listening on port {port}; ready browsers: {driverCount}.",
        ManagedProcessState.Starting => IsCzech ? "Spouštím lokální Standalone Grid…" : "Starting the local Standalone Grid…",
        ManagedProcessState.Stopping => IsCzech ? "Ukončuji Grid a jeho relace…" : "Stopping the Grid and its sessions…",
        _ when driverCount == 0 => IsCzech
            ? $"Verze {version} je ověřená. Před spuštěním stáhněte na kartě Browser prostředí alespoň jeden spravovaný browser."
            : $"Version {version} is verified. Download at least one managed browser on the Browser environments tab before starting.",
        _ => IsCzech
            ? $"Verze {version} je ověřená; připravené browsery: {driverCount}."
            : $"Version {version} is verified; ready browsers: {driverCount}."
    };

    public string Language => IsCzech ? "Jazyk rozhraní" : "Interface language";

    public string CacheManagement => IsCzech ? "Správa cache" : "Cache management";

    public string CacheManagementHelp => IsCzech
        ? "Instalační archivy jsou po úspěšné instalaci automaticky odstraněny. Zde lze bezpečně vyčistit pouze obnovitelné cache; nainstalované moduly ani projektová data se nemažou."
        : "Installation archives are removed automatically after a successful installation. Only reproducible caches can be cleared here; installed modules and project data are never deleted.";

    public string RuntimePackageCache => IsCzech ? "Stažené instalační balíčky" : "Downloaded installation packages";

    public string ComposerCache => "Composer cache";

    public string NpmCache => "npm cache";

    public string PipCache => "pip cache";

    public string TotalCache => IsCzech ? "Cache celkem" : "Total cache";

    public string ClearCache => IsCzech ? "Vyčistit" : "Clear";

    public string ClearAllCaches => IsCzech ? "Vyčistit vše" : "Clear all";

    public string ClearAllCachesQuestion => IsCzech
        ? "Vyčistit všechny obnovitelné cache? Nainstalované moduly ani projektová data se nesmažou."
        : "Clear every reproducible cache? Installed modules and project data will not be deleted.";

    public string AllCachesCleared(string size) => IsCzech
        ? $"Všechny cache byly vyčištěny. Uvolněno: {size}."
        : $"All caches were cleared. Reclaimed: {size}.";

    public string RefreshStorage => IsCzech ? "Přepočítat" : "Refresh";

    public string ProtectedStorage => IsCzech ? "Chráněná data" : "Protected storage";

    public string ProtectedStorageHelp => IsCzech
        ? "Tyto položky jsou pouze informativní. Automatické čištění se jich nikdy nedotkne."
        : "These values are informational only. Automatic cleanup never touches them.";

    public string InstalledRuntimes => IsCzech ? "Nainstalované moduly, drivery a nástroje" : "Installed modules, drivers, and tools";

    public string PersistentProjectData => IsCzech ? "Instance, projekty a profily" : "Instances, projects, and profiles";

    public string MeasuringStorage => IsCzech ? "Počítám využití úložiště…" : "Measuring storage usage…";

    public string StorageMeasured => IsCzech ? "Využití úložiště je aktuální." : "Storage usage is up to date.";

    public string StorageBusy => IsCzech
        ? "Cache nelze čistit během instalace balíčku nebo příkazu v terminálu. Počkejte na dokončení operace."
        : "Caches cannot be cleared while a package installation or terminal command is running. Wait for the operation to finish.";

    public string StorageMeasureFailed(string detail) => IsCzech
        ? $"Využití úložiště se nepodařilo zjistit: {detail}"
        : $"Storage usage could not be measured: {detail}";

    public string ClearCacheTitle => IsCzech ? "Vyčištění cache" : "Clear cache";

    public string ClearCacheQuestion(string cache) => IsCzech
        ? $"Opravdu vyčistit {cache}? Data lze znovu stáhnout a projektové soubory zůstanou beze změny."
        : $"Clear {cache}? The data can be downloaded again and project files will remain unchanged.";

    public string ClearingCache(string cache) => IsCzech ? $"Čistím {cache}…" : $"Clearing {cache}…";

    public string CacheCleared(string cache, string size) => IsCzech
        ? $"{cache} byla vyčištěna; uvolněno {size}."
        : $"{cache} was cleared; {size} released.";

    public string CacheClearFailed(string cache, string detail) => IsCzech
        ? $"{cache} se nepodařilo vyčistit: {detail}"
        : $"{cache} could not be cleared: {detail}";

    public string StorageCacheName(StorageCacheKind cache) => cache switch
    {
        StorageCacheKind.RuntimePackages => RuntimePackageCache,
        StorageCacheKind.Composer => ComposerCache,
        StorageCacheKind.Npm => NpmCache,
        StorageCacheKind.Pip => PipCache,
        _ => IsCzech ? "cache" : "cache"
    };

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

    public string ApacheAction(ManagedProcessState state) => state switch
    {
        ManagedProcessState.Running => IsCzech ? "Zastavit Apache" : "Stop Apache",
        ManagedProcessState.Starting => IsCzech ? "Spouštím…" : "Starting…",
        ManagedProcessState.Stopping => IsCzech ? "Zastavuji…" : "Stopping…",
        ManagedProcessState.Failed => IsCzech ? "Zkusit znovu" : "Try again",
        _ => IsCzech ? "Spustit Apache" : "Start Apache"
    };

    public string RestartApacheService => IsCzech ? "Restartovat Apache" : "Restart Apache";

    public string RestartingApacheService => IsCzech ? "Restartuji Apache…" : "Restarting Apache…";

    public string ApacheServiceRestarted => IsCzech ? "Apache byl restartován." : "Apache was restarted.";

    public string StackStatus(ManagedProcessState state) => state switch
    {
        ManagedProcessState.Stopped => IsCzech ? "Zastaveno" : "Stopped",
        ManagedProcessState.Starting => IsCzech ? "Spouští se" : "Starting",
        ManagedProcessState.Running => IsCzech ? "Běží" : "Running",
        ManagedProcessState.Stopping => IsCzech ? "Zastavuje se" : "Stopping",
        ManagedProcessState.Failed => IsCzech ? "Spuštění selhalo" : "Startup failed",
        _ => state.ToString()
    };

    public string StackSummary(ManagedProcessState state, string errorDetail, int apachePort, bool phpEnabled) => state switch
    {
        ManagedProcessState.Stopped => IsCzech
            ? phpEnabled ? "Apache je připravený; při startu připojí PHP FastCGI." : "Apache je připravený ke spuštění bez PHP."
            : phpEnabled ? "Apache is ready and will attach PHP FastCGI when started." : "Apache is ready to start without PHP.",
        ManagedProcessState.Starting => IsCzech
            ? phpEnabled ? "Spouštím Apache a jeho PHP FastCGI pracovní proces." : "Spouštím Apache bez PHP."
            : phpEnabled ? "Starting Apache and its PHP FastCGI worker." : "Starting Apache without PHP.",
        ManagedProcessState.Running => IsCzech
            ? $"Web je dostupný na http://127.0.0.1:{apachePort}. PHP je {(phpEnabled ? "aktivní" : "vypnuté")}."
            : $"The web server is available at http://127.0.0.1:{apachePort}. PHP is {(phpEnabled ? "enabled" : "not installed")}.",
        ManagedProcessState.Stopping => IsCzech
            ? phpEnabled ? "Ukončuji Apache a jeho PHP FastCGI pracovní proces." : "Ukončuji Apache."
            : phpEnabled ? "Stopping Apache and its PHP FastCGI worker." : "Stopping Apache.",
        ManagedProcessState.Failed => errorDetail,
        _ => string.Empty
    };

    public string ApacheReadyDetail(string version, bool phpEnabled) => IsCzech
        ? phpEnabled ? $"Ověřený Apache {version}; PHP FastCGI se připojí při startu." : $"Ověřený Apache {version}; statický server je připravený i bez PHP."
        : phpEnabled ? $"Verified Apache {version}; PHP FastCGI will attach on start." : $"Verified Apache {version}; the static server is ready without PHP.";

    public string ApacheRuntimeDetail(string version, int port, bool phpEnabled) => IsCzech
        ? $"Apache {version} běží na 127.0.0.1:{port}; PHP je {(phpEnabled ? "aktivní" : "vypnuté")}."
        : $"Apache {version} is running on 127.0.0.1:{port}; PHP is {(phpEnabled ? "enabled" : "not installed")}.";

    public string InitialStatus => IsCzech
        ? "Offline komponenty byly zkontrolovány."
        : "Offline components have been verified.";

    public string LanguageChanged => IsCzech ? "Jazyk aplikace byl změněn." : "Application language was changed.";

    public string OperationCanceled => IsCzech ? "Operace byla zrušena." : "The operation was cancelled.";

    public string OpenPortableDeveloper => IsCzech ? "Otevřít Portable Developer" : "Open Portable Developer";

    public string ExitPortableDeveloper => IsCzech ? "Ukončit Portable Developer" : "Exit Portable Developer";

    public string ExitPortableDeveloperTitle => IsCzech ? "Ukončit aplikaci" : "Exit application";

    public string ExitPortableDeveloperQuestion => IsCzech
        ? "Opravdu ukončit Portable Developer? Plánovač přestane spouštět úlohy a aplikace bezpečně zastaví všechny spravované služby a procesy."
        : "Exit Portable Developer? The scheduler will stop running tasks and the application will safely stop every managed service and process.";

    public string ExitPortableDeveloperConfirm => IsCzech ? "Ukončit" : "Exit";

    public string ApplicationContinuesInBackgroundTitle => IsCzech
        ? "Portable Developer stále běží"
        : "Portable Developer is still running";

    public string ApplicationContinuesInBackgroundMessage => IsCzech
        ? "Aplikace byla skryta do oznamovací oblasti. Otevřete ji dvojklikem na ikonu nebo ji ukončete z nabídky ikony."
        : "The application was hidden in the notification area. Double-click its icon to open it or exit from the icon menu.";

    public string OperationPleaseWait => IsCzech
        ? "Aplikace stále odpovídá. Počkejte prosím na dokončení operace."
        : "The application is still responsive. Please wait for the operation to finish.";

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

    public string SchedulerTasksTab => IsCzech ? "Úlohy" : "Tasks";

    public string SchedulerHistoryTab => IsCzech ? "Historie" : "History";

    public string NewScheduledTask => IsCzech ? "Nová úloha" : "New task";

    public string EditScheduledTask => IsCzech ? "Upravit" : "Edit";

    public string DeleteScheduledTask => IsCzech ? "Odstranit" : "Delete";

    public string RunScheduledTaskNow => IsCzech ? "Spustit nyní" : "Run now";

    public string NoScheduledTasks => IsCzech
        ? "Aktivní projekt zatím nemá žádné naplánované úlohy."
        : "The active project does not have any scheduled tasks yet.";

    public string NoScheduledTaskHistory => IsCzech
        ? "Aktivní projekt zatím nemá žádnou historii běhů."
        : "The active project does not have any run history yet.";

    public string SchedulerRunsOnlyWhileOpen => IsCzech
        ? "Úlohy se spouštějí pouze během běhu Portable Developeru. Zameškané běhy se nepouštějí zpětně."
        : "Tasks run only while Portable Developer is open. Missed runs are not replayed.";

    public string ScheduledTaskName => IsCzech ? "Název" : "Name";

    public string ScheduledTaskCommand => IsCzech ? "Typ" : "Type";

    public string ScheduledTaskTarget => IsCzech ? "Skript / npm úloha" : "Script / npm task";

    public string ScheduledTaskSchedule => IsCzech ? "Plán" : "Schedule";

    public string ScheduledTaskNextRun => IsCzech ? "Příští běh" : "Next run";

    public string ScheduledTaskLastRun => IsCzech ? "Poslední běh" : "Last run";

    public string ScheduledTaskStatus => IsCzech ? "Stav" : "Status";

    public string ScheduledTaskOutput => IsCzech ? "Výstup" : "Output";

    public string ScheduledTaskStarted => IsCzech ? "Spuštěno" : "Started";

    public string ScheduledTaskDuration => IsCzech ? "Doba" : "Duration";

    public string ScheduledTaskTriggerLabel => IsCzech ? "Spouštěč" : "Trigger";

    public string ScheduledTaskResult => IsCzech ? "Výsledek" : "Result";

    public string ScheduledTaskEnabled => IsCzech ? "Zapnutá" : "Enabled";

    public string ScheduledTaskDisabled => IsCzech ? "Vypnutá" : "Disabled";

    public string ScheduledTaskRunning => IsCzech ? "Právě běží" : "Running";

    public string ScheduledTaskNever => IsCzech ? "Nikdy" : "Never";

    public string ScheduledTaskNotScheduled => IsCzech ? "—" : "—";

    public string ScheduledTaskDialogTitle(bool editing) => IsCzech
        ? editing ? "Upravit naplánovanou úlohu" : "Nová naplánovaná úloha"
        : editing ? "Edit scheduled task" : "New scheduled task";

    public string ScheduledTaskTargetHelp => IsCzech
        ? "U skriptů zadejte cestu relativní ke kořeni projektu. U npm zadejte název skriptu z package.json."
        : "For scripts, enter a path relative to the project root. For npm, enter a script name from package.json.";

    public string ScheduledTaskArguments => IsCzech ? "Argumenty" : "Arguments";

    public string ScheduledTaskArgumentsHelp => IsCzech
        ? "Volitelné argumenty. Uvozovky zachovají mezery; nepoužívá se systémový shell."
        : "Optional arguments. Quotes preserve spaces; no system shell is used.";

    public string ScheduledTaskIntervalMinutes => IsCzech ? "Interval v minutách" : "Interval in minutes";

    public string ScheduledTaskTime => IsCzech ? "Čas (HH:mm)" : "Time (HH:mm)";

    public string ScheduledTaskDay => IsCzech ? "Den" : "Day";

    public string ScheduledTaskTimeout => IsCzech ? "Timeout v minutách" : "Timeout in minutes";

    public string SaveScheduledTask => IsCzech ? "Uložit úlohu" : "Save task";

    public string ScheduledTaskDeleteConfirmation(string name) => IsCzech
        ? $"Opravdu odstranit úlohu „{name}“? Její historie zůstane zachovaná."
        : $"Delete the task “{name}”? Its run history will be preserved.";

    public string ScheduledTaskSaved => IsCzech ? "Naplánovaná úloha byla uložena." : "The scheduled task was saved.";

    public string ScheduledTaskDeleted => IsCzech ? "Naplánovaná úloha byla odstraněna." : "The scheduled task was deleted.";

    public string ScheduledTaskOperationFailed(string detail) => IsCzech
        ? $"Operace plánovače selhala: {detail}"
        : $"Scheduler operation failed: {detail}";

    public string ScheduledTaskValidationFailed => IsCzech
        ? "Zkontrolujte název, relativní cestu nebo npm skript, plán a číselné hodnoty."
        : "Check the name, relative path or npm script, schedule, and numeric values.";

    public string ScheduledTaskCompleted(ScheduledTaskOutcome outcome) => IsCzech
        ? $"Úloha skončila: {ScheduledTaskOutcomeLabel(outcome)}."
        : $"Task finished: {ScheduledTaskOutcomeLabel(outcome)}.";

    public string ScheduledTaskCommandLabel(ScheduledTaskCommandKind kind) => kind switch
    {
        ScheduledTaskCommandKind.PhpScript => "PHP",
        ScheduledTaskCommandKind.PythonScript => "Python",
        ScheduledTaskCommandKind.NodeScript => "Node.js",
        ScheduledTaskCommandKind.NpmScript => "npm run",
        _ => kind.ToString()
    };

    public string ScheduledTaskScheduleLabel(ScheduledTaskSchedule schedule) => schedule.Kind switch
    {
        ScheduledTaskScheduleKind.ApplicationStart => IsCzech ? "Při spuštění aplikace" : "When the application starts",
        ScheduledTaskScheduleKind.Interval => IsCzech
            ? $"Každých {schedule.IntervalMinutes} min"
            : $"Every {schedule.IntervalMinutes} min",
        ScheduledTaskScheduleKind.Daily => IsCzech
            ? $"Denně v {schedule.Hour:00}:{schedule.Minute:00}"
            : $"Daily at {schedule.Hour:00}:{schedule.Minute:00}",
        ScheduledTaskScheduleKind.Weekly => IsCzech
            ? $"{ScheduledTaskDayLabel(schedule.DayOfWeek)} v {schedule.Hour:00}:{schedule.Minute:00}"
            : $"{ScheduledTaskDayLabel(schedule.DayOfWeek)} at {schedule.Hour:00}:{schedule.Minute:00}",
        _ => schedule.Kind.ToString()
    };

    public string ScheduledTaskScheduleKindLabel(ScheduledTaskScheduleKind kind) => kind switch
    {
        ScheduledTaskScheduleKind.ApplicationStart => IsCzech ? "Při spuštění aplikace" : "Application start",
        ScheduledTaskScheduleKind.Interval => IsCzech ? "Interval" : "Interval",
        ScheduledTaskScheduleKind.Daily => IsCzech ? "Denně" : "Daily",
        ScheduledTaskScheduleKind.Weekly => IsCzech ? "Týdně" : "Weekly",
        _ => kind.ToString()
    };

    public string ScheduledTaskDayLabel(DayOfWeek day) => day switch
    {
        DayOfWeek.Monday => IsCzech ? "Pondělí" : "Monday",
        DayOfWeek.Tuesday => IsCzech ? "Úterý" : "Tuesday",
        DayOfWeek.Wednesday => IsCzech ? "Středa" : "Wednesday",
        DayOfWeek.Thursday => IsCzech ? "Čtvrtek" : "Thursday",
        DayOfWeek.Friday => IsCzech ? "Pátek" : "Friday",
        DayOfWeek.Saturday => IsCzech ? "Sobota" : "Saturday",
        DayOfWeek.Sunday => IsCzech ? "Neděle" : "Sunday",
        _ => day.ToString()
    };

    public string ScheduledTaskOutcomeLabel(ScheduledTaskOutcome outcome) => outcome switch
    {
        ScheduledTaskOutcome.Succeeded => IsCzech ? "Úspěch" : "Succeeded",
        ScheduledTaskOutcome.Failed => IsCzech ? "Selhalo" : "Failed",
        ScheduledTaskOutcome.TimedOut => "Timeout",
        ScheduledTaskOutcome.Canceled => IsCzech ? "Zrušeno" : "Canceled",
        _ => outcome.ToString()
    };

    public string ScheduledTaskTrigger(ScheduledTaskTrigger trigger) => trigger switch
    {
        Application.Scheduling.ScheduledTaskTrigger.Manual => IsCzech ? "Ručně" : "Manual",
        Application.Scheduling.ScheduledTaskTrigger.ApplicationStart => IsCzech ? "Start aplikace" : "Application start",
        _ => IsCzech ? "Plán" : "Schedule"
    };

    public void SetLanguage(ApplicationLanguage language)
    {
        if (_currentLanguage == language)
        {
            return;
        }

        _currentLanguage = language;
        _settingsStore.Save(_settingsStore.Load() with { Language = language });
        OnPropertyChanged(string.Empty);
    }

    private bool IsCzech => _currentLanguage == ApplicationLanguage.Czech;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
