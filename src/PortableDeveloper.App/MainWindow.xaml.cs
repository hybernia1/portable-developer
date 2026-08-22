using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using PortableDeveloper.App.ViewModels;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.ApachePhp;
using PortableDeveloper.Application.Settings;
using PortableDeveloper.Application.MariaDb;
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
    private readonly ISeleniumSettingsStore _seleniumSettingsStore;
    private readonly IProjectPackageManagerService _composerPackageManager;
    private readonly IProjectPackageManagerService _pythonPackageManager;
    private readonly IWebProjectCatalog _webProjects;
    private readonly IPortableEditorService _editorService;
    private readonly IPortableTerminalService _terminalService;
    private readonly IWorkspaceFileManager _workspaceFileManager;
    private readonly IPhpSettingsStore _phpSettingsStore;
    private readonly IPortSettingsStore _portSettingsStore;
    private readonly ITcpPortUsageScanner _portUsageScanner;
    private readonly IPortablePathResolver _paths;
    private MariaDbInstanceOptions _mariaDbOptions;
    private PortSettings _portSettings;
    private IReadOnlyList<TcpPortListenerInfo> _tcpListeners = [];
    private SeleniumServerOptions _seleniumOptions;
    private PhpSettings _phpSettings;
    private readonly CancellationTokenSource _applicationLifetime = new();
    private bool _closeAfterStoppingStack;
    private string _terminalWorkingDirectory = string.Empty;
    private string _workspaceDirectory = string.Empty;
    private readonly Stack<string> _workspaceHistory = new();
    private readonly List<string> _terminalHistory = [];
    private int _terminalHistoryIndex;
    private int _terminalInputStart;
    private bool _terminalBusy;
    private bool _changingWebProject;

    public MainWindow()
    {
        InitializeComponent();

        var app = (App)System.Windows.Application.Current;
        _paths = app.Paths;
        _logger = app.Logger;
        app.Paths.EnsureDirectory("modules");
        app.Paths.EnsureDirectory("instances");
        app.Paths.EnsureDirectory("state");
        var moduleInventory = new FileModuleInventory(app.Paths);
        var packageCatalog = new JsonModulePackageCatalog(app.Paths);
        var apacheRuntimePreflight = new ApacheRuntimePreflight(app.Paths);
        var phpRuntimePreflight = new PhpRuntimePreflight(app.Paths);
        var moduleVerifier = new ModuleInstallationVerifier(moduleInventory, packageCatalog, app.Paths);
        var commandRunner = new PortableCommandRunner(app.Paths, app.Logger);
        _phpSettingsStore = new JsonPhpSettingsStore(app.Paths);
        _phpSettings = _phpSettingsStore.Load();
        var toolInventory = new PortableToolRuntimeInventory(app.Paths);
        _webProjects = new JsonWebProjectCatalog(app.Paths);
        _editorService = new PortableEditorService(toolInventory, app.Paths, app.Logger);
        _terminalService = new PortableTerminalService(moduleVerifier, toolInventory, commandRunner, app.Paths, _webProjects);
        _workspaceFileManager = new WorkspaceFileManager(app.Paths, _webProjects);
        _composerPackageManager = new ComposerProjectPackageManager(
            toolInventory,
            moduleVerifier,
            commandRunner,
            app.Paths,
            _webProjects);
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
            _seleniumDriverInventory,
            new SeleniumConfigurationGenerator(app.Paths),
            _seleniumGrid,
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
            _mariaDbInitializer.GetState(_mariaDbOptions),
            _portSettings,
            new UiText(new JsonApplicationSettingsStore(app.Paths)));
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
        LanguageSelector.SelectedValue = _dashboard.Text.CurrentLanguage.ToString();
        var stackSnapshot = _apachePhpStack.GetSnapshot();
        _dashboard.SetStackStatus(stackSnapshot.State, stackSnapshot.Detail);
        _dashboard.SetSeleniumOptions(_seleniumOptions);
        RefreshPortUsage();
        var phpInstallation = moduleInventory.GetInstalled(PortableDeveloper.Domain.Modules.ModuleKind.Php).FirstOrDefault();
        var enabledPhpExtensions = _phpSettings.EnabledExtensions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        _dashboard.SetPhpExtensions(PhpExtensionCatalog.All.Select(extension => new PhpExtensionViewModel(
            extension.Name,
            extension.IsRequired,
            phpInstallation is not null && File.Exists(app.Paths.Resolve(Path.Combine(
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
        _dashboard.SetSeleniumDrivers(_seleniumDriverInventory.Scan());
        _dashboard.Composer.SetRuntime(_composerPackageManager.GetRuntime());
        RefreshWebProjectBindings();
        _dashboard.Python.SetRuntime(_pythonPackageManager.GetRuntime());
        _dashboard.SetEditorRuntime(_editorService.GetRuntime());
        var seleniumSnapshot = _seleniumServer.GetSnapshot();
        _dashboard.SetSeleniumStatus(seleniumSnapshot.State, seleniumSnapshot.Detail);
        PopulateSeleniumSettingsFields();
        PopulatePortSettingsFields();
        PopulatePhpSettingsFields(_phpSettings);
        ResetTerminalConsole();
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
        _dashboard.SetStackStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Stopping, "");
        try
        {
            await Task.WhenAll(
                _apachePhpStack.DisposeAsync().AsTask(),
                _mariaDbServer.DisposeAsync().AsTask(),
                _seleniumServer.DisposeAsync().AsTask());
        }
        finally
        {
            _closeAfterStoppingStack = true;
            Close();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
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
        RefreshWebProjectBindings();
        RefreshWorkspaceFiles();
        UpdatePortInputStatuses();
        InstallationStatusText.Text = _dashboard.Text.LanguageChanged;
        await _logger.LogAsync(
            ApplicationLogLevel.Information,
            "ui",
            "language.changed",
            $"language={language}");
    }

    private void NavigationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is not ListBox { SelectedItem: NavigationItemViewModel item })
        {
            return;
        }

        if (item.Page == NavigationPage.Ports)
        {
            RefreshPortUsage();
            PopulatePortSettingsFields();
        }

        InstallationStatusText.Text = item.Page switch
        {
            NavigationPage.Composer => _dashboard.Composer.Status,
            NavigationPage.Python => _dashboard.Python.Status,
            NavigationPage.Tools => _dashboard.EditorDetail,
            NavigationPage.Files => WorkspacePathText.Text,
            NavigationPage.Ports => _dashboard.PortSettingsAvailability,
            _ => InstallationStatusText.Text,
        };
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
            (TextBox: ApachePortTextBox, Status: ApachePortStatusText, CurrentPort: _portSettings.ApachePort, Owned: _dashboard.StackIsRunning),
            (TextBox: PhpFastCgiPortTextBox, Status: PhpPortStatusText, CurrentPort: _portSettings.PhpFastCgiPort, Owned: _dashboard.StackIsRunning),
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
            var wasRunning = _dashboard.StackIsRunning;
            await _logger.LogAsync(
                ApplicationLogLevel.Information,
                "php",
                "settings.saved",
                "Portable PHP settings were saved.");
            if (wasRunning)
            {
                var restarted = await RestartWebStackAsync(announce: false);
                if (!restarted)
                {
                    return;
                }
            }

            InstallationStatusText.Text = _dashboard.Text.PhpSettingsSaved(_dashboard.StackProcessState);
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

    private async void ToggleStack_Click(object sender, RoutedEventArgs e) => await ToggleStackAsync();

    private async void RestartWebStack_Click(object sender, RoutedEventArgs e) => await RestartWebStackAsync();

    private async Task ToggleStackAsync()
    {
        if (!_dashboard.StackActionEnabled)
        {
            return;
        }

        var shouldStop = _dashboard.StackProcessState == PortableDeveloper.Domain.Processes.ManagedProcessState.Running;
        _dashboard.SetStackStatus(
            shouldStop
                ? PortableDeveloper.Domain.Processes.ManagedProcessState.Stopping
                : PortableDeveloper.Domain.Processes.ManagedProcessState.Starting,
            "");
        try
        {
            var snapshot = shouldStop
                ? await _apachePhpStack.StopAsync()
                : await _apachePhpStack.StartAsync(CreateApachePhpOptions());
            _dashboard.SetStackStatus(snapshot.State, snapshot.Detail);
        }
        catch (Exception exception)
        {
            _dashboard.SetStackStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Failed, exception.Message);
        }
    }

    private async Task<bool> RestartWebStackAsync(bool announce = true)
    {
        if (!_dashboard.StackRestartEnabled)
        {
            return false;
        }

        InstallationStatusText.Text = _dashboard.Text.RestartingWebService;
        try
        {
            _dashboard.SetStackStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Stopping, string.Empty);
            var stopped = await _apachePhpStack.StopAsync(_applicationLifetime.Token);
            _dashboard.SetStackStatus(stopped.State, stopped.Detail);
            if (stopped.State == PortableDeveloper.Domain.Processes.ManagedProcessState.Failed)
            {
                return false;
            }

            _dashboard.SetStackStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Starting, string.Empty);
            var started = await _apachePhpStack.StartAsync(CreateApachePhpOptions(), _applicationLifetime.Token);
            _dashboard.SetStackStatus(started.State, started.Detail);
            if (started.State != PortableDeveloper.Domain.Processes.ManagedProcessState.Running)
            {
                return false;
            }

            if (announce)
            {
                InstallationStatusText.Text = _dashboard.Text.WebServiceRestarted;
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
            _dashboard.SetStackStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Failed, exception.Message);
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
        else if (sender is Button { Tag: "restart-web" })
        {
            await RestartWebStackAsync();
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainWindow_Loaded;
        await BootstrapMariaDbAsync();
        await RefreshPackageManagerAsync(_composerPackageManager, _dashboard.Composer);
        await RefreshPackageManagerAsync(_pythonPackageManager, _dashboard.Python);
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
        _dashboard.SetSeleniumOperationInProgress(true);
        _dashboard.SetSeleniumStatus(
            shouldStop
                ? PortableDeveloper.Domain.Processes.ManagedProcessState.Stopping
                : PortableDeveloper.Domain.Processes.ManagedProcessState.Starting,
            string.Empty);
        try
        {
            var snapshot = shouldStop
                ? await _seleniumServer.StopAsync(_applicationLifetime.Token)
                : await _seleniumServer.StartAsync(_seleniumOptions, _applicationLifetime.Token);
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
                SessionTimeoutSeconds = sessionTimeout
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
    }

    private void OpenSeleniumDriversFolder_Click(object sender, RoutedEventArgs e)
    {
        var folder = _paths.EnsureDirectory(Path.Combine(_seleniumDriverInventory.DriversRelativePath, "custom"));
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
    }

    private void ReloadSeleniumDrivers_Click(object sender, RoutedEventArgs e)
    {
        if (!_dashboard.SeleniumSettingsEnabled)
        {
            return;
        }

        _dashboard.SetSeleniumDrivers(_seleniumDriverInventory.Scan());
        InstallationStatusText.Text = _dashboard.SeleniumDriverCount;
    }

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

        var confirmed = MessageBox.Show(
            this,
            _dashboard.Text.TerminateSessionQuestion,
            _dashboard.Text.TerminateSessionTitle,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (confirmed != MessageBoxResult.Yes)
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

    private async void WebProjectSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_changingWebProject || sender is not ComboBox { SelectedValue: string projectId } ||
            string.Equals(projectId, _webProjects.ActiveProject.Id, StringComparison.OrdinalIgnoreCase))
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

    private async Task SelectWebProjectAsync(string projectId)
    {
        if (_changingWebProject || string.Equals(projectId, _webProjects.ActiveProject.Id, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!CanChangeWebProject())
        {
            RefreshWebProjectBindings();
            InstallationStatusText.Text = _dashboard.Text.ProjectChangeBusy;
            return;
        }

        _changingWebProject = true;
        try
        {
            _webProjects.SetActive(projectId);
            RefreshWebProjectBindings();
            _workspaceDirectory = string.Empty;
            _workspaceHistory.Clear();
            _terminalWorkingDirectory = _terminalService.InitialWorkingDirectory;
            ResetTerminalConsole();
            RefreshWorkspaceFiles();
            await RefreshPackageManagerAsync(_composerPackageManager, _dashboard.Composer);
            InstallationStatusText.Text = _dashboard.Text.ProjectSelected(_dashboard.ActiveWebProjectName);
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "projects",
                "project.selected",
                $"project={_webProjects.ActiveProject.Id}");
        }
        catch (Exception exception) when (exception is IOException or ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectOperationFailed(exception.Message);
            RefreshWebProjectBindings();
        }
        finally
        {
            _changingWebProject = false;
        }
    }

    private async void CreateWebProject_Click(object sender, RoutedEventArgs e)
    {
        if (!CanChangeWebProject())
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectChangeBusy;
            return;
        }

        try
        {
            var project = _webProjects.Create(ProjectNameTextBox.Text, ProjectWebRootTextBox.Text);
            ProjectNameTextBox.Clear();
            ProjectWebRootTextBox.Text = "public";
            RefreshWebProjectBindings();
            ResetProjectTools();
            await ApplyWebProjectConfigurationAsync(_dashboard.Text.ProjectCreated(project.Name));
            await RefreshPackageManagerAsync(_composerPackageManager, _dashboard.Composer);
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "projects",
                "project.created",
                $"project={project.Id}; webRoot={project.WebRootRelativePath}");
        }
        catch (Exception exception) when (exception is IOException or ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectOperationFailed(exception.Message);
        }
    }

    private async void ToggleWebProjectHtaccess_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string projectId })
        {
            return;
        }

        try
        {
            var project = _webProjects.Projects.First(item => item.Id == projectId);
            _webProjects.SetHtaccess(projectId, !project.AllowHtaccess);
            RefreshWebProjectBindings();
            await ApplyWebProjectConfigurationAsync(_dashboard.Text.ApacheConfigurationSaved);
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "projects",
                "project.htaccess.changed",
                $"project={projectId}; allow={!project.AllowHtaccess}");
        }
        catch (Exception exception) when (exception is IOException or ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectOperationFailed(exception.Message);
        }
    }

    private async void ToggleWebProjectEnabled_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string projectId })
        {
            return;
        }

        try
        {
            var project = _webProjects.Projects.First(item => item.Id == projectId);
            _webProjects.SetEnabled(projectId, !project.IsEnabled);
            RefreshWebProjectBindings();
            await ApplyWebProjectConfigurationAsync(_dashboard.Text.ApacheConfigurationSaved);
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "projects",
                "project.apache.changed",
                $"project={projectId}; enabled={!project.IsEnabled}");
        }
        catch (Exception exception) when (exception is IOException or ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectOperationFailed(exception.Message);
        }
    }

    private async void RemoveWebProject_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string projectId })
        {
            return;
        }

        var project = _webProjects.Projects.First(item => item.Id == projectId);
        if (!CanChangeWebProject())
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectChangeBusy;
            return;
        }

        if (MessageBox.Show(
                this,
                _dashboard.Text.RemoveProjectQuestion(project.Name),
                _dashboard.Text.RemoveProject,
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning,
                MessageBoxResult.No) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            _webProjects.Remove(projectId);
            RefreshWebProjectBindings();
            ResetProjectTools();
            await ApplyWebProjectConfigurationAsync(_dashboard.Text.ProjectRemoved(project.Name));
            await RefreshPackageManagerAsync(_composerPackageManager, _dashboard.Composer);
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "projects",
                "project.removed",
                $"project={projectId}; filesPreserved=true");
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectOperationFailed(exception.Message);
        }
    }

    private void OpenWebProject_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string projectId })
        {
            var project = _webProjects.Projects.First(item => item.Id == projectId);
            OpenProjectDirectory(project.ProjectRootRelativePath);
        }
    }

    private void OpenWebProjectUrl_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string projectId })
        {
            var project = _webProjects.Projects.First(item => item.Id == projectId);
            Process.Start(new ProcessStartInfo($"http://{project.HostName}:{_portSettings.ApachePort}/") { UseShellExecute = true });
        }
    }

    private async Task ApplyWebProjectConfigurationAsync(string message)
    {
        if (_dashboard.StackIsRunning)
        {
            if (!await RestartWebStackAsync(announce: false))
            {
                return;
            }
        }

        InstallationStatusText.Text = message;
    }

    private void ResetProjectTools()
    {
        _workspaceDirectory = string.Empty;
        _workspaceHistory.Clear();
        _terminalWorkingDirectory = _terminalService.InitialWorkingDirectory;
        RefreshWorkspaceFiles();
        ResetTerminalConsole();
    }

    private void RefreshWebProjectBindings()
    {
        _dashboard.SetWebProjects(_webProjects.Projects, _webProjects.ActiveProject.Id);
        _dashboard.Composer.SetProjectRelativePath(_composerPackageManager.ProjectRelativePath);
    }

    private bool CanChangeWebProject() => !_dashboard.Composer.IsBusy && !_terminalBusy;

    private void OpenPythonProject_Click(object sender, RoutedEventArgs e) =>
        OpenProjectDirectory(_pythonPackageManager.ProjectRelativePath, _dashboard.Python);

    private async void StartEditor_Click(object sender, RoutedEventArgs e) =>
        await OpenEditorAsync();

    private async void EditCustomPhpIni_Click(object sender, RoutedEventArgs e) =>
        await OpenEditorAsync(PhpCustomIni.GetRelativePath("default"), PhpCustomIni.InitialContent);

    private async Task OpenEditorAsync(string? relativeFilePath = null, string? initialContent = null)
    {
        _dashboard.SetEditorRuntime(_editorService.GetRuntime());
        if (!_dashboard.EditorReady)
        {
            InstallationStatusText.Text = _dashboard.Text.EditorStartFailed(_dashboard.EditorDetail);
            return;
        }

        var result = await _editorService.OpenAsync(
            _dashboard.Text.CurrentLanguage,
            relativeFilePath,
            initialContent,
            _applicationLifetime.Token);
        InstallationStatusText.Text = result.IsSuccess
            ? _dashboard.Text.EditorStarted
            : _dashboard.Text.EditorStartFailed(result.Detail);
    }

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
        if (_terminalBusy)
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
            NavigateTerminalHistory(e.Key == Key.Up ? -1 : 1);
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
        if (_terminalBusy)
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
            _terminalBusy = false;
            TerminalConsoleTextBox.IsReadOnly = false;
            WriteTerminalPrompt();
            TerminalConsoleTextBox.Focus();
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
            case PortableServiceTarget.Web when (_dashboard.StackProcessState == PortableDeveloper.Domain.Processes.ManagedProcessState.Running) != shouldRun:
                await ToggleStackAsync();
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
        $"web: {_dashboard.Text.StackStatus(_dashboard.StackProcessState)}",
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
        const int maximumCharacters = 100_000;
        if (next.Length > maximumCharacters)
        {
            var removed = next.Length - maximumCharacters;
            next = next[removed..];
            _terminalInputStart = Math.Max(0, _terminalInputStart - removed);
        }

        TerminalConsoleTextBox.Text = next;
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
            ? $"{_webProjects.ActiveProject.Id}:/"
            : $"{_webProjects.ActiveProject.Id}:/{relativePath}";

    private void RefreshWorkspace_Click(object sender, RoutedEventArgs e) => RefreshWorkspaceFiles();

    private void WorkspaceBack_Click(object sender, RoutedEventArgs e)
    {
        if (_workspaceHistory.Count == 0)
        {
            return;
        }

        _workspaceDirectory = _workspaceHistory.Pop();
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

    private async void OpenWorkspaceItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: WorkspaceEntryViewModel entry } || !entry.IsSafe)
        {
            return;
        }

        await OpenWorkspaceEntryAsync(entry);
    }

    private async void WorkspaceEntry_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount != 2 || sender is not Border { Tag: WorkspaceEntryViewModel entry } || !entry.IsSafe)
        {
            return;
        }

        e.Handled = true;
        await OpenWorkspaceEntryAsync(entry);
    }

    private void RenameWorkspaceItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: WorkspaceEntryViewModel entry } || !entry.IsSafe)
        {
            return;
        }

        var name = PromptForWorkspaceName(
            _dashboard.Text.RenameItemTitle,
            _dashboard.Text.EnterNewName,
            entry.Name);
        if (name is null)
        {
            return;
        }

        RunWorkspaceOperation(() => _workspaceFileManager.Rename(entry.RelativePath, name));
    }

    private void DeleteWorkspaceItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: WorkspaceEntryViewModel entry } || !entry.IsSafe)
        {
            return;
        }

        var confirmed = MessageBox.Show(
            this,
            _dashboard.Text.DeleteItemQuestion(entry.Name),
            _dashboard.Text.DeleteItemTitle,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (confirmed == MessageBoxResult.Yes)
        {
            RunWorkspaceOperation(() => _workspaceFileManager.Delete(entry.RelativePath));
        }
    }

    private void RunWorkspaceOperation(Action operation)
    {
        try
        {
            operation();
            RefreshWorkspaceFiles();
            InstallationStatusText.Text = string.Empty;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            InstallationStatusText.Text = _dashboard.Text.WorkspaceOperationFailed(exception.Message);
        }
    }

    private async Task OpenWorkspaceEntryAsync(WorkspaceEntryViewModel entry)
    {
        if (entry.IsDirectory)
        {
            _workspaceHistory.Push(_workspaceDirectory);
            _workspaceDirectory = entry.RelativePath;
            RefreshWorkspaceFiles();
            return;
        }

        await OpenEditorAsync(Path.Combine(
            _workspaceFileManager.RootRelativePath,
            entry.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
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

    private void RefreshWorkspaceFiles()
    {
        try
        {
            _dashboard.SetWorkspaceEntries(_workspaceFileManager.List(_workspaceDirectory));
            WorkspacePathText.Text = DisplayTerminalPath(_workspaceDirectory);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            _workspaceDirectory = string.Empty;
            _workspaceHistory.Clear();
            _dashboard.SetWorkspaceEntries([]);
            WorkspacePathText.Text = $"{_webProjects.ActiveProject.Id}:/";
            InstallationStatusText.Text = _dashboard.Text.WorkspaceOperationFailed(exception.Message);
        }
    }

    private async void RefreshComposerPackages_Click(object sender, RoutedEventArgs e) =>
        await RefreshPackageManagerAsync(_composerPackageManager, _dashboard.Composer);

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
        SetPackageStatus(page, _dashboard.Text.LoadingPackages);
        try
        {
            page.SetPackages(await service.ListPackagesAsync(_applicationLifetime.Token));
            SetPackageStatus(page, page.ProjectRelativePath);
        }
        catch (OperationCanceledException)
        {
            SetPackageStatus(page, _dashboard.Text.OperationCanceled);
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException or UnauthorizedAccessException)
        {
            SetPackageStatus(page, _dashboard.Text.PackageListFailed(exception.Message));
        }
        finally
        {
            page.SetBusy(false);
        }
    }

    private async void InstallComposerPackage_Click(object sender, RoutedEventArgs e) =>
        await InstallPackageAsync(
            _composerPackageManager,
            _dashboard.Composer,
            ComposerPackageNameTextBox,
            ComposerVersionConstraintTextBox);

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
        page.SetBusy(true);
        SetPackageStatus(page, _dashboard.Text.InstallingPackage);
        try
        {
            var result = await service.InstallPackageAsync(
                packageName,
                versionConstraintTextBox.Text.Trim(),
                _applicationLifetime.Token);
            if (!result.IsSuccess)
            {
                SetPackageStatus(page, _dashboard.Text.PackageOperationFailed(result.Detail));
                return;
            }

            page.SetPackages(await service.ListPackagesAsync(_applicationLifetime.Token));
            packageNameTextBox.Clear();
            versionConstraintTextBox.Clear();
            SetPackageStatus(page, _dashboard.Text.PackageInstalled(packageName));
        }
        catch (OperationCanceledException)
        {
            SetPackageStatus(page, _dashboard.Text.OperationCanceled);
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException or UnauthorizedAccessException)
        {
            SetPackageStatus(page, _dashboard.Text.PackageOperationFailed(exception.Message));
        }
        finally
        {
            page.SetBusy(false);
        }
    }

    private async void RemoveComposerPackage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string packageName })
        {
            await RemovePackageAsync(_composerPackageManager, _dashboard.Composer, packageName);
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

        var confirmed = MessageBox.Show(
            this,
            _dashboard.Text.RemovePackageQuestion(packageName),
            _dashboard.Text.RemovePackageTitle,
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (confirmed != MessageBoxResult.Yes)
        {
            return;
        }

        page.SetBusy(true);
        SetPackageStatus(page, _dashboard.Text.RemovingPackage);
        try
        {
            var result = await service.RemovePackageAsync(packageName, _applicationLifetime.Token);
            if (!result.IsSuccess)
            {
                SetPackageStatus(page, _dashboard.Text.PackageOperationFailed(result.Detail));
                return;
            }

            page.SetPackages(await service.ListPackagesAsync(_applicationLifetime.Token));
            SetPackageStatus(page, _dashboard.Text.PackageRemoved(packageName));
        }
        catch (OperationCanceledException)
        {
            SetPackageStatus(page, _dashboard.Text.OperationCanceled);
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException or UnauthorizedAccessException)
        {
            SetPackageStatus(page, _dashboard.Text.PackageOperationFailed(exception.Message));
        }
        finally
        {
            page.SetBusy(false);
        }
    }

    private void SetPackageStatus(PackageManagerPageViewModel page, string status)
    {
        page.SetStatus(status);
        var selectedPage = ReferenceEquals(page, _dashboard.Composer)
            ? NavigationPage.Composer
            : NavigationPage.Python;
        if (_dashboard.SelectedPage == selectedPage)
        {
            InstallationStatusText.Text = status;
        }
    }
}
