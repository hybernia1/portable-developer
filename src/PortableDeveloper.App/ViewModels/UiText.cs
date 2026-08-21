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
        NavigationPage.Settings => IsCzech ? "Nastavení aplikace" : "Application settings",
        _ => page.ToString()
    };

    public string WebStack => IsCzech ? "WEBOVÝ STACK" : "WEB STACK";

    public string TechnicalDetails => IsCzech ? "Technické informace" : "Technical information";

    public string ServiceControl => IsCzech ? "Ovládání služby" : "Service control";

    public string CurrentConfiguration => IsCzech ? "Aktuální konfigurace" : "Current configuration";

    public string PlannedConfiguration => IsCzech ? "Připravovaná konfigurace" : "Planned configuration";

    public string Planned => IsCzech ? "Plánováno" : "Planned";

    public string PhpConfigurationPlan => IsCzech
        ? "Editor php.ini zde nabídne bezpečné volby pro memory_limit, upload_max_filesize, error reporting a PHP extensions."
        : "The php.ini editor will expose safe options for memory_limit, upload_max_filesize, error reporting, and PHP extensions.";

    public string ApacheConfigurationPlan => IsCzech
        ? "Zde bude konfigurace portu, document rootu, modulů a virtual hosts bez zásahů do systémového hosts souboru."
        : "This page will configure the port, document root, modules, and virtual hosts without modifying the system hosts file.";

    public string DatabaseManagementPlan => IsCzech
        ? "Po doplnění MariaDB controlleru zde půjde vytvářet a mazat lokální databáze. První verze používá pouze účet root."
        : "After the MariaDB controller is added, this page will create and remove local databases. The first version uses only the root account.";

    public string SeleniumConfigurationPlan => IsCzech
        ? "Další krok přidá start/stop, port, health check a volby Selenium standalone serveru."
        : "The next step will add start/stop, port, health checks, and Selenium standalone server options.";

    public string ConnectionDetails => IsCzech ? "Připojení" : "Connection";

    public string Host => "Host";

    public string Port => IsCzech ? "Port" : "Port";

    public string User => IsCzech ? "Uživatel" : "User";

    public string Password => IsCzech ? "Heslo" : "Password";

    public string NoPassword => IsCzech ? "bez hesla" : "no password";

    public string Version => IsCzech ? "Verze" : "Version";

    public string BinaryStatus => IsCzech ? "Stav komponenty" : "Component status";

    public string PhpIni => "php.ini";

    public string DocumentRoot => "Document root";

    public string DocumentRootValue => "instances/default/www";

    public string LocalOnly => IsCzech ? "Pouze lokální vývoj" : "Local development only";

    public string RootAccountNote => IsCzech
        ? "Účet root bez hesla je dostupný pouze na 127.0.0.1 v této lokální portable instanci. Nepoužívejte jej pro produkci."
        : "The passwordless root account is only available at 127.0.0.1 in this local portable instance. Do not use it for production.";

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
