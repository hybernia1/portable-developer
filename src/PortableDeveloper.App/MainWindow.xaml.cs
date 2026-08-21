using System.ComponentModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using PortableDeveloper.App.ViewModels;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.ApachePhp;
using PortableDeveloper.Application.Settings;
using PortableDeveloper.Application.MariaDb;
using PortableDeveloper.Infrastructure.Modules;
using PortableDeveloper.Infrastructure.Packages;
using PortableDeveloper.Infrastructure.Php;
using PortableDeveloper.Infrastructure.Processes;
using PortableDeveloper.Infrastructure.Settings;
using PortableDeveloper.Infrastructure.Health;
using PortableDeveloper.Infrastructure.ApachePhp;
using PortableDeveloper.Infrastructure.MariaDb;

namespace PortableDeveloper.App;

public partial class MainWindow : Window
{
    private readonly DashboardViewModel _dashboard;
    private readonly IApplicationLogger _logger;
    private readonly IApachePhpStackController _apachePhpStack;
    private readonly IMariaDbInstanceInitializer _mariaDbInitializer;
    private readonly IMariaDbServerController _mariaDbServer;
    private readonly IDatabaseCatalogService _databaseCatalog;
    private readonly MariaDbInstanceOptions _mariaDbOptions = new();
    private readonly CancellationTokenSource _applicationLifetime = new();
    private bool _closeAfterStoppingStack;

    public MainWindow()
    {
        InitializeComponent();

        var app = (App)System.Windows.Application.Current;
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
        _dashboard = new DashboardViewModel(
            app.Paths.RootPath,
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
        Loaded += MainWindow_Loaded;
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
                _mariaDbServer.DisposeAsync().AsTask());
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
        InstallationStatusText.Text = _dashboard.Text.LanguageChanged;
        await _logger.LogAsync(
            ApplicationLogLevel.Information,
            "ui",
            "language.changed",
            $"language={language}");
    }

    private async void ToggleStack_Click(object sender, RoutedEventArgs e)
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
                : await _apachePhpStack.StartAsync(new ApachePhpStackOptions());
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
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainWindow_Loaded;
        await BootstrapMariaDbAsync();
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
}
