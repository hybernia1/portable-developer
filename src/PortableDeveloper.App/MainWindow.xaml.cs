using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
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
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Application.Selenium;
using PortableDeveloper.Application.Workspace;
using PortableDeveloper.Infrastructure.Modules;
using PortableDeveloper.Infrastructure.Packages;
using PortableDeveloper.Infrastructure.Php;
using PortableDeveloper.Infrastructure.Processes;
using PortableDeveloper.Infrastructure.Settings;
using PortableDeveloper.Infrastructure.Health;
using PortableDeveloper.Infrastructure.ApachePhp;
using PortableDeveloper.Infrastructure.MariaDb;
using PortableDeveloper.Infrastructure.ProjectTools;
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
    private readonly IPortableEditorService _editorService;
    private readonly IPortableTerminalService _terminalService;
    private readonly IWorkspaceFileManager _workspaceFileManager;
    private readonly IPhpSettingsStore _phpSettingsStore;
    private readonly IPortablePathResolver _paths;
    private readonly MariaDbInstanceOptions _mariaDbOptions = new();
    private SeleniumServerOptions _seleniumOptions;
    private PhpSettings _phpSettings;
    private readonly CancellationTokenSource _applicationLifetime = new();
    private bool _closeAfterStoppingStack;
    private string _terminalWorkingDirectory = string.Empty;
    private string _workspaceDirectory = string.Empty;
    private bool _terminalBusy;

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
        _editorService = new PortableEditorService(toolInventory, app.Paths, app.Logger);
        _terminalService = new PortableTerminalService(moduleVerifier, toolInventory, commandRunner, app.Paths);
        _workspaceFileManager = new WorkspaceFileManager(app.Paths);
        _composerPackageManager = new ComposerProjectPackageManager(
            toolInventory,
            moduleVerifier,
            commandRunner,
            app.Paths);
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
        _dashboard.Python.SetRuntime(_pythonPackageManager.GetRuntime());
        _dashboard.SetEditorRuntime(_editorService.GetRuntime());
        var seleniumSnapshot = _seleniumServer.GetSnapshot();
        _dashboard.SetSeleniumStatus(seleniumSnapshot.State, seleniumSnapshot.Detail);
        PopulateSeleniumSettingsFields();
        PopulatePhpSettingsFields(_phpSettings);
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
        RefreshWorkspaceFiles();
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

        InstallationStatusText.Text = item.Page switch
        {
            NavigationPage.Composer => _dashboard.Composer.Status,
            NavigationPage.Python => _dashboard.Python.Status,
            NavigationPage.Tools => _dashboard.EditorDetail,
            NavigationPage.Files => WorkspacePathText.Text,
            _ => InstallationStatusText.Text,
        };
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
            InstallationStatusText.Text = _dashboard.Text.PhpSettingsSaved(_dashboard.StackProcessState);
            await _logger.LogAsync(
                ApplicationLogLevel.Information,
                "php",
                "settings.saved",
                "Portable PHP settings were saved.");
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
        MariaDbPort: _mariaDbOptions.Port,
        PhpSettings: _phpSettings);

    private async void ToggleStack_Click(object sender, RoutedEventArgs e) => await ToggleStackAsync();

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
        try
        {
            var state = _mariaDbInitializer.GetState(_mariaDbOptions);
            var initializedNow = false;
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
                initializedNow = true;
            }

            _dashboard.SetMariaDbStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Starting, string.Empty);
            var server = await _mariaDbServer.StartAsync(_mariaDbOptions, _applicationLifetime.Token);
            _dashboard.SetMariaDbStatus(server.State, server.Detail);
            if (server.State != PortableDeveloper.Domain.Processes.ManagedProcessState.Running)
            {
                InstallationStatusText.Text = _dashboard.Text.MariaDbInitializationFailed(server.Detail);
                return;
            }

            if (initializedNow)
            {
                var cleanup = await _databaseCatalog.RemoveGeneratedTestDatabaseAsync(
                    _mariaDbOptions,
                    _applicationLifetime.Token);
                if (!cleanup.IsSuccess)
                {
                    InstallationStatusText.Text = _dashboard.Text.DatabaseCreateFailed(cleanup.Detail);
                    return;
                }
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

            await RefreshDatabasesAsync();
            _dashboard.SetRootPasswordState(_mariaDbAccount.HasRootPassword(_mariaDbOptions));
            InstallationStatusText.Text = _dashboard.Text.MariaDbReady;
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

    private async void OpenPhpMyAdmin_Click(object sender, RoutedEventArgs e)
    {
        InstallationStatusText.Text = _dashboard.Text.OpeningPhpMyAdmin;
        try
        {
            if (!_dashboard.MariaDbIsRunning)
            {
                await ToggleMariaDbAsync();
                if (!_dashboard.MariaDbIsRunning)
                {
                    return;
                }
            }

            if (_dashboard.StackProcessState != PortableDeveloper.Domain.Processes.ManagedProcessState.Running)
            {
                _dashboard.SetStackStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Starting, string.Empty);
                var stack = await _apachePhpStack.StartAsync(
                    CreateApachePhpOptions(),
                    _applicationLifetime.Token);
                _dashboard.SetStackStatus(stack.State, stack.Detail);
                if (stack.State != PortableDeveloper.Domain.Processes.ManagedProcessState.Running)
                {
                    return;
                }
            }

            Process.Start(new ProcessStartInfo(_dashboard.PhpMyAdminUrl) { UseShellExecute = true });
            InstallationStatusText.Text = _dashboard.PhpMyAdminUrl;
        }
        catch (OperationCanceledException)
        {
            InstallationStatusText.Text = _dashboard.Text.OperationCanceled;
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
            !int.TryParse(SeleniumPortTextBox.Text.Trim(), out var port) ||
            !int.TryParse(SeleniumMaxSessionsTextBox.Text.Trim(), out var maxSessions) ||
            !int.TryParse(SeleniumSessionTimeoutTextBox.Text.Trim(), out var sessionTimeout) ||
            port is < 1024 or > 65535 ||
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
                Port = port,
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
        SeleniumPortTextBox.Text = _seleniumOptions.Port.ToString();
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

    private async void TerminalCommandTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await ExecuteTerminalCommandAsync();
        }
    }

    private async void RunTerminalCommand_Click(object sender, RoutedEventArgs e) =>
        await ExecuteTerminalCommandAsync();

    private void ClearTerminal_Click(object sender, RoutedEventArgs e) => TerminalOutputTextBox.Clear();

    private async Task ExecuteTerminalCommandAsync()
    {
        if (_terminalBusy)
        {
            return;
        }

        var command = TerminalCommandTextBox.Text;
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        _terminalBusy = true;
        TerminalCommandTextBox.IsEnabled = false;
        RunTerminalCommandButton.IsEnabled = false;
        AppendTerminal($"> {DisplayTerminalPath(_terminalWorkingDirectory)} {command}");
        TerminalCommandTextBox.Clear();
        try
        {
            var result = await _terminalService.ExecuteAsync(
                command,
                _terminalWorkingDirectory,
                _applicationLifetime.Token);
            _terminalWorkingDirectory = result.WorkingDirectory;
            TerminalPromptText.Text = DisplayTerminalPath(_terminalWorkingDirectory);
            if (result.ClearScreen)
            {
                TerminalOutputTextBox.Clear();
            }

            if (result.ServiceRequest is not null)
            {
                await ExecuteTerminalServiceRequestAsync(result.ServiceRequest);
                AppendTerminal(GetServiceStatusText());
            }
            else if (!string.IsNullOrWhiteSpace(result.Output))
            {
                AppendTerminal(result.Output);
            }
        }
        catch (OperationCanceledException)
        {
            AppendTerminal(_dashboard.Text.OperationCanceled);
        }
        catch (Exception exception)
        {
            AppendTerminal(exception.Message);
        }
        finally
        {
            _terminalBusy = false;
            TerminalCommandTextBox.IsEnabled = true;
            RunTerminalCommandButton.IsEnabled = true;
            TerminalCommandTextBox.Focus();
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

    private void AppendTerminal(string text)
    {
        var next = string.IsNullOrEmpty(TerminalOutputTextBox.Text)
            ? text
            : TerminalOutputTextBox.Text + Environment.NewLine + text;
        const int maximumCharacters = 100_000;
        TerminalOutputTextBox.Text = next.Length <= maximumCharacters
            ? next
            : next[^maximumCharacters..];
        TerminalOutputTextBox.CaretIndex = TerminalOutputTextBox.Text.Length;
        TerminalOutputTextBox.ScrollToEnd();
    }

    private static string DisplayTerminalPath(string relativePath) =>
        string.IsNullOrEmpty(relativePath) ? "www:/" : $"www:/{relativePath}";

    private void RefreshWorkspace_Click(object sender, RoutedEventArgs e) => RefreshWorkspaceFiles();

    private void WorkspaceUp_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(_workspaceDirectory))
        {
            return;
        }

        var parent = Path.GetDirectoryName(_workspaceDirectory.Replace('/', Path.DirectorySeparatorChar));
        _workspaceDirectory = string.IsNullOrWhiteSpace(parent) ? string.Empty : parent.Replace(Path.DirectorySeparatorChar, '/');
        RefreshWorkspaceFiles();
    }

    private void CreateWorkspaceFile_Click(object sender, RoutedEventArgs e) =>
        RunWorkspaceOperation(() => _workspaceFileManager.CreateFile(_workspaceDirectory, WorkspaceItemNameTextBox.Text));

    private void CreateWorkspaceFolder_Click(object sender, RoutedEventArgs e) =>
        RunWorkspaceOperation(() => _workspaceFileManager.CreateDirectory(_workspaceDirectory, WorkspaceItemNameTextBox.Text));

    private async void OpenWorkspaceItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: WorkspaceEntryViewModel entry } || !entry.IsSafe)
        {
            return;
        }

        if (entry.IsDirectory)
        {
            _workspaceDirectory = entry.RelativePath;
            RefreshWorkspaceFiles();
            return;
        }

        await OpenEditorAsync(Path.Combine(
            _workspaceFileManager.RootRelativePath,
            entry.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private void RenameWorkspaceItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: WorkspaceEntryViewModel entry } || !entry.IsSafe)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(WorkspaceItemNameTextBox.Text))
        {
            InstallationStatusText.Text = _dashboard.Text.RenameItemQuestion(entry.Name);
            return;
        }

        RunWorkspaceOperation(() => _workspaceFileManager.Rename(entry.RelativePath, WorkspaceItemNameTextBox.Text));
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
            WorkspaceItemNameTextBox.Clear();
            RefreshWorkspaceFiles();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            InstallationStatusText.Text = _dashboard.Text.WorkspaceOperationFailed(exception.Message);
        }
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
            _dashboard.SetWorkspaceEntries([]);
            WorkspacePathText.Text = "www:/";
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
