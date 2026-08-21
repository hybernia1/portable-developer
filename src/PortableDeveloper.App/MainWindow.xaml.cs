using System.ComponentModel;
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
        _mariaDbInitializer = new MariaDbInstanceInitializer(
            moduleVerifier,
            app.Paths,
            new PortableCommandRunner(app.Paths, app.Logger),
            app.Logger);
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
            await _apachePhpStack.DisposeAsync();
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
        if (sender is Button { Tag: "initialize-mariadb" })
        {
            await InitializeMariaDbAsync();
        }
    }

    private async Task InitializeMariaDbAsync()
    {
        _dashboard.SetMariaDbOperationInProgress(true);
        InstallationStatusText.Text = _dashboard.Text.InitializingMariaDb;
        try
        {
            var result = await _mariaDbInitializer.InitializeAsync(_mariaDbOptions, _applicationLifetime.Token);
            InstallationStatusText.Text = result.Status switch
            {
                MariaDbInitializationStatus.Initialized => _dashboard.Text.MariaDbInitialized,
                MariaDbInitializationStatus.AlreadyInitialized => _dashboard.Text.MariaDbAlreadyInitialized,
                _ => _dashboard.Text.MariaDbInitializationFailed(result.Detail)
            };
        }
        catch (OperationCanceledException)
        {
            InstallationStatusText.Text = _dashboard.Text.OperationCanceled;
        }
        finally
        {
            _dashboard.SetMariaDbState(_mariaDbInitializer.GetState(_mariaDbOptions));
            _dashboard.SetMariaDbOperationInProgress(false);
        }
    }
}
