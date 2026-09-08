using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using PortableDeveloper.App.Shell;
using PortableDeveloper.Application.ApachePhp;
using PortableDeveloper.Application.MariaDb;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Application.Packages;
using PortableDeveloper.Application.Ports;
using PortableDeveloper.Application.Settings;

namespace PortableDeveloper.App.ViewModels;

public sealed class DashboardViewModel : INotifyPropertyChanged
{
    private static readonly NavigationPage[] WorkspacePages =
    [
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
    ];

    public DashboardViewModel(
        string rootPath,
        string applicationVersion,
        IModuleInventory moduleInventory,
        IModuleInstallationVerifier moduleVerifier,
        IApacheRuntimePreflight apacheRuntimePreflight,
        IRuntimePackageManager runtimePackages,
        MariaDbInstanceState mariaDbState,
        PortSettings portSettings,
        UiText text)
    {
        Text = text;
        GlobalOperation = new GlobalOperationViewModel();
        Notifications = new TransientNotificationViewModel();
        var projects = new ObservableCollection<ProjectViewModel>();
        Shell = new WorkspaceShellViewModel(Text, applicationVersion, projects, GlobalOperation);
        Shell.PropertyChanged += Shell_PropertyChanged;

        Runtime = new WorkspaceRuntimeCoordinator(
            Text,
            moduleInventory,
            moduleVerifier,
            apacheRuntimePreflight,
            runtimePackages,
            mariaDbState,
            portSettings);

        Composer = new PackageManagerPageViewModel(
            PackageManagerKind.Composer,
            Path.Combine("instances", "default", "www"));
        Node = new PackageManagerPageViewModel(
            PackageManagerKind.Node,
            Path.Combine("instances", "default", "www"));
        Python = new PackageManagerPageViewModel(
            PackageManagerKind.Python,
            Path.Combine("instances", "default", "python"));
        ComposerHost = new PackageManagerHostViewModel(Text, Composer);
        NodeHost = new PackageManagerHostViewModel(Text, Node);
        PythonHost = new PackageManagerHostViewModel(Text, Python);

        ModulesPage = new ModulesPageViewModel(Text, Runtime.RuntimePackages);
        ApachePage = new ApachePageViewModel(Text);
        PortsPage = new PortsPageViewModel(Text, Shell);
        SettingsPage = new SettingsPageViewModel(Text, Shell, applicationVersion);
        PhpPage = new PhpPageViewModel(Text, Shell);
        DatabasesPage = new DatabasesPageViewModel(Text, Shell);
        ProjectsPage = new ProjectsPageViewModel(Text, Shell, rootPath);
        SchedulerPage = new SchedulerPageViewModel(Text, Shell);
        GuidesPage = new GuidesPageViewModel(Text);
        TerminalPage = new TerminalPageViewModel(Text);
        FilesPage = new FilesPageViewModel(Text);
        SeleniumPage = new SeleniumPageViewModel(Text, Shell, Runtime.SeleniumDriverPackages);

        Runtime.StateChanged += Runtime_StateChanged;
        Runtime.AvailabilityChanged += Runtime_AvailabilityChanged;
        ApplyRuntimeState();
        RefreshNavigation();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UiText Text { get; }

    public WorkspaceShellViewModel Shell { get; }

    public WorkspaceRuntimeCoordinator Runtime { get; }

    public GlobalOperationViewModel GlobalOperation { get; }

    public TransientNotificationViewModel Notifications { get; }

    public ModulesPageViewModel ModulesPage { get; }

    public ApachePageViewModel ApachePage { get; }

    public PortsPageViewModel PortsPage { get; }

    public SettingsPageViewModel SettingsPage { get; }

    public PhpPageViewModel PhpPage { get; }

    public DatabasesPageViewModel DatabasesPage { get; }

    public ProjectsPageViewModel ProjectsPage { get; }

    public SchedulerPageViewModel SchedulerPage { get; }

    public GuidesPageViewModel GuidesPage { get; }

    public TerminalPageViewModel TerminalPage { get; }

    public FilesPageViewModel FilesPage { get; }

    public SeleniumPageViewModel SeleniumPage { get; }

    public PackageManagerPageViewModel Composer { get; }

    public PackageManagerPageViewModel Node { get; }

    public PackageManagerPageViewModel Python { get; }

    public PackageManagerHostViewModel ComposerHost { get; }

    public PackageManagerHostViewModel NodeHost { get; }

    public PackageManagerHostViewModel PythonHost { get; }

    public NavigationPage SelectedPage
    {
        get => Shell.SelectedPage;
        set => Shell.SelectedPage = value;
    }

    public object? CurrentPage => SelectedPage switch
    {
        NavigationPage.Modules => ModulesPage,
        NavigationPage.Apache => ApachePage,
        NavigationPage.Ports => PortsPage,
        NavigationPage.Settings => SettingsPage,
        NavigationPage.Php => PhpPage,
        NavigationPage.Databases => DatabasesPage,
        NavigationPage.Projects => ProjectsPage,
        NavigationPage.Scheduler => SchedulerPage,
        NavigationPage.Guides => GuidesPage,
        NavigationPage.Terminal => TerminalPage,
        NavigationPage.Files => FilesPage,
        NavigationPage.Selenium => SeleniumPage,
        NavigationPage.Composer => ComposerHost,
        NavigationPage.Node => NodeHost,
        NavigationPage.Python => PythonHost,
        _ => null
    };

    public void SetLanguage(ApplicationLanguage language)
    {
        Text.SetLanguage(language);
        Runtime.RefreshLocalizedPresentation();
        RefreshNavigation();
    }

    public void RefreshRuntimeAvailability() => Runtime.RefreshRuntimeAvailability();

    private void RefreshNavigation() => Shell.SetAvailablePages(WorkspacePages.Where(Runtime.IsPageAvailable));

    private void ApplyRuntimeState()
    {
        Shell.SetServiceStatus(NavigationPage.Apache, Runtime.ApacheIsRunning);
        Shell.SetServiceStatus(NavigationPage.Databases, Runtime.MariaDbIsRunning);
        Shell.SetServiceStatus(NavigationPage.Selenium, Runtime.SeleniumIsRunning);

        ApachePage.SetRuntimeState(new ApachePageRuntimeState(
            Runtime.ApacheService,
            Runtime.ApacheActionEnabled,
            Runtime.ApacheIsRunning,
            Runtime.ApacheActionLabel,
            Runtime.ApachePort));
        PortsPage.SetRuntimeState(
            Runtime.PortSettings,
            Runtime.ApacheIsRunning,
            Runtime.MariaDbIsRunning,
            Runtime.SeleniumIsRunning,
            Runtime.PortSettingsEnabled,
            Runtime.PortSettingsAvailability);
        SettingsPage.SetPorts(Runtime.PortSettings);
        PhpPage.SetRuntimeState(
            Runtime.PhpSettingsEnabled,
            Runtime.PhpSettingsActionLabel,
            Runtime.PhpRuntimeVersion);
        DatabasesPage.SetRuntimeState(new DatabasesPageRuntimeState(
            Runtime.MariaDbService,
            Runtime.MariaDbActionEnabled,
            Runtime.MariaDbIsRunning,
            Runtime.MariaDbActionLabel,
            Runtime.DatabaseActionsEnabled,
            Runtime.MariaDbPort,
            Runtime.RootPasswordState,
            Runtime.RootPasswordActionLabel,
            Runtime.PhpMyAdminInstalled,
            Runtime.PhpMyAdminUrl,
            Runtime.PhpMyAdminActionEnabled,
            Runtime.PhpMyAdminDependencyState));
        SeleniumPage.SetRuntimeState(new SeleniumPageRuntimeState(
            Runtime.SeleniumService,
            Runtime.SeleniumHubUrl,
            Runtime.SeleniumIsRunning,
            Runtime.SeleniumActionEnabled,
            Runtime.SeleniumSettingsEnabled,
            Runtime.SeleniumProfileActionsEnabled,
            Runtime.SeleniumSessionActionsEnabled,
            Runtime.SeleniumActionLabel,
            Runtime.SeleniumSettingsActionLabel,
            Runtime.SeleniumMaxSessions));
        ProjectsPage.SetRuntimeState(new ProjectsPageRuntimeState(
            Runtime.WebConfigurationRestartPromptVisible,
            Runtime.WebConfigurationApplyEnabled,
            Runtime.ApacheIsRunning,
            Runtime.ApacheReady,
            Runtime.PhpReady,
            Runtime.IsRuntimePackageInstalled(RuntimePackageKind.Node),
            Runtime.IsRuntimePackageInstalled(RuntimePackageKind.Python),
            Runtime.SeleniumInstalled));
    }

    private void Runtime_StateChanged(object? sender, EventArgs e) => ApplyRuntimeState();

    private void Runtime_AvailabilityChanged(object? sender, EventArgs e) => RefreshNavigation();

    private void Shell_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName)
            || e.PropertyName == nameof(WorkspaceShellViewModel.SelectedPage))
        {
            OnPropertyChanged(nameof(SelectedPage));
            OnPropertyChanged(nameof(CurrentPage));
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
