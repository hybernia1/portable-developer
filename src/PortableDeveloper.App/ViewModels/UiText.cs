using System.ComponentModel;
using System.Runtime.CompilerServices;
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
        NavigationPage.Php => "PHP",
        NavigationPage.Apache => "Apache",
        NavigationPage.Databases => IsCzech ? "Databáze" : "Databases",
        NavigationPage.Selenium => "Selenium",
        NavigationPage.Composer => "Composer",
        NavigationPage.Python => "Python",
        NavigationPage.Settings => IsCzech ? "Nastavení" : "Settings",
        _ => page.ToString()
    };

    public string PageTitle(NavigationPage page) => page switch
    {
        NavigationPage.Dashboard => IsCzech ? "Přehled prostředí" : "Environment overview",
        NavigationPage.Php => IsCzech ? "PHP runtime" : "PHP runtime",
        NavigationPage.Apache => IsCzech ? "Apache server" : "Apache server",
        NavigationPage.Databases => IsCzech ? "Databáze" : "Databases",
        NavigationPage.Selenium => "Selenium Server",
        NavigationPage.Composer => IsCzech ? "Composer balíčky" : "Composer packages",
        NavigationPage.Python => IsCzech ? "Python balíčky" : "Python packages",
        NavigationPage.Settings => IsCzech ? "Nastavení aplikace" : "Application settings",
        _ => page.ToString()
    };

    public string WebStack => IsCzech ? "WEBOVÝ STACK" : "WEB STACK";

    public string TechnicalDetails => IsCzech ? "Technické informace" : "Technical information";

    public string ServiceControl => IsCzech ? "Ovládání služby" : "Service control";

    public string CurrentConfiguration => IsCzech ? "Aktuální konfigurace" : "Current configuration";

    public string PlannedConfiguration => IsCzech ? "Připravovaná konfigurace" : "Planned configuration";

    public string Planned => IsCzech ? "Plánováno" : "Planned";

    public string PackageRuntime => IsCzech ? "Portable runtime" : "Portable runtime";

    public string ProjectDirectory => IsCzech ? "Složka projektu" : "Project directory";

    public string OpenProjectDirectory => IsCzech ? "Otevřít projekt" : "Open project";

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
        ? "Balíčky se instalují do instances/default/www/vendor. Například php-webdriver/webdriver umožní PHP projektu volat Selenium."
        : "Packages are installed into instances/default/www/vendor. For example, php-webdriver/webdriver lets a PHP project call Selenium.";

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

    public string ResetDefaults => IsCzech ? "Výchozí hodnoty" : "Default values";

    public string PhpSettingsInvalid => IsCzech
        ? "Zkontrolujte rozsahy: paměť 32–8192 MB, upload 1–2048 MB, POST 1–4096 MB, timeout 0–3600 s a max_input_vars 100–100000. POST limit nesmí být menší než upload."
        : "Check the ranges: memory 32–8192 MB, upload 1–2048 MB, POST 1–4096 MB, timeout 0–3600 s, and max_input_vars 100–100000. The POST limit cannot be smaller than the upload limit.";

    public string PhpSettingsSaved(ManagedProcessState stackState) => stackState == ManagedProcessState.Running
        ? IsCzech
            ? "PHP nastavení bylo uloženo. Pro použití zastavte a znovu spusťte webový stack."
            : "PHP settings were saved. Stop and restart the web stack to apply them."
        : IsCzech
            ? "PHP nastavení bylo uloženo a použije se při příštím startu webového stacku."
            : "PHP settings were saved and will be used the next time the web stack starts.";

    public string PhpSettingsSaveFailed(string detail) => IsCzech
        ? $"PHP nastavení se nepodařilo uložit: {detail}"
        : $"PHP settings could not be saved: {detail}";

    public string PhpDefaultsPrepared => IsCzech
        ? "Výchozí hodnoty jsou připravené ve formuláři. Potvrďte je tlačítkem Uložit PHP nastavení."
        : "Default values are ready in the form. Confirm them with Save PHP settings.";

    public string ApacheConfigurationPlan => IsCzech
        ? "Zde bude konfigurace portu, document rootu, modulů a virtual hosts bez zásahů do systémového hosts souboru."
        : "This page will configure the port, document root, modules, and virtual hosts without modifying the system hosts file.";

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
        ? "Firefox driver je součástí balíku. Další geckodriver.exe, chromedriver.exe nebo msedgedriver.exe vložte do drivers/custom a obnovte přehled. Vždy se použije nejvyšší nalezená verze pro daný prohlížeč."
        : "The Firefox driver is bundled. Add geckodriver.exe, chromedriver.exe, or msedgedriver.exe under drivers/custom and refresh. The highest detected version for each browser is used.";

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

    public string OpeningPhpMyAdmin => IsCzech ? "Spouštím potřebné servery a otevírám phpMyAdmin…" : "Starting required servers and opening phpMyAdmin…";

    public string Version => IsCzech ? "Verze" : "Version";

    public string BinaryStatus => IsCzech ? "Stav komponenty" : "Component status";

    public string PhpIni => "php.ini";

    public string DocumentRoot => "Document root";

    public string DocumentRootValue => "instances/default/www";

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

    public string StackStatus(ManagedProcessState state) => state switch
    {
        ManagedProcessState.Stopped => IsCzech ? "Zastaveno" : "Stopped",
        ManagedProcessState.Starting => IsCzech ? "Spouští se" : "Starting",
        ManagedProcessState.Running => IsCzech ? "Běží" : "Running",
        ManagedProcessState.Stopping => IsCzech ? "Zastavuje se" : "Stopping",
        ManagedProcessState.Failed => IsCzech ? "Spuštění selhalo" : "Startup failed",
        _ => state.ToString()
    };

    public string StackSummary(ManagedProcessState state, string errorDetail) => state switch
    {
        ManagedProcessState.Stopped => IsCzech
            ? "Apache a PHP jsou připravené ke spuštění."
            : "Apache and PHP are ready to start.",
        ManagedProcessState.Starting => IsCzech
            ? "Spouštím PHP FastCGI a potom Apache."
            : "Starting PHP FastCGI and then Apache.",
        ManagedProcessState.Running => IsCzech
            ? "Web je dostupný na http://127.0.0.1:8080."
            : "The web server is available at http://127.0.0.1:8080.",
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

    public string ModuleNotFound => IsCzech ? "Komponenta v offline balíku chybí." : "The component is missing from the offline bundle.";

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
