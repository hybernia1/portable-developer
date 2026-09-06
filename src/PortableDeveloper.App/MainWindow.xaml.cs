using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
using PortableDeveloper.App.Controls;
using PortableDeveloper.App.Guides;
using PortableDeveloper.App.ViewModels;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.ApachePhp;
using PortableDeveloper.Application.Settings;
using PortableDeveloper.Application.Storage;
using PortableDeveloper.Application.MariaDb;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Application.Packages;
using PortableDeveloper.Application.Php;
using PortableDeveloper.Application.Ports;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Application.Projects;
using PortableDeveloper.Application.Selenium;
using PortableDeveloper.Application.Scheduling;
using PortableDeveloper.Application.Workspace;
using PortableDeveloper.Infrastructure.Modules;
using PortableDeveloper.Infrastructure.Packages;
using PortableDeveloper.Infrastructure.Php;
using PortableDeveloper.Infrastructure.Ports;
using PortableDeveloper.Infrastructure.Processes;
using PortableDeveloper.Infrastructure.Settings;
using PortableDeveloper.Infrastructure.Storage;
using PortableDeveloper.Infrastructure.Health;
using PortableDeveloper.Infrastructure.ApachePhp;
using PortableDeveloper.Infrastructure.MariaDb;
using PortableDeveloper.Infrastructure.ProjectTools;
using PortableDeveloper.Infrastructure.Projects;
using PortableDeveloper.Infrastructure.Selenium;
using PortableDeveloper.Infrastructure.Scheduling;
using PortableDeveloper.Infrastructure.Workspace;

namespace PortableDeveloper.App;

public partial class MainWindow : Window
{
    private const int MaximumTerminalCharacters = 250_000;
    private const int MaximumPendingTerminalOutputCharacters = 400_000;
    private const string WorkspaceDragDataFormat = "PortableDeveloper.WorkspaceFileDrop";
    private readonly DashboardViewModel _dashboard;
    private readonly IApplicationLogger _logger;
    private readonly IApachePhpStackController _apachePhpStack;
    private readonly IMariaDbInstanceInitializer _mariaDbInitializer;
    private readonly IMariaDbServerController _mariaDbServer;
    private readonly IDatabaseCatalogService _databaseCatalog;
    private readonly IMariaDbAccountService _mariaDbAccount;
    private readonly ISeleniumServerController _seleniumServer;
    private readonly ISeleniumGridClient _seleniumGrid;
    private readonly ISeleniumDriverInventory _seleniumDriverInventory;
    private readonly ISeleniumBrowserEnvironmentInventory _seleniumEnvironmentInventory;
    private readonly ISeleniumSettingsStore _seleniumSettingsStore;
    private readonly ISeleniumProfileStore _seleniumProfileStore;
    private readonly ISeleniumCookieVaultStore _seleniumCookieVaultStore;
    private readonly IProjectPackageManagerService _composerPackageManager;
    private readonly IProjectPackageManagerService _nodePackageManager;
    private readonly IProjectPackageManagerService _pythonPackageManager;
    private readonly IProjectCatalog _projects;
    private readonly IWebProjectCatalog _webProjects;
    private readonly ProjectContext _projectContext;
    private readonly IProjectTemplateService _projectTemplateService;
    private readonly IProjectCapabilityDetector _projectCapabilityDetector;
    private readonly IProjectWebConfigurationService _projectWebConfigurationService;
    private readonly IPortableFileLauncher _fileLauncher;
    private readonly IApplicationSettingsStore _applicationSettingsStore;
    private readonly IPortableTerminalService _terminalService;
    private readonly IWorkspaceFileManager _workspaceFileManager;
    private readonly IPortableTaskScheduler _taskScheduler;
    private readonly IPhpSettingsStore _phpSettingsStore;
    private readonly IPortSettingsStore _portSettingsStore;
    private readonly ITcpPortUsageScanner _portUsageScanner;
    private readonly IRuntimePackageManager _runtimePackageManager;
    private readonly IStorageMaintenanceService _storageMaintenance;
    private readonly IModuleInventory _moduleInventory;
    private readonly IPortablePathResolver _paths;
    private readonly BuiltInGuideLibrary _guideLibrary;
    private readonly Forms.NotifyIcon _trayIcon;
    private readonly System.Drawing.Icon _trayIconImage;
    private MariaDbInstanceOptions _mariaDbOptions;
    private PortSettings _portSettings;
    private IReadOnlyList<TcpPortListenerInfo> _tcpListeners = [];
    private SeleniumServerOptions _seleniumOptions;
    private PhpSettings _phpSettings;
    private ApplicationSettings _applicationSettings;
    private readonly CancellationTokenSource _applicationLifetime = new();
    private bool _closeAfterStoppingStack;
    private bool _explicitExitRequested;
    private bool _sessionEnding;
    private bool _trayNotificationShown;
    private string _terminalWorkingDirectory = string.Empty;
    private string _workspaceDirectory = string.Empty;
    private WorkspaceClipboardEntry? _workspaceClipboard;
    private Point _workspaceDragStartPoint;
    private WorkspaceEntryViewModel? _workspaceDragAnchor;
    private WorkspaceEntryViewModel? _workspaceRenameCandidate;
    private readonly Stack<string> _workspaceHistory = new();
    private readonly List<string> _terminalHistory = [];
    private IReadOnlyList<SeleniumBrowserEnvironmentInfo> _seleniumEnvironments = [];
    private string? _selectedCookieFilePath;
    private int _terminalHistoryIndex;
    private int _terminalInputStart;
    private bool _terminalBusy;
    private IPortableProcessSession? _terminalSession;
    private readonly object _terminalOutputLock = new();
    private readonly StringBuilder _terminalOutputBuffer = new();
    private bool _terminalOutputFlushScheduled;
    private bool _runtimePackageInstallationInProgress;
    private bool _changingWebProject;
    private int _workspacePageNumber = 1;
    private int _workspacePageSize = 50;
    private WorkspaceSortColumn _workspaceSortColumn = WorkspaceSortColumn.Name;
    private WorkspaceSortDirection _workspaceSortDirection = WorkspaceSortDirection.Ascending;
    private CancellationTokenSource? _workspaceRefreshCancellation;
    private IReadOnlyDictionary<string, ProjectCapabilitySnapshot> _projectCapabilitySnapshots =
        new Dictionary<string, ProjectCapabilitySnapshot>(StringComparer.OrdinalIgnoreCase);
    private int _projectCapabilityRefreshRevision;
    private string _guideCategoryId = string.Empty;
    private string? _guideArticleId;
    private bool _updatingGuides;

    public MainWindow()
    {
        _guideLibrary = BuiltInGuideLibrary.Load();
        AppWindowChrome.Apply(this);
        InitializeComponent();

        var app = (App)System.Windows.Application.Current;
        _paths = app.Paths;
        _logger = app.Logger;
        app.Paths.EnsureDirectory("modules");
        app.Paths.EnsureDirectory("instances");
        app.Paths.EnsureDirectory("state");
        var moduleInventory = new FileModuleInventory(app.Paths);
        _moduleInventory = moduleInventory;
        var packageCatalog = new JsonModulePackageCatalog(app.Paths);
        var apacheRuntimePreflight = new ApacheRuntimePreflight(app.Paths);
        var phpRuntimePreflight = new PhpRuntimePreflight(app.Paths);
        var moduleVerifier = new ModuleInstallationVerifier(moduleInventory, packageCatalog, app.Paths);
        var commandRunner = new PortableCommandRunner(app.Paths, app.Logger);
        _phpSettingsStore = new JsonPhpSettingsStore(app.Paths);
        _phpSettings = _phpSettingsStore.Load();
        var toolInventory = new PortableToolRuntimeInventory(app.Paths);
        var dependencyCatalog = new JsonDependencyLockCatalog(app.Paths);
        _runtimePackageManager = new RuntimePackageManager(
            dependencyCatalog,
            packageCatalog,
            moduleVerifier,
            toolInventory,
            commandRunner,
            app.Paths,
            app.Logger);
        _storageMaintenance = new StorageMaintenanceService(app.Paths, app.Logger);
        _projects = new JsonProjectCatalog(app.Paths);
        _projectContext = new ProjectContext(_projects);
        var scheduledTaskCatalog = new JsonScheduledTaskCatalog(app.Paths);
        _taskScheduler = new PortableTaskScheduler(
            scheduledTaskCatalog,
            new JsonScheduledTaskHistoryStore(app.Paths),
            new PortableScheduledTaskExecutor(
                _projects,
                moduleVerifier,
                toolInventory,
                commandRunner,
                app.Paths),
            app.Logger);
        _taskScheduler.Changed += TaskScheduler_Changed;
        _webProjects = new ProjectWebCatalogAdapter(_projects, _projectContext, app.Paths);
        _projectTemplateService = new ProjectTemplateService(app.Paths, _projects, _projectContext);
        _projectCapabilityDetector = new ProjectCapabilityDetector(app.Paths);
        _projectWebConfigurationService = new ProjectWebConfigurationService(app.Paths, _projects);
        _applicationSettingsStore = new JsonApplicationSettingsStore(app.Paths);
        _applicationSettings = _applicationSettingsStore.Load();
        var editorService = new PortableEditorService(toolInventory, app.Paths, app.Logger);
        _fileLauncher = new PortableFileLauncher(app.Paths, editorService, _applicationSettingsStore, app.Logger);
        _terminalService = new PortableTerminalService(
            moduleVerifier,
            toolInventory,
            commandRunner,
            app.Paths,
            _projectContext,
            new PortableInteractiveCommandRunner(app.Paths, app.Logger));
        _workspaceFileManager = new WorkspaceFileManager(app.Paths, _projectContext);
        _composerPackageManager = new ComposerProjectPackageManager(
            toolInventory,
            moduleVerifier,
            commandRunner,
            app.Paths,
            _projectContext);
        _nodePackageManager = new NpmProjectPackageManager(toolInventory, commandRunner, app.Paths, _projectContext);
        _pythonPackageManager = new PythonProjectPackageManager(toolInventory, commandRunner, app.Paths);
        _mariaDbInitializer = new MariaDbInstanceInitializer(
            moduleVerifier,
            app.Paths,
            commandRunner,
            app.Logger);
        _mariaDbServer = new MariaDbServerController(
            moduleVerifier,
            new ManagedProcessSupervisor(app.Paths, app.Logger),
            commandRunner,
            new TcpPortHealthCheck(),
            app.Paths,
            app.Logger);
        _databaseCatalog = new MariaDbDatabaseCatalogService(moduleVerifier, commandRunner, app.Paths);
        _mariaDbAccount = new MariaDbAccountService(moduleVerifier, commandRunner, app.Paths, app.Logger);
        _seleniumDriverInventory = new SeleniumDriverInventory(app.Paths);
        _seleniumEnvironmentInventory = new SeleniumBrowserEnvironmentInventory(
            app.Paths,
            dependencyCatalog,
            _seleniumDriverInventory);
        _seleniumProfileStore = new SeleniumProfileStore(app.Paths, app.Logger);
        _seleniumProfileStore.DeleteInactiveManagedDrafts();
        _seleniumProfileStore.DeleteAllSessionCopies();
        _seleniumCookieVaultStore = new SeleniumCookieVaultStore(app.Paths, app.Logger);
        _seleniumSettingsStore = new JsonSeleniumSettingsStore(app.Paths);
        _seleniumOptions = _seleniumSettingsStore.Load();
        _portSettingsStore = new JsonPortSettingsStore(app.Paths);
        _portSettings = _portSettingsStore.Load(PortSettings.Default with
        {
            SeleniumPort = _seleniumOptions.Port
        });
        _portUsageScanner = new TcpPortUsageScanner();
        _mariaDbOptions = new MariaDbInstanceOptions(Port: _portSettings.MariaDbPort);
        _seleniumOptions = _seleniumOptions with { Port = _portSettings.SeleniumPort };
        _seleniumGrid = new SeleniumGridClient();
        _seleniumServer = new SeleniumServerController(
            moduleVerifier,
            _seleniumEnvironmentInventory,
            new SeleniumConfigurationGenerator(app.Paths),
            _seleniumGrid,
            new SeleniumProfileNodeExtension(app.Paths, commandRunner),
            new ManagedProcessSupervisor(app.Paths, app.Logger),
            app.Paths,
            app.Logger);
        _dashboard = new DashboardViewModel(
            app.Paths.RootPath,
            GetApplicationVersion(),
            moduleInventory,
            moduleVerifier,
            apacheRuntimePreflight,
            phpRuntimePreflight,
            _runtimePackageManager,
            _mariaDbInitializer.GetState(_mariaDbOptions),
            _portSettings,
            new UiText(_applicationSettingsStore));
        _projectContext.SetBlockReasonProvider(GetProjectSwitchBlockReason);
        _apachePhpStack = new ApachePhpStackController(
            moduleVerifier,
            apacheRuntimePreflight,
            phpRuntimePreflight,
            new ApachePhpConfigurationGenerator(app.Paths),
            new ManagedProcessSupervisor(app.Paths, app.Logger),
            new TcpPortHealthCheck(),
            app.Paths,
            app.Logger);
        DataContext = _dashboard;
        ProjectTemplateSelector.SelectedValue = ProjectTemplateKind.Empty;
        Sidebar.SelectedLanguage = _dashboard.Text.CurrentLanguage.ToString();
        EditorPreferenceSelector.SelectedValue = _applicationSettings.EditorPreference.ToString();
        var stackSnapshot = _apachePhpStack.GetSnapshot();
        _dashboard.SetApacheStatus(stackSnapshot.State, stackSnapshot.Detail);
        _dashboard.SetSeleniumOptions(_seleniumOptions);
        RefreshPortUsage();
        RefreshPhpExtensions();
        RefreshSeleniumEnvironments();
        _dashboard.SetSeleniumProfiles(_seleniumProfileStore.GetProfiles());
        RefreshCookieVaults();
        _dashboard.Composer.SetRuntime(_composerPackageManager.GetRuntime());
        _dashboard.Node.SetRuntime(_nodePackageManager.GetRuntime());
        RefreshWebProjectBindings();
        RefreshScheduledTaskBindings();
        _ = RefreshProjectCapabilitiesAsync();
        _dashboard.Python.SetRuntime(_pythonPackageManager.GetRuntime());
        var seleniumSnapshot = _seleniumServer.GetSnapshot();
        _dashboard.SetSeleniumStatus(seleniumSnapshot.State, seleniumSnapshot.Detail);
        PopulateSeleniumSettingsFields();
        PopulatePortSettingsFields();
        PopulatePhpSettingsFields(_phpSettings);
        ResetTerminalConsole();
        UpdateWorkspaceSortHeaders();
        RefreshWorkspaceFiles();
        _trayIconImage = LoadTrayIcon();
        _trayIcon = new Forms.NotifyIcon
        {
            Icon = _trayIconImage,
            Text = "Portable Developer",
            Visible = true
        };
        _trayIcon.DoubleClick += TrayIcon_DoubleClick;
        RebuildTrayMenu();
        System.Windows.Application.Current.SessionEnding += Application_SessionEnding;
        Loaded += MainWindow_Loaded;
    }

    private static System.Drawing.Icon LoadTrayIcon()
    {
        var executablePath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(executablePath))
        {
            using var executableIcon = System.Drawing.Icon.ExtractAssociatedIcon(executablePath);
            if (executableIcon is not null)
            {
                return (System.Drawing.Icon)executableIcon.Clone();
            }
        }

        return (System.Drawing.Icon)System.Drawing.SystemIcons.Application.Clone();
    }

    private static string GetApplicationVersion()
    {
        var assembly = typeof(MainWindow).Assembly;
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;
        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion.Split('+', 2)[0];
        }

        return assembly.GetName().Version?.ToString(3) ?? "0.0.0";
    }

    public void RestoreFromTray()
    {
        ShowInTaskbar = true;
        if (!IsVisible)
        {
            Show();
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        _ = Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    private void HideToTray()
    {
        ShowInTaskbar = false;
        Hide();
        if (_trayNotificationShown)
        {
            return;
        }

        _trayNotificationShown = true;
        _trayIcon.ShowBalloonTip(
            5000,
            _dashboard.Text.ApplicationContinuesInBackgroundTitle,
            _dashboard.Text.ApplicationContinuesInBackgroundMessage,
            Forms.ToolTipIcon.Info);
    }

    private void RebuildTrayMenu()
    {
        var previousMenu = _trayIcon.ContextMenuStrip;
        var menu = new Forms.ContextMenuStrip();
        var openItem = new Forms.ToolStripMenuItem(_dashboard.Text.OpenPortableDeveloper);
        openItem.Font = new System.Drawing.Font(openItem.Font, System.Drawing.FontStyle.Bold);
        openItem.Click += (_, _) => Dispatcher.BeginInvoke(RestoreFromTray);
        var exitItem = new Forms.ToolStripMenuItem(_dashboard.Text.ExitPortableDeveloper);
        exitItem.Click += (_, _) => Dispatcher.BeginInvoke(ConfirmExitFromTray);
        menu.Items.Add(openItem);
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add(exitItem);
        _trayIcon.ContextMenuStrip = menu;
        previousMenu?.Dispose();
    }

    private void TrayIcon_DoubleClick(object? sender, EventArgs e) =>
        Dispatcher.BeginInvoke(RestoreFromTray);

    private void ConfirmExitFromTray()
    {
        var wasHidden = !IsVisible;
        RestoreFromTray();
        if (!ConfirmationDialog.Show(
                this,
                _dashboard.Text.ExitPortableDeveloperTitle,
                _dashboard.Text.ExitPortableDeveloperQuestion,
                _dashboard.Text.ExitPortableDeveloperConfirm,
                _dashboard.Text.Cancel))
        {
            if (wasHidden)
            {
                HideToTray();
            }

            return;
        }

        _explicitExitRequested = true;
        Close();
    }

    private void Application_SessionEnding(object sender, SessionEndingCancelEventArgs e)
    {
        _sessionEnding = true;
        _explicitExitRequested = true;
    }

    protected override async void OnClosing(CancelEventArgs e)
    {
        if (_closeAfterStoppingStack)
        {
            base.OnClosing(e);
            return;
        }

        if (_sessionEnding)
        {
            _applicationLifetime.Cancel();
            base.OnClosing(e);
            return;
        }

        if (!_explicitExitRequested)
        {
            e.Cancel = true;
            HideToTray();
            return;
        }

        e.Cancel = true;
        _applicationLifetime.Cancel();
        IsEnabled = false;
        _dashboard.SetApacheStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Stopping, "");
        try
        {
            await Task.WhenAll(
                _apachePhpStack.DisposeAsync().AsTask(),
                _mariaDbServer.DisposeAsync().AsTask(),
                _seleniumServer.DisposeAsync().AsTask(),
                _taskScheduler.DisposeAsync().AsTask(),
                StopTerminalForShutdownAsync());
        }
        finally
        {
            _closeAfterStoppingStack = true;
            Close();
        }
    }

    private async Task StopTerminalForShutdownAsync()
    {
        var session = _terminalSession;
        if (session is null)
        {
            return;
        }

        try
        {
            await session.StopAsync(CancellationToken.None);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ObjectDisposedException)
        {
            await _logger.LogAsync(
                ApplicationLogLevel.Error,
                "terminal",
                "interactive-command.shutdown.failed",
                exception.Message);
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        System.Windows.Application.Current.SessionEnding -= Application_SessionEnding;
        _trayIcon.DoubleClick -= TrayIcon_DoubleClick;
        _trayIcon.Visible = false;
        _trayIcon.ContextMenuStrip?.Dispose();
        _trayIcon.Dispose();
        _trayIconImage.Dispose();
        if (_runtimePackageManager is IDisposable disposablePackageManager)
        {
            disposablePackageManager.Dispose();
        }

        _applicationLifetime.Dispose();
        base.OnClosed(e);
    }

    private async void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { SelectedValue: string languageName }
            || !Enum.TryParse<ApplicationLanguage>(languageName, out var language)
            || _dashboard.Text.CurrentLanguage == language)
        {
            return;
        }

        _dashboard.SetLanguage(language);
        RebuildTrayMenu();
        _applicationSettings = _applicationSettingsStore.Load();
        if (_selectedCookieFilePath is null)
        {
            SelectedCookieFileText.Text = _dashboard.Text.NoCookieFileSelected;
        }
        RefreshWebProjectBindings();
        RefreshScheduledTaskBindings();
        UpdateWorkspaceSortHeaders();
        RefreshWorkspaceFiles();
        UpdatePortInputStatuses();
        if (_dashboard.SelectedPage == NavigationPage.Guides)
        {
            RefreshGuides(resetSearch: true);
        }
        InstallationStatusText.Text = _dashboard.Text.LanguageChanged;
        await _logger.LogAsync(
            ApplicationLogLevel.Information,
            "ui",
            "language.changed",
            $"language={language}");
    }

    private void EditorPreferenceSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ComboBox { SelectedValue: string preferenceName }
            || !Enum.TryParse<FileEditorPreference>(preferenceName, out var preference)
            || !Enum.IsDefined(preference)
            || _applicationSettings.EditorPreference == preference)
        {
            return;
        }

        _applicationSettings = _applicationSettings with { EditorPreference = preference };
        _applicationSettingsStore.Save(_applicationSettings);
        InstallationStatusText.Text = _dashboard.Text.EditorSelectionSaved;
    }

    private async void NavigationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox { SelectedItem: NavigationItemViewModel item })
        {
            return;
        }

        _dashboard.SelectedPage = item.Page;

        if (item.Page == NavigationPage.Ports)
        {
            RefreshPortUsage();
            PopulatePortSettingsFields();
        }

        if (item.Page == NavigationPage.Settings)
        {
            await RefreshStorageUsageAsync();
        }

        if (item.Page == NavigationPage.Guides)
        {
            RefreshGuides();
        }

        if (item.Page == NavigationPage.Projects)
        {
            await RefreshProjectCapabilitiesAsync();
        }

        if (item.Page == NavigationPage.Scheduler)
        {
            RefreshScheduledTaskBindings();
        }

        InstallationStatusText.Text = item.Page switch
        {
            NavigationPage.Composer or NavigationPage.Node or NavigationPage.Python => string.Empty,
            NavigationPage.Files => DisplayTerminalPath(_workspaceDirectory),
            NavigationPage.Ports => _dashboard.PortSettingsAvailability,
            _ => InstallationStatusText.Text,
        };
    }

}
