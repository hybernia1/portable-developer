using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using PortableDeveloper.Application.ApachePhp;
using PortableDeveloper.Application.MariaDb;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Application.Packages;
using PortableDeveloper.Application.Php;
using PortableDeveloper.Application.Ports;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Application.Projects;
using PortableDeveloper.Application.Selenium;
using PortableDeveloper.Application.Services;
using PortableDeveloper.Application.Settings;
using PortableDeveloper.Application.Workspace;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.App.ViewModels;

public sealed class DashboardViewModel : INotifyPropertyChanged
{
    private readonly IModuleInventory _moduleInventory;
    private readonly IModuleInstallationVerifier _moduleVerifier;
    private readonly IApacheRuntimePreflight _apacheRuntimePreflight;
    private readonly IPhpRuntimePreflight _phpRuntimePreflight;
    private readonly IRuntimePackageManager _runtimePackages;
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
    private PortSettings _portSettings;
    private IReadOnlyList<TcpPortListenerInfo> _tcpListeners = [];
    private IReadOnlyList<SeleniumBrowserEnvironmentInfo> _seleniumEnvironments = [];
    private IReadOnlyList<SeleniumSessionInfo> _seleniumSessions = [];
    private IReadOnlyList<SeleniumProfileInfo> _seleniumProfiles = [];
    private PortableToolRuntimeInfo _editorRuntime = new(
        PortableToolKind.Editor,
        false,
        string.Empty,
        string.Empty,
        "Portable editor has not been checked yet.");
    private NavigationPage _selectedPage;

    public DashboardViewModel(
        string rootPath,
        string applicationVersion,
        IModuleInventory moduleInventory,
        IModuleInstallationVerifier moduleVerifier,
        IApacheRuntimePreflight apacheRuntimePreflight,
        IPhpRuntimePreflight phpRuntimePreflight,
        IRuntimePackageManager runtimePackages,
        MariaDbInstanceState mariaDbState,
        PortSettings portSettings,
        UiText text)
    {
        RootPath = rootPath;
        ApplicationVersion = applicationVersion;
        _moduleInventory = moduleInventory;
        _moduleVerifier = moduleVerifier;
        _apacheRuntimePreflight = apacheRuntimePreflight;
        _phpRuntimePreflight = phpRuntimePreflight;
        _runtimePackages = runtimePackages;
        _mariaDbState = mariaDbState;
        _portSettings = PortSettingsValidator.Validate(portSettings);
        Text = text;
        Services = new ObservableCollection<ServiceCardViewModel>();
        Databases = new ObservableCollection<DatabaseCardViewModel>();
        SeleniumDrivers = new ObservableCollection<SeleniumDriverCardViewModel>();
        SeleniumSessions = new ObservableCollection<SeleniumSessionCardViewModel>();
        SeleniumProfiles = new ObservableCollection<SeleniumProfileCardViewModel>();
        SeleniumBrowserChoices = new ObservableCollection<SeleniumBrowserChoiceViewModel>();
        PhpExtensions = new ObservableCollection<PhpExtensionViewModel>();
        Composer = new PackageManagerPageViewModel(Path.Combine("instances", "default", "www"));
        Python = new PackageManagerPageViewModel(Path.Combine("instances", "default", "python"));
        WorkspaceEntries = new ObservableCollection<WorkspaceEntryViewModel>();
        WebProjects = new ObservableCollection<WebProjectViewModel>();
        TcpListeners = new ObservableCollection<TcpPortListenerViewModel>();
        NavigationItems = new ObservableCollection<NavigationItemViewModel>();
        RuntimePackages = new ObservableCollection<RuntimePackageViewModel>();
        SeleniumDriverPackages = new ObservableCollection<RuntimePackageViewModel>();
        RefreshRuntimePackages();
        RefreshNavigation();
        RefreshServices();
    }

    public string RootPath { get; }

    public string ApplicationVersion { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UiText Text { get; }

    public ObservableCollection<ServiceCardViewModel> Services { get; }

    public bool NoInstalledServices => Services.Count == 0;

    public ObservableCollection<DatabaseCardViewModel> Databases { get; }

    public ObservableCollection<SeleniumDriverCardViewModel> SeleniumDrivers { get; }

    public ObservableCollection<SeleniumSessionCardViewModel> SeleniumSessions { get; }

    public ObservableCollection<SeleniumProfileCardViewModel> SeleniumProfiles { get; }

    public ObservableCollection<SeleniumBrowserChoiceViewModel> SeleniumBrowserChoices { get; }

    public ObservableCollection<PhpExtensionViewModel> PhpExtensions { get; }

    public PackageManagerPageViewModel Composer { get; }

    public PackageManagerPageViewModel Python { get; }

    public ObservableCollection<WorkspaceEntryViewModel> WorkspaceEntries { get; }

    public ObservableCollection<WebProjectViewModel> WebProjects { get; }

    public string ActiveWebProjectId { get; private set; } = WebProjectCatalogDefaults.DefaultProjectId;

    public WebProjectViewModel? ActiveWebProject => WebProjects.FirstOrDefault(project => project.IsActive);

    public string ActiveWebProjectName => ActiveWebProject?.Name ?? "Default";

    public string ActiveDocumentRoot => ActiveWebProject is null
        ? WebProjectCatalogDefaults.DefaultProject.DocumentRootRelativePath
        : ActiveWebProject.WebRootRelativePath == "."
            ? ActiveWebProject.ProjectRootRelativePath
            : Path.Combine(ActiveWebProject.ProjectRootRelativePath, ActiveWebProject.WebRootRelativePath);

    public ObservableCollection<TcpPortListenerViewModel> TcpListeners { get; }

    public bool NoWorkspaceEntries => WorkspaceEntries.Count == 0;

    public bool EditorReady => _editorRuntime.IsReady;

    public string EditorVersionLabel => string.IsNullOrWhiteSpace(_editorRuntime.Version)
        ? Text.NotInstalled
        : $"{Text.Version} {_editorRuntime.Version}";

    public string EditorDetail => _editorRuntime.IsReady
        ? Text.VerifiedPortableEditor(_editorRuntime.Version)
        : _editorRuntime.Detail;

    public ObservableCollection<NavigationItemViewModel> NavigationItems { get; }

    public ObservableCollection<RuntimePackageViewModel> RuntimePackages { get; }

    public ObservableCollection<RuntimePackageViewModel> SeleniumDriverPackages { get; }

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

    public ServiceCardViewModel ApacheService => GetServiceCard("Apache");

    public ServiceCardViewModel PhpService => GetServiceCard("PHP");

    public ServiceCardViewModel MariaDbService => GetServiceCard("MariaDB");

    public ServiceCardViewModel SeleniumService => GetServiceCard("Selenium");

    public bool WebStackInstalled => IsVerified(ModuleKind.Apache, "Apache") && IsVerified(ModuleKind.Php, "PHP");

    public bool MariaDbInstalled => IsVerified(ModuleKind.MariaDb, "MariaDB");

    public bool SeleniumInstalled => RuntimePackages.FirstOrDefault(item => item.Kind == RuntimePackageKind.Selenium)?.IsInstalled == true;

    public bool PhpMyAdminInstalled => RuntimePackages.FirstOrDefault(item => item.Kind == RuntimePackageKind.PhpMyAdmin)?.IsInstalled == true;

    public int ApachePort => _portSettings.ApachePort;

    public int PhpFastCgiPort => _portSettings.PhpFastCgiPort;

    public int MariaDbPort => _portSettings.MariaDbPort;

    public int SeleniumPort => _portSettings.SeleniumPort;

    public bool PortSettingsEnabled =>
        !_mariaDbOperationInProgress
        && !_seleniumOperationInProgress
        && _stackState is not ManagedProcessState.Running and not ManagedProcessState.Starting and not ManagedProcessState.Stopping
        && _mariaDbProcessState is not ManagedProcessState.Running and not ManagedProcessState.Starting and not ManagedProcessState.Stopping
        && _seleniumProcessState is not ManagedProcessState.Running and not ManagedProcessState.Starting and not ManagedProcessState.Stopping;

    public string PortSettingsAvailability => PortSettingsEnabled
        ? Text.PortSettingsReady
        : Text.PortSettingsRequireStoppedServices;

    public string ApachePortStatus => GetPortStatus(ApachePort, StackIsRunning);

    public string PhpPortStatus => GetPortStatus(PhpFastCgiPort, StackIsRunning);

    public string MariaDbPortStatus => GetPortStatus(MariaDbPort, MariaDbIsRunning);

    public string SeleniumPortStatus => GetPortStatus(SeleniumPort, SeleniumIsRunning);

    public string TcpListenerCount => Text.TcpListenerCount(TcpListeners.Count);

    public int SeleniumMaxSessions => _seleniumOptions.MaxSessions;

    public int SeleniumSessionTimeoutSeconds => _seleniumOptions.SessionTimeoutSeconds;

    public string SeleniumHubUrl => $"http://127.0.0.1:{SeleniumPort}/";

    public ManagedProcessState SeleniumProcessState => _seleniumProcessState;

    public bool SeleniumIsRunning => _seleniumProcessState == ManagedProcessState.Running;

    public bool SeleniumActionEnabled => !_seleniumOperationInProgress
        && _seleniumProcessState is not ManagedProcessState.Starting and not ManagedProcessState.Stopping
        && (SeleniumIsRunning || _seleniumEnvironments.Any(environment => environment.IsReady));

    public bool SeleniumSettingsEnabled => !SeleniumIsRunning && !_seleniumOperationInProgress;

    public bool SeleniumSessionActionsEnabled => SeleniumIsRunning && !_seleniumOperationInProgress;

    public string SeleniumActionLabel => Text.SeleniumAction(_seleniumProcessState);

    public string SeleniumActionBackground => SeleniumIsRunning ? "#6B3434" : "#2D6A4F";

    public string SeleniumActionBorder => SeleniumIsRunning ? "#A25B5B" : "#4F9A70";

    public string SeleniumSessionCount => Text.SeleniumSessionCount(SeleniumSessions.Count, SeleniumMaxSessions);

    public bool NoSeleniumSessions => SeleniumSessions.Count == 0;

    public bool NoSeleniumProfiles => SeleniumProfiles.Count == 0;

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

    public PhpMyAdminAvailability PhpMyAdminState =>
        ServiceDependencyPolicy.GetPhpMyAdminAvailability(_stackState, _mariaDbProcessState);

    public bool PhpMyAdminActionEnabled => PhpMyAdminInstalled
        && !_mariaDbOperationInProgress
        && PhpMyAdminState == PhpMyAdminAvailability.Ready;

    public string PhpMyAdminDependencyState => PhpMyAdminState switch
    {
        PhpMyAdminAvailability.Ready => Text.PhpMyAdminReady,
        PhpMyAdminAvailability.NeedsWeb => Text.PhpMyAdminNeedsWeb,
        PhpMyAdminAvailability.NeedsDatabase => Text.PhpMyAdminNeedsDatabase,
        _ => Text.PhpMyAdminNeedsBoth
    };

    public ManagedProcessState StackProcessState => _stackState;

    public bool StackIsRunning => _stackState == ManagedProcessState.Running;

    public string StackState => Text.StackStatus(_stackState);

    public string StackDetail => Text.StackSummary(_stackState, _stackErrorDetail, ApachePort);

    public string StackActionLabel => Text.StackAction(_stackState);

    public bool StackActionEnabled => WebStackInstalled
        && _stackState is not ManagedProcessState.Starting and not ManagedProcessState.Stopping;

    public bool StackRestartEnabled => StackIsRunning && StackActionEnabled;

    public string PhpSettingsActionLabel => StackIsRunning ? Text.SaveAndRestartPhp : Text.SavePhpSettings;

    public bool PhpSettingsEnabled => _stackState is not ManagedProcessState.Starting and not ManagedProcessState.Stopping;

    public string StackActionBackground => _stackState == ManagedProcessState.Running ? "#6B3434" : "#2D6A4F";

    public string StackActionBorder => _stackState == ManagedProcessState.Running ? "#A25B5B" : "#4F9A70";

    public void SetLanguage(ApplicationLanguage language)
    {
        Text.SetLanguage(language);
        RefreshRuntimePackages();
        RefreshNavigation();
        RefreshServices();
        NotifyStackProperties();
        NotifyMariaDbProperties();
        NotifySeleniumProperties();
        SetSeleniumEnvironments(_seleniumEnvironments);
        SetSeleniumSessions(_seleniumSessions);
        SetSeleniumProfiles(_seleniumProfiles);
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(DatabaseCount));
        OnPropertyChanged(nameof(RootPasswordState));
        OnPropertyChanged(nameof(RootPasswordActionLabel));
        OnPropertyChanged(nameof(EditorVersionLabel));
        OnPropertyChanged(nameof(EditorDetail));
        RefreshTcpListeners(_tcpListeners);
        NotifyPortProperties();
    }

    public void SetStackStatus(ManagedProcessState state, string detail)
    {
        _stackState = state;
        _stackErrorDetail = state == ManagedProcessState.Failed ? detail : string.Empty;
        RefreshServices();
        NotifyStackProperties();
        NotifyPortProperties();
    }

    public void SetPhpExtensions(IEnumerable<PhpExtensionViewModel> extensions)
    {
        PhpExtensions.Clear();
        foreach (var extension in extensions)
        {
            PhpExtensions.Add(extension);
        }
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
        NotifyPortProperties();
    }

    public void SetMariaDbStatus(ManagedProcessState state, string detail)
    {
        _mariaDbProcessState = state;
        _mariaDbErrorDetail = state == ManagedProcessState.Failed ? detail : string.Empty;
        RefreshServices();
        NotifyMariaDbProperties();
        NotifyPortProperties();
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

    public void SetEditorRuntime(PortableToolRuntimeInfo runtime)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        _editorRuntime = runtime;
        OnPropertyChanged(nameof(EditorReady));
        OnPropertyChanged(nameof(EditorVersionLabel));
        OnPropertyChanged(nameof(EditorDetail));
    }

    public void RefreshRuntimeAvailability()
    {
        RefreshRuntimePackages();
        RefreshNavigation();
        RefreshServices();
        NotifyStackProperties();
        NotifyMariaDbProperties();
        NotifySeleniumProperties();
        OnPropertyChanged(nameof(WebStackInstalled));
        OnPropertyChanged(nameof(MariaDbInstalled));
        OnPropertyChanged(nameof(SeleniumInstalled));
        OnPropertyChanged(nameof(PhpMyAdminInstalled));
    }

    public void SetWorkspaceEntries(IEnumerable<WorkspaceEntry> entries)
    {
        WorkspaceEntries.Clear();
        foreach (var entry in entries)
        {
            WorkspaceEntries.Add(WorkspaceEntryViewModel.From(entry, Text));
        }

        OnPropertyChanged(nameof(NoWorkspaceEntries));
    }

    public void SetWebProjects(IEnumerable<WebProject> projects, string activeProjectId)
    {
        ActiveWebProjectId = activeProjectId;
        WebProjects.Clear();
        foreach (var project in projects)
        {
            WebProjects.Add(WebProjectViewModel.From(project, activeProjectId, ApachePort, Text));
        }

        OnPropertyChanged(nameof(ActiveWebProjectId));
        OnPropertyChanged(nameof(ActiveWebProject));
        OnPropertyChanged(nameof(ActiveWebProjectName));
        OnPropertyChanged(nameof(ActiveDocumentRoot));
    }

    public void SetSeleniumOptions(SeleniumServerOptions options)
    {
        _seleniumOptions = options;
        NotifySeleniumProperties();
    }

    public void SetPortSettings(PortSettings settings)
    {
        _portSettings = PortSettingsValidator.Validate(settings);
        RefreshServices();
        NotifyPortProperties();
        NotifyStackProperties();
        NotifyMariaDbProperties();
        NotifySeleniumProperties();
        OnPropertyChanged(nameof(PhpMyAdminUrl));
    }

    public void SetTcpListeners(IEnumerable<TcpPortListenerInfo> listeners)
    {
        _tcpListeners = listeners.ToArray();
        RefreshTcpListeners(_tcpListeners);
        NotifyPortProperties();
    }

    public void SetSeleniumOperationInProgress(bool inProgress)
    {
        _seleniumOperationInProgress = inProgress;
        RefreshServices();
        NotifySeleniumProperties();
        NotifyPortProperties();
    }

    public void SetSeleniumStatus(ManagedProcessState state, string detail)
    {
        _seleniumProcessState = state;
        _seleniumErrorDetail = state == ManagedProcessState.Failed ? detail : string.Empty;
        RefreshServices();
        NotifySeleniumProperties();
        NotifyPortProperties();
    }

    public void SetSeleniumEnvironments(IEnumerable<SeleniumBrowserEnvironmentInfo> environments)
    {
        _seleniumEnvironments = environments.ToArray();
        SeleniumDrivers.Clear();
        SeleniumBrowserChoices.Clear();
        foreach (var environment in _seleniumEnvironments)
        {
            SeleniumDrivers.Add(new(
                environment.DisplayName,
                environment.BrowserVersion,
                environment.IsPortableBrowser ? environment.BrowserExecutablePath : Text.SystemBrowser,
                Text.SeleniumEnvironmentState(environment.State),
                environment.Detail,
                environment.IsReady));
            SeleniumBrowserChoices.Add(new(environment.Id, environment.DisplayName, environment.BrowserVersion));
        }

        OnPropertyChanged(nameof(SeleniumDriverCount));
        SetSeleniumProfiles(_seleniumProfiles);
        RefreshServices();
        NotifySeleniumProperties();
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

    public void SetSeleniumProfiles(IEnumerable<SeleniumProfileInfo> profiles)
    {
        _seleniumProfiles = profiles.ToArray();
        SeleniumProfiles.Clear();
        foreach (var profile in _seleniumProfiles)
        {
            var browserName = profile.Browser switch
            {
                SeleniumProfileBrowser.Edge => "MicrosoftEdge",
                SeleniumProfileBrowser.Chrome => "chrome",
                SeleniumProfileBrowser.Firefox => "firefox",
                _ => string.Empty
            };
            var hasReadyEnvironment = _seleniumEnvironments.Any(environment =>
                environment.IsReady && string.Equals(environment.BrowserName, browserName, StringComparison.OrdinalIgnoreCase));
            SeleniumProfiles.Add(new(
                profile.Id,
                profile.Name,
                Text.SeleniumProfileBrowserLabel(profile.Browser),
                FormatSize(profile.ApproximateSizeBytes),
                $"portable:profile = {profile.Id}",
                !profile.IsVerified
                    ? Text.DamagedProfile(profile.VerificationDetail)
                    : hasReadyEnvironment
                        ? Text.VerifiedProfile
                        : Text.ProfileBrowserUnavailable));
        }

        OnPropertyChanged(nameof(NoSeleniumProfiles));
        OnPropertyChanged(nameof(SeleniumProfileCount));
    }

    public string SeleniumProfileCount => Text.SeleniumProfileCount(SeleniumProfiles.Count);

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
        OnPropertyChanged(nameof(NoInstalledServices));
    }

    private void RefreshNavigation()
    {
        NavigationItems.Clear();
        var pages = new[]
        {
            NavigationPage.Dashboard,
            NavigationPage.Modules,
            NavigationPage.Ports,
            NavigationPage.Apache,
            NavigationPage.Php,
            NavigationPage.Databases,
            NavigationPage.Selenium,
            NavigationPage.Composer,
            NavigationPage.Python,
            NavigationPage.Terminal,
            NavigationPage.Files,
            NavigationPage.Tools,
            NavigationPage.Settings
        };
        foreach (var page in pages.Where(IsPageAvailable))
        {
            var (groupOrder, itemOrder) = GetNavigationOrder(page);
            NavigationItems.Add(new NavigationItemViewModel(
                page,
                Text.NavigationLabel(page),
                Text.NavigationGroup(groupOrder),
                groupOrder,
                itemOrder));
        }

        if (NavigationItems.All(item => item.Page != SelectedPage))
        {
            SelectedPage = NavigationPage.Dashboard;
        }
    }

    private void RefreshRuntimePackages()
    {
        RuntimePackages.Clear();
        SeleniumDriverPackages.Clear();
        foreach (var package in _runtimePackages.GetPackages())
        {
            if (package.Kind == RuntimePackageKind.SeleniumChromeDriver)
            {
                continue;
            }
            var viewModel = new RuntimePackageViewModel(
                package.Kind,
                Text.RuntimePackageName(package.Kind),
                Text.RuntimePackageDescription(package.Kind),
                package.Version,
                package.IsInstalled,
                package.IsInstalled ? Text.PackageInstalledAndVerified : Text.PackageMissingComponents);
            if (IsSeleniumDriverPackage(package.Kind))
            {
                SeleniumDriverPackages.Add(viewModel);
            }
            else
            {
                RuntimePackages.Add(viewModel);
            }
        }
    }

    private static bool IsSeleniumDriverPackage(RuntimePackageKind kind) => kind is
        RuntimePackageKind.SeleniumChromeEnvironment or
        RuntimePackageKind.SeleniumEdgeDriver or
        RuntimePackageKind.SeleniumChromeDriver or
        RuntimePackageKind.SeleniumFirefoxDriver;

    private bool IsPageAvailable(NavigationPage page) => page switch
    {
        NavigationPage.Apache => IsVerified(ModuleKind.Apache, "Apache"),
        NavigationPage.Php => IsVerified(ModuleKind.Php, "PHP"),
        NavigationPage.Databases => MariaDbInstalled,
        NavigationPage.Selenium => SeleniumInstalled,
        NavigationPage.Composer => IsRuntimePackageInstalled(RuntimePackageKind.Composer),
        NavigationPage.Python => IsRuntimePackageInstalled(RuntimePackageKind.Python),
        NavigationPage.Tools => IsRuntimePackageInstalled(RuntimePackageKind.Editor),
        _ => true
    };

    private bool IsRuntimePackageInstalled(RuntimePackageKind kind) =>
        RuntimePackages.FirstOrDefault(item => item.Kind == kind)?.IsInstalled == true;

    private static (int GroupOrder, int ItemOrder) GetNavigationOrder(NavigationPage page) => page switch
    {
        NavigationPage.Dashboard => (0, 0),
        NavigationPage.Modules => (0, 1),
        NavigationPage.Ports => (0, 2),
        NavigationPage.Apache => (1, 0),
        NavigationPage.Php => (1, 1),
        NavigationPage.Databases => (1, 2),
        NavigationPage.Selenium => (1, 3),
        NavigationPage.Composer => (2, 0),
        NavigationPage.Python => (2, 1),
        NavigationPage.Terminal => (2, 2),
        NavigationPage.Files => (2, 3),
        NavigationPage.Tools => (2, 4),
        NavigationPage.Settings => (3, 0),
        _ => (3, 99)
    };

    private bool IsVerified(ModuleKind kind, string displayName) => _moduleVerifier.Verify(kind, displayName).IsVerified;

    private ServiceCardViewModel GetServiceCard(string name) =>
        Services.FirstOrDefault(card => string.Equals(card.Name, name, StringComparison.OrdinalIgnoreCase))
        ?? new ServiceCardViewModel(name, string.Empty, Text.ModuleNotFound, Text.NotInstalled);

    private void AddModuleCard(ModuleKind kind, string name, string descriptionKey)
    {
        var description = Text.ServiceDescription(descriptionKey);
        var installation = _moduleInventory.GetInstalled(kind).FirstOrDefault();
        if (installation is null)
        {
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
            ModuleKind.Apache => CreateStackServiceCard(name, description, installation.Version, ApachePort),
            ModuleKind.Php => CreateStackServiceCard(name, description, installation.Version, PhpFastCgiPort),
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
        var canRestartFromCard = port == ApachePort && _stackState == ManagedProcessState.Running;
        return new ServiceCardViewModel(
            name,
            description,
            detail,
            state,
            canRestartFromCard ? "restart-web" : null,
            canRestartFromCard ? Text.RestartWebService : null,
            canRestartFromCard && StackRestartEnabled,
            version);
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
        OnPropertyChanged(nameof(StackIsRunning));
        OnPropertyChanged(nameof(StackState));
        OnPropertyChanged(nameof(StackDetail));
        OnPropertyChanged(nameof(StackActionLabel));
        OnPropertyChanged(nameof(StackActionEnabled));
        OnPropertyChanged(nameof(StackRestartEnabled));
        OnPropertyChanged(nameof(PhpSettingsEnabled));
        OnPropertyChanged(nameof(PhpSettingsActionLabel));
        OnPropertyChanged(nameof(StackActionBackground));
        OnPropertyChanged(nameof(StackActionBorder));
        OnPropertyChanged(nameof(PhpMyAdminActionEnabled));
        OnPropertyChanged(nameof(PhpMyAdminState));
        OnPropertyChanged(nameof(PhpMyAdminDependencyState));
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
        OnPropertyChanged(nameof(PhpMyAdminState));
        OnPropertyChanged(nameof(PhpMyAdminDependencyState));
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

    private void NotifyPortProperties()
    {
        OnPropertyChanged(nameof(ApachePort));
        OnPropertyChanged(nameof(PhpFastCgiPort));
        OnPropertyChanged(nameof(MariaDbPort));
        OnPropertyChanged(nameof(SeleniumPort));
        OnPropertyChanged(nameof(PortSettingsEnabled));
        OnPropertyChanged(nameof(PortSettingsAvailability));
        OnPropertyChanged(nameof(ApachePortStatus));
        OnPropertyChanged(nameof(PhpPortStatus));
        OnPropertyChanged(nameof(MariaDbPortStatus));
        OnPropertyChanged(nameof(SeleniumPortStatus));
        OnPropertyChanged(nameof(TcpListenerCount));
    }

    private void RefreshTcpListeners(IEnumerable<TcpPortListenerInfo> listeners)
    {
        TcpListeners.Clear();
        foreach (var listener in listeners)
        {
            TcpListeners.Add(new TcpPortListenerViewModel(
                listener.Address,
                listener.Port,
                Text.TcpListenerEndpoint(listener.Address, listener.Port)));
        }
    }

    private string GetPortStatus(int port, bool ownedByApplication)
    {
        if (ownedByApplication)
        {
            return Text.PortUsedByApplication;
        }

        return _tcpListeners.Any(listener => listener.Port == port)
            ? Text.PortOccupied
            : Text.PortAvailable;
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

public sealed record TcpPortListenerViewModel(string Address, int Port, string Endpoint);

public sealed record SeleniumDriverCardViewModel(
    string Name,
    string Version,
    string RelativePath,
    string Source,
    string Detail,
    bool IsReady);

public sealed record SeleniumBrowserChoiceViewModel(string Id, string Name, string Version)
{
    public string Label => string.IsNullOrWhiteSpace(Version) || Version == "unknown" ? Name : $"{Name} · {Version}";
}

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
