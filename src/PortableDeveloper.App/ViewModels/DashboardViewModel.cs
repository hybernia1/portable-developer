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
using PortableDeveloper.Application.Scheduling;
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
    private ManagedProcessState _apacheProcessState = ManagedProcessState.Stopped;
    private string _apacheErrorDetail = string.Empty;
    private bool _webConfigurationRestartRequired;
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
    private IReadOnlyList<SeleniumCookieVaultInfo> _seleniumCookieVaults = [];
    private NavigationPage _selectedPage;
    private ProjectViewModel? _selectedProject;

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
        SeleniumCookieVaults = new ObservableCollection<SeleniumCookieVaultCardViewModel>();
        SeleniumBrowserChoices = new ObservableCollection<SeleniumBrowserChoiceViewModel>();
        PhpExtensions = new ObservableCollection<PhpExtensionViewModel>();
        Composer = new PackageManagerPageViewModel(Path.Combine("instances", "default", "www"));
        Node = new PackageManagerPageViewModel(Path.Combine("instances", "default", "www"));
        Python = new PackageManagerPageViewModel(Path.Combine("instances", "default", "python"));
        GlobalOperation = new GlobalOperationViewModel();
        WorkspaceEntries = new ObservableCollection<WorkspaceEntryViewModel>();
        Projects = new ObservableCollection<ProjectViewModel>();
        ProjectTemplates = new ObservableCollection<ProjectTemplateChoiceViewModel>();
        RegistrableProjectDirectories = new ObservableCollection<ManagedProjectDirectoryCandidate>();
        ScheduledTasks = new ObservableCollection<ScheduledTaskViewModel>();
        ScheduledTaskHistory = new ObservableCollection<ScheduledTaskRunViewModel>();
        WebProjects = new ObservableCollection<WebProjectViewModel>();
        TcpListeners = new ObservableCollection<TcpPortListenerViewModel>();
        NavigationItems = new ObservableCollection<NavigationItemViewModel>();
        RuntimePackages = new ObservableCollection<RuntimePackageViewModel>();
        SeleniumDriverPackages = new ObservableCollection<RuntimePackageViewModel>();
        RefreshRuntimePackages();
        RefreshProjectTemplates();
        RefreshNavigation();
        RefreshServices();
    }

    public string RootPath { get; }

    public string ApplicationVersion { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UiText Text { get; }

    public ObservableCollection<ServiceCardViewModel> Services { get; }

    public ObservableCollection<DatabaseCardViewModel> Databases { get; }

    public ObservableCollection<SeleniumDriverCardViewModel> SeleniumDrivers { get; }

    public ObservableCollection<SeleniumSessionCardViewModel> SeleniumSessions { get; }

    public ObservableCollection<SeleniumProfileCardViewModel> SeleniumProfiles { get; }

    public ObservableCollection<SeleniumCookieVaultCardViewModel> SeleniumCookieVaults { get; }

    public ObservableCollection<SeleniumBrowserChoiceViewModel> SeleniumBrowserChoices { get; }

    public ObservableCollection<PhpExtensionViewModel> PhpExtensions { get; }

    public PackageManagerPageViewModel Composer { get; }

    public PackageManagerPageViewModel Node { get; }

    public PackageManagerPageViewModel Python { get; }

    public GlobalOperationViewModel GlobalOperation { get; }

    public ObservableCollection<WorkspaceEntryViewModel> WorkspaceEntries { get; }

    public ObservableCollection<ProjectViewModel> Projects { get; }

    public ProjectViewModel? SelectedProject
    {
        get => _selectedProject;
        set
        {
            if (ReferenceEquals(_selectedProject, value))
            {
                return;
            }

            _selectedProject = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<ProjectTemplateChoiceViewModel> ProjectTemplates { get; }

    public ObservableCollection<ManagedProjectDirectoryCandidate> RegistrableProjectDirectories { get; }

    public ObservableCollection<ScheduledTaskViewModel> ScheduledTasks { get; }

    public ObservableCollection<ScheduledTaskRunViewModel> ScheduledTaskHistory { get; }

    public bool NoScheduledTasks => ScheduledTasks.Count == 0;

    public bool NoScheduledTaskHistory => ScheduledTaskHistory.Count == 0;

    public void SetScheduledTasks(
        IEnumerable<ScheduledTaskViewModel> tasks,
        IEnumerable<ScheduledTaskRunViewModel> history)
    {
        ScheduledTasks.Clear();
        foreach (var task in tasks)
        {
            ScheduledTasks.Add(task);
        }

        ScheduledTaskHistory.Clear();
        foreach (var record in history)
        {
            ScheduledTaskHistory.Add(record);
        }

        OnPropertyChanged(nameof(NoScheduledTasks));
        OnPropertyChanged(nameof(NoScheduledTaskHistory));
    }

    public bool NoRegistrableProjectDirectories => RegistrableProjectDirectories.Count == 0;

    public string ActiveProjectId { get; private set; } = ProjectCatalogDefaults.DefaultProjectId;

    public ProjectViewModel? ActiveProject => Projects.FirstOrDefault(project => project.IsActive);

    public string ActiveProjectName => ActiveProject?.Name ?? Text.DefaultProjectName;

    public ObservableCollection<WebProjectViewModel> WebProjects { get; }

    public string ActiveWebProjectId { get; private set; } = WebProjectCatalogDefaults.DefaultProjectId;

    public WebProjectViewModel? ActiveWebProject => WebProjects.FirstOrDefault(project => project.IsActive);

    public string ActiveWebProjectName => ActiveWebProject?.Name ?? "Default";

    public string ActiveDocumentRoot => ActiveWebProject is null
        ? Text.NotServedByApache
        : !ActiveWebProject.IsEnabled
            ? Text.NotServedByApache
        : ActiveWebProject.WebRootRelativePath == "."
            ? ActiveWebProject.ProjectRootRelativePath
            : Path.Combine(ActiveWebProject.ProjectRootRelativePath, ActiveWebProject.WebRootRelativePath);

    public ObservableCollection<TcpPortListenerViewModel> TcpListeners { get; }

    public bool NoWorkspaceEntries => WorkspaceEntries.Count == 0;

    public int WorkspacePageNumber { get; private set; } = 1;

    public int WorkspaceTotalPages { get; private set; } = 1;

    public int WorkspaceTotalCount { get; private set; }

    public int WorkspacePageSize { get; private set; } = 50;

    public bool WorkspaceHasPreviousPage => WorkspacePageNumber > 1;

    public bool WorkspaceHasNextPage => WorkspacePageNumber < WorkspaceTotalPages;

    public string WorkspacePageSummary => Text.WorkspacePageSummary(
        WorkspaceTotalCount == 0 ? 0 : ((WorkspacePageNumber - 1) * WorkspacePageSize) + 1,
        Math.Min(WorkspacePageNumber * WorkspacePageSize, WorkspaceTotalCount),
        WorkspaceTotalCount);

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

    public ServiceCardViewModel MariaDbService => GetServiceCard("MariaDB");

    public ServiceCardViewModel SeleniumService => GetServiceCard("Selenium");

    public bool ApacheReady => IsVerified(ModuleKind.Apache, "Apache");

    public bool PhpReady => IsVerified(ModuleKind.Php, "PHP");

    public string PhpRuntimeVersion => _moduleInventory.GetInstalled(ModuleKind.Php).FirstOrDefault()?.Version ?? string.Empty;

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
        && _apacheProcessState is not ManagedProcessState.Running and not ManagedProcessState.Starting and not ManagedProcessState.Stopping
        && _mariaDbProcessState is not ManagedProcessState.Running and not ManagedProcessState.Starting and not ManagedProcessState.Stopping
        && _seleniumProcessState is not ManagedProcessState.Running and not ManagedProcessState.Starting and not ManagedProcessState.Stopping;

    public string PortSettingsAvailability => PortSettingsEnabled
        ? Text.PortSettingsReady
        : Text.PortSettingsRequireStoppedServices;

    public string ApachePortStatus => GetPortStatus(ApachePort, ApacheIsRunning);

    public string PhpPortStatus => GetPortStatus(PhpFastCgiPort, ApacheIsRunning);

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

    public bool SeleniumProfileActionsEnabled => !_seleniumOperationInProgress;

    public bool SeleniumSessionActionsEnabled => SeleniumIsRunning && !_seleniumOperationInProgress;

    public string SeleniumActionLabel => Text.SeleniumAction(_seleniumProcessState);

    public string SeleniumSessionCount => Text.SeleniumSessionCount(SeleniumSessions.Count, SeleniumMaxSessions);

    public bool NoSeleniumSessions => SeleniumSessions.Count == 0;

    public bool NoSeleniumProfiles => SeleniumProfiles.Count == 0;

    public bool NoSeleniumCookieVaults => SeleniumCookieVaults.Count == 0;

    public string SeleniumDriverCount => Text.SeleniumDriverCount(_seleniumEnvironments.Count(environment => environment.IsReady));

    public ManagedProcessState MariaDbProcessState => _mariaDbProcessState;

    public bool MariaDbIsRunning => _mariaDbProcessState == ManagedProcessState.Running;

    public bool DatabaseActionsEnabled => MariaDbIsRunning && !_mariaDbOperationInProgress;

    public bool MariaDbActionEnabled => _mariaDbState == MariaDbInstanceState.Initialized
        && !_mariaDbOperationInProgress
        && _mariaDbProcessState is not ManagedProcessState.Starting and not ManagedProcessState.Stopping;

    public string MariaDbActionLabel => Text.MariaDbAction(_mariaDbProcessState);

    public string DatabaseCount => Text.DatabaseCount(Databases.Count);

    public bool RootPasswordSet => _rootPasswordSet;

    public string RootPasswordState => _rootPasswordSet ? Text.PasswordConfigured : Text.NoPasswordConfigured;

    public string RootPasswordActionLabel => _rootPasswordSet ? Text.ChangePassword : Text.SetPassword;

    public string PhpMyAdminUrl => $"http://127.0.0.1:{ApachePort}/phpmyadmin/";

    public PhpMyAdminAvailability PhpMyAdminState =>
        ServiceDependencyPolicy.GetPhpMyAdminAvailability(_apacheProcessState, _mariaDbProcessState);

    public bool PhpMyAdminActionEnabled => PhpMyAdminInstalled
        && PhpReady
        && !_mariaDbOperationInProgress
        && PhpMyAdminState == PhpMyAdminAvailability.Ready;

    public string PhpMyAdminDependencyState => !PhpReady ? Text.PhpMyAdminNeedsPhp : PhpMyAdminState switch
    {
        PhpMyAdminAvailability.Ready => Text.PhpMyAdminReady,
        PhpMyAdminAvailability.NeedsWeb => Text.PhpMyAdminNeedsWeb,
        PhpMyAdminAvailability.NeedsDatabase => Text.PhpMyAdminNeedsDatabase,
        _ => Text.PhpMyAdminNeedsBoth
    };

    public ManagedProcessState ApacheProcessState => _apacheProcessState;

    public bool ApacheIsRunning => _apacheProcessState == ManagedProcessState.Running;

    public string ApacheState => Text.StackStatus(_apacheProcessState);

    public string ApacheDetail => Text.StackSummary(_apacheProcessState, _apacheErrorDetail, ApachePort, PhpReady);

    public string ApacheActionLabel => Text.ApacheAction(_apacheProcessState);

    public bool ApacheActionEnabled => ApacheReady
        && _apacheProcessState is not ManagedProcessState.Starting and not ManagedProcessState.Stopping;

    public bool ApacheRestartEnabled => ApacheIsRunning && ApacheActionEnabled;

    public bool WebConfigurationRestartRequired => _webConfigurationRestartRequired;

    public bool WebConfigurationRestartPromptVisible => WebConfigurationRestartRequired && ApacheIsRunning;

    public bool WebConfigurationApplyEnabled => WebConfigurationRestartRequired && ApacheRestartEnabled;

    public string PhpSettingsActionLabel => ApacheIsRunning ? Text.SaveAndRestartPhp : Text.SavePhpSettings;

    public bool PhpSettingsEnabled => _apacheProcessState is not ManagedProcessState.Starting and not ManagedProcessState.Stopping;

    public void SetWebConfigurationRestartRequired(bool required)
    {
        if (_webConfigurationRestartRequired == required)
        {
            return;
        }

        _webConfigurationRestartRequired = required;
        OnPropertyChanged(nameof(WebConfigurationRestartRequired));
        OnPropertyChanged(nameof(WebConfigurationRestartPromptVisible));
        OnPropertyChanged(nameof(WebConfigurationApplyEnabled));
    }

    public void SetLanguage(ApplicationLanguage language)
    {
        Text.SetLanguage(language);
        RefreshRuntimePackages();
        RefreshProjectTemplates();
        RefreshNavigation();
        RefreshServices();
        NotifyApacheProperties();
        NotifyMariaDbProperties();
        NotifySeleniumProperties();
        SetSeleniumEnvironments(_seleniumEnvironments);
        SetSeleniumSessions(_seleniumSessions);
        SetSeleniumProfiles(_seleniumProfiles);
        SetSeleniumCookieVaults(_seleniumCookieVaults);
        OnPropertyChanged(nameof(PageTitle));
        OnPropertyChanged(nameof(DatabaseCount));
        OnPropertyChanged(nameof(RootPasswordState));
        OnPropertyChanged(nameof(RootPasswordActionLabel));
        RefreshTcpListeners(_tcpListeners);
        NotifyPortProperties();
    }

    public void SetApacheStatus(ManagedProcessState state, string detail)
    {
        _apacheProcessState = state;
        _apacheErrorDetail = state == ManagedProcessState.Failed ? detail : string.Empty;
        RefreshServices();
        NotifyApacheProperties();
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

    public void RefreshRuntimeAvailability()
    {
        RefreshRuntimePackages();
        RefreshNavigation();
        RefreshServices();
        NotifyApacheProperties();
        NotifyMariaDbProperties();
        NotifySeleniumProperties();
        OnPropertyChanged(nameof(ApacheReady));
        OnPropertyChanged(nameof(PhpReady));
        OnPropertyChanged(nameof(PhpRuntimeVersion));
        OnPropertyChanged(nameof(MariaDbInstalled));
        OnPropertyChanged(nameof(SeleniumInstalled));
        OnPropertyChanged(nameof(PhpMyAdminInstalled));
    }

    public void SetWorkspacePage(WorkspacePage page)
    {
        WorkspaceEntries.Clear();
        foreach (var entry in page.Entries)
        {
            WorkspaceEntries.Add(WorkspaceEntryViewModel.From(entry, Text));
        }

        WorkspacePageNumber = page.PageNumber;
        WorkspaceTotalPages = page.TotalPages;
        WorkspaceTotalCount = page.TotalCount;
        WorkspacePageSize = page.PageSize;
        OnPropertyChanged(nameof(NoWorkspaceEntries));
        OnPropertyChanged(nameof(WorkspacePageNumber));
        OnPropertyChanged(nameof(WorkspaceTotalPages));
        OnPropertyChanged(nameof(WorkspaceTotalCount));
        OnPropertyChanged(nameof(WorkspacePageSize));
        OnPropertyChanged(nameof(WorkspaceHasPreviousPage));
        OnPropertyChanged(nameof(WorkspaceHasNextPage));
        OnPropertyChanged(nameof(WorkspacePageSummary));
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

    public void SetProjects(
        IEnumerable<PortableProject> projects,
        string activeProjectId,
        IReadOnlyDictionary<string, ProjectCapabilitySnapshot>? capabilities = null)
    {
        var selectedProjectId = SelectedProject?.Id;
        if (!string.Equals(ActiveProjectId, activeProjectId, StringComparison.OrdinalIgnoreCase))
        {
            selectedProjectId = activeProjectId;
        }

        ActiveProjectId = activeProjectId;
        Projects.Clear();
        foreach (var project in projects)
        {
            ProjectCapabilitySnapshot? snapshot = null;
            capabilities?.TryGetValue(project.Id, out snapshot);
            Projects.Add(ProjectViewModel.From(
                project,
                activeProjectId,
                RootPath,
                Text,
                snapshot,
                GetMissingSharedRuntimes(snapshot)));
        }

        SelectedProject = Projects.FirstOrDefault(project =>
                              string.Equals(project.Id, selectedProjectId, StringComparison.OrdinalIgnoreCase))
                          ?? ActiveProject
                          ?? Projects.FirstOrDefault();

        OnPropertyChanged(nameof(ActiveProjectId));
        OnPropertyChanged(nameof(ActiveProject));
        OnPropertyChanged(nameof(ActiveProjectName));
    }

    public void SetRegistrableProjectDirectories(IEnumerable<ManagedProjectDirectoryCandidate> directories)
    {
        RegistrableProjectDirectories.Clear();
        foreach (var directory in directories)
        {
            RegistrableProjectDirectories.Add(directory);
        }

        OnPropertyChanged(nameof(NoRegistrableProjectDirectories));
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
        NotifyApacheProperties();
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
                environment.BrowserExecutablePath,
                Text.SeleniumEnvironmentState(environment.State),
                environment.Detail,
                environment.IsReady));
            if (environment.IsReady)
            {
                SeleniumBrowserChoices.Add(new(environment.Id, environment.DisplayName, environment.BrowserVersion));
            }
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

    public void SetSeleniumCookieVaults(IEnumerable<SeleniumCookieVaultInfo> vaults)
    {
        _seleniumCookieVaults = vaults.ToArray();
        SeleniumCookieVaults.Clear();
        foreach (var vault in _seleniumCookieVaults)
        {
            SeleniumCookieVaults.Add(new(
                vault.Id,
                vault.Name,
                vault.Domains.Count == 0 ? Text.NoCookieDomains : string.Join(", ", vault.Domains),
                Text.CookieCount(vault.CookieCount),
                $"portable:vault = {vault.Id}",
                vault.IsDamaged
                    ? Text.DamagedVault(vault.Detail)
                    : Text.CookieVaultReady));
        }

        OnPropertyChanged(nameof(NoSeleniumCookieVaults));
        OnPropertyChanged(nameof(SeleniumCookieVaultCount));
    }

    public string SeleniumCookieVaultCount => Text.CookieVaultCount(SeleniumCookieVaults.Count);

    private void RefreshServices()
    {
        Services.Clear();
        AddModuleCard(ModuleKind.Apache, "Apache", "apache");
        AddModuleCard(ModuleKind.MariaDb, "MariaDB", "mariadb");
        AddModuleCard(ModuleKind.Selenium, "Selenium", "selenium");
        OnPropertyChanged(nameof(ApacheService));
        OnPropertyChanged(nameof(MariaDbService));
        OnPropertyChanged(nameof(SeleniumService));
    }

    private void RefreshNavigation()
    {
        NavigationItems.Clear();
        var pages = new[]
        {
            NavigationPage.Projects,
            NavigationPage.Modules,
            NavigationPage.Ports,
            NavigationPage.Apache,
            NavigationPage.Php,
            NavigationPage.Databases,
            NavigationPage.Selenium,
            NavigationPage.Composer,
            NavigationPage.Node,
            NavigationPage.Python,
            NavigationPage.Scheduler,
            NavigationPage.Terminal,
            NavigationPage.Files,
            NavigationPage.Guides,
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
            SelectedPage = NavigationPage.Projects;
        }
    }

    private void RefreshRuntimePackages()
    {
        RuntimePackages.Clear();
        SeleniumDriverPackages.Clear();
        foreach (var package in _runtimePackages.GetPackages())
        {
            var viewModel = new RuntimePackageViewModel(
                package.Kind,
                Text.RuntimePackageName(package.Kind),
                Text.RuntimePackageDescription(package.Kind),
                package.Version,
                package.IsInstalled,
                string.Empty);
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

    private void RefreshProjectTemplates()
    {
        ProjectTemplates.Clear();
        foreach (var kind in Enum.GetValues<ProjectTemplateKind>())
        {
            ProjectTemplates.Add(new ProjectTemplateChoiceViewModel(
                kind,
                Text.ProjectTemplateName(kind),
                Text.ProjectTemplateDescription(kind)));
        }
    }

    private IReadOnlyList<string> GetMissingSharedRuntimes(ProjectCapabilitySnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return [];
        }

        var missing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var capability in snapshot.Capabilities.Select(capability => capability.Kind))
        {
            switch (capability)
            {
                case ProjectCapabilityKind.Web when !ApacheReady:
                    missing.Add("Apache");
                    break;
                case ProjectCapabilityKind.Php when !PhpReady:
                    missing.Add("PHP");
                    break;
                case ProjectCapabilityKind.NodeJs when !IsRuntimePackageInstalled(RuntimePackageKind.Node):
                    missing.Add("Node.js");
                    break;
                case ProjectCapabilityKind.Python when !IsRuntimePackageInstalled(RuntimePackageKind.Python):
                    missing.Add("Python");
                    break;
                case ProjectCapabilityKind.BrowserAutomation when !SeleniumInstalled:
                    missing.Add("Selenium");
                    break;
            }
        }

        return missing.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static bool IsSeleniumDriverPackage(RuntimePackageKind kind) => kind is
        RuntimePackageKind.SeleniumChromeEnvironment or
        RuntimePackageKind.SeleniumFirefoxEnvironment;

    private bool IsPageAvailable(NavigationPage page) => page switch
    {
        NavigationPage.Apache => IsVerified(ModuleKind.Apache, "Apache"),
        NavigationPage.Php => IsVerified(ModuleKind.Php, "PHP"),
        NavigationPage.Databases => MariaDbInstalled,
        NavigationPage.Selenium => SeleniumInstalled,
        NavigationPage.Composer => IsRuntimePackageInstalled(RuntimePackageKind.Composer),
        NavigationPage.Node => IsRuntimePackageInstalled(RuntimePackageKind.Node),
        NavigationPage.Python => IsRuntimePackageInstalled(RuntimePackageKind.Python),
        _ => true
    };

    private bool IsRuntimePackageInstalled(RuntimePackageKind kind) =>
        RuntimePackages.FirstOrDefault(item => item.Kind == kind)?.IsInstalled == true;

    private static (int GroupOrder, int ItemOrder) GetNavigationOrder(NavigationPage page) => page switch
    {
        NavigationPage.Projects => (0, 0),
        NavigationPage.Modules => (0, 1),
        NavigationPage.Ports => (0, 2),
        NavigationPage.Apache => (1, 0),
        NavigationPage.Databases => (1, 1),
        NavigationPage.Selenium => (1, 2),
        NavigationPage.Php => (2, 0),
        NavigationPage.Composer => (2, 1),
        NavigationPage.Node => (2, 2),
        NavigationPage.Python => (2, 3),
        NavigationPage.Scheduler => (2, 4),
        NavigationPage.Terminal => (2, 5),
        NavigationPage.Files => (2, 6),
        NavigationPage.Guides => (3, 0),
        NavigationPage.Settings => (3, 1),
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
            ModuleKind.Apache => CreateApacheServiceCard(name, description, installation.Version),
            ModuleKind.Php => throw new InvalidOperationException("PHP is a runtime, not a controllable service."),
            ModuleKind.MariaDb => CreateMariaDbCard(name, description, installation.Version),
            ModuleKind.Selenium => CreateSeleniumCard(name, description, installation.Version),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        });
    }

    private ServiceCardViewModel CreateApacheServiceCard(string name, string description, string version)
    {
        var state = _apacheProcessState switch
        {
            ManagedProcessState.Running => Text.Running,
            ManagedProcessState.Starting => Text.Starting,
            ManagedProcessState.Stopping => Text.Stopping,
            ManagedProcessState.Failed => Text.Failed,
            _ => Text.Stopped
        };
        var detail = _apacheProcessState == ManagedProcessState.Running
            ? Text.ApacheRuntimeDetail(version, ApachePort, PhpReady)
            : Text.ApacheReadyDetail(version, PhpReady);
        return new ServiceCardViewModel(
            name,
            description,
            detail,
            state,
            "toggle-apache",
            Text.ApacheAction(_apacheProcessState),
            ApacheActionEnabled,
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

    private void NotifyApacheProperties()
    {
        OnPropertyChanged(nameof(ApacheProcessState));
        OnPropertyChanged(nameof(ApacheIsRunning));
        OnPropertyChanged(nameof(ApacheState));
        OnPropertyChanged(nameof(ApacheDetail));
        OnPropertyChanged(nameof(ApacheActionLabel));
        OnPropertyChanged(nameof(ApacheActionEnabled));
        OnPropertyChanged(nameof(ApacheRestartEnabled));
        OnPropertyChanged(nameof(WebConfigurationRestartPromptVisible));
        OnPropertyChanged(nameof(WebConfigurationApplyEnabled));
        OnPropertyChanged(nameof(PhpSettingsEnabled));
        OnPropertyChanged(nameof(PhpSettingsActionLabel));
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
        OnPropertyChanged(nameof(SeleniumProfileActionsEnabled));
        OnPropertyChanged(nameof(SeleniumSessionActionsEnabled));
        OnPropertyChanged(nameof(SeleniumActionLabel));
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

public sealed record ProjectViewModel(
    string Id,
    string Name,
    string RootRelativePath,
    string WebStatus,
    string WebDetail,
    string Availability,
    string Capabilities,
    string RuntimeReadiness,
    bool IsActive,
    bool IsDefault,
    bool IsDirectoryAvailable,
    bool HasWebConfiguration,
    bool IsWebEnabled,
    bool AllowHtaccess,
    string HostName,
    string HtaccessStatus,
    string HtaccessAction,
    string ApacheAction)
{
    public bool CanUnregister => !IsDefault;

    public bool CanToggleWeb => HasWebConfiguration && !IsDefault;

    public static ProjectViewModel From(
        PortableProject project,
        string activeProjectId,
        string portableRoot,
        UiText text,
        ProjectCapabilitySnapshot? snapshot,
        IReadOnlyList<string> missingSharedRuntimes)
    {
        var isDefault = string.Equals(project.Id, ProjectCatalogDefaults.DefaultProjectId, StringComparison.OrdinalIgnoreCase);
        var webStatus = project.Web switch
        {
            null => text.WebNotConfigured,
            { IsEnabled: true } => text.WebEnabled,
            _ => text.WebDisabled
        };
        var webDetail = project.Web is null
            ? text.WebNotConfiguredDetail
            : text.WebRootSummary(project.Web.RootRelativePath);
        var available = Directory.Exists(Path.Combine(portableRoot, project.RootRelativePath));
        var capabilityNames = snapshot?.Capabilities
            .Select(capability => text.ProjectCapability(capability.Kind))
            .ToArray() ?? [];
        return new ProjectViewModel(
            project.Id,
            isDefault ? text.DefaultProjectName : project.Name,
            project.RootRelativePath,
            webStatus,
            webDetail,
            available ? string.Empty : text.ProjectDirectoryMissing,
            capabilityNames.Length == 0 ? text.NoCapabilitiesDetected : string.Join(" · ", capabilityNames),
            capabilityNames.Length == 0
                ? text.CapabilityDetectionHint
                : missingSharedRuntimes.Count == 0
                    ? text.SharedRuntimesReady
                    : text.MissingSharedRuntimes(missingSharedRuntimes),
            string.Equals(project.Id, activeProjectId, StringComparison.OrdinalIgnoreCase),
            isDefault,
            available,
            project.Web is not null,
            project.Web?.IsEnabled == true,
            project.Web?.AllowHtaccess == true,
            isDefault ? "localhost" : $"{project.Id}.localhost",
            text.HtaccessStatus(project.Web?.AllowHtaccess == true),
            project.Web?.AllowHtaccess == true ? text.DisableHtaccess : text.EnableHtaccess,
            project.Web?.IsEnabled == true ? text.DisableInApache : text.EnableInApache);
    }
}

public sealed record ProjectTemplateChoiceViewModel(
    ProjectTemplateKind Kind,
    string Name,
    string Description);

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
