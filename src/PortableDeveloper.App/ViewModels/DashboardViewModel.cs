using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using PortableDeveloper.Application.ApachePhp;
using PortableDeveloper.Application.MariaDb;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Application.Php;
using PortableDeveloper.Application.Selenium;
using PortableDeveloper.Application.Settings;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.App.ViewModels;

public sealed class DashboardViewModel : INotifyPropertyChanged
{
    private readonly IModuleInventory _moduleInventory;
    private readonly IModuleInstallationVerifier _moduleVerifier;
    private readonly IApacheRuntimePreflight _apacheRuntimePreflight;
    private readonly IPhpRuntimePreflight _phpRuntimePreflight;
    private ManagedProcessState _stackState = ManagedProcessState.Stopped;
    private string _stackErrorDetail = string.Empty;
    private MariaDbInstanceState _mariaDbState;
    private ManagedProcessState _mariaDbProcessState = ManagedProcessState.Stopped;
    private string _mariaDbErrorDetail = string.Empty;
    private bool _mariaDbOperationInProgress;
    private bool _rootPasswordSet;
    private ManagedProcessState _seleniumProcessState = ManagedProcessState.Stopped;
    private string _seleniumErrorDetail = string.Empty;
    private bool _seleniumOperationInProgress;
    private SeleniumServerOptions _seleniumOptions = SeleniumServerOptions.Default;
    private IReadOnlyList<SeleniumDriverInfo> _seleniumDrivers = [];
    private IReadOnlyList<SeleniumSessionInfo> _seleniumSessions = [];
    private NavigationPage _selectedPage;

    public DashboardViewModel(
        string rootPath,
        IModuleInventory moduleInventory,
        IModuleInstallationVerifier moduleVerifier,
        IApacheRuntimePreflight apacheRuntimePreflight,
        IPhpRuntimePreflight phpRuntimePreflight,
        MariaDbInstanceState mariaDbState,
        UiText text)
    {
        RootPath = rootPath;
        _moduleInventory = moduleInventory;
        _moduleVerifier = moduleVerifier;
        _apacheRuntimePreflight = apacheRuntimePreflight;
        _phpRuntimePreflight = phpRuntimePreflight;
        _mariaDbState = mariaDbState;
        Text = text;
        Services = new ObservableCollection<ServiceCardViewModel>();
        Databases = new ObservableCollection<DatabaseCardViewModel>();
        SeleniumDrivers = new ObservableCollection<SeleniumDriverCardViewModel>();
        SeleniumSessions = new ObservableCollection<SeleniumSessionCardViewModel>();
        Composer = new PackageManagerPageViewModel(Path.Combine("instances", "default", "www"));
        Python = new PackageManagerPageViewModel(Path.Combine("instances", "default", "python"));
        NavigationItems = new ObservableCollection<NavigationItemViewModel>();
        RefreshNavigation();
        RefreshServices();
    }

    public string RootPath { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UiText Text { get; }

    public ObservableCollection<ServiceCardViewModel> Services { get; }

    public ObservableCollection<DatabaseCardViewModel> Databases { get; }

    public ObservableCollection<SeleniumDriverCardViewModel> SeleniumDrivers { get; }

    public ObservableCollection<SeleniumSessionCardViewModel> SeleniumSessions { get; }

    public PackageManagerPageViewModel Composer { get; }

    public PackageManagerPageViewModel Python { get; }

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

    public NavigationPage SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (_selectedPage == value)
            {
                return;
            }

            _selectedPage = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PageTitle));
        }
    }

    public string PageTitle => Text.PageTitle(_selectedPage);

    public ServiceCardViewModel ApacheService => Services[0];

    public ServiceCardViewModel PhpService => Services[1];

    public ServiceCardViewModel MariaDbService => Services[2];

    public ServiceCardViewModel SeleniumService => Services[3];

    public int ApachePort => 8080;

    public int PhpFastCgiPort => 9000;

    public int MariaDbPort => 3307;

    public int SeleniumPort => _seleniumOptions.Port;

    public int SeleniumMaxSessions => _seleniumOptions.MaxSessions;

    public int SeleniumSessionTimeoutSeconds => _seleniumOptions.SessionTimeoutSeconds;

    public string SeleniumHubUrl => $"http://127.0.0.1:{SeleniumPort}/";

    public ManagedProcessState SeleniumProcessState => _seleniumProcessState;

    public bool SeleniumIsRunning => _seleniumProcessState == ManagedProcessState.Running;

    public bool SeleniumActionEnabled => !_seleniumOperationInProgress
        && _seleniumProcessState is not ManagedProcessState.Starting and not ManagedProcessState.Stopping;

    public bool SeleniumSettingsEnabled => !SeleniumIsRunning && !_seleniumOperationInProgress;

    public bool SeleniumSessionActionsEnabled => SeleniumIsRunning && !_seleniumOperationInProgress;

    public string SeleniumActionLabel => Text.SeleniumAction(_seleniumProcessState);

    public string SeleniumActionBackground => SeleniumIsRunning ? "#6B3434" : "#2D6A4F";

    public string SeleniumActionBorder => SeleniumIsRunning ? "#A25B5B" : "#4F9A70";

    public string SeleniumSessionCount => Text.SeleniumSessionCount(SeleniumSessions.Count, SeleniumMaxSessions);

    public bool NoSeleniumSessions => SeleniumSessions.Count == 0;

    public string SeleniumDriverCount => Text.SeleniumDriverCount(SeleniumDrivers.Count);

    public ManagedProcessState MariaDbProcessState => _mariaDbProcessState;

    public bool MariaDbIsRunning => _mariaDbProcessState == ManagedProcessState.Running;

    public bool DatabaseActionsEnabled => MariaDbIsRunning && !_mariaDbOperationInProgress;

    public bool MariaDbActionEnabled => _mariaDbState == MariaDbInstanceState.Initialized
        && !_mariaDbOperationInProgress
        && _mariaDbProcessState is not ManagedProcessState.Starting and not ManagedProcessState.Stopping;

    public string MariaDbActionLabel => Text.MariaDbAction(_mariaDbProcessState);

    public string MariaDbActionBackground => MariaDbIsRunning ? "#6B3434" : "#2D6A4F";

    public string MariaDbActionBorder => MariaDbIsRunning ? "#A25B5B" : "#4F9A70";

    public string DatabaseCount => Text.DatabaseCount(Databases.Count);

    public bool RootPasswordSet => _rootPasswordSet;

    public string RootPasswordState => _rootPasswordSet ? Text.PasswordConfigured : Text.NoPasswordConfigured;

    public string RootPasswordActionLabel => _rootPasswordSet ? Text.ChangePassword : Text.SetPassword;

    public string PhpMyAdminUrl => $"http://127.0.0.1:{ApachePort}/phpmyadmin/";

    public bool PhpMyAdminActionEnabled => !_mariaDbOperationInProgress
        && _stackState is not ManagedProcessState.Starting and not ManagedProcessState.Stopping;

    public ManagedProcessState StackProcessState => _stackState;

    public string StackState => Text.StackStatus(_stackState);

    public string StackDetail => Text.StackSummary(_stackState, _stackErrorDetail);

    public string StackActionLabel => Text.StackAction(_stackState);

    public bool StackActionEnabled => _stackState is not ManagedProcessState.Starting and not ManagedProcessState.Stopping;

    public string StackActionBackground => _stackState == ManagedProcessState.Running ? "#6B3434" : "#2D6A4F";

    public string StackActionBorder => _stackState == ManagedProcessState.Running ? "#A25B5B" : "#4F9A70";

    public void SetLanguage(ApplicationLanguage language)
    {
        Text.SetLanguage(language);
        RefreshNavigation();
        RefreshServices();
        NotifyStackProperties();
        NotifyMariaDbProperties();
        NotifySeleniumProperties();
        SetSeleniumDrivers(_seleniumDrivers);
        SetSeleniumSessions(_seleniumSessions);
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(DatabaseCount));
        OnPropertyChanged(nameof(RootPasswordState));
        OnPropertyChanged(nameof(RootPasswordActionLabel));
    }

    public void SetStackStatus(ManagedProcessState state, string detail)
    {
        _stackState = state;
        _stackErrorDetail = state == ManagedProcessState.Failed ? detail : string.Empty;
        RefreshServices();
        NotifyStackProperties();
    }

    public void SetMariaDbState(MariaDbInstanceState state)
    {
        _mariaDbState = state;
        RefreshServices();
    }

    public void SetMariaDbOperationInProgress(bool inProgress)
    {
        _mariaDbOperationInProgress = inProgress;
        RefreshServices();
        NotifyMariaDbProperties();
    }

    public void SetMariaDbStatus(ManagedProcessState state, string detail)
    {
        _mariaDbProcessState = state;
        _mariaDbErrorDetail = state == ManagedProcessState.Failed ? detail : string.Empty;
        RefreshServices();
        NotifyMariaDbProperties();
    }

    public void SetDatabases(IEnumerable<DatabaseInfo> databases)
    {
        Databases.Clear();
        foreach (var database in databases)
        {
            Databases.Add(new DatabaseCardViewModel(
                database.Name,
                FormatSize(database.ApproximateSizeBytes),
                database.ApproximateSizeBytes));
        }

        OnPropertyChanged(nameof(DatabaseCount));
    }

    public void SetRootPasswordState(bool isSet)
    {
        _rootPasswordSet = isSet;
        OnPropertyChanged(nameof(RootPasswordSet));
        OnPropertyChanged(nameof(RootPasswordState));
        OnPropertyChanged(nameof(RootPasswordActionLabel));
    }

    public void SetSeleniumOptions(SeleniumServerOptions options)
    {
        _seleniumOptions = options;
        NotifySeleniumProperties();
    }

    public void SetSeleniumOperationInProgress(bool inProgress)
    {
        _seleniumOperationInProgress = inProgress;
        RefreshServices();
        NotifySeleniumProperties();
    }

    public void SetSeleniumStatus(ManagedProcessState state, string detail)
    {
        _seleniumProcessState = state;
        _seleniumErrorDetail = state == ManagedProcessState.Failed ? detail : string.Empty;
        RefreshServices();
        NotifySeleniumProperties();
    }

    public void SetSeleniumDrivers(IEnumerable<SeleniumDriverInfo> drivers)
    {
        _seleniumDrivers = drivers.ToArray();
        SeleniumDrivers.Clear();
        foreach (var driver in _seleniumDrivers)
        {
            SeleniumDrivers.Add(new(
                driver.DisplayName,
                driver.Version,
                driver.RelativePath,
                driver.IsBundled ? Text.VerifiedBundledDriver : Text.CustomDriver));
        }

        OnPropertyChanged(nameof(SeleniumDriverCount));
    }

    public void SetSeleniumSessions(IEnumerable<SeleniumSessionInfo> sessions)
    {
        _seleniumSessions = sessions.ToArray();
        SeleniumSessions.Clear();
        foreach (var session in _seleniumSessions)
        {
            SeleniumSessions.Add(new(
                session.Id,
                string.IsNullOrWhiteSpace(session.BrowserVersion)
                    ? session.BrowserName
                    : $"{session.BrowserName} {session.BrowserVersion}",
                session.PlatformName,
                session.StartedAtUtc?.ToLocalTime().ToString("g") ?? "—",
                FormatDuration(session.Duration)));
        }

        OnPropertyChanged(nameof(SeleniumSessionCount));
        OnPropertyChanged(nameof(NoSeleniumSessions));
    }

    private void RefreshServices()
    {
        Services.Clear();
        AddModuleCard(ModuleKind.Apache, "Apache", "apache");
        AddModuleCard(ModuleKind.Php, "PHP", "php");
        AddModuleCard(ModuleKind.MariaDb, "MariaDB", "mariadb");
        AddModuleCard(ModuleKind.Selenium, "Selenium", "selenium");
        OnPropertyChanged(nameof(ApacheService));
        OnPropertyChanged(nameof(PhpService));
        OnPropertyChanged(nameof(MariaDbService));
        OnPropertyChanged(nameof(SeleniumService));
    }

    private void RefreshNavigation()
    {
        NavigationItems.Clear();
        foreach (var page in Enum.GetValues<NavigationPage>())
        {
            NavigationItems.Add(new NavigationItemViewModel(page, Text.NavigationLabel(page)));
        }
    }

    private void AddModuleCard(ModuleKind kind, string name, string descriptionKey)
    {
        var description = Text.ServiceDescription(descriptionKey);
        var installation = _moduleInventory.GetInstalled(kind).FirstOrDefault();
        if (installation is null)
        {
            Services.Add(new ServiceCardViewModel(name, description, Text.ModuleNotFound, Text.NotInstalled));
            return;
        }

        if (kind == ModuleKind.Apache)
        {
            var readiness = _apacheRuntimePreflight.Check(installation.ModuleRootRelativePath);
            if (!readiness.IsReady)
            {
                Services.Add(new ServiceCardViewModel(name, description, Text.RuntimeMissing(readiness.MissingFiles), Text.WaitingRuntime));
                return;
            }
        }
        else if (kind == ModuleKind.Php)
        {
            var readiness = _phpRuntimePreflight.Check(installation.ModuleRootRelativePath);
            if (!readiness.IsReady)
            {
                Services.Add(new ServiceCardViewModel(name, description, Text.RuntimeMissing(readiness.MissingFiles), Text.WaitingRuntime));
                return;
            }
        }

        var verification = _moduleVerifier.Verify(kind, name);
        if (!verification.IsVerified)
        {
            Services.Add(new ServiceCardViewModel(name, description, verification.Detail, Text.VerificationFailed));
            return;
        }

        Services.Add(kind switch
        {
            ModuleKind.Apache => CreateStackServiceCard(name, description, installation.Version, 8080),
            ModuleKind.Php => CreateStackServiceCard(name, description, installation.Version, 9000),
            ModuleKind.MariaDb => CreateMariaDbCard(name, description, installation.Version),
            ModuleKind.Selenium => CreateSeleniumCard(name, description, installation.Version),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        });
    }

    private ServiceCardViewModel CreateStackServiceCard(string name, string description, string version, int port)
    {
        var state = _stackState switch
        {
            ManagedProcessState.Running => Text.Running,
            ManagedProcessState.Starting => Text.Starting,
            ManagedProcessState.Stopping => Text.Stopping,
            ManagedProcessState.Failed => Text.Failed,
            _ => Text.Stopped
        };
        var detail = _stackState == ManagedProcessState.Running
            ? Text.RunningModule(version, port)
            : Text.VerifiedModule(version);
        return new ServiceCardViewModel(name, description, detail, state, Version: version);
    }

    private ServiceCardViewModel CreateMariaDbCard(string name, string description, string version) => _mariaDbState switch
    {
        MariaDbInstanceState.Initialized => new(
            name,
            description,
            _mariaDbProcessState == ManagedProcessState.Failed
                ? _mariaDbErrorDetail
                : Text.MariaDbRuntimeDetail(version, _mariaDbProcessState, MariaDbPort),
            Text.StackStatus(_mariaDbProcessState),
            "toggle-mariadb",
            Text.MariaDbAction(_mariaDbProcessState),
            MariaDbActionEnabled,
            version),
        MariaDbInstanceState.Incomplete => new(
            name,
            description,
            Text.MariaDbInstanceIncomplete,
            Text.NeedsAttention,
            Version: version),
        _ => new(
            name,
            description,
            Text.MariaDbNeedsPreparation(version),
            _mariaDbOperationInProgress ? Text.Starting : Text.NeedsSetup,
            Version: version)
    };

    private ServiceCardViewModel CreateSeleniumCard(string name, string description, string version) => new(
        name,
        description,
        _seleniumProcessState == ManagedProcessState.Failed
            ? _seleniumErrorDetail
            : Text.SeleniumRuntimeDetail(version, _seleniumProcessState, SeleniumPort, SeleniumDrivers.Count),
        Text.StackStatus(_seleniumProcessState),
        "toggle-selenium",
        Text.SeleniumAction(_seleniumProcessState),
        SeleniumActionEnabled,
        version);

    private void NotifyStackProperties()
    {
        OnPropertyChanged(nameof(StackProcessState));
        OnPropertyChanged(nameof(StackState));
        OnPropertyChanged(nameof(StackDetail));
        OnPropertyChanged(nameof(StackActionLabel));
        OnPropertyChanged(nameof(StackActionEnabled));
        OnPropertyChanged(nameof(StackActionBackground));
        OnPropertyChanged(nameof(StackActionBorder));
        OnPropertyChanged(nameof(PhpMyAdminActionEnabled));
    }

    private void NotifyMariaDbProperties()
    {
        OnPropertyChanged(nameof(MariaDbProcessState));
        OnPropertyChanged(nameof(MariaDbIsRunning));
        OnPropertyChanged(nameof(DatabaseActionsEnabled));
        OnPropertyChanged(nameof(MariaDbActionEnabled));
        OnPropertyChanged(nameof(MariaDbActionLabel));
        OnPropertyChanged(nameof(MariaDbActionBackground));
        OnPropertyChanged(nameof(MariaDbActionBorder));
        OnPropertyChanged(nameof(PhpMyAdminActionEnabled));
    }

    private void NotifySeleniumProperties()
    {
        OnPropertyChanged(nameof(SeleniumPort));
        OnPropertyChanged(nameof(SeleniumMaxSessions));
        OnPropertyChanged(nameof(SeleniumSessionTimeoutSeconds));
        OnPropertyChanged(nameof(SeleniumHubUrl));
        OnPropertyChanged(nameof(SeleniumProcessState));
        OnPropertyChanged(nameof(SeleniumIsRunning));
        OnPropertyChanged(nameof(SeleniumActionEnabled));
        OnPropertyChanged(nameof(SeleniumSettingsEnabled));
        OnPropertyChanged(nameof(SeleniumSessionActionsEnabled));
        OnPropertyChanged(nameof(SeleniumActionLabel));
        OnPropertyChanged(nameof(SeleniumActionBackground));
        OnPropertyChanged(nameof(SeleniumActionBorder));
        OnPropertyChanged(nameof(SeleniumSessionCount));
        OnPropertyChanged(nameof(NoSeleniumSessions));
        OnPropertyChanged(nameof(SeleniumDriverCount));
    }

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)Math.Max(0, bytes);
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024d;
            unit++;
        }

        return unit == 0 ? $"{value:0} {units[unit]}" : $"{value:0.##} {units[unit]}";
    }

    private static string FormatDuration(TimeSpan duration) => duration.TotalHours >= 1
        ? $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}"
        : $"{duration.Minutes}:{duration.Seconds:00}";

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record DatabaseCardViewModel(string Name, string ApproximateSize, long ApproximateSizeBytes);

public sealed record SeleniumDriverCardViewModel(string Name, string Version, string RelativePath, string Source);

public sealed record SeleniumSessionCardViewModel(
    string Id,
    string Browser,
    string Platform,
    string StartedAt,
    string Duration);

public sealed record ServiceCardViewModel(
    string Name,
    string Description,
    string Detail,
    string State,
    string? ActionKey = null,
    string? ActionLabel = null,
    bool IsActionEnabled = true,
    string Version = "")
{
    public bool HasAction => !string.IsNullOrWhiteSpace(ActionKey);
}
