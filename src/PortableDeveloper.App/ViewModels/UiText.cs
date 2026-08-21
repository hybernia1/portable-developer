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

    public string WebStack => IsCzech ? "WEBOVÝ STACK" : "WEB STACK";

    public string TechnicalDetails => IsCzech ? "Technické informace" : "Technical information";

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
