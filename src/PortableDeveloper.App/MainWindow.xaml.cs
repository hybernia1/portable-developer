using System.ComponentModel;
using System.Diagnostics;
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
    private readonly IPhpSettingsStore _phpSettingsStore;
    private readonly IPortSettingsStore _portSettingsStore;
    private readonly ITcpPortUsageScanner _portUsageScanner;
    private readonly IRuntimePackageManager _runtimePackageManager;
    private readonly IStorageMaintenanceService _storageMaintenance;
    private readonly IModuleInventory _moduleInventory;
    private readonly IPortablePathResolver _paths;
    private MariaDbInstanceOptions _mariaDbOptions;
    private PortSettings _portSettings;
    private IReadOnlyList<TcpPortListenerInfo> _tcpListeners = [];
    private SeleniumServerOptions _seleniumOptions;
    private PhpSettings _phpSettings;
    private ApplicationSettings _applicationSettings;
    private readonly CancellationTokenSource _applicationLifetime = new();
    private bool _closeAfterStoppingStack;
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

    public MainWindow()
    {
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
        LanguageSelector.SelectedValue = _dashboard.Text.CurrentLanguage.ToString();
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
        Loaded += MainWindow_Loaded;
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

    protected override async void OnClosing(CancelEventArgs e)
    {
        if (_closeAfterStoppingStack)
        {
            base.OnClosing(e);
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
        _applicationSettings = _applicationSettingsStore.Load();
        if (_selectedCookieFilePath is null)
        {
            SelectedCookieFileText.Text = _dashboard.Text.NoCookieFileSelected;
        }
        RefreshWebProjectBindings();
        UpdateWorkspaceSortHeaders();
        RefreshWorkspaceFiles();
        UpdatePortInputStatuses();
        if (_dashboard.SelectedPage == NavigationPage.Guides)
        {
            RefreshGuides();
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

        InstallationStatusText.Text = item.Page switch
        {
            NavigationPage.Composer or NavigationPage.Node or NavigationPage.Python => string.Empty,
            NavigationPage.Files => DisplayTerminalPath(_workspaceDirectory),
            NavigationPage.Ports => _dashboard.PortSettingsAvailability,
            _ => InstallationStatusText.Text,
        };
    }

    private async void RefreshStorageUsage_Click(object sender, RoutedEventArgs e) =>
        await RefreshStorageUsageAsync();

    private void RefreshGuides()
    {
        var markdown = BuiltInGuide.Load(
            _dashboard.Text.CurrentLanguage,
            _dashboard.ApachePort,
            _dashboard.MariaDbPort,
            _dashboard.SeleniumPort);
        GuidesDocumentViewer.Document = MarkdownGuideRenderer.Render(
            markdown,
            _dashboard.Text.CurrentLanguage == ApplicationLanguage.Czech);
    }

    private async void ClearStorageCache_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string cacheName }
            || !Enum.TryParse<StorageCacheKind>(cacheName, out var cache))
        {
            return;
        }

        if (StorageMaintenanceIsBusy())
        {
            InstallationStatusText.Text = _dashboard.Text.StorageBusy;
            return;
        }

        var label = _dashboard.Text.StorageCacheName(cache);
        if (!ConfirmationDialog.Show(
                this,
                _dashboard.Text.ClearCacheTitle,
                _dashboard.Text.ClearCacheQuestion(label),
                _dashboard.Text.ClearCache,
                _dashboard.Text.Cancel))
        {
            return;
        }

        StorageActionsPanel.IsEnabled = false;
        var status = _dashboard.Text.ClearingCache(label);
        InstallationStatusText.Text = status;
        _dashboard.GlobalOperation.Begin(status);
        try
        {
            var result = await _storageMaintenance.ClearCacheAsync(cache, _applicationLifetime.Token);
            InstallationStatusText.Text = result.Success
                ? _dashboard.Text.CacheCleared(label, FormatStorageSize(result.RemovedBytes))
                : _dashboard.Text.CacheClearFailed(label, result.Detail);
            await RefreshStorageUsageAsync();
        }
        catch (OperationCanceledException)
        {
            // Application shutdown cancels background storage work.
        }
        finally
        {
            _dashboard.GlobalOperation.End();
            StorageActionsPanel.IsEnabled = !StorageMaintenanceIsBusy();
        }
    }

    private async void ClearAllStorageCaches_Click(object sender, RoutedEventArgs e)
    {
        if (StorageMaintenanceIsBusy())
        {
            InstallationStatusText.Text = _dashboard.Text.StorageBusy;
            return;
        }

        if (!ConfirmationDialog.Show(
                this,
                _dashboard.Text.ClearCacheTitle,
                _dashboard.Text.ClearAllCachesQuestion,
                _dashboard.Text.ClearAllCaches,
                _dashboard.Text.Cancel))
        {
            return;
        }

        StorageActionsPanel.IsEnabled = false;
        _dashboard.GlobalOperation.Begin(_dashboard.Text.ClearingCache(_dashboard.Text.CacheManagement));
        long removedBytes = 0;
        try
        {
            foreach (var cache in Enum.GetValues<StorageCacheKind>())
            {
                var result = await _storageMaintenance.ClearCacheAsync(cache, _applicationLifetime.Token);
                if (!result.Success)
                {
                    InstallationStatusText.Text = _dashboard.Text.CacheClearFailed(
                        _dashboard.Text.StorageCacheName(cache),
                        result.Detail);
                    return;
                }

                removedBytes += result.RemovedBytes;
            }

            InstallationStatusText.Text = _dashboard.Text.AllCachesCleared(FormatStorageSize(removedBytes));
            await RefreshStorageUsageAsync();
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _dashboard.GlobalOperation.End();
            StorageActionsPanel.IsEnabled = !StorageMaintenanceIsBusy();
        }
    }

    private async Task RefreshStorageUsageAsync()
    {
        StorageActionsPanel.IsEnabled = false;
        StorageOverviewStatusText.Text = _dashboard.Text.MeasuringStorage;
        _dashboard.GlobalOperation.Begin(_dashboard.Text.MeasuringStorage);
        try
        {
            var usage = await _storageMaintenance.InspectAsync(_applicationLifetime.Token);
            RuntimePackageCacheSizeText.Text = FormatStorageSize(usage.RuntimePackageCacheBytes);
            ComposerCacheSizeText.Text = FormatStorageSize(usage.ComposerCacheBytes);
            NpmCacheSizeText.Text = FormatStorageSize(usage.NpmCacheBytes);
            PipCacheSizeText.Text = FormatStorageSize(usage.PipCacheBytes);
            TotalCacheSizeText.Text = FormatStorageSize(usage.TotalCacheBytes);
            ClearRuntimePackageCacheButton.IsEnabled = usage.RuntimePackageCacheBytes > 0;
            ClearComposerCacheButton.IsEnabled = usage.ComposerCacheBytes > 0;
            ClearNpmCacheButton.IsEnabled = usage.NpmCacheBytes > 0;
            ClearPipCacheButton.IsEnabled = usage.PipCacheBytes > 0;
            ClearAllCachesButton.IsEnabled = usage.TotalCacheBytes > 0;
            InstalledRuntimeSizeText.Text = FormatStorageSize(usage.InstalledRuntimeBytes);
            PersistentDataSizeText.Text = FormatStorageSize(usage.PersistentDataBytes);
            StorageOverviewStatusText.Text = _dashboard.Text.StorageMeasured;
        }
        catch (OperationCanceledException)
        {
            // Application shutdown cancels background storage work.
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            StorageOverviewStatusText.Text = _dashboard.Text.StorageMeasureFailed(exception.Message);
        }
        finally
        {
            _dashboard.GlobalOperation.End();
            StorageActionsPanel.IsEnabled = !StorageMaintenanceIsBusy();
        }
    }

    private bool StorageMaintenanceIsBusy() =>
        _runtimePackageInstallationInProgress
        || _dashboard.Composer.IsBusy
        || _dashboard.Node.IsBusy
        || _dashboard.Python.IsBusy
        || _terminalBusy;

    private string FormatStorageSize(long bytes)
    {
        string[] units = ["B", "KiB", "MiB", "GiB", "TiB"];
        var value = Math.Max(0, bytes);
        var unit = 0;
        var display = (double)value;
        while (display >= 1024 && unit < units.Length - 1)
        {
            display /= 1024;
            unit++;
        }

        var culture = System.Globalization.CultureInfo.GetCultureInfo(
            _dashboard.Text.CurrentLanguage == ApplicationLanguage.Czech ? "cs-CZ" : "en-US");
        return $"{display.ToString(unit == 0 ? "N0" : "N1", culture)} {units[unit]}";
    }

    private void RefreshPorts_Click(object sender, RoutedEventArgs e)
    {
        RefreshPortUsage();
        InstallationStatusText.Text = _dashboard.TcpListenerCount;
    }

    private void PortTextBox_TextChanged(object sender, TextChangedEventArgs e) => UpdatePortInputStatuses();

    private void SavePorts_Click(object sender, RoutedEventArgs e)
    {
        if (!_dashboard.PortSettingsEnabled)
        {
            InstallationStatusText.Text = _dashboard.Text.PortSettingsRequireStoppedServices;
            return;
        }

        if (!int.TryParse(ApachePortTextBox.Text.Trim(), out var apachePort)
            || !int.TryParse(PhpFastCgiPortTextBox.Text.Trim(), out var phpPort)
            || !int.TryParse(MariaDbPortTextBox.Text.Trim(), out var mariaDbPort)
            || !int.TryParse(CentralSeleniumPortTextBox.Text.Trim(), out var seleniumPort))
        {
            InstallationStatusText.Text = _dashboard.Text.PortsInvalid;
            return;
        }

        try
        {
            var settings = PortSettingsValidator.Validate(new PortSettings(
                apachePort,
                phpPort,
                mariaDbPort,
                seleniumPort));
            var listeners = _portUsageScanner.Scan();
            _tcpListeners = listeners;
            _dashboard.SetTcpListeners(listeners);
            var occupied = new[] { apachePort, phpPort, mariaDbPort, seleniumPort }
                .Where(port => listeners.Any(listener => listener.Port == port) || !_portUsageScanner.IsAvailable(port))
                .Distinct()
                .Order()
                .ToArray();
            if (occupied.Length > 0)
            {
                InstallationStatusText.Text = _dashboard.Text.PortsOccupied(occupied);
                return;
            }

            _portSettingsStore.Save(settings);
            _portSettings = settings;
            _mariaDbOptions = _mariaDbOptions with { Port = settings.MariaDbPort };
            _seleniumOptions = _seleniumOptions with { Port = settings.SeleniumPort };
            _seleniumSettingsStore.Save(_seleniumOptions);
            _dashboard.SetPortSettings(settings);
            _dashboard.SetSeleniumOptions(_seleniumOptions);
            RefreshWebProjectBindings();
            PopulatePortSettingsFields();
            InstallationStatusText.Text = _dashboard.Text.PortsSaved;
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "ports",
                "settings.saved",
                $"apache={settings.ApachePort}; php={settings.PhpFastCgiPort}; mariadb={settings.MariaDbPort}; selenium={settings.SeleniumPort}");
        }
        catch (ArgumentException)
        {
            InstallationStatusText.Text = _dashboard.Text.PortsInvalid;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NetworkInformationException)
        {
            InstallationStatusText.Text = _dashboard.Text.PortScanFailed(exception.Message);
        }
    }

    private void RefreshPortUsage()
    {
        try
        {
            _tcpListeners = _portUsageScanner.Scan();
            _dashboard.SetTcpListeners(_tcpListeners);
            UpdatePortInputStatuses();
        }
        catch (NetworkInformationException exception)
        {
            _tcpListeners = [];
            _dashboard.SetTcpListeners([]);
            UpdatePortInputStatuses();
            InstallationStatusText.Text = _dashboard.Text.PortScanFailed(exception.Message);
        }
    }

    private void PopulatePortSettingsFields()
    {
        ApachePortTextBox.Text = _portSettings.ApachePort.ToString();
        PhpFastCgiPortTextBox.Text = _portSettings.PhpFastCgiPort.ToString();
        MariaDbPortTextBox.Text = _portSettings.MariaDbPort.ToString();
        CentralSeleniumPortTextBox.Text = _portSettings.SeleniumPort.ToString();
        UpdatePortInputStatuses();
    }

    private void UpdatePortInputStatuses()
    {
        if (!IsInitialized
            || ApachePortStatusText is null
            || PhpPortStatusText is null
            || MariaDbPortStatusText is null
            || SeleniumPortStatusText is null)
        {
            return;
        }

        var inputs = new[]
        {
            (TextBox: ApachePortTextBox, Status: ApachePortStatusText, CurrentPort: _portSettings.ApachePort, Owned: _dashboard.ApacheIsRunning),
            (TextBox: PhpFastCgiPortTextBox, Status: PhpPortStatusText, CurrentPort: _portSettings.PhpFastCgiPort, Owned: _dashboard.ApacheIsRunning),
            (TextBox: MariaDbPortTextBox, Status: MariaDbPortStatusText, CurrentPort: _portSettings.MariaDbPort, Owned: _dashboard.MariaDbIsRunning),
            (TextBox: CentralSeleniumPortTextBox, Status: SeleniumPortStatusText, CurrentPort: _portSettings.SeleniumPort, Owned: _dashboard.SeleniumIsRunning)
        };
        var parsed = inputs.Select(input => int.TryParse(input.TextBox.Text.Trim(), out var port) ? port : -1).ToArray();

        for (var index = 0; index < inputs.Length; index++)
        {
            var port = parsed[index];
            inputs[index].Status.Text = port is < PortSettingsValidator.MinimumPort or > PortSettingsValidator.MaximumPort
                ? _dashboard.Text.PortInvalid
                : parsed.Count(other => other == port) > 1
                    ? _dashboard.Text.PortDuplicate
                    : inputs[index].Owned && port == inputs[index].CurrentPort
                        ? _dashboard.Text.PortUsedByApplication
                        : _tcpListeners.Any(listener => listener.Port == port) || !_portUsageScanner.IsAvailable(port)
                            ? _dashboard.Text.PortOccupied
                            : _dashboard.Text.PortAvailable;
        }
    }

    private async void SavePhpSettings_Click(object sender, RoutedEventArgs e)
    {
        if (!_dashboard.PhpSettingsEnabled ||
            !int.TryParse(PhpMemoryLimitTextBox.Text.Trim(), out var memoryLimit) ||
            !int.TryParse(PhpUploadLimitTextBox.Text.Trim(), out var uploadLimit) ||
            !int.TryParse(PhpPostLimitTextBox.Text.Trim(), out var postLimit) ||
            !int.TryParse(PhpExecutionTimeTextBox.Text.Trim(), out var executionTime) ||
            !int.TryParse(PhpMaxInputVariablesTextBox.Text.Trim(), out var maxInputVariables))
        {
            InstallationStatusText.Text = _dashboard.Text.PhpSettingsInvalid;
            return;
        }

        try
        {
            var settings = PhpSettingsValidator.Normalize(new PhpSettings
            {
                MemoryLimitMb = memoryLimit,
                UploadMaxFileSizeMb = uploadLimit,
                PostMaxSizeMb = postLimit,
                MaxExecutionTimeSeconds = executionTime,
                MaxInputVariables = maxInputVariables,
                DisplayErrors = PhpDisplayErrorsCheckBox.IsChecked == true,
                EnabledExtensions = _dashboard.PhpExtensions
                    .Where(extension => extension.IsEnabled)
                    .Select(extension => extension.Name)
                    .ToArray()
            });
            _phpSettingsStore.Save(settings);
            _phpSettings = settings;
            PopulatePhpSettingsFields(settings);
            var wasRunning = _dashboard.ApacheIsRunning;
            await _logger.LogAsync(
                ApplicationLogLevel.Information,
                "php",
                "settings.saved",
                "Portable PHP settings were saved.");
            if (wasRunning)
            {
                var restarted = await RestartApacheAsync(announce: false);
                if (!restarted)
                {
                    return;
                }
            }

            InstallationStatusText.Text = _dashboard.Text.PhpSettingsSaved(_dashboard.ApacheProcessState);
        }
        catch (ArgumentException)
        {
            InstallationStatusText.Text = _dashboard.Text.PhpSettingsInvalid;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            InstallationStatusText.Text = _dashboard.Text.PhpSettingsSaveFailed(exception.Message);
        }
    }

    private void ResetPhpSettings_Click(object sender, RoutedEventArgs e)
    {
        if (!_dashboard.PhpSettingsEnabled)
        {
            return;
        }

        PopulatePhpSettingsFields(PhpSettings.Default);
        InstallationStatusText.Text = _dashboard.Text.PhpDefaultsPrepared;
    }

    private void PopulatePhpSettingsFields(PhpSettings settings)
    {
        PhpMemoryLimitTextBox.Text = settings.MemoryLimitMb.ToString();
        PhpUploadLimitTextBox.Text = settings.UploadMaxFileSizeMb.ToString();
        PhpPostLimitTextBox.Text = settings.PostMaxSizeMb.ToString();
        PhpExecutionTimeTextBox.Text = settings.MaxExecutionTimeSeconds.ToString();
        PhpMaxInputVariablesTextBox.Text = settings.MaxInputVariables.ToString();
        PhpDisplayErrorsCheckBox.IsChecked = settings.DisplayErrors;
        var enabled = settings.EnabledExtensions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var extension in _dashboard.PhpExtensions)
        {
            extension.IsEnabled = enabled.Contains(extension.Name);
        }
    }

    private ApachePhpStackOptions CreateApachePhpOptions() => new(
        ApachePort: _portSettings.ApachePort,
        PhpFastCgiPort: _portSettings.PhpFastCgiPort,
        MariaDbPort: _mariaDbOptions.Port,
        PhpSettings: _phpSettings,
        WebProjects: _webProjects.Projects);

    private async void ToggleApache_Click(object sender, RoutedEventArgs e) => await ToggleApacheAsync();

    private async Task ToggleApacheAsync()
    {
        if (!_dashboard.ApacheActionEnabled)
        {
            return;
        }

        var shouldStop = _dashboard.ApacheProcessState == PortableDeveloper.Domain.Processes.ManagedProcessState.Running;
        _dashboard.SetApacheStatus(
            shouldStop
                ? PortableDeveloper.Domain.Processes.ManagedProcessState.Stopping
                : PortableDeveloper.Domain.Processes.ManagedProcessState.Starting,
            "");
        try
        {
            var snapshot = shouldStop
                ? await _apachePhpStack.StopAsync()
                : await _apachePhpStack.StartAsync(CreateApachePhpOptions());
            _dashboard.SetApacheStatus(snapshot.State, snapshot.Detail);
            if (!shouldStop && snapshot.State == PortableDeveloper.Domain.Processes.ManagedProcessState.Running)
            {
                _dashboard.SetWebConfigurationRestartRequired(false);
            }
        }
        catch (Exception exception)
        {
            _dashboard.SetApacheStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Failed, exception.Message);
        }
    }

    private async Task<bool> RestartApacheAsync(bool announce = true)
    {
        if (!_dashboard.ApacheRestartEnabled)
        {
            return false;
        }

        InstallationStatusText.Text = _dashboard.Text.RestartingApacheService;
        try
        {
            _dashboard.SetApacheStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Stopping, string.Empty);
            var stopped = await _apachePhpStack.StopAsync(_applicationLifetime.Token);
            _dashboard.SetApacheStatus(stopped.State, stopped.Detail);
            if (stopped.State == PortableDeveloper.Domain.Processes.ManagedProcessState.Failed)
            {
                return false;
            }

            _dashboard.SetApacheStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Starting, string.Empty);
            var started = await _apachePhpStack.StartAsync(CreateApachePhpOptions(), _applicationLifetime.Token);
            _dashboard.SetApacheStatus(started.State, started.Detail);
            if (started.State != PortableDeveloper.Domain.Processes.ManagedProcessState.Running)
            {
                return false;
            }

            _dashboard.SetWebConfigurationRestartRequired(false);

            if (announce)
            {
                InstallationStatusText.Text = _dashboard.Text.ApacheServiceRestarted;
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            InstallationStatusText.Text = _dashboard.Text.OperationCanceled;
            return false;
        }
        catch (Exception exception)
        {
            _dashboard.SetApacheStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Failed, exception.Message);
            InstallationStatusText.Text = exception.Message;
            return false;
        }
    }

    private async void ServiceAction_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: "toggle-mariadb" })
        {
            await ToggleMariaDbAsync();
        }
        else if (sender is Button { Tag: "toggle-selenium" })
        {
            await ToggleSeleniumAsync();
        }
        else if (sender is Button { Tag: "toggle-apache" })
        {
            await ToggleApacheAsync();
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainWindow_Loaded;
        if (_dashboard.MariaDbInstalled)
        {
            await BootstrapMariaDbAsync();
        }

        if (_dashboard.Composer.RuntimeReady)
        {
            await RefreshPackageManagerAsync(_composerPackageManager, _dashboard.Composer);
        }

        if (_dashboard.Node.RuntimeReady)
        {
            await RefreshPackageManagerAsync(_nodePackageManager, _dashboard.Node);
        }

        if (_dashboard.Python.RuntimeReady)
        {
            await RefreshPackageManagerAsync(_pythonPackageManager, _dashboard.Python);
        }
    }

    private async void InstallRuntimePackage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: RuntimePackageViewModel package } || !package.CanInstall)
        {
            return;
        }

        var installationFinished = false;
        var progress = new Progress<RuntimePackageInstallProgress>(update =>
        {
            if (installationFinished)
            {
                return;
            }

            var status = _dashboard.Text.PackageInstallProgress(update);
            package.SetProgress(
                update.Percentage,
                status,
                _dashboard.Text.PackageDownloadSize(update));
            _dashboard.GlobalOperation.Update(
                status,
                update.Stage == RuntimePackageInstallStage.Preparing,
                update.Percentage,
                package.DownloadDetail);
            InstallationStatusText.Text = package.Status;
        });
        package.BeginInstallation(0, _dashboard.Text.PackageInstallProgress(new(
            package.Kind,
            RuntimePackageInstallStage.Preparing,
            string.Empty,
            0)));
        foreach (var item in _dashboard.RuntimePackages.Concat(_dashboard.SeleniumDriverPackages))
        {
            item.SetManagerBusy(true);
        }

        RuntimePackageInstallResult result;
        _runtimePackageInstallationInProgress = true;
        _dashboard.GlobalOperation.Begin(package.Status);
        try
        {
            result = await Task.Run(
                () => _runtimePackageManager.InstallAsync(package.Kind, progress, _applicationLifetime.Token),
                _applicationLifetime.Token);
        }
        finally
        {
            installationFinished = true;
            _runtimePackageInstallationInProgress = false;
            _dashboard.GlobalOperation.End();
        }
        if (!result.Success)
        {
            var failure = _dashboard.Text.PackageInstallFailed(result.Detail);
            package.Complete(false, failure);
            SetRuntimePackageManagerBusy(false);

            InstallationStatusText.Text = failure;
            return;
        }

        package.Complete(true, string.Empty);
        SetRuntimePackageManagerBusy(false);
        _dashboard.Composer.SetRuntime(_composerPackageManager.GetRuntime());
        _dashboard.Node.SetRuntime(_nodePackageManager.GetRuntime());
        _dashboard.Python.SetRuntime(_pythonPackageManager.GetRuntime());
        RefreshSeleniumEnvironments();
        RefreshPhpExtensions();
        _dashboard.RefreshRuntimeAvailability();
        RefreshWebProjectBindings();
        RefreshWorkspaceFiles();
        if (package.Kind is RuntimePackageKind.Database or RuntimePackageKind.PhpMyAdmin)
        {
            await BootstrapMariaDbAsync();
        }

        if (package.Kind == RuntimePackageKind.Composer)
        {
            await RefreshPackageManagerAsync(_composerPackageManager, _dashboard.Composer);
        }

        if (package.Kind == RuntimePackageKind.Node)
        {
            await RefreshPackageManagerAsync(_nodePackageManager, _dashboard.Node);
        }
        else if (package.Kind == RuntimePackageKind.Python)
        {
            await RefreshPackageManagerAsync(_pythonPackageManager, _dashboard.Python);
        }

        var installed = _dashboard.RuntimePackages
            .Concat(_dashboard.SeleniumDriverPackages)
            .First(item => item.Kind == package.Kind);
        InstallationStatusText.Text = _dashboard.Text.PackageInstallSucceeded(installed.Name);
    }

    private void SetRuntimePackageManagerBusy(bool busy)
    {
        foreach (var item in _dashboard.RuntimePackages.Concat(_dashboard.SeleniumDriverPackages))
        {
            item.SetManagerBusy(busy);
        }
    }

    private void RefreshPhpExtensions()
    {
        var phpInstallation = _moduleInventory.GetInstalled(PortableDeveloper.Domain.Modules.ModuleKind.Php).FirstOrDefault();
        var enabledPhpExtensions = _phpSettings.EnabledExtensions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        _dashboard.SetPhpExtensions(PhpExtensionCatalog.All.Select(extension => new PhpExtensionViewModel(
            extension.Name,
            extension.IsRequired,
            phpInstallation is not null && File.Exists(_paths.Resolve(Path.Combine(
                phpInstallation.ModuleRootRelativePath,
                "ext",
                $"php_{extension.Name}.dll"))),
            enabledPhpExtensions.Contains(extension.Name))));
        _phpSettings = _phpSettings with
        {
            EnabledExtensions = _dashboard.PhpExtensions
                .Where(extension => extension.IsEnabled)
                .Select(extension => extension.Name)
                .ToArray()
        };
    }

    private async Task BootstrapMariaDbAsync()
    {
        _dashboard.SetMariaDbOperationInProgress(true);
        InstallationStatusText.Text = _dashboard.Text.InitializingMariaDb;
        var startedForBootstrap = false;
        try
        {
            var state = _mariaDbInitializer.GetState(_mariaDbOptions);
            if (state == MariaDbInstanceState.Incomplete)
            {
                _dashboard.SetMariaDbState(state);
                InstallationStatusText.Text = _dashboard.Text.MariaDbInitializationFailed(
                    "Existing database files are incomplete and were left unchanged.");
                return;
            }

            if (state == MariaDbInstanceState.NotInitialized)
            {
                var initialization = await _mariaDbInitializer.InitializeAsync(_mariaDbOptions, _applicationLifetime.Token);
                if (initialization.Status == MariaDbInitializationStatus.Failed)
                {
                    InstallationStatusText.Text = _dashboard.Text.MariaDbInitializationFailed(initialization.Detail);
                    return;
                }

                state = _mariaDbInitializer.GetState(_mariaDbOptions);
                _dashboard.SetMariaDbState(state);
            }
            else
            {
                _dashboard.SetMariaDbState(state);
                _dashboard.SetMariaDbStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Stopped, string.Empty);
                _dashboard.SetRootPasswordState(_mariaDbAccount.HasRootPassword(_mariaDbOptions));
                InstallationStatusText.Text = _dashboard.Text.MariaDbPreparedStopped;
                return;
            }

            _dashboard.SetMariaDbStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Starting, string.Empty);
            var server = await _mariaDbServer.StartAsync(_mariaDbOptions, _applicationLifetime.Token);
            _dashboard.SetMariaDbStatus(server.State, server.Detail);
            if (server.State != PortableDeveloper.Domain.Processes.ManagedProcessState.Running)
            {
                InstallationStatusText.Text = _dashboard.Text.MariaDbInitializationFailed(server.Detail);
                return;
            }
            startedForBootstrap = true;

            var cleanup = await _databaseCatalog.RemoveGeneratedTestDatabaseAsync(
                _mariaDbOptions,
                _applicationLifetime.Token);
            if (!cleanup.IsSuccess)
            {
                InstallationStatusText.Text = _dashboard.Text.DatabaseCreateFailed(cleanup.Detail);
                return;
            }

            var databases = await _databaseCatalog.ListAsync(_mariaDbOptions, _applicationLifetime.Token);
            if (!databases.Any(database => string.Equals(database.Name, "portable_dev", StringComparison.OrdinalIgnoreCase)))
            {
                var created = await _databaseCatalog.CreateAsync(_mariaDbOptions, "portable_dev", _applicationLifetime.Token);
                if (!created.IsSuccess)
                {
                    InstallationStatusText.Text = _dashboard.Text.DatabaseCreateFailed(created.Detail);
                    return;
                }
            }

            _dashboard.SetRootPasswordState(_mariaDbAccount.HasRootPassword(_mariaDbOptions));
            InstallationStatusText.Text = _dashboard.Text.MariaDbPreparedStopped;
        }
        catch (OperationCanceledException)
        {
            InstallationStatusText.Text = _dashboard.Text.OperationCanceled;
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException or UnauthorizedAccessException)
        {
            _dashboard.SetMariaDbStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Failed, exception.Message);
            InstallationStatusText.Text = _dashboard.Text.MariaDbInitializationFailed(exception.Message);
        }
        finally
        {
            if (startedForBootstrap)
            {
                try
                {
                    var stopped = await _mariaDbServer.StopAsync(_applicationLifetime.Token);
                    _dashboard.SetMariaDbStatus(stopped.State, stopped.Detail);
                }
                catch (Exception exception) when (exception is IOException or InvalidOperationException)
                {
                    _dashboard.SetMariaDbStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Failed, exception.Message);
                }
            }

            _dashboard.SetMariaDbState(_mariaDbInitializer.GetState(_mariaDbOptions));
            _dashboard.SetMariaDbOperationInProgress(false);
        }
    }

    private async Task ToggleMariaDbAsync()
    {
        if (!_dashboard.MariaDbActionEnabled)
        {
            return;
        }

        var shouldStop = _dashboard.MariaDbIsRunning;
        _dashboard.SetMariaDbOperationInProgress(true);
        _dashboard.SetMariaDbStatus(
            shouldStop
                ? PortableDeveloper.Domain.Processes.ManagedProcessState.Stopping
                : PortableDeveloper.Domain.Processes.ManagedProcessState.Starting,
            string.Empty);
        InstallationStatusText.Text = shouldStop ? _dashboard.Text.MariaDbStopping : _dashboard.Text.MariaDbStarting;
        try
        {
            var snapshot = shouldStop
                ? await _mariaDbServer.StopAsync(_applicationLifetime.Token)
                : await _mariaDbServer.StartAsync(_mariaDbOptions, _applicationLifetime.Token);
            _dashboard.SetMariaDbStatus(snapshot.State, snapshot.Detail);
            if (snapshot.State == PortableDeveloper.Domain.Processes.ManagedProcessState.Running)
            {
                await RefreshDatabasesAsync();
                InstallationStatusText.Text = _dashboard.Text.MariaDbReady;
            }
        }
        catch (OperationCanceledException)
        {
            InstallationStatusText.Text = _dashboard.Text.OperationCanceled;
        }
        catch (Exception exception)
        {
            _dashboard.SetMariaDbStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Failed, exception.Message);
            InstallationStatusText.Text = _dashboard.Text.MariaDbInitializationFailed(exception.Message);
        }
        finally
        {
            _dashboard.SetMariaDbOperationInProgress(false);
        }
    }

    private async void CreateDatabase_Click(object sender, RoutedEventArgs e)
    {
        if (!_dashboard.MariaDbIsRunning)
        {
            return;
        }

        var databaseName = NewDatabaseNameTextBox.Text.Trim();
        InstallationStatusText.Text = _dashboard.Text.CreatingDatabase;
        try
        {
            var result = await _databaseCatalog.CreateAsync(_mariaDbOptions, databaseName, _applicationLifetime.Token);
            if (!result.IsSuccess)
            {
                InstallationStatusText.Text = _dashboard.Text.DatabaseCreateFailed(result.Detail);
                return;
            }

            NewDatabaseNameTextBox.Clear();
            await RefreshDatabasesAsync();
            InstallationStatusText.Text = _dashboard.Text.DatabaseCreated(databaseName);
        }
        catch (OperationCanceledException)
        {
            InstallationStatusText.Text = _dashboard.Text.OperationCanceled;
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException or UnauthorizedAccessException)
        {
            InstallationStatusText.Text = _dashboard.Text.DatabaseCreateFailed(exception.Message);
        }
    }

    private async void RefreshDatabases_Click(object sender, RoutedEventArgs e) => await RefreshDatabasesAsync();

    private async Task RefreshDatabasesAsync()
    {
        if (!_dashboard.MariaDbIsRunning)
        {
            return;
        }

        try
        {
            var databases = await _databaseCatalog.ListAsync(_mariaDbOptions, _applicationLifetime.Token);
            _dashboard.SetDatabases(databases);
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException or UnauthorizedAccessException)
        {
            InstallationStatusText.Text = _dashboard.Text.DatabaseOverviewFailed(exception.Message);
        }
    }

    private async void ChangeRootPassword_Click(object sender, RoutedEventArgs e)
    {
        if (!_dashboard.MariaDbIsRunning)
        {
            return;
        }

        var newPassword = RootPasswordBox.Password;
        if (!string.Equals(newPassword, ConfirmRootPasswordBox.Password, StringComparison.Ordinal))
        {
            InstallationStatusText.Text = _dashboard.Text.PasswordMismatch;
            return;
        }

        _dashboard.SetMariaDbOperationInProgress(true);
        InstallationStatusText.Text = _dashboard.Text.PasswordChanging;
        try
        {
            var result = await _mariaDbAccount.ChangeRootPasswordAsync(
                _mariaDbOptions,
                newPassword,
                _applicationLifetime.Token);
            if (!result.IsSuccess)
            {
                InstallationStatusText.Text = _dashboard.Text.PasswordChangeFailed(result.Detail);
                return;
            }

            RootPasswordBox.Clear();
            ConfirmRootPasswordBox.Clear();
            _dashboard.SetRootPasswordState(true);
            InstallationStatusText.Text = _dashboard.Text.PasswordChanged;
            await RefreshDatabasesAsync();
        }
        catch (OperationCanceledException)
        {
            InstallationStatusText.Text = _dashboard.Text.OperationCanceled;
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException or UnauthorizedAccessException)
        {
            InstallationStatusText.Text = _dashboard.Text.PasswordChangeFailed(exception.Message);
        }
        finally
        {
            _dashboard.SetMariaDbOperationInProgress(false);
        }
    }

    private void OpenPhpMyAdmin_Click(object sender, RoutedEventArgs e)
    {
        if (!_dashboard.PhpMyAdminActionEnabled)
        {
            InstallationStatusText.Text = _dashboard.PhpMyAdminDependencyState;
            return;
        }

        InstallationStatusText.Text = _dashboard.Text.OpeningPhpMyAdmin;
        try
        {
            Process.Start(new ProcessStartInfo(_dashboard.PhpMyAdminUrl) { UseShellExecute = true });
            InstallationStatusText.Text = _dashboard.PhpMyAdminUrl;
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            InstallationStatusText.Text = exception.Message;
        }
    }

    private async void ToggleSelenium_Click(object sender, RoutedEventArgs e) => await ToggleSeleniumAsync();

    private async Task ToggleSeleniumAsync()
    {
        if (!_dashboard.SeleniumActionEnabled)
        {
            return;
        }

        var shouldStop = _dashboard.SeleniumIsRunning;
        if (!shouldStop && !_applicationSettings.SeleniumFirewallNoticeAcknowledged)
        {
            if (!ConfirmationDialog.Show(
                    this,
                    _dashboard.Text.SeleniumFirewallNoticeTitle,
                    _dashboard.Text.SeleniumFirewallNotice,
                    _dashboard.Text.ContinueSeleniumStart,
                    _dashboard.Text.Cancel))
            {
                return;
            }

            _applicationSettings = _applicationSettings with { SeleniumFirewallNoticeAcknowledged = true };
            _applicationSettingsStore.Save(_applicationSettings);
        }

        _dashboard.SetSeleniumOperationInProgress(true);
        _dashboard.SetSeleniumStatus(
            shouldStop
                ? PortableDeveloper.Domain.Processes.ManagedProcessState.Stopping
                : PortableDeveloper.Domain.Processes.ManagedProcessState.Starting,
            string.Empty);
        try
        {
            var runtimeOptions = _seleniumOptions with
            {
                DownloadDirectoryRelativePath = Path.Combine(
                    _projectContext.ActiveProject.RootRelativePath,
                    "seldownloads")
            };
            var snapshot = shouldStop
                ? await _seleniumServer.StopAsync(_applicationLifetime.Token)
                : await _seleniumServer.StartAsync(runtimeOptions, _applicationLifetime.Token);
            _dashboard.SetSeleniumStatus(snapshot.State, snapshot.Detail);
            if (snapshot.State == PortableDeveloper.Domain.Processes.ManagedProcessState.Running)
            {
                await RefreshSeleniumSessionsAsync();
                InstallationStatusText.Text = _dashboard.SeleniumHubUrl;
            }
            else
            {
                _dashboard.SetSeleniumSessions([]);
            }
        }
        catch (OperationCanceledException)
        {
            InstallationStatusText.Text = _dashboard.Text.OperationCanceled;
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or UnauthorizedAccessException or HttpRequestException)
        {
            _dashboard.SetSeleniumStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Failed, exception.Message);
            InstallationStatusText.Text = _dashboard.Text.SeleniumOperationFailed(exception.Message);
        }
        finally
        {
            _dashboard.SetSeleniumOperationInProgress(false);
        }
    }

    private void SaveSeleniumSettings_Click(object sender, RoutedEventArgs e)
    {
        if (!_dashboard.SeleniumSettingsEnabled ||
            !int.TryParse(SeleniumMaxSessionsTextBox.Text.Trim(), out var maxSessions) ||
            !int.TryParse(SeleniumSessionTimeoutTextBox.Text.Trim(), out var sessionTimeout) ||
            maxSessions is < 1 or > 32 ||
            sessionTimeout is < 30 or > 86400)
        {
            InstallationStatusText.Text = _dashboard.Text.SeleniumSettingsInvalid;
            return;
        }

        try
        {
            _seleniumOptions = _seleniumOptions with
            {
                Port = _portSettings.SeleniumPort,
                MaxSessions = maxSessions,
                SessionTimeoutSeconds = sessionTimeout,
                DownloadsEnabled = SeleniumDownloadsEnabledCheckBox.IsChecked == true
            };
            _seleniumSettingsStore.Save(_seleniumOptions);
            _dashboard.SetSeleniumOptions(_seleniumOptions);
            InstallationStatusText.Text = _dashboard.Text.SeleniumSettingsSaved;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            InstallationStatusText.Text = _dashboard.Text.SeleniumOperationFailed(exception.Message);
        }
    }

    private void PopulateSeleniumSettingsFields()
    {
        SeleniumMaxSessionsTextBox.Text = _seleniumOptions.MaxSessions.ToString();
        SeleniumSessionTimeoutTextBox.Text = _seleniumOptions.SessionTimeoutSeconds.ToString();
        SeleniumDownloadsEnabledCheckBox.IsChecked = _seleniumOptions.DownloadsEnabled;
    }

    private void ReloadSeleniumDrivers_Click(object sender, RoutedEventArgs e)
    {
        if (!_dashboard.SeleniumSettingsEnabled)
        {
            return;
        }

        RefreshSeleniumEnvironments();
        InstallationStatusText.Text = _dashboard.SeleniumDriverCount;
    }

    private void RefreshSeleniumEnvironments()
    {
        _seleniumEnvironments = _seleniumEnvironmentInventory.Scan();
        _dashboard.SetSeleniumEnvironments(_seleniumEnvironments);
        if (SeleniumProfileEnvironmentSelector is not null && SeleniumProfileEnvironmentSelector.SelectedIndex < 0)
        {
            SeleniumProfileEnvironmentSelector.SelectedIndex = 0;
        }

    }

    private async void CreateCleanSeleniumProfile_Click(object sender, RoutedEventArgs e)
    {
        if (!_dashboard.SeleniumProfileActionsEnabled)
        {
            return;
        }

        if (!SeleniumProfileName.TryNormalize(SeleniumCleanProfileNameTextBox.Text, out var profileName))
        {
            InstallationStatusText.Text = _dashboard.Text.ProfileNameRequired;
            SeleniumCleanProfileNameTextBox.Focus();
            return;
        }

        if (SeleniumProfileEnvironmentSelector.SelectedValue is not string environmentId
            || _seleniumEnvironments.FirstOrDefault(item => item.Id == environmentId) is not { } environment)
        {
            InstallationStatusText.Text = _dashboard.Text.SelectBrowserEnvironment;
            return;
        }

        var browser = environment.BrowserName switch
        {
            "chrome" => SeleniumProfileBrowser.Chrome,
            "MicrosoftEdge" => SeleniumProfileBrowser.Edge,
            "firefox" => SeleniumProfileBrowser.Firefox,
            _ => (SeleniumProfileBrowser?)null
        };
        if (browser is null)
        {
            InstallationStatusText.Text = _dashboard.Text.UnsupportedBrowserEnvironment;
            return;
        }

        var token = Guid.NewGuid().ToString("N");
        var draftRelativePath = Path.Combine("temp", "selenium-profile-creation", token);
        var draftPath = _paths.EnsureDirectory(draftRelativePath);
        var executable = _paths.Resolve(environment.BrowserExecutablePath);
        var accountPage = browser switch
        {
            SeleniumProfileBrowser.Firefox => "about:preferences#sync",
            SeleniumProfileBrowser.Edge => "edge://settings/profiles",
            _ => "chrome://settings/youAndGoogle"
        };

        var startInfo = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(executable)!
        };
        startInfo.Environment["MOZ_CRASHREPORTER_DISABLE"] = "1";
        startInfo.Environment["MOZ_CRASHREPORTER_NO_REPORT"] = "1";
        if (browser == SeleniumProfileBrowser.Firefox)
        {
            startInfo.ArgumentList.Add("-no-remote");
            startInfo.ArgumentList.Add("-profile");
            startInfo.ArgumentList.Add(draftPath);
            startInfo.ArgumentList.Add("-new-window");
            startInfo.ArgumentList.Add(accountPage);
        }
        else
        {
            startInfo.ArgumentList.Add($"--user-data-dir={draftPath}");
            startInfo.ArgumentList.Add("--no-first-run");
            startInfo.ArgumentList.Add("--no-default-browser-check");
            startInfo.ArgumentList.Add("--new-window");
            startInfo.ArgumentList.Add(accountPage);
        }

        _dashboard.SetSeleniumOperationInProgress(true);
        SetSeleniumProfileProgress(true, _dashboard.Text.SeleniumProfileWaiting);
        var browserStopwatch = Stopwatch.StartNew();
        var sealingMilliseconds = 0L;
        try
        {
            InstallationStatusText.Text = _dashboard.Text.ConfigureBrowserAndClose;
            var process = Process.Start(startInfo);
            if (process is null)
            {
                InstallationStatusText.Text = _dashboard.Text.BrowserCouldNotStart;
                return;
            }

            using (process)
            {
                await process.WaitForExitAsync();
            }

            if (browser == SeleniumProfileBrowser.Firefox)
            {
                await WaitForManagedFirefoxShutdownAsync(draftRelativePath, draftPath, _applicationLifetime.Token);
            }

            browserStopwatch.Stop();
            SetSeleniumProfileProgress(true, _dashboard.Text.SeleniumProfileSealing);
            var selectedBrowser = browser.Value;
            var browserVersion = environment.BrowserVersion;
            var sealingStopwatch = Stopwatch.StartNew();
            var result = await Task.Run(() => _seleniumProfileStore.CreateFromManagedDraft(
                profileName,
                selectedBrowser,
                draftRelativePath,
                browserVersion));
            sealingStopwatch.Stop();
            sealingMilliseconds = sealingStopwatch.ElapsedMilliseconds;
            if (!result.IsSuccess)
            {
                InstallationStatusText.Text = _dashboard.Text.SeleniumProfileCreateFailed(result.Detail);
                return;
            }

            SeleniumCleanProfileNameTextBox.Clear();
            _dashboard.SetSeleniumProfiles(_seleniumProfileStore.GetProfiles());
            InstallationStatusText.Text = _dashboard.Text.SeleniumProfileCreated(result.Profile!.Name);
        }
        catch (OperationCanceledException) when (_applicationLifetime.IsCancellationRequested)
        {
            // Closing the application cancels profile enrollment without surfacing an unhandled UI exception.
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            InstallationStatusText.Text = _dashboard.Text.SeleniumProfileCreateFailed(exception.Message);
        }
        finally
        {
            browserStopwatch.Stop();
            SetSeleniumProfileProgress(true, _dashboard.Text.SeleniumProfileCleaning);
            var cleanupStopwatch = Stopwatch.StartNew();
            await Task.Run(() => TryDeleteProfileDraft(draftPath));
            cleanupStopwatch.Stop();
            SetSeleniumProfileProgress(false, string.Empty);
            _dashboard.SetSeleniumOperationInProgress(false);
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "selenium-profiles",
                "selenium.profile.enrollment.timing",
                $"browser={browser.Value}; browserOpenMs={browserStopwatch.ElapsedMilliseconds}; sealingMs={sealingMilliseconds}; cleanupMs={cleanupStopwatch.ElapsedMilliseconds}");
        }
    }

    private async void EditSeleniumProfile_Click(object sender, RoutedEventArgs e)
    {
        if (!_dashboard.SeleniumProfileActionsEnabled || sender is not Button { Tag: string id })
        {
            return;
        }

        var profile = _seleniumProfileStore.GetProfiles().FirstOrDefault(item => item.Id == id && item.IsVerified);
        if (profile is null)
        {
            InstallationStatusText.Text = _dashboard.Text.SeleniumProfileUpdateFailed("The profile does not exist or is damaged.");
            return;
        }

        if (!ConfirmationDialog.Show(
                this,
                _dashboard.Text.EditSeleniumProfileTitle,
                _dashboard.Text.EditSeleniumProfileQuestion(profile.Name),
                _dashboard.Text.EditSeleniumProfile,
                _dashboard.Text.Cancel))
        {
            return;
        }

        var browserName = profile.Browser switch
        {
            SeleniumProfileBrowser.Edge => "MicrosoftEdge",
            SeleniumProfileBrowser.Chrome => "chrome",
            SeleniumProfileBrowser.Firefox => "firefox",
            _ => string.Empty
        };
        var environment = _seleniumEnvironments.FirstOrDefault(item =>
            item.IsReady && string.Equals(item.BrowserName, browserName, StringComparison.OrdinalIgnoreCase));
        if (environment is null)
        {
            InstallationStatusText.Text = _dashboard.Text.SeleniumProfileUpdateFailed(_dashboard.Text.ProfileBrowserUnavailable);
            return;
        }

        var token = Guid.NewGuid().ToString("N");
        var draftRelativePath = Path.Combine("temp", "selenium-profile-creation", token);
        var draftPath = _paths.Resolve(draftRelativePath);
        _dashboard.SetSeleniumOperationInProgress(true);
        SetSeleniumProfileProgress(true, _dashboard.Text.SeleniumProfilePreparingEdit);
        var browserStopwatch = new Stopwatch();
        var sealingMilliseconds = 0L;
        try
        {
            draftRelativePath = await Task.Run(
                () => _seleniumProfileStore.CreateEditDraft(profile.Id, token),
                _applicationLifetime.Token);
            draftPath = _paths.Resolve(draftRelativePath);
            var executable = _paths.Resolve(environment.BrowserExecutablePath);
            var startInfo = CreateManagedBrowserStartInfo(executable, profile.Browser, draftPath, "about:blank");

            InstallationStatusText.Text = _dashboard.Text.SeleniumProfileEditing;
            SetSeleniumProfileProgress(true, _dashboard.Text.SeleniumProfileEditing);
            browserStopwatch.Start();
            var process = Process.Start(startInfo);
            if (process is null)
            {
                InstallationStatusText.Text = _dashboard.Text.BrowserCouldNotStart;
                return;
            }

            using (process)
            {
                await process.WaitForExitAsync(_applicationLifetime.Token);
            }

            if (profile.Browser == SeleniumProfileBrowser.Firefox)
            {
                await WaitForManagedFirefoxShutdownAsync(draftRelativePath, draftPath, _applicationLifetime.Token);
            }

            browserStopwatch.Stop();
            SetSeleniumProfileProgress(true, _dashboard.Text.SeleniumProfileSealing);
            var sealingStopwatch = Stopwatch.StartNew();
            var result = await Task.Run(
                () => _seleniumProfileStore.UpdateFromManagedDraft(profile.Id, draftRelativePath, environment.BrowserVersion),
                _applicationLifetime.Token);
            sealingStopwatch.Stop();
            sealingMilliseconds = sealingStopwatch.ElapsedMilliseconds;
            if (!result.IsSuccess)
            {
                InstallationStatusText.Text = _dashboard.Text.SeleniumProfileUpdateFailed(result.Detail);
                return;
            }

            _dashboard.SetSeleniumProfiles(_seleniumProfileStore.GetProfiles());
            InstallationStatusText.Text = _dashboard.Text.SeleniumProfileUpdated(profile.Name);
        }
        catch (OperationCanceledException) when (_applicationLifetime.IsCancellationRequested)
        {
            // Closing the application cancels editing and leaves the original master intact.
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            InstallationStatusText.Text = _dashboard.Text.SeleniumProfileUpdateFailed(exception.Message);
        }
        finally
        {
            browserStopwatch.Stop();
            SetSeleniumProfileProgress(true, _dashboard.Text.SeleniumProfileCleaning);
            var cleanupStopwatch = Stopwatch.StartNew();
            await Task.Run(() => TryDeleteProfileDraft(draftPath));
            cleanupStopwatch.Stop();
            SetSeleniumProfileProgress(false, string.Empty);
            _dashboard.SetSeleniumOperationInProgress(false);
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "selenium-profiles",
                "selenium.profile.edit.timing",
                $"profile={profile.Id}; browser={profile.Browser}; browserOpenMs={browserStopwatch.ElapsedMilliseconds}; sealingMs={sealingMilliseconds}; cleanupMs={cleanupStopwatch.ElapsedMilliseconds}");
        }
    }

    private static ProcessStartInfo CreateManagedBrowserStartInfo(
        string executable,
        SeleniumProfileBrowser browser,
        string draftPath,
        string startPage)
    {
        var startInfo = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(executable)!
        };
        startInfo.Environment["MOZ_CRASHREPORTER_DISABLE"] = "1";
        startInfo.Environment["MOZ_CRASHREPORTER_NO_REPORT"] = "1";
        if (browser == SeleniumProfileBrowser.Firefox)
        {
            startInfo.ArgumentList.Add("-no-remote");
            startInfo.ArgumentList.Add("-profile");
            startInfo.ArgumentList.Add(draftPath);
            startInfo.ArgumentList.Add("-new-window");
            startInfo.ArgumentList.Add(startPage);
        }
        else
        {
            startInfo.ArgumentList.Add($"--user-data-dir={draftPath}");
            startInfo.ArgumentList.Add("--no-first-run");
            startInfo.ArgumentList.Add("--no-default-browser-check");
            startInfo.ArgumentList.Add("--new-window");
            startInfo.ArgumentList.Add(startPage);
        }

        return startInfo;
    }

    private void SetSeleniumProfileProgress(bool visible, string message)
    {
        SeleniumProfileProgressText.Text = message;
        SeleniumProfileProgressBar.IsIndeterminate = visible;
        SeleniumProfileProgressPanel.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
    }

    private async Task WaitForManagedFirefoxShutdownAsync(
        string draftRelativePath,
        string draftPath,
        CancellationToken cancellationToken)
    {
        var activationDeadline = DateTimeOffset.UtcNow.AddSeconds(20);
        while (!_seleniumProfileStore.IsManagedDraftInUse(draftRelativePath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (DateTimeOffset.UtcNow >= activationDeadline)
            {
                throw new InvalidOperationException("Firefox did not attach to the managed profile. Close any browser window and try again.");
            }

            await Task.Delay(100, cancellationToken);
        }

        while (_seleniumProfileStore.IsManagedDraftInUse(draftRelativePath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(200, cancellationToken);
        }

        var flushDeadline = DateTimeOffset.UtcNow.AddSeconds(10);
        while (!CanOpenFirefoxDraftFilesExclusively(draftPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (DateTimeOffset.UtcNow >= flushDeadline)
            {
                throw new IOException("Firefox closed, but its profile files are still in use. Wait a moment and try again.");
            }

            await Task.Delay(200, cancellationToken);
        }
    }

    private static bool CanOpenFirefoxDraftFilesExclusively(string draftPath)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(draftPath, "*", SearchOption.TopDirectoryOnly))
            {
                if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                {
                    return false;
                }

                using var stream = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            }

            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private void TryDeleteProfileDraft(string draftPath)
    {
        try
        {
            var expectedRoot = _paths.Resolve(Path.Combine("temp", "selenium-profile-creation"))
                .TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            var fullPath = Path.GetFullPath(draftPath);
            if (fullPath.StartsWith(expectedRoot, StringComparison.OrdinalIgnoreCase) && Directory.Exists(fullPath))
            {
                var pending = new Stack<string>();
                pending.Push(fullPath);
                while (pending.Count > 0)
                {
                    var directory = pending.Pop();
                    if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
                    {
                        throw new InvalidDataException("Refusing to remove a profile draft containing a reparse point.");
                    }

                    foreach (var child in Directory.EnumerateDirectories(directory, "*", SearchOption.TopDirectoryOnly))
                    {
                        pending.Push(child);
                    }

                    foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.TopDirectoryOnly))
                    {
                        if ((File.GetAttributes(file) & FileAttributes.ReparsePoint) != 0)
                        {
                            throw new InvalidDataException("Refusing to remove a profile draft containing a reparse point.");
                        }
                        File.SetAttributes(file, File.GetAttributes(file) & ~FileAttributes.ReadOnly);
                    }
                }
                Directory.Delete(fullPath, recursive: true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            _ = _logger.LogAsync(ApplicationLogLevel.Warning, "selenium-profiles", "selenium.profile.draft.cleanup-failed", exception.Message);
        }
    }

    private void RemoveSeleniumProfile_Click(object sender, RoutedEventArgs e)
    {
        if (!_dashboard.SeleniumProfileActionsEnabled || sender is not Button { Tag: string id })
        {
            return;
        }

        var profile = _dashboard.SeleniumProfiles.FirstOrDefault(item => item.Id == id);
        if (profile is null || !ConfirmationDialog.Show(
                this,
                _dashboard.Text.RemoveSeleniumProfileTitle,
                _dashboard.Text.RemoveSeleniumProfileQuestion(profile.Name),
                _dashboard.Text.Delete,
                _dashboard.Text.Cancel))
        {
            return;
        }

        var result = _seleniumProfileStore.Remove(id);
        if (!result.IsSuccess)
        {
            InstallationStatusText.Text = _dashboard.Text.SeleniumOperationFailed(result.Detail);
            return;
        }

        _dashboard.SetSeleniumProfiles(_seleniumProfileStore.GetProfiles());
        InstallationStatusText.Text = _dashboard.Text.SeleniumProfileRemoved;
    }

    private void CopySeleniumProfileId_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id })
        {
            CopySeleniumIdentifier(id, _dashboard.Text.ProfileIdCopied);
        }
    }

    private void CopyCookieVaultId_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string id })
        {
            CopySeleniumIdentifier(id, _dashboard.Text.CookieVaultIdCopied);
        }
    }

    private void CopySeleniumIdentifier(string id, string successMessage)
    {
        try
        {
            Clipboard.SetText(id);
            InstallationStatusText.Text = successMessage;
        }
        catch (Exception exception) when (exception is System.Runtime.InteropServices.ExternalException)
        {
            InstallationStatusText.Text = _dashboard.Text.CopyIdFailed(exception.Message);
        }
    }

    private void ChooseCookieFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = _dashboard.Text.ChooseCookieFile,
            Filter = "JSON (*.json)|*.json|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        _selectedCookieFilePath = dialog.FileName;
        SelectedCookieFileText.Text = Path.GetFileName(dialog.FileName);
    }

    private async void ImportCookieVault_Click(object sender, RoutedEventArgs e)
    {
        if (!_dashboard.SeleniumProfileActionsEnabled)
        {
            return;
        }

        if (_selectedCookieFilePath is null || !File.Exists(_selectedCookieFilePath))
        {
            InstallationStatusText.Text = _dashboard.Text.NoCookieFileSelected;
            return;
        }

        byte[]? json = null;
        var vaultName = CookieVaultNameTextBox.Text;
        _dashboard.SetSeleniumOperationInProgress(true);
        try
        {
            var file = new FileInfo(_selectedCookieFilePath);
            if (file.Length is < 1 or > 5 * 1024 * 1024)
            {
                InstallationStatusText.Text = _dashboard.Text.CookieVaultImportFailed("The cookie export must be between 1 byte and 5 MiB.");
                return;
            }

            json = await File.ReadAllBytesAsync(_selectedCookieFilePath, _applicationLifetime.Token);
            var result = await Task.Run(
                () => _seleniumCookieVaultStore.ImportJson(vaultName, json),
                _applicationLifetime.Token);
            if (!result.IsSuccess)
            {
                InstallationStatusText.Text = _dashboard.Text.CookieVaultImportFailed(result.Detail);
                return;
            }

            CookieVaultNameTextBox.Clear();
            _selectedCookieFilePath = null;
            SelectedCookieFileText.Text = _dashboard.Text.NoCookieFileSelected;
            RefreshCookieVaults();
            InstallationStatusText.Text = _dashboard.Text.CookieVaultImported(result.Vault!.Name, result.SkippedCookies);
        }
        catch (OperationCanceledException)
        {
            InstallationStatusText.Text = _dashboard.Text.OperationCanceled;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            InstallationStatusText.Text = _dashboard.Text.CookieVaultImportFailed(exception.Message);
        }
        finally
        {
            if (json is not null)
            {
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(json);
            }
            _dashboard.SetSeleniumOperationInProgress(false);
        }
    }

    private void RemoveCookieVault_Click(object sender, RoutedEventArgs e)
    {
        if (!_dashboard.SeleniumProfileActionsEnabled || sender is not Button { Tag: string id })
        {
            return;
        }

        var vault = _dashboard.SeleniumCookieVaults.FirstOrDefault(item => item.Id == id);
        if (vault is null || !ConfirmationDialog.Show(
                this,
                _dashboard.Text.RemoveCookieVaultTitle,
                _dashboard.Text.RemoveCookieVaultQuestion(vault.Name),
                _dashboard.Text.Delete,
                _dashboard.Text.Cancel))
        {
            return;
        }

        var result = _seleniumCookieVaultStore.Remove(id);
        InstallationStatusText.Text = result.IsSuccess
            ? _dashboard.Text.CookieVaultRemoved
            : _dashboard.Text.SeleniumOperationFailed(result.Detail);
        RefreshCookieVaults();
    }

    private void RefreshCookieVaults() =>
        _dashboard.SetSeleniumCookieVaults(_seleniumCookieVaultStore.GetVaults());

    private void OpenSeleniumHub_Click(object sender, RoutedEventArgs e)
    {
        if (!_dashboard.SeleniumIsRunning)
        {
            return;
        }

        Process.Start(new ProcessStartInfo(_dashboard.SeleniumHubUrl) { UseShellExecute = true });
        InstallationStatusText.Text = _dashboard.SeleniumHubUrl;
    }

    private async void RefreshSeleniumSessions_Click(object sender, RoutedEventArgs e) => await RefreshSeleniumSessionsAsync();

    private async Task RefreshSeleniumSessionsAsync()
    {
        if (!_dashboard.SeleniumIsRunning)
        {
            _dashboard.SetSeleniumSessions([]);
            return;
        }

        try
        {
            var sessions = await _seleniumGrid.ListSessionsAsync(_seleniumOptions.Port, _applicationLifetime.Token);
            _dashboard.SetSeleniumSessions(sessions);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidDataException or JsonException or TaskCanceledException)
        {
            InstallationStatusText.Text = _dashboard.Text.SeleniumSessionsFailed(exception.Message);
        }
    }

    private async void TerminateSeleniumSession_Click(object sender, RoutedEventArgs e)
    {
        if (!_dashboard.SeleniumSessionActionsEnabled || sender is not Button { Tag: string sessionId })
        {
            return;
        }

        var confirmed = ConfirmationDialog.Show(
            this,
            _dashboard.Text.TerminateSessionTitle,
            _dashboard.Text.TerminateSessionQuestion,
            _dashboard.Text.TerminateSession,
            _dashboard.Text.Cancel);
        if (!confirmed)
        {
            return;
        }

        _dashboard.SetSeleniumOperationInProgress(true);
        InstallationStatusText.Text = _dashboard.Text.TerminatingSession;
        try
        {
            var result = await _seleniumGrid.TerminateSessionAsync(
                _seleniumOptions.Port,
                sessionId,
                _applicationLifetime.Token);
            if (!result.IsSuccess)
            {
                InstallationStatusText.Text = _dashboard.Text.SeleniumOperationFailed(result.Detail);
                return;
            }

            await RefreshSeleniumSessionsAsync();
            InstallationStatusText.Text = _dashboard.Text.SeleniumSessionTerminated;
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidDataException or TaskCanceledException)
        {
            InstallationStatusText.Text = _dashboard.Text.SeleniumOperationFailed(exception.Message);
        }
        finally
        {
            _dashboard.SetSeleniumOperationInProgress(false);
        }
    }

    private void OpenComposerProject_Click(object sender, RoutedEventArgs e) =>
        OpenProjectDirectory(_composerPackageManager.ProjectRelativePath, _dashboard.Composer);

    private void OpenNodeProject_Click(object sender, RoutedEventArgs e) =>
        OpenProjectDirectory(_nodePackageManager.ProjectRelativePath, _dashboard.Node);

    private async void ProjectSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_changingWebProject || sender is not ComboBox { SelectedValue: string projectId } ||
            string.Equals(projectId, _projectContext.ActiveProject.Id, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await SelectWebProjectAsync(projectId);
    }

    private async void SelectWebProject_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string projectId })
        {
            await SelectWebProjectAsync(projectId);
        }
    }

    private async Task<bool> SelectWebProjectAsync(string projectId)
    {
        if (_changingWebProject || string.Equals(projectId, _projectContext.ActiveProject.Id, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var requestedProject = _projects.GetRequired(projectId);
        if (!Directory.Exists(_paths.Resolve(requestedProject.RootRelativePath)))
        {
            RefreshWebProjectBindings();
            InstallationStatusText.Text = _dashboard.Text.ProjectDirectoryUnavailable;
            return false;
        }

        if (!CanChangeWebProject())
        {
            RefreshWebProjectBindings();
            InstallationStatusText.Text = _dashboard.Text.ProjectChangeBusy;
            return false;
        }

        _changingWebProject = true;
        try
        {
            var activation = _projectContext.Activate(projectId);
            if (!activation.IsSuccess)
            {
                RefreshWebProjectBindings();
                InstallationStatusText.Text = _dashboard.Text.ProjectChangeBusy;
                return false;
            }
            RefreshWebProjectBindings();
            _workspaceDirectory = string.Empty;
            _workspaceClipboard = null;
            _workspaceHistory.Clear();
            _workspacePageNumber = 1;
            _terminalWorkingDirectory = _terminalService.InitialWorkingDirectory;
            ResetTerminalConsole();
            RefreshWorkspaceFiles();
            await RefreshPackageManagerAsync(_composerPackageManager, _dashboard.Composer);
            await RefreshPackageManagerAsync(_nodePackageManager, _dashboard.Node);
            InstallationStatusText.Text = _dashboard.Text.ProjectSelected(_dashboard.ActiveWebProjectName);
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "projects",
                "project.selected",
                $"project={_projectContext.ActiveProject.Id}");
            return true;
        }
        catch (Exception exception) when (exception is IOException or ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectOperationFailed(exception.Message);
            RefreshWebProjectBindings();
            return false;
        }
        finally
        {
            _changingWebProject = false;
        }
    }

    private void ManageProjects_Click(object sender, RoutedEventArgs e) =>
        _dashboard.SelectedPage = NavigationPage.Projects;

    private async void OpenProjectFiles_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string projectId } || !await SelectWebProjectAsync(projectId))
        {
            return;
        }

        _dashboard.SelectedPage = NavigationPage.Files;
        RefreshWorkspaceFiles();
        InstallationStatusText.Text = DisplayTerminalPath(_workspaceDirectory);
    }

    private async void OpenProjectTerminal_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string projectId } || !await SelectWebProjectAsync(projectId))
        {
            return;
        }

        _dashboard.SelectedPage = NavigationPage.Terminal;
        TerminalConsoleTextBox.Focus();
    }

    private void OpenManagedProject_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string projectId })
        {
            OpenProjectDirectory(_projects.GetRequired(projectId).RootRelativePath);
        }
    }

    private void RenameProject_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string projectId })
        {
            return;
        }

        var project = _projects.GetRequired(projectId);
        var dialog = new NamePromptDialog(
            this,
            _dashboard.Text.RenameProject,
            _dashboard.Text.RenameProjectPrompt,
            _dashboard.Text.RenameProject,
            _dashboard.Text.Cancel,
            _dashboard.Text.RenameProjectValidation,
            project.Name);
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var renamed = project with { Name = dialog.ItemName };
            ProjectCatalogValidator.ValidateProject(renamed);
            _projects.Update(renamed);
            RefreshWebProjectBindings();
            InstallationStatusText.Text = _dashboard.Text.ProjectRenamed(renamed.Name);
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "projects",
                "project.renamed",
                $"project={project.Id}");
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException or UnauthorizedAccessException)
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectOperationFailed(exception.Message);
        }
    }

    private async void UnregisterProject_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string projectId })
        {
            return;
        }

        var project = _projects.GetRequired(projectId);
        if (!CanChangeWebProject())
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectChangeBusy;
            return;
        }

        if (!ConfirmationDialog.Show(
                this,
                _dashboard.Text.UnregisterProject,
                _dashboard.Text.UnregisterProjectQuestion(project.Name),
                _dashboard.Text.UnregisterProject,
                _dashboard.Text.Cancel))
        {
            return;
        }

        try
        {
            if (string.Equals(project.Id, _projectContext.ActiveProject.Id, StringComparison.OrdinalIgnoreCase) &&
                !await SelectWebProjectAsync(ProjectCatalogDefaults.DefaultProjectId))
            {
                return;
            }

            _projects.Remove(project.Id);
            RefreshWebProjectBindings();
            if (project.Web?.IsEnabled == true)
            {
                RecordWebConfigurationChange();
            }
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "projects",
                "project.unregistered",
                $"project={project.Id}; filesPreserved=true");
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectOperationFailed(exception.Message);
        }
    }

    private async void CreateGeneralProject_Click(object sender, RoutedEventArgs e)
    {
        if (ProjectTemplateSelector.SelectedValue is not ProjectTemplateKind template)
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectOperationFailed(_dashboard.Text.ProjectTemplate);
            return;
        }

        if (!CanChangeWebProject())
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectChangeBusy;
            return;
        }

        try
        {
            var result = await _projectTemplateService.CreateAsync(
                new ProjectTemplateRequest(GeneralProjectNameTextBox.Text, template),
                _applicationLifetime.Token);
            GeneralProjectNameTextBox.Clear();
            ProjectTemplateSelector.SelectedValue = ProjectTemplateKind.Empty;
            ResetProjectTools();
            RefreshWebProjectBindings();
            await RefreshProjectCapabilitiesAsync();
            InstallationStatusText.Text = _dashboard.Text.ProjectCreatedWithoutDownloads(result.Project.Name);
            if (result.Project.Web?.IsEnabled == true)
            {
                RecordWebConfigurationChange();
            }
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "projects",
                "project.template.created",
                $"project={result.Project.Id}; template={template}; files={result.CreatedRelativePaths.Count}");
        }
        catch (OperationCanceledException) when (_applicationLifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectOperationFailed(exception.Message);
        }
    }

    private async void RegisterExistingProject_Click(object sender, RoutedEventArgs e)
    {
        if (ExistingProjectDirectorySelector.SelectedValue is not string directoryId)
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectOperationFailed(_dashboard.Text.NoExistingProjectDirectories);
            return;
        }

        if (!CanChangeWebProject())
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectChangeBusy;
            return;
        }

        try
        {
            var displayName = string.IsNullOrWhiteSpace(ExistingProjectNameTextBox.Text)
                ? directoryId
                : ExistingProjectNameTextBox.Text;
            var project = await _projectTemplateService.RegisterExistingAsync(
                directoryId,
                displayName,
                _applicationLifetime.Token);
            ExistingProjectNameTextBox.Clear();
            ResetProjectTools();
            RefreshWebProjectBindings();
            await RefreshProjectCapabilitiesAsync();
            InstallationStatusText.Text = _dashboard.Text.ProjectRegistered(project.Name);
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "projects",
                "project.existing.registered",
                $"project={project.Id}; contentModified=false");
        }
        catch (OperationCanceledException) when (_applicationLifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectOperationFailed(exception.Message);
            RefreshWebProjectBindings();
        }
    }

    private void ConfigureProjectWeb_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string projectId })
        {
            return;
        }

        if (!_dashboard.PhpSettingsEnabled)
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectChangeBusy;
            return;
        }

        var project = _projects.GetRequired(projectId);
        var initialSettings = project.Web ?? new ProjectWebSettings(true, "public", true);
        var dialog = new ProjectWebSettingsDialog(
            this,
            _dashboard.Text.ConfigureWebProject,
            _dashboard.Text.ConfigureWebRootPrompt,
            _dashboard.Text.ServeProjectThroughApache,
            _dashboard.Text.AllowHtaccessLabel,
            _dashboard.Text.WebSettingsHelp,
            _dashboard.Text.DefaultProjectWebRequired,
            _dashboard.Text.SaveWebConfiguration,
            _dashboard.Text.Cancel,
            _dashboard.Text.ConfigureWebRootValidation,
            initialSettings,
            !string.Equals(project.Id, ProjectCatalogDefaults.DefaultProjectId, StringComparison.OrdinalIgnoreCase));
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var settings = dialog.Settings;
            var affectsRunningConfiguration = project.Web?.IsEnabled == true || settings.IsEnabled;
            var result = _projectWebConfigurationService.Configure(projectId, settings);
            RefreshWebProjectBindings();
            RecordWebConfigurationChange(affectsRunningConfiguration);
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "projects",
                "project.web.configured",
                $"project={projectId}; webRoot={result.Project.Web!.RootRelativePath}; enabled={settings.IsEnabled}; allowHtaccess={settings.AllowHtaccess}; directoryCreated={result.WebRootDirectoryCreated}; starterFileCreated={result.StarterFileCreated}");
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectOperationFailed(exception.Message);
        }
    }

    private void OpenWebProjectUrl_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string projectId })
        {
            var hostName = string.Equals(projectId, ProjectCatalogDefaults.DefaultProjectId, StringComparison.OrdinalIgnoreCase)
                ? "localhost"
                : $"{projectId}.localhost";
            Process.Start(new ProcessStartInfo($"http://{hostName}:{_portSettings.ApachePort}/") { UseShellExecute = true });
        }
    }

    private void RecordWebConfigurationChange(bool affectsRunningConfiguration = true)
    {
        if (_dashboard.ApacheIsRunning &&
            (affectsRunningConfiguration || _dashboard.WebConfigurationRestartRequired))
        {
            _dashboard.SetWebConfigurationRestartRequired(true);
            InstallationStatusText.Text = _dashboard.Text.WebConfigurationRestartPending;
            return;
        }

        InstallationStatusText.Text = _dashboard.Text.WebConfigurationSavedForNextStart;
    }

    private async void ApplyWebConfiguration_Click(object sender, RoutedEventArgs e)
    {
        if (!_dashboard.WebConfigurationApplyEnabled || !await RestartApacheAsync(announce: false))
        {
            return;
        }

        InstallationStatusText.Text = _dashboard.Text.WebConfigurationApplied;
    }

    private void ResetProjectTools()
    {
        _workspaceDirectory = string.Empty;
        _workspaceHistory.Clear();
        _workspacePageNumber = 1;
        _terminalWorkingDirectory = _terminalService.InitialWorkingDirectory;
        RefreshWorkspaceFiles();
        ResetTerminalConsole();
    }

    private void RefreshWebProjectBindings()
    {
        _dashboard.SetProjects(_projects.Projects, _projectContext.ActiveProject.Id, _projectCapabilitySnapshots);
        _dashboard.SetRegistrableProjectDirectories(_projectTemplateService.GetRegistrableDirectories());
        _dashboard.SetWebProjects(_webProjects.Projects, _projectContext.ActiveProject.Id);
        _dashboard.Composer.SetProjectRelativePath(_composerPackageManager.ProjectRelativePath);
        _dashboard.Node.SetProjectRelativePath(_nodePackageManager.ProjectRelativePath);
    }

    private async Task RefreshProjectCapabilitiesAsync()
    {
        var revision = ++_projectCapabilityRefreshRevision;
        var projects = _projects.Projects.ToArray();
        var tasks = projects.Select(async project =>
        {
            try
            {
                return await Task.Run(
                    () => _projectCapabilityDetector.DetectAsync(project, _applicationLifetime.Token),
                    _applicationLifetime.Token);
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException)
            {
                await _logger.LogAsync(
                    ApplicationLogLevel.Warning,
                    "projects",
                    "project.capabilities.failed",
                    $"project={project.Id}; detail={exception.Message}");
                return new ProjectCapabilitySnapshot(project.Id, []);
            }
        });

        try
        {
            var snapshots = await Task.WhenAll(tasks);
            if (revision != _projectCapabilityRefreshRevision || _applicationLifetime.IsCancellationRequested)
            {
                return;
            }

            _projectCapabilitySnapshots = snapshots.ToDictionary(
                snapshot => snapshot.ProjectId,
                StringComparer.OrdinalIgnoreCase);
            RefreshWebProjectBindings();
        }
        catch (OperationCanceledException) when (_applicationLifetime.IsCancellationRequested)
        {
        }
    }

    private bool CanChangeWebProject() =>
        !_projectContext.IsSwitchBlocked;

    private ProjectSwitchBlockReason GetProjectSwitchBlockReason()
    {
        if (_terminalBusy)
        {
            return ProjectSwitchBlockReason.InteractiveTerminal;
        }

        return _dashboard.Composer.IsBusy || _dashboard.Node.IsBusy
            ? ProjectSwitchBlockReason.ProjectOperation
            : ProjectSwitchBlockReason.None;
    }

    private void OpenPythonProject_Click(object sender, RoutedEventArgs e) =>
        OpenProjectDirectory(_pythonPackageManager.ProjectRelativePath, _dashboard.Python);

    private async void EditCustomPhpIni_Click(object sender, RoutedEventArgs e) =>
        await OpenPortableFileAsync(
            PhpCustomIni.GetRelativePath("default"),
            Path.Combine("instances", "default", "config"),
            PortableFileLaunchIntent.Edit,
            PhpCustomIni.InitialContent);

    private void OpenProjectDirectory(string relativePath, PackageManagerPageViewModel page)
    {
        var folder = _paths.EnsureDirectory(relativePath);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
        SetPackageStatus(page, relativePath);
    }

    private void OpenProjectDirectory(string relativePath)
    {
        var folder = _paths.EnsureDirectory(relativePath);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
        InstallationStatusText.Text = relativePath;
    }

    private async void TerminalConsoleTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var sessionRunning = _terminalSession is { IsRunning: true };
        if (sessionRunning && Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C &&
            TerminalConsoleTextBox.SelectionLength == 0)
        {
            e.Handled = true;
            await StopTerminalSessionAsync();
            return;
        }

        if (sessionRunning && e.Key == Key.Enter)
        {
            e.Handled = true;
            await SendTerminalSessionInputAsync();
            return;
        }

        if (_terminalBusy && !sessionRunning)
        {
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await ExecuteTerminalCommandAsync();
            return;
        }

        if (e.Key == Key.Up || e.Key == Key.Down)
        {
            e.Handled = true;
            if (!sessionRunning)
            {
                NavigateTerminalHistory(e.Key == Key.Up ? -1 : 1);
            }
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.A)
        {
            e.Handled = true;
            TerminalConsoleTextBox.Select(_terminalInputStart, TerminalConsoleTextBox.Text.Length - _terminalInputStart);
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
        {
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.V &&
            TerminalConsoleTextBox.SelectionStart < _terminalInputStart)
        {
            MoveTerminalCaretToEnd();
            return;
        }

        if (e.Key is Key.Back or Key.Delete or Key.Left or Key.Home ||
            (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.X))
        {
            var selectionTouchesOutput = TerminalConsoleTextBox.SelectionLength > 0 &&
                                         TerminalConsoleTextBox.SelectionStart < _terminalInputStart;
            var caretTouchesOutput = TerminalConsoleTextBox.SelectionLength == 0 && (
                TerminalConsoleTextBox.CaretIndex < _terminalInputStart ||
                (e.Key is Key.Back or Key.Left or Key.Home &&
                 TerminalConsoleTextBox.CaretIndex == _terminalInputStart));
            if (selectionTouchesOutput || caretTouchesOutput)
            {
                e.Handled = true;
                MoveTerminalCaretToEnd();
            }
        }
    }

    private void TerminalConsoleTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (_terminalBusy && _terminalSession is not { IsRunning: true })
        {
            e.Handled = true;
            return;
        }

        if (TerminalConsoleTextBox.SelectionStart < _terminalInputStart)
        {
            MoveTerminalCaretToEnd();
        }
    }

    private async Task ExecuteTerminalCommandAsync()
    {
        if (_terminalBusy)
        {
            return;
        }

        var command = TerminalConsoleTextBox.Text[_terminalInputStart..].TrimEnd('\r', '\n');
        if (string.IsNullOrWhiteSpace(command))
        {
            AppendTerminalRaw(Environment.NewLine);
            WriteTerminalPrompt();
            return;
        }

        _terminalBusy = true;
        TerminalConsoleTextBox.IsReadOnly = true;
        AppendTerminalRaw(Environment.NewLine);
        _terminalHistory.Remove(command);
        _terminalHistory.Add(command);
        _terminalHistoryIndex = _terminalHistory.Count;
        try
        {
            var sessionStart = await _terminalService.TryStartSessionAsync(
                command,
                _terminalWorkingDirectory,
                new DelegateProgress<PortableProcessOutput>(QueueTerminalProcessOutput),
                _applicationLifetime.Token);
            if (sessionStart.IsRuntimeCommand)
            {
                if (!sessionStart.IsSuccess)
                {
                    AppendTerminalLine(sessionStart.Error);
                    return;
                }

                _terminalSession = sessionStart.Session;
                TerminalConsoleTextBox.IsReadOnly = false;
                _terminalInputStart = TerminalConsoleTextBox.Text.Length;
                MoveTerminalCaretToEnd();
                TerminalConsoleTextBox.Focus();
                _ = ObserveTerminalSessionAsync(sessionStart.Session!);
                return;
            }

            var result = await _terminalService.ExecuteAsync(
                command,
                _terminalWorkingDirectory,
                _applicationLifetime.Token);
            _terminalWorkingDirectory = result.WorkingDirectory;
            if (result.ClearScreen)
            {
                TerminalConsoleTextBox.Clear();
            }

            if (result.ServiceRequest is not null)
            {
                await ExecuteTerminalServiceRequestAsync(result.ServiceRequest);
                AppendTerminalLine(GetServiceStatusText());
            }
            else if (!string.IsNullOrWhiteSpace(result.Output))
            {
                AppendTerminalLine(result.Output);
            }
        }
        catch (OperationCanceledException)
        {
            AppendTerminalLine(_dashboard.Text.OperationCanceled);
        }
        catch (Exception exception)
        {
            AppendTerminalLine(exception.Message);
        }
        finally
        {
            if (_terminalSession is null)
            {
                _terminalBusy = false;
                TerminalConsoleTextBox.IsReadOnly = false;
                WriteTerminalPrompt();
                TerminalConsoleTextBox.Focus();
            }
        }
    }

    private async Task SendTerminalSessionInputAsync()
    {
        var session = _terminalSession;
        if (session is null || !session.IsRunning)
        {
            return;
        }

        var input = TerminalConsoleTextBox.Text[_terminalInputStart..].TrimEnd('\r', '\n');
        AppendTerminalRaw(Environment.NewLine);
        _terminalInputStart = TerminalConsoleTextBox.Text.Length;
        try
        {
            await session.WriteLineAsync(input, _applicationLifetime.Token);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ObjectDisposedException)
        {
            AppendTerminalLine(exception.Message);
        }
    }

    private async Task StopTerminalSessionAsync()
    {
        var session = _terminalSession;
        if (session is null)
        {
            return;
        }

        AppendTerminalRaw("^C");
        AppendTerminalRaw(Environment.NewLine);
        _terminalInputStart = TerminalConsoleTextBox.Text.Length;
        try
        {
            await session.StopAsync(_applicationLifetime.Token);
        }
        catch (OperationCanceledException)
        {
            // Application shutdown owns the final process cleanup.
        }
    }

    private async Task ObserveTerminalSessionAsync(IPortableProcessSession session)
    {
        try
        {
            var result = await session.Completion;
            FlushTerminalProcessOutput();
            if (result.TimedOut)
            {
                AppendTerminalLine(_dashboard.Text.TerminalProcessTimedOut);
            }
            else if (!result.WasStopped && result.ExitCode is not 0)
            {
                AppendTerminalLine(_dashboard.Text.TerminalProcessExited(result.ExitCode));
            }
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        {
            AppendTerminalLine(exception.Message);
        }
        finally
        {
            FlushTerminalProcessOutput();
            await session.DisposeAsync();
            if (ReferenceEquals(_terminalSession, session))
            {
                _terminalSession = null;
            }

            _terminalBusy = false;
            TerminalConsoleTextBox.IsReadOnly = false;
            _terminalInputStart = TerminalConsoleTextBox.Text.Length;
            WriteTerminalPrompt();
            TerminalConsoleTextBox.Focus();
        }
    }

    private void QueueTerminalProcessOutput(PortableProcessOutput output)
    {
        lock (_terminalOutputLock)
        {
            _terminalOutputBuffer.Append(output.Text);
            if (_terminalOutputBuffer.Length > MaximumPendingTerminalOutputCharacters)
            {
                _terminalOutputBuffer.Remove(
                    0,
                    _terminalOutputBuffer.Length - MaximumPendingTerminalOutputCharacters);
            }

            if (_terminalOutputFlushScheduled)
            {
                return;
            }

            _terminalOutputFlushScheduled = true;
        }

        _ = Dispatcher.BeginInvoke(DispatcherPriority.Background, FlushTerminalProcessOutput);
    }

    private void FlushTerminalProcessOutput()
    {
        string output;
        lock (_terminalOutputLock)
        {
            output = _terminalOutputBuffer.ToString();
            _terminalOutputBuffer.Clear();
            _terminalOutputFlushScheduled = false;
        }

        if (output.Length > 0)
        {
            AppendTerminalProcessOutput(output);
        }
    }

    private async Task ExecuteTerminalServiceRequestAsync(PortableTerminalServiceRequest request)
    {
        if (request.Operation == PortableTerminalServiceOperation.Status)
        {
            return;
        }

        var targets = request.Service == PortableServiceTarget.All
            ? new[] { PortableServiceTarget.MariaDb, PortableServiceTarget.Web, PortableServiceTarget.Selenium }
            : new[] { request.Service };
        if (request.Operation == PortableTerminalServiceOperation.Stop)
        {
            targets = targets.Reverse().ToArray();
        }

        foreach (var target in targets)
        {
            if (request.Operation == PortableTerminalServiceOperation.Restart)
            {
                await SetTerminalServiceStateAsync(target, shouldRun: false);
                await SetTerminalServiceStateAsync(target, shouldRun: true);
            }
            else
            {
                await SetTerminalServiceStateAsync(
                    target,
                    request.Operation == PortableTerminalServiceOperation.Start);
            }
        }
    }

    private async Task SetTerminalServiceStateAsync(PortableServiceTarget service, bool shouldRun)
    {
        switch (service)
        {
            case PortableServiceTarget.Web when (_dashboard.ApacheProcessState == PortableDeveloper.Domain.Processes.ManagedProcessState.Running) != shouldRun:
                await ToggleApacheAsync();
                break;
            case PortableServiceTarget.MariaDb when _dashboard.MariaDbIsRunning != shouldRun:
                await ToggleMariaDbAsync();
                break;
            case PortableServiceTarget.Selenium when _dashboard.SeleniumIsRunning != shouldRun:
                await ToggleSeleniumAsync();
                break;
        }
    }

    private string GetServiceStatusText() => string.Join(Environment.NewLine,
        $"apache: {_dashboard.Text.StackStatus(_dashboard.ApacheProcessState)}",
        $"mariadb: {_dashboard.Text.StackStatus(_dashboard.MariaDbProcessState)}",
        $"selenium: {_dashboard.Text.StackStatus(_dashboard.SeleniumProcessState)}");

    private void ResetTerminalConsole()
    {
        TerminalConsoleTextBox.Clear();
        WriteTerminalPrompt();
    }

    private void WriteTerminalPrompt()
    {
        if (TerminalConsoleTextBox.Text.Length > 0 &&
            !TerminalConsoleTextBox.Text.EndsWith(Environment.NewLine, StringComparison.Ordinal))
        {
            AppendTerminalRaw(Environment.NewLine);
        }

        AppendTerminalRaw($"{DisplayTerminalPath(_terminalWorkingDirectory)}> ");
        _terminalInputStart = TerminalConsoleTextBox.Text.Length;
        MoveTerminalCaretToEnd();
    }

    private void AppendTerminalLine(string text)
    {
        AppendTerminalRaw(text.TrimEnd('\r', '\n'));
        AppendTerminalRaw(Environment.NewLine);
    }

    private void AppendTerminalRaw(string text)
    {
        var next = TerminalConsoleTextBox.Text + text;
        SetTerminalText(next, _terminalInputStart);
    }

    private void AppendTerminalProcessOutput(string text)
    {
        var current = TerminalConsoleTextBox.Text;
        var inputStart = Math.Clamp(_terminalInputStart, 0, current.Length);
        var next = current[..inputStart] + text + current[inputStart..];
        SetTerminalText(next, inputStart + text.Length);
    }

    private void SetTerminalText(string next, int inputStart)
    {
        if (next.Length > MaximumTerminalCharacters)
        {
            var truncationNotice = _dashboard.Text.TerminalOutputTruncated + Environment.NewLine;
            var retainedCharacters = MaximumTerminalCharacters - truncationNotice.Length;
            var removed = next.Length - retainedCharacters;
            next = truncationNotice + next[removed..];
            inputStart = Math.Max(truncationNotice.Length, inputStart - removed + truncationNotice.Length);
        }

        TerminalConsoleTextBox.Text = next;
        _terminalInputStart = Math.Clamp(inputStart, 0, next.Length);
        MoveTerminalCaretToEnd();
    }

    private void MoveTerminalCaretToEnd()
    {
        TerminalConsoleTextBox.CaretIndex = TerminalConsoleTextBox.Text.Length;
        TerminalConsoleTextBox.SelectionLength = 0;
        TerminalConsoleTextBox.ScrollToEnd();
    }

    private void NavigateTerminalHistory(int offset)
    {
        if (_terminalHistory.Count == 0)
        {
            return;
        }

        _terminalHistoryIndex = Math.Clamp(_terminalHistoryIndex + offset, 0, _terminalHistory.Count);
        var command = _terminalHistoryIndex == _terminalHistory.Count
            ? string.Empty
            : _terminalHistory[_terminalHistoryIndex];
        TerminalConsoleTextBox.Text = TerminalConsoleTextBox.Text[.._terminalInputStart] + command;
        MoveTerminalCaretToEnd();
    }

    private string DisplayTerminalPath(string relativePath) =>
        string.IsNullOrEmpty(relativePath)
            ? $"{_projectContext.ActiveProject.Id}:/"
            : $"{_projectContext.ActiveProject.Id}:/{relativePath}";

    private sealed class DelegateProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }

    private void RefreshWorkspace_Click(object sender, RoutedEventArgs e) => RefreshWorkspaceFiles();

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_dashboard.SelectedPage == NavigationPage.Files &&
            Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.L)
        {
            e.Handled = true;
            WorkspacePathTextBox.Focus();
            WorkspacePathTextBox.SelectAll();
        }
    }

    private void WorkspacePathTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) =>
        WorkspacePathTextBox.SelectAll();

    private void WorkspacePathTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            WorkspacePathTextBox.Text = DisplayTerminalPath(_workspaceDirectory);
            Keyboard.ClearFocus();
            return;
        }

        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        try
        {
            var requested = WorkspacePathTextBox.Text.Trim();
            var prefix = $"{_projectContext.ActiveProject.Id}:/";
            if (requested.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                requested = requested[prefix.Length..];
            }

            requested = requested.Replace('\\', '/').TrimStart('/');
            var normalized = _workspaceFileManager.NormalizeDirectory(requested);
            if (!string.Equals(normalized, _workspaceDirectory, StringComparison.OrdinalIgnoreCase))
            {
                _workspaceHistory.Push(_workspaceDirectory);
                _workspaceDirectory = normalized;
                _workspacePageNumber = 1;
            }

            RefreshWorkspaceFiles();
            Keyboard.ClearFocus();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            WorkspacePathTextBox.Text = DisplayTerminalPath(_workspaceDirectory);
            InstallationStatusText.Text = _dashboard.Text.WorkspaceOperationFailed(exception.Message);
        }
    }

    private void WorkspaceSort_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string columnText } ||
            !Enum.TryParse<WorkspaceSortColumn>(columnText, out var column))
        {
            return;
        }

        if (_workspaceSortColumn == column)
        {
            _workspaceSortDirection = _workspaceSortDirection == WorkspaceSortDirection.Ascending
                ? WorkspaceSortDirection.Descending
                : WorkspaceSortDirection.Ascending;
        }
        else
        {
            _workspaceSortColumn = column;
            _workspaceSortDirection = WorkspaceSortDirection.Ascending;
        }

        _workspacePageNumber = 1;
        UpdateWorkspaceSortHeaders();
        RefreshWorkspaceFiles();
    }

    private void UpdateWorkspaceSortHeaders()
    {
        WorkspaceNameSortButton.Content = GetWorkspaceSortHeader(_dashboard.Text.Name, WorkspaceSortColumn.Name);
        WorkspaceTypeSortButton.Content = GetWorkspaceSortHeader(_dashboard.Text.Type, WorkspaceSortColumn.Type);
        WorkspaceSizeSortButton.Content = GetWorkspaceSortHeader(_dashboard.Text.Size, WorkspaceSortColumn.Size);
        WorkspaceModifiedSortButton.Content = GetWorkspaceSortHeader(_dashboard.Text.Modified, WorkspaceSortColumn.Modified);
    }

    private string GetWorkspaceSortHeader(string label, WorkspaceSortColumn column)
    {
        if (_workspaceSortColumn != column)
        {
            return label;
        }

        return $"{label} {(_workspaceSortDirection == WorkspaceSortDirection.Ascending ? "↑" : "↓")}";
    }

    private void WorkspacePageSizeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || sender is not ComboBox { SelectedValue: string value } || !int.TryParse(value, out var pageSize))
        {
            return;
        }

        _workspacePageSize = pageSize;
        _workspacePageNumber = 1;
        RefreshWorkspaceFiles();
    }

    private void WorkspacePage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string action })
        {
            return;
        }

        _workspacePageNumber = action switch
        {
            "First" => 1,
            "Previous" => Math.Max(1, _workspacePageNumber - 1),
            "Next" => Math.Min(_dashboard.WorkspaceTotalPages, _workspacePageNumber + 1),
            "Last" => _dashboard.WorkspaceTotalPages,
            _ => _workspacePageNumber
        };
        RefreshWorkspaceFiles();
    }

    private void WorkspaceBack_Click(object sender, RoutedEventArgs e)
    {
        if (_workspaceHistory.Count == 0)
        {
            return;
        }

        _workspaceDirectory = _workspaceHistory.Pop();
        _workspacePageNumber = 1;
        RefreshWorkspaceFiles();
    }

    private void CreateWorkspaceFile_Click(object sender, RoutedEventArgs e)
    {
        var name = PromptForWorkspaceName(
            _dashboard.Text.CreateFileTitle,
            _dashboard.Text.EnterFileName);
        if (name is null)
        {
            return;
        }

        RunWorkspaceOperation(() => _workspaceFileManager.CreateFile(_workspaceDirectory, name));
    }

    private void CreateWorkspaceFolder_Click(object sender, RoutedEventArgs e)
    {
        var name = PromptForWorkspaceName(
            _dashboard.Text.CreateFolderTitle,
            _dashboard.Text.EnterFolderName);
        if (name is null)
        {
            return;
        }

        RunWorkspaceOperation(() => _workspaceFileManager.CreateDirectory(_workspaceDirectory, name));
    }

    private async void WorkspaceEntry_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is TextBox
            || e.ClickCount != 2
            || sender is not Border { Tag: WorkspaceEntryViewModel entry }
            || !entry.IsSafe)
        {
            return;
        }

        e.Handled = true;
        await OpenWorkspaceEntryAsync(entry);
    }

    private void WorkspaceEntriesListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _workspaceDragAnchor = null;
        _workspaceRenameCandidate = null;
        if (e.OriginalSource is TextBox || e.OriginalSource is not DependencyObject source)
        {
            return;
        }

        var item = ItemsControl.ContainerFromElement(WorkspaceEntriesListBox, source) as ListBoxItem;
        if (item?.DataContext is not WorkspaceEntryViewModel { IsSafe: true } entry)
        {
            return;
        }

        _workspaceDragStartPoint = e.GetPosition(WorkspaceEntriesListBox);
        _workspaceDragAnchor = entry;
        if (e.ClickCount == 1
            && source is TextBlock { Tag: "WorkspaceEntryName" }
            && WorkspaceEntriesListBox.SelectedItems.Count == 1
            && ReferenceEquals(WorkspaceEntriesListBox.SelectedItem, entry))
        {
            _workspaceRenameCandidate = entry;
        }
    }

    private void WorkspaceEntriesListBox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _workspaceDragAnchor is not { IsSafe: true } anchor)
        {
            return;
        }

        var current = e.GetPosition(WorkspaceEntriesListBox);
        if (Math.Abs(current.X - _workspaceDragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(current.Y - _workspaceDragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var entries = GetSelectedWorkspaceEntries();
        if (!entries.Contains(anchor))
        {
            entries = [anchor];
        }

        _workspaceDragAnchor = null;
        _workspaceRenameCandidate = null;
        try
        {
            var sourcePaths = entries
                .Select(entry => _workspaceFileManager.GetExportPath(entry.RelativePath))
                .ToArray();
            var data = new DataObject(DataFormats.FileDrop, sourcePaths);
            data.SetData(WorkspaceDragDataFormat, true);
            data.SetData(
                "Preferred DropEffect",
                new MemoryStream(BitConverter.GetBytes((int)DragDropEffects.Copy)));
            DragDrop.DoDragDrop(
                WorkspaceEntriesListBox,
                data,
                DragDropEffects.Copy | DragDropEffects.Move);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            InstallationStatusText.Text = _dashboard.Text.WorkspaceOperationFailed(exception.Message);
        }
    }

    private void WorkspaceName_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var candidate = _workspaceRenameCandidate;
        _workspaceDragAnchor = null;
        _workspaceRenameCandidate = null;
        if (sender is not TextBlock { DataContext: WorkspaceEntryViewModel entry }
            || !ReferenceEquals(candidate, entry))
        {
            return;
        }

        e.Handled = true;
        BeginWorkspaceRename(entry);
    }

    private void WorkspaceFileList_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = GetWorkspaceDropPaths(e.Data) is not { Length: > 0 }
            ? DragDropEffects.None
            : e.Data.GetDataPresent(WorkspaceDragDataFormat)
                ? DragDropEffects.Move
                : DragDropEffects.Copy;
        e.Handled = true;
    }

    private void WorkspaceFileList_Drop(object sender, DragEventArgs e)
    {
        var sourcePaths = GetWorkspaceDropPaths(e.Data);
        if (sourcePaths is not { Length: > 0 })
        {
            return;
        }

        var destinationDirectory = _workspaceDirectory;
        if (e.OriginalSource is DependencyObject source
            && ItemsControl.ContainerFromElement(WorkspaceEntriesListBox, source) is ListBoxItem
            {
                DataContext: WorkspaceEntryViewModel { IsDirectory: true, IsSafe: true } directory
            })
        {
            destinationDirectory = directory.RelativePath;
        }

        e.Handled = true;
        if (e.Data.GetDataPresent(WorkspaceDragDataFormat))
        {
            MoveDroppedWorkspaceEntries(sourcePaths, destinationDirectory);
            e.Effects = DragDropEffects.Move;
            return;
        }

        var importedCount = 0;
        var resolveConflict = CreateWorkspaceConflictResolver();
        RunWorkspaceOperation(
            () => importedCount = _workspaceFileManager.Import(
                sourcePaths,
                destinationDirectory,
                _dashboard.Text.WorkspaceCopyNameSuffix,
                resolveConflict),
            () => _dashboard.Text.WorkspaceItemsImported(importedCount));
        e.Effects = DragDropEffects.Copy;
    }

    private void MoveDroppedWorkspaceEntries(IReadOnlyList<string> sourcePaths, string destinationDirectory)
    {
        var relativePaths = new List<string>(sourcePaths.Count);
        foreach (var sourcePath in sourcePaths)
        {
            if (!_workspaceFileManager.TryGetRelativePath(sourcePath, out var relativePath))
            {
                InstallationStatusText.Text = _dashboard.Text.WorkspaceOperationFailed(
                    _dashboard.Text.WorkspaceDraggedItemUnavailable);
                return;
            }

            if (!IsWorkspaceDropNoOp(relativePath, destinationDirectory))
            {
                relativePaths.Add(relativePath);
            }
        }

        if (relativePaths.Count == 0)
        {
            InstallationStatusText.Text = string.Empty;
            return;
        }

        var movedCount = 0;
        var resolveConflict = CreateWorkspaceConflictResolver();
        RunWorkspaceOperation(
            () =>
            {
                foreach (var relativePath in relativePaths)
                {
                    if (_workspaceFileManager.Move(
                        relativePath,
                        destinationDirectory,
                        _dashboard.Text.WorkspaceCopyNameSuffix,
                        resolveConflict))
                    {
                        movedCount++;
                    }
                }
            },
            () => _dashboard.Text.WorkspaceItemsMoved(movedCount, relativePaths.Count));
    }

    private static bool IsWorkspaceDropNoOp(string sourceRelativePath, string destinationRelativeDirectory)
    {
        var normalizedSource = sourceRelativePath.Replace('\\', '/').Trim('/');
        var normalizedDestination = destinationRelativeDirectory.Replace('\\', '/').Trim('/');
        var separatorIndex = normalizedSource.LastIndexOf('/');
        var sourceParent = separatorIndex < 0 ? string.Empty : normalizedSource[..separatorIndex];
        return string.Equals(normalizedSource, normalizedDestination, StringComparison.OrdinalIgnoreCase)
            || string.Equals(sourceParent, normalizedDestination, StringComparison.OrdinalIgnoreCase);
    }

    private static string[]? GetWorkspaceDropPaths(IDataObject data) =>
        data.GetDataPresent(DataFormats.FileDrop)
            ? data.GetData(DataFormats.FileDrop) as string[]
            : null;

    private async void WorkspaceEntriesListBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.OriginalSource is TextBox)
        {
            return;
        }

        var controlPressed = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        if (controlPressed && e.Key == Key.A)
        {
            e.Handled = true;
            WorkspaceEntriesListBox.SelectAll();
            return;
        }

        if (controlPressed && e.Key == Key.V)
        {
            e.Handled = true;
            PasteWorkspaceClipboard();
            return;
        }

        var entries = GetSelectedWorkspaceEntries();
        if (entries.Count == 0)
        {
            return;
        }

        if (controlPressed && e.Key is Key.C or Key.X)
        {
            e.Handled = true;
            SetWorkspaceClipboard(entries, isCut: e.Key == Key.X);
            return;
        }

        var entry = entries[0];

        switch (e.Key)
        {
            case Key.F2 when entries.Count == 1 && entry.IsSafe:
                e.Handled = true;
                BeginWorkspaceRename(entry);
                break;
            case Key.Enter when entries.Count == 1 && entry.IsSafe:
                e.Handled = true;
                await OpenWorkspaceEntryAsync(entry);
                break;
            case Key.Delete when entries.All(candidate => candidate.IsSafe):
                e.Handled = true;
                DeleteWorkspaceEntries(entries);
                break;
        }
    }

    private void WorkspaceName_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TextBlock { DataContext: WorkspaceEntryViewModel entry })
        {
            return;
        }

        if (!WorkspaceEntriesListBox.SelectedItems.Contains(entry))
        {
            WorkspaceEntriesListBox.UnselectAll();
            WorkspaceEntriesListBox.SelectedItem = entry;
        }
    }

    private void WorkspaceBackgroundContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        WorkspaceContextPasteMenuItem.Header = _dashboard.Text.Paste;
        WorkspaceContextPasteMenuItem.IsEnabled = CanPasteWorkspaceClipboard();
        WorkspaceContextNewFileMenuItem.Header = _dashboard.Text.NewFile;
        WorkspaceContextNewFolderMenuItem.Header = _dashboard.Text.NewFolder;
    }

    private void WorkspaceItemContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu contextMenu
            || contextMenu.PlacementTarget is not FrameworkElement { DataContext: WorkspaceEntryViewModel entry })
        {
            return;
        }

        contextMenu.DataContext = entry;
        var entries = GetSelectedWorkspaceEntries();
        var hasSafeSelection = entries.Count > 0 && entries.All(candidate => candidate.IsSafe);
        var hasSingleSafeSelection = entries.Count == 1 && hasSafeSelection;
        foreach (var menuItem in contextMenu.Items.OfType<MenuItem>())
        {
            menuItem.Header = menuItem.Tag switch
            {
                "Open" => _dashboard.Text.Open,
                "Copy" => _dashboard.Text.Copy,
                "Cut" => _dashboard.Text.Cut,
                "Rename" => _dashboard.Text.Rename,
                "Delete" => _dashboard.Text.Delete,
                _ => string.Empty
            };
            menuItem.IsEnabled = menuItem.Tag is "Open" or "Rename"
                ? hasSingleSafeSelection
                : hasSafeSelection;
        }
    }

    private async void WorkspaceContextOpen_Click(object sender, RoutedEventArgs e)
    {
        var entries = GetSelectedWorkspaceEntries();
        if (entries is [{ IsSafe: true } entry])
        {
            await OpenWorkspaceEntryAsync(entry);
        }
    }

    private void WorkspaceContextRename_Click(object sender, RoutedEventArgs e)
    {
        var entries = GetSelectedWorkspaceEntries();
        if (entries is [{ IsSafe: true } entry])
        {
            BeginWorkspaceRename(entry);
        }
    }

    private void WorkspaceContextCopy_Click(object sender, RoutedEventArgs e)
    {
        var entries = GetSelectedWorkspaceEntries();
        if (entries.Count > 0 && entries.All(entry => entry.IsSafe))
        {
            SetWorkspaceClipboard(entries, isCut: false);
        }
    }

    private void WorkspaceContextCut_Click(object sender, RoutedEventArgs e)
    {
        var entries = GetSelectedWorkspaceEntries();
        if (entries.Count > 0 && entries.All(entry => entry.IsSafe))
        {
            SetWorkspaceClipboard(entries, isCut: true);
        }
    }

    private void WorkspaceContextPaste_Click(object sender, RoutedEventArgs e) => PasteWorkspaceClipboard();

    private void WorkspaceContextDelete_Click(object sender, RoutedEventArgs e)
    {
        var entries = GetSelectedWorkspaceEntries();
        if (entries.Count > 0 && entries.All(entry => entry.IsSafe))
        {
            DeleteWorkspaceEntries(entries);
        }
    }

    private IReadOnlyList<WorkspaceEntryViewModel> GetSelectedWorkspaceEntries() =>
        WorkspaceEntriesListBox.SelectedItems
            .OfType<WorkspaceEntryViewModel>()
            .ToArray();

    private void SetWorkspaceClipboard(IReadOnlyList<WorkspaceEntryViewModel> entries, bool isCut)
    {
        if (entries.Count == 0 || entries.Any(entry => !entry.IsSafe))
        {
            return;
        }

        _workspaceClipboard = new WorkspaceClipboardEntry(
            _projectContext.ActiveProject.Id,
            entries.Select(entry => new WorkspaceClipboardItem(entry.RelativePath)).ToArray(),
            isCut);
        InstallationStatusText.Text = entries.Count == 1
            ? isCut
                ? _dashboard.Text.WorkspaceItemCut(entries[0].Name)
                : _dashboard.Text.WorkspaceItemCopied(entries[0].Name)
            : isCut
                ? _dashboard.Text.WorkspaceItemsCut(entries.Count)
                : _dashboard.Text.WorkspaceItemsCopied(entries.Count);
    }

    private bool CanPasteWorkspaceClipboard() =>
        _workspaceClipboard is not null
        && string.Equals(
            _workspaceClipboard.ProjectId,
            _projectContext.ActiveProject.Id,
            StringComparison.OrdinalIgnoreCase);

    private void PasteWorkspaceClipboard()
    {
        if (_workspaceClipboard is not { } clipboard || !CanPasteWorkspaceClipboard())
        {
            if (_workspaceClipboard is not null)
            {
                InstallationStatusText.Text = _dashboard.Text.WorkspaceClipboardUnavailable;
            }

            return;
        }

        var transferredPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resolveConflict = CreateWorkspaceConflictResolver();
        Action operation = () =>
        {
            foreach (var item in clipboard.Items)
            {
                var transferred = clipboard.IsCut
                    ? _workspaceFileManager.Move(
                        item.RelativePath,
                        _workspaceDirectory,
                        _dashboard.Text.WorkspaceCopyNameSuffix,
                        resolveConflict)
                    : _workspaceFileManager.Copy(
                        item.RelativePath,
                        _workspaceDirectory,
                        _dashboard.Text.WorkspaceCopyNameSuffix,
                        resolveConflict);
                if (transferred)
                {
                    transferredPaths.Add(item.RelativePath);
                }
            }
        };
        RunWorkspaceOperation(
            operation,
            () => clipboard.Items.Count == 1
                ? transferredPaths.Count == 1
                    ? _dashboard.Text.WorkspacePasteCompleted
                    : _dashboard.Text.WorkspaceTransferSkipped
                : _dashboard.Text.WorkspaceItemsPasteCompleted(transferredPaths.Count, clipboard.Items.Count),
            clipboard.IsCut
                ? () =>
                {
                    if (!ReferenceEquals(_workspaceClipboard, clipboard))
                    {
                        return;
                    }

                    var remaining = clipboard.Items
                        .Where(item => !transferredPaths.Contains(item.RelativePath))
                        .ToArray();
                    _workspaceClipboard = remaining.Length == 0
                        ? null
                        : clipboard with { Items = remaining };
                }
        : null);
    }

    private Func<WorkspaceConflict, WorkspaceConflictDecision> CreateWorkspaceConflictResolver()
    {
        WorkspaceConflictDecision? applyToRemaining = null;
        return conflict =>
        {
            if (applyToRemaining is not null)
            {
                return applyToRemaining;
            }

            var decision = Dispatcher.Invoke(() => FileConflictDialog.Show(
                this,
                _dashboard.Text.WorkspaceConflictTitle,
                _dashboard.Text.WorkspaceConflictMessage(conflict),
                _dashboard.Text.Overwrite,
                _dashboard.Text.RenameCopy,
                _dashboard.Text.Skip,
                _dashboard.Text.ApplyToRemainingConflicts));
            if (decision.ApplyToRemaining && decision.Action != WorkspaceConflictAction.Cancel)
            {
                applyToRemaining = decision with { ApplyToRemaining = false };
            }

            return decision;
        };
    }

    private void BeginWorkspaceRename(WorkspaceEntryViewModel entry)
    {
        foreach (var candidate in _dashboard.WorkspaceEntries.Where(candidate => candidate.IsRenaming && !ReferenceEquals(candidate, entry)))
        {
            candidate.EditName = candidate.Name;
            candidate.IsRenaming = false;
        }

        WorkspaceEntriesListBox.SelectedItem = entry;
        entry.EditName = entry.Name;
        entry.IsRenaming = true;
    }

    private void WorkspaceRenameTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not TextBox textBox || e.NewValue is not true)
        {
            return;
        }

        Dispatcher.BeginInvoke(
            DispatcherPriority.Input,
            () =>
            {
                textBox.Focus();
                textBox.SelectAll();
            });
    }

    private void WorkspaceRenameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox { DataContext: WorkspaceEntryViewModel entry })
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            CommitWorkspaceRename(entry);
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            CancelWorkspaceRename(entry);
            WorkspaceEntriesListBox.Focus();
        }
    }

    private void WorkspaceRenameTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox { DataContext: WorkspaceEntryViewModel entry } && entry.IsRenaming)
        {
            CommitWorkspaceRename(entry);
        }
    }

    private void CommitWorkspaceRename(WorkspaceEntryViewModel entry)
    {
        if (!entry.IsRenaming)
        {
            return;
        }

        var newName = entry.EditName.Trim();
        entry.IsRenaming = false;
        if (newName.Length == 0)
        {
            entry.EditName = entry.Name;
            InstallationStatusText.Text = _dashboard.Text.WorkspaceItemNameRequired;
            return;
        }

        if (string.Equals(newName, entry.Name, StringComparison.Ordinal))
        {
            return;
        }

        RunWorkspaceOperation(() => _workspaceFileManager.Rename(entry.RelativePath, newName));
    }

    private static void CancelWorkspaceRename(WorkspaceEntryViewModel entry)
    {
        entry.EditName = entry.Name;
        entry.IsRenaming = false;
    }

    private void DeleteWorkspaceEntries(IReadOnlyList<WorkspaceEntryViewModel> entries)
    {
        if (entries.Count == 0)
        {
            return;
        }

        var confirmed = ConfirmationDialog.Show(
            this,
            _dashboard.Text.DeleteItemTitle,
            entries.Count == 1
                ? _dashboard.Text.DeleteItemQuestion(entries[0].Name)
                : _dashboard.Text.DeleteItemsQuestion(entries.Count),
            _dashboard.Text.Delete,
            _dashboard.Text.Cancel);
        if (confirmed)
        {
            RunWorkspaceOperation(() =>
            {
                foreach (var entry in entries)
                {
                    _workspaceFileManager.Delete(entry.RelativePath);
                }
            });
        }
    }

    private async void RunWorkspaceOperation(
        Action operation,
        Func<string>? successMessage = null,
        Action? onFinished = null)
    {
        try
        {
            await Task.Run(operation, _applicationLifetime.Token);
            RefreshWorkspaceFiles();
            InstallationStatusText.Text = successMessage?.Invoke() ?? string.Empty;
        }
        catch (OperationCanceledException)
        {
            RefreshWorkspaceFiles();
            InstallationStatusText.Text = _dashboard.Text.OperationCanceled;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            RefreshWorkspaceFiles();
            InstallationStatusText.Text = _dashboard.Text.WorkspaceOperationFailed(exception.Message);
        }
        finally
        {
            onFinished?.Invoke();
        }
    }

    private async Task OpenWorkspaceEntryAsync(WorkspaceEntryViewModel entry)
    {
        if (entry.IsDirectory)
        {
            _workspaceHistory.Push(_workspaceDirectory);
            _workspaceDirectory = entry.RelativePath;
            _workspacePageNumber = 1;
            RefreshWorkspaceFiles();
            return;
        }

        await OpenPortableFileAsync(
            Path.Combine(
                _workspaceFileManager.RootRelativePath,
                entry.RelativePath.Replace('/', Path.DirectorySeparatorChar)),
            _workspaceFileManager.RootRelativePath,
            PortableFileLaunchIntent.Open);
    }

    private async Task OpenPortableFileAsync(
        string relativeFilePath,
        string allowedRootRelativePath,
        PortableFileLaunchIntent intent,
        string? initialContent = null)
    {
        var result = await _fileLauncher.LaunchAsync(
            relativeFilePath,
            allowedRootRelativePath,
            intent,
            _dashboard.Text.CurrentLanguage,
            initialContent,
            _applicationLifetime.Token);
        InstallationStatusText.Text = result.Detail;
    }

    private string? PromptForWorkspaceName(string title, string prompt, string initialValue = "")
    {
        var dialog = new NamePromptDialog(
            this,
            title,
            prompt,
            _dashboard.Text.Confirm,
            _dashboard.Text.Cancel,
            _dashboard.Text.WorkspaceItemNameRequired,
            initialValue);
        return dialog.ShowDialog() == true ? dialog.ItemName : null;
    }

    private async void RefreshWorkspaceFiles()
    {
        _workspaceRefreshCancellation?.Cancel();
        _workspaceRefreshCancellation?.Dispose();
        _workspaceRefreshCancellation = CancellationTokenSource.CreateLinkedTokenSource(_applicationLifetime.Token);
        var cancellationToken = _workspaceRefreshCancellation.Token;
        try
        {
            var request = new WorkspacePageRequest(
                _workspaceDirectory,
                _workspacePageNumber,
                _workspacePageSize,
                _workspaceSortColumn,
                _workspaceSortDirection);
            var page = await Task.Run(() => _workspaceFileManager.ListPage(request), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            _workspacePageNumber = page.PageNumber;
            _dashboard.SetWorkspacePage(page);
            WorkspacePathTextBox.Text = DisplayTerminalPath(_workspaceDirectory);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            _workspaceDirectory = string.Empty;
            _workspaceHistory.Clear();
            _workspacePageNumber = 1;
            _dashboard.SetWorkspacePage(new WorkspacePage([], 1, _workspacePageSize, 0));
            WorkspacePathTextBox.Text = $"{_projectContext.ActiveProject.Id}:/";
            InstallationStatusText.Text = _dashboard.Text.WorkspaceOperationFailed(exception.Message);
        }
    }

    private async void RefreshComposerPackages_Click(object sender, RoutedEventArgs e) =>
        await RefreshPackageManagerAsync(_composerPackageManager, _dashboard.Composer);

    private async void RefreshNodePackages_Click(object sender, RoutedEventArgs e) =>
        await RefreshPackageManagerAsync(_nodePackageManager, _dashboard.Node);

    private async void RefreshPythonPackages_Click(object sender, RoutedEventArgs e) =>
        await RefreshPackageManagerAsync(_pythonPackageManager, _dashboard.Python);

    private async Task RefreshPackageManagerAsync(
        IProjectPackageManagerService service,
        PackageManagerPageViewModel page)
    {
        if (page.IsBusy)
        {
            return;
        }

        page.SetRuntime(service.GetRuntime());
        if (!page.RuntimeReady)
        {
            SetPackageStatus(page, page.RuntimeDetail);
            return;
        }

        page.SetBusy(true);
        var progress = CreatePackageProgress(page);
        SetPackageStatus(page, _dashboard.Text.LoadingPackages);
        _dashboard.GlobalOperation.Begin(_dashboard.Text.LoadingPackages);
        try
        {
            var packages = await Task.Run(
                () => service.ListPackagesAsync(_applicationLifetime.Token, progress),
                _applicationLifetime.Token);
            page.SetPackages(packages);
            SetPackageStatus(page, page.ProjectRelativePath);
            page.SetOperationResult(
                _dashboard.Text.PackageOperationProgress(new(
                    ProjectPackageOperationKind.Refresh,
                    ProjectPackageOperationPhase.Completed,
                    IsIndeterminate: false,
                    Percentage: 100)),
                isSuccess: true);
        }
        catch (OperationCanceledException)
        {
            SetPackageStatus(page, _dashboard.Text.OperationCanceled);
            page.SetOperationResult(_dashboard.Text.OperationCanceled, isSuccess: false);
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException or UnauthorizedAccessException)
        {
            var status = _dashboard.Text.PackageListFailed(exception.Message);
            SetPackageStatus(page, status);
            page.SetOperationResult(status, isSuccess: false);
        }
        finally
        {
            page.SetBusy(false);
            _dashboard.GlobalOperation.End();
        }
    }

    private async void InstallComposerPackage_Click(object sender, RoutedEventArgs e) =>
        await InstallPackageAsync(
            _composerPackageManager,
            _dashboard.Composer,
            ComposerPackageNameTextBox,
            ComposerVersionConstraintTextBox);

    private async void InstallNodePackage_Click(object sender, RoutedEventArgs e) =>
        await InstallPackageAsync(
            _nodePackageManager,
            _dashboard.Node,
            NodePackageNameTextBox,
            NodeVersionConstraintTextBox);

    private async void InstallPythonPackage_Click(object sender, RoutedEventArgs e) =>
        await InstallPackageAsync(
            _pythonPackageManager,
            _dashboard.Python,
            PythonPackageNameTextBox,
            PythonVersionConstraintTextBox);

    private async Task InstallPackageAsync(
        IProjectPackageManagerService service,
        PackageManagerPageViewModel page,
        TextBox packageNameTextBox,
        TextBox versionConstraintTextBox)
    {
        if (!page.CanOperate)
        {
            return;
        }

        var packageName = packageNameTextBox.Text.Trim();
        var initialProgress = new ProjectPackageOperationProgress(
            ProjectPackageOperationKind.Install,
            ProjectPackageOperationPhase.Preparing,
            packageName);
        var initialStatus = _dashboard.Text.PackageOperationProgress(initialProgress);
        var initialDetail = _dashboard.Text.PackageOperationDetail(initialProgress);
        page.SetBusy(true);
        var progress = CreatePackageProgress(page, packageName);
        page.SetOperationProgress(initialProgress, initialStatus, initialDetail);
        SetPackageStatus(page, initialStatus);
        _dashboard.GlobalOperation.Begin(initialStatus, detail: initialDetail);
        try
        {
            var versionConstraint = versionConstraintTextBox.Text.Trim();
            var result = await Task.Run(
                () => service.InstallPackageAsync(
                    packageName,
                    versionConstraint,
                    _applicationLifetime.Token,
                    progress),
                _applicationLifetime.Token);
            if (!result.IsSuccess)
            {
                var failure = _dashboard.Text.PackageOperationFailed(result.Detail);
                SetPackageStatus(page, failure);
                page.SetOperationResult(failure, isSuccess: false);
                return;
            }

            var packages = await Task.Run(
                () => service.ListPackagesAsync(_applicationLifetime.Token, progress),
                _applicationLifetime.Token);
            page.SetPackages(packages);
            packageNameTextBox.Clear();
            versionConstraintTextBox.Clear();
            var success = _dashboard.Text.PackageOperationSucceeded(packageName, result.Outcome);
            SetPackageStatus(page, success);
            page.SetOperationResult(success, isSuccess: true);
        }
        catch (OperationCanceledException)
        {
            SetPackageStatus(page, _dashboard.Text.OperationCanceled);
            page.SetOperationResult(_dashboard.Text.OperationCanceled, isSuccess: false);
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException or UnauthorizedAccessException)
        {
            var failure = _dashboard.Text.PackageOperationFailed(exception.Message);
            SetPackageStatus(page, failure);
            page.SetOperationResult(failure, isSuccess: false);
        }
        finally
        {
            page.SetBusy(false);
            _dashboard.GlobalOperation.End();
        }
    }

    private async void RemoveComposerPackage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string packageName })
        {
            await RemovePackageAsync(_composerPackageManager, _dashboard.Composer, packageName);
        }
    }

    private async void RemoveNodePackage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string packageName })
        {
            await RemovePackageAsync(_nodePackageManager, _dashboard.Node, packageName);
        }
    }

    private async void RemovePythonPackage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string packageName })
        {
            await RemovePackageAsync(_pythonPackageManager, _dashboard.Python, packageName);
        }
    }

    private async Task RemovePackageAsync(
        IProjectPackageManagerService service,
        PackageManagerPageViewModel page,
        string packageName)
    {
        if (!page.CanOperate)
        {
            return;
        }

        var confirmed = ConfirmationDialog.Show(
            this,
            _dashboard.Text.RemovePackageTitle,
            _dashboard.Text.RemovePackageQuestion(packageName),
            _dashboard.Text.RemovePackage,
            _dashboard.Text.Cancel);
        if (!confirmed)
        {
            return;
        }

        var initialProgress = new ProjectPackageOperationProgress(
            ProjectPackageOperationKind.Remove,
            ProjectPackageOperationPhase.Preparing,
            packageName);
        var initialStatus = _dashboard.Text.PackageOperationProgress(initialProgress);
        var initialDetail = _dashboard.Text.PackageOperationDetail(initialProgress);
        page.SetBusy(true);
        var progress = CreatePackageProgress(page, packageName);
        page.SetOperationProgress(initialProgress, initialStatus, initialDetail);
        SetPackageStatus(page, initialStatus);
        _dashboard.GlobalOperation.Begin(initialStatus, detail: initialDetail);
        try
        {
            var result = await Task.Run(
                () => service.RemovePackageAsync(packageName, _applicationLifetime.Token, progress),
                _applicationLifetime.Token);
            if (!result.IsSuccess)
            {
                var failure = _dashboard.Text.PackageOperationFailed(result.Detail);
                SetPackageStatus(page, failure);
                page.SetOperationResult(failure, isSuccess: false);
                return;
            }

            var packages = await Task.Run(
                () => service.ListPackagesAsync(_applicationLifetime.Token, progress),
                _applicationLifetime.Token);
            page.SetPackages(packages);
            var success = _dashboard.Text.PackageRemoved(packageName);
            SetPackageStatus(page, success);
            page.SetOperationResult(success, isSuccess: true);
        }
        catch (OperationCanceledException)
        {
            SetPackageStatus(page, _dashboard.Text.OperationCanceled);
            page.SetOperationResult(_dashboard.Text.OperationCanceled, isSuccess: false);
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException or UnauthorizedAccessException)
        {
            var failure = _dashboard.Text.PackageOperationFailed(exception.Message);
            SetPackageStatus(page, failure);
            page.SetOperationResult(failure, isSuccess: false);
        }
        finally
        {
            page.SetBusy(false);
            _dashboard.GlobalOperation.End();
        }
    }

    private IProgress<ProjectPackageOperationProgress> CreatePackageProgress(
        PackageManagerPageViewModel page,
        string fallbackPackageName = "") =>
        new DispatcherProgress<ProjectPackageOperationProgress>(Dispatcher, progress =>
        {
            var status = _dashboard.Text.PackageOperationProgress(progress);
            var detail = _dashboard.Text.PackageOperationDetail(progress, fallbackPackageName);
            page.SetOperationProgress(progress, status, detail);
            _dashboard.GlobalOperation.Update(status, progress.IsIndeterminate, progress.Percentage, detail);
            SetPackageStatus(page, status);
        });

    private sealed class DispatcherProgress<T>(
        System.Windows.Threading.Dispatcher dispatcher,
        Action<T> handler) : IProgress<T>
    {
        public void Report(T value)
        {
            if (dispatcher.CheckAccess())
            {
                handler(value);
                return;
            }

            dispatcher.Invoke(() => handler(value));
        }
    }

    private sealed record WorkspaceClipboardEntry(
        string ProjectId,
        IReadOnlyList<WorkspaceClipboardItem> Items,
        bool IsCut);

    private sealed record WorkspaceClipboardItem(string RelativePath);

    private void SetPackageStatus(PackageManagerPageViewModel page, string status)
    {
        page.SetStatus(status);
    }
}
