using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.App.ViewModels;

public sealed partial class UiText
{
    public string InitializingMariaDb => IsCzech
        ? "Připravuji datový adresář MariaDB…"
        : "Preparing the MariaDB data directory…";

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

    public string ApacheReadyDetail(string version, bool phpEnabled) => IsCzech
        ? phpEnabled ? $"Ověřený Apache {version}; PHP FastCGI se připojí při startu." : $"Ověřený Apache {version}; statický server je připravený i bez PHP."
        : phpEnabled ? $"Verified Apache {version}; PHP FastCGI will attach on start." : $"Verified Apache {version}; the static server is ready without PHP.";

    public string ApacheRuntimeDetail(string version, int port, bool phpEnabled) => IsCzech
        ? $"Apache {version} běží na 127.0.0.1:{port}; PHP je {(phpEnabled ? "aktivní" : "vypnuté")}."
        : $"Apache {version} is running on 127.0.0.1:{port}; PHP is {(phpEnabled ? "enabled" : "not installed")}.";

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

    public string MariaDbInstanceIncomplete => IsCzech
        ? "Datový adresář nebo přihlašovací údaje jsou neúplné. Existující soubory zůstaly beze změny."
        : "The data directory or credentials are incomplete. Existing files were left unchanged.";

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

}
