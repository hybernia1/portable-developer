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

    public string RefreshModules => IsCzech ? "Obnovit moduly" : "Refresh modules";

    public string ApplicationRoot => IsCzech ? "KOŘEN APLIKACE" : "APPLICATION ROOT";

    public string InitializeMariaDb => IsCzech ? "Inicializovat MariaDB" : "Initialize MariaDB";

    public string InitializingMariaDb => IsCzech
        ? "Inicializuji databázový adresář MariaDB…"
        : "Initializing the MariaDB data directory…";

    public string MariaDbInitialized => IsCzech
        ? "MariaDB byla inicializována v portable instanci. Přihlašovací údaje jsou uloženy v její privátní state složce."
        : "MariaDB was initialized in the portable instance. Credentials are stored in its private state folder.";

    public string MariaDbAlreadyInitialized => IsCzech
        ? "MariaDB už je v této instanci inicializovaná."
        : "MariaDB is already initialized in this instance.";

    public string MariaDbInitializationFailed(string detail) => IsCzech
        ? $"Inicializace MariaDB selhala: {detail}"
        : $"MariaDB initialization failed: {detail}";

    public string StartStack => IsCzech ? "Spustit stack" : "Start stack";

    public string StopStack => IsCzech ? "Zastavit stack" : "Stop stack";

    public string StackStatus(ManagedProcessState state) => state switch
    {
        ManagedProcessState.Stopped => IsCzech ? "Stack je zastavený" : "Stack is stopped",
        ManagedProcessState.Starting => IsCzech ? "Stack se spouští" : "Stack is starting",
        ManagedProcessState.Running => IsCzech ? "Stack běží" : "Stack is running",
        ManagedProcessState.Stopping => IsCzech ? "Stack se zastavuje" : "Stack is stopping",
        ManagedProcessState.Failed => IsCzech ? "Stack selhal" : "Stack failed",
        _ => state.ToString()
    };

    public string InitialStatus => IsCzech
        ? "Všechny serverové moduly jsou součástí offline balíku."
        : "All server modules are included in the offline bundle.";

    public string ModulesRefreshed => IsCzech ? "Stav modulů byl obnoven." : "Module status was refreshed.";

    public string LanguageChanged => IsCzech ? "Jazyk aplikace byl změněn." : "Application language was changed.";

    public string InstallationCanceled => IsCzech ? "Instalace byla zrušena." : "Installation was cancelled.";

    public string ServiceDescription(string key) => key switch
    {
        "apache" => IsCzech ? "Webový server" : "Web server",
        "php" => IsCzech ? "Runtime pro webové aplikace" : "Runtime for web applications",
        "mariadb" => IsCzech ? "Lokální databáze" : "Local database",
        "selenium" => IsCzech ? "WebDriver server" : "WebDriver server",
        _ => throw new ArgumentOutOfRangeException(nameof(key))
    };

    public string ModuleNotFound => IsCzech ? "Modul nebyl nalezen ve složce modules/." : "Module was not found in the modules/ folder.";

    public string WaitingRuntime => IsCzech ? "Čeká na runtime" : "Waiting for runtime";

    public string RuntimeMissing(IEnumerable<string> missingFiles) => IsCzech
        ? $"Chybí app-local runtime: {string.Join(", ", missingFiles)}."
        : $"Missing app-local runtime: {string.Join(", ", missingFiles)}.";

    public string ReadyModule(string version) => IsCzech
        ? $"Verze {version} je součástí offline balíku a prošla kontrolou integrity."
        : $"Version {version} is bundled offline and passed its integrity check.";

    public string NotInstalled => IsCzech ? "Nenainstalováno" : "Not installed";

    public string Ready => IsCzech ? "Připraveno" : "Ready";

    public string VerificationFailed => IsCzech ? "Chyba integrity" : "Integrity error";

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
