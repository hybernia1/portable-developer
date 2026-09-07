using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using PortableDeveloper.App.ViewModels;
using PortableDeveloper.App.Views;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.ApachePhp;
using PortableDeveloper.Application.MariaDb;
using PortableDeveloper.Application.Packages;
using PortableDeveloper.Application.Php;
using PortableDeveloper.Application.Ports;

namespace PortableDeveloper.App;

public partial class MainWindow
{

    private void RefreshPorts_Click(object sender, RoutedEventArgs e)
    {
        RefreshPortUsage();
        _dashboard.PortsPage.SetStatus(_dashboard.PortsPage.TcpListenerCount);
    }

    private void PortTextBox_TextChanged(object sender, RoutedEventArgs e) => UpdatePortInputStatuses();

    private void SavePorts_Click(object sender, RoutedEventArgs e)
    {
        if (!_dashboard.Runtime.PortSettingsEnabled)
        {
            _dashboard.PortsPage.SetStatus(_dashboard.Text.PortSettingsRequireStoppedServices);
            return;
        }

        if (!_dashboard.PortsPage.TryCreateSettings(out var settings))
        {
            _dashboard.PortsPage.SetStatus(_dashboard.Text.PortsInvalid);
            return;
        }

        try
        {
            var listeners = _portUsageScanner.Scan();
            _tcpListeners = listeners;
            _dashboard.PortsPage.SetTcpListeners(listeners);
            var occupied = new[] { settings.ApachePort, settings.PhpFastCgiPort, settings.MariaDbPort, settings.SeleniumPort }
                .Where(port => listeners.Any(listener => listener.Port == port) || !_portUsageScanner.IsAvailable(port))
                .Distinct()
                .Order()
                .ToArray();
            if (occupied.Length > 0)
            {
                _dashboard.PortsPage.SetStatus(_dashboard.Text.PortsOccupied(occupied));
                return;
            }

            _portSettingsStore.Save(settings);
            _portSettings = settings;
            _mariaDbOptions = _mariaDbOptions with { Port = settings.MariaDbPort };
            _seleniumOptions = _seleniumOptions with { Port = settings.SeleniumPort };
            _seleniumSettingsStore.Save(_seleniumOptions);
            _dashboard.Runtime.SetPortSettings(settings);
            _dashboard.Runtime.SetSeleniumOptions(_seleniumOptions);
            RefreshWebProjectBindings();
            PopulatePortSettingsFields();
            _dashboard.PortsPage.SetStatus(_dashboard.Text.PortsSaved);
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "ports",
                "settings.saved",
                $"apache={settings.ApachePort}; php={settings.PhpFastCgiPort}; mariadb={settings.MariaDbPort}; selenium={settings.SeleniumPort}");
        }
        catch (ArgumentException)
        {
            _dashboard.PortsPage.SetStatus(_dashboard.Text.PortsInvalid);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NetworkInformationException)
        {
            _dashboard.PortsPage.SetStatus(_dashboard.Text.PortScanFailed(exception.Message));
        }
    }

    private void RefreshPortUsage()
    {
        try
        {
            _tcpListeners = _portUsageScanner.Scan();
            _dashboard.PortsPage.SetTcpListeners(_tcpListeners);
            UpdatePortInputStatuses();
        }
        catch (NetworkInformationException exception)
        {
            _tcpListeners = [];
            _dashboard.PortsPage.SetTcpListeners([]);
            UpdatePortInputStatuses();
            _dashboard.PortsPage.SetStatus(_dashboard.Text.PortScanFailed(exception.Message));
        }
    }

    private void PopulatePortSettingsFields()
    {
        _dashboard.PortsPage.SetPortSettings(_portSettings);
        UpdatePortInputStatuses();
    }

    private void UpdatePortInputStatuses() =>
        _dashboard.PortsPage.UpdateInputStatuses(_tcpListeners, _portUsageScanner.IsAvailable);

    private async void SavePhpSettings_Click(object sender, RoutedEventArgs e)
    {
        if (!_dashboard.Runtime.PhpSettingsEnabled || !_dashboard.PhpPage.TryCreateSettings(out var settings))
        {
            _dashboard.PhpPage.SetStatus(_dashboard.Text.PhpSettingsInvalid);
            return;
        }

        try
        {
            _phpSettingsStore.Save(settings);
            _phpSettings = settings;
            PopulatePhpSettingsFields(settings);
            var wasRunning = _dashboard.Runtime.ApacheIsRunning;
            await _logger.LogAsync(
                ApplicationLogLevel.Information,
                "php",
                "settings.saved",
                "Portable PHP settings were saved.");
            if (wasRunning)
            {
                var restarted = await RestartApacheAsync(announce: false, _dashboard.PhpPage.SetStatus);
                if (!restarted)
                {
                    return;
                }
            }

            _dashboard.PhpPage.SetStatus(_dashboard.Text.PhpSettingsSaved(_dashboard.Runtime.ApacheProcessState));
        }
        catch (ArgumentException)
        {
            _dashboard.PhpPage.SetStatus(_dashboard.Text.PhpSettingsInvalid);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _dashboard.PhpPage.SetStatus(_dashboard.Text.PhpSettingsSaveFailed(exception.Message));
        }
    }

    private void ResetPhpSettings_Click(object sender, RoutedEventArgs e)
    {
        if (!_dashboard.Runtime.PhpSettingsEnabled)
        {
            return;
        }

        PopulatePhpSettingsFields(PhpSettings.Default);
        _dashboard.PhpPage.SetStatus(_dashboard.Text.PhpDefaultsPrepared);
    }

    private void PopulatePhpSettingsFields(PhpSettings settings) => _dashboard.PhpPage.SetSettings(settings);

    private ApachePhpStackOptions CreateApachePhpOptions() => new(
        ApachePort: _portSettings.ApachePort,
        PhpFastCgiPort: _portSettings.PhpFastCgiPort,
        MariaDbPort: _mariaDbOptions.Port,
        PhpSettings: _phpSettings,
        WebProjects: _webProjects.Projects);

    private async void ToggleApache_Click(object sender, RoutedEventArgs e) => await ToggleApacheAsync();

    private async Task ToggleApacheAsync()
    {
        if (!_dashboard.Runtime.ApacheActionEnabled)
        {
            return;
        }

        var shouldStop = _dashboard.Runtime.ApacheProcessState == PortableDeveloper.Domain.Processes.ManagedProcessState.Running;
        _dashboard.Runtime.SetApacheStatus(
            shouldStop
                ? PortableDeveloper.Domain.Processes.ManagedProcessState.Stopping
                : PortableDeveloper.Domain.Processes.ManagedProcessState.Starting,
            "");
        try
        {
            var snapshot = shouldStop
                ? await _apachePhpStack.StopAsync()
                : await _apachePhpStack.StartAsync(CreateApachePhpOptions());
            _dashboard.Runtime.SetApacheStatus(snapshot.State, snapshot.Detail);
            if (!shouldStop && snapshot.State == PortableDeveloper.Domain.Processes.ManagedProcessState.Running)
            {
                _dashboard.Runtime.SetWebConfigurationRestartRequired(false);
            }
        }
        catch (Exception exception)
        {
            _dashboard.Runtime.SetApacheStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Failed, exception.Message);
        }
    }

    private async Task<bool> RestartApacheAsync(bool announce, Action<string> statusSink)
    {
        if (!_dashboard.Runtime.ApacheRestartEnabled)
        {
            return false;
        }

        statusSink(_dashboard.Text.RestartingApacheService);
        try
        {
            _dashboard.Runtime.SetApacheStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Stopping, string.Empty);
            var stopped = await _apachePhpStack.StopAsync(_applicationLifetime.Token);
            _dashboard.Runtime.SetApacheStatus(stopped.State, stopped.Detail);
            if (stopped.State == PortableDeveloper.Domain.Processes.ManagedProcessState.Failed)
            {
                return false;
            }

            _dashboard.Runtime.SetApacheStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Starting, string.Empty);
            var started = await _apachePhpStack.StartAsync(CreateApachePhpOptions(), _applicationLifetime.Token);
            _dashboard.Runtime.SetApacheStatus(started.State, started.Detail);
            if (started.State != PortableDeveloper.Domain.Processes.ManagedProcessState.Running)
            {
                return false;
            }

            _dashboard.Runtime.SetWebConfigurationRestartRequired(false);

            if (announce)
            {
                statusSink(_dashboard.Text.ApacheServiceRestarted);
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            statusSink(_dashboard.Text.OperationCanceled);
            return false;
        }
        catch (Exception exception)
        {
            _dashboard.Runtime.SetApacheStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Failed, exception.Message);
            statusSink(exception.Message);
            return false;
        }
    }

    private async void DatabasesPage_ToggleRequested(object sender, RoutedEventArgs e) => await ToggleMariaDbAsync();

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        Loaded -= MainWindow_Loaded;
        _taskScheduler.Start();
        RefreshScheduledTaskBindings();
        if (_dashboard.Runtime.MariaDbInstalled)
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
        if (sender is Button { DataContext: RuntimePackageViewModel package })
        {
            await InstallRuntimePackageAsync(package);
        }
    }

    private async void ModulesPage_InstallRequested(object? sender, RuntimePackageInstallRequestedEventArgs e) =>
        await InstallRuntimePackageAsync(e.Package);

    private async Task InstallRuntimePackageAsync(RuntimePackageViewModel package)
    {
        if (!package.CanInstall || _dashboard.GlobalOperation.IsBusy)
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
        });
        package.BeginInstallation(0, _dashboard.Text.PackageInstallProgress(new(
            package.Kind,
            RuntimePackageInstallStage.Preparing,
            string.Empty,
            0)));
        foreach (var item in _dashboard.Runtime.RuntimePackages.Concat(_dashboard.Runtime.SeleniumDriverPackages))
        {
            item.SetManagerBusy(true);
        }

        RuntimePackageInstallResult result;
        _runtimePackageInstallationInProgress = true;
        _dashboard.GlobalOperation.Begin();
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

        var installed = _dashboard.Runtime.RuntimePackages
            .Concat(_dashboard.Runtime.SeleniumDriverPackages)
            .First(item => item.Kind == package.Kind);
        installed.Complete(true, _dashboard.Text.PackageInstallSucceeded(installed.Name));
    }

    private void SetRuntimePackageManagerBusy(bool busy)
    {
        foreach (var item in _dashboard.Runtime.RuntimePackages.Concat(_dashboard.Runtime.SeleniumDriverPackages))
        {
            item.SetManagerBusy(busy);
        }
    }

    private void RefreshPhpExtensions()
    {
        var phpInstallation = _moduleInventory.GetInstalled(PortableDeveloper.Domain.Modules.ModuleKind.Php).FirstOrDefault();
        var enabledPhpExtensions = _phpSettings.EnabledExtensions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        _dashboard.PhpPage.SetExtensions(PhpExtensionCatalog.All.Select(extension => new PhpExtensionViewModel(
            extension.Name,
            extension.IsRequired,
            phpInstallation is not null && File.Exists(_paths.Resolve(Path.Combine(
                phpInstallation.ModuleRootRelativePath,
                "ext",
                $"php_{extension.Name}.dll"))),
            enabledPhpExtensions.Contains(extension.Name))));
        _phpSettings = _phpSettings with
        {
            EnabledExtensions = _dashboard.PhpPage.PhpExtensions
                .Where(extension => extension.IsEnabled)
                .Select(extension => extension.Name)
                .ToArray()
        };
    }

    private async Task BootstrapMariaDbAsync()
    {
        _dashboard.Runtime.SetMariaDbOperationInProgress(true);
        _dashboard.DatabasesPage.SetStatus(_dashboard.Text.InitializingMariaDb);
        var startedForBootstrap = false;
        try
        {
            var state = _mariaDbInitializer.GetState(_mariaDbOptions);
            if (state == MariaDbInstanceState.Incomplete)
            {
                _dashboard.Runtime.SetMariaDbState(state);
                _dashboard.DatabasesPage.SetStatus(_dashboard.Text.MariaDbInitializationFailed(
                    "Existing database files are incomplete and were left unchanged."));
                return;
            }

            if (state == MariaDbInstanceState.NotInitialized)
            {
                var initialization = await _mariaDbInitializer.InitializeAsync(_mariaDbOptions, _applicationLifetime.Token);
                if (initialization.Status == MariaDbInitializationStatus.Failed)
                {
                    _dashboard.DatabasesPage.SetStatus(_dashboard.Text.MariaDbInitializationFailed(initialization.Detail));
                    return;
                }

                state = _mariaDbInitializer.GetState(_mariaDbOptions);
                _dashboard.Runtime.SetMariaDbState(state);
            }
            else
            {
                _dashboard.Runtime.SetMariaDbState(state);
                _dashboard.Runtime.SetMariaDbStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Stopped, string.Empty);
                _dashboard.Runtime.SetRootPasswordState(_mariaDbAccount.HasRootPassword(_mariaDbOptions));
                _dashboard.DatabasesPage.SetStatus(_dashboard.Text.MariaDbPreparedStopped);
                return;
            }

            _dashboard.Runtime.SetMariaDbStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Starting, string.Empty);
            var server = await _mariaDbServer.StartAsync(_mariaDbOptions, _applicationLifetime.Token);
            _dashboard.Runtime.SetMariaDbStatus(server.State, server.Detail);
            if (server.State != PortableDeveloper.Domain.Processes.ManagedProcessState.Running)
            {
                _dashboard.DatabasesPage.SetStatus(_dashboard.Text.MariaDbInitializationFailed(server.Detail));
                return;
            }
            startedForBootstrap = true;

            var cleanup = await _databaseCatalog.RemoveGeneratedTestDatabaseAsync(
                _mariaDbOptions,
                _applicationLifetime.Token);
            if (!cleanup.IsSuccess)
            {
                _dashboard.DatabasesPage.SetStatus(_dashboard.Text.DatabaseCreateFailed(cleanup.Detail));
                return;
            }

            var databases = await _databaseCatalog.ListAsync(_mariaDbOptions, _applicationLifetime.Token);
            if (!databases.Any(database => string.Equals(database.Name, "portable_dev", StringComparison.OrdinalIgnoreCase)))
            {
                var created = await _databaseCatalog.CreateAsync(_mariaDbOptions, "portable_dev", _applicationLifetime.Token);
                if (!created.IsSuccess)
                {
                    _dashboard.DatabasesPage.SetStatus(_dashboard.Text.DatabaseCreateFailed(created.Detail));
                    return;
                }
            }

            _dashboard.Runtime.SetRootPasswordState(_mariaDbAccount.HasRootPassword(_mariaDbOptions));
            _dashboard.DatabasesPage.SetStatus(_dashboard.Text.MariaDbPreparedStopped);
        }
        catch (OperationCanceledException)
        {
            _dashboard.DatabasesPage.SetStatus(_dashboard.Text.OperationCanceled);
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException or UnauthorizedAccessException)
        {
            _dashboard.Runtime.SetMariaDbStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Failed, exception.Message);
            _dashboard.DatabasesPage.SetStatus(_dashboard.Text.MariaDbInitializationFailed(exception.Message));
        }
        finally
        {
            if (startedForBootstrap)
            {
                try
                {
                    var stopped = await _mariaDbServer.StopAsync(_applicationLifetime.Token);
                    _dashboard.Runtime.SetMariaDbStatus(stopped.State, stopped.Detail);
                }
                catch (Exception exception) when (exception is IOException or InvalidOperationException)
                {
                    _dashboard.Runtime.SetMariaDbStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Failed, exception.Message);
                }
            }

            _dashboard.Runtime.SetMariaDbState(_mariaDbInitializer.GetState(_mariaDbOptions));
            _dashboard.Runtime.SetMariaDbOperationInProgress(false);
        }
    }

    private async Task ToggleMariaDbAsync()
    {
        if (!_dashboard.Runtime.MariaDbActionEnabled)
        {
            return;
        }

        var shouldStop = _dashboard.Runtime.MariaDbIsRunning;
        _dashboard.Runtime.SetMariaDbOperationInProgress(true);
        _dashboard.Runtime.SetMariaDbStatus(
            shouldStop
                ? PortableDeveloper.Domain.Processes.ManagedProcessState.Stopping
                : PortableDeveloper.Domain.Processes.ManagedProcessState.Starting,
            string.Empty);
        _dashboard.DatabasesPage.SetStatus(shouldStop ? _dashboard.Text.MariaDbStopping : _dashboard.Text.MariaDbStarting);
        try
        {
            var snapshot = shouldStop
                ? await _mariaDbServer.StopAsync(_applicationLifetime.Token)
                : await _mariaDbServer.StartAsync(_mariaDbOptions, _applicationLifetime.Token);
            _dashboard.Runtime.SetMariaDbStatus(snapshot.State, snapshot.Detail);
            if (snapshot.State == PortableDeveloper.Domain.Processes.ManagedProcessState.Running)
            {
                await RefreshDatabasesAsync();
                _dashboard.DatabasesPage.SetStatus(_dashboard.Text.MariaDbReady);
            }
        }
        catch (OperationCanceledException)
        {
            _dashboard.DatabasesPage.SetStatus(_dashboard.Text.OperationCanceled);
        }
        catch (Exception exception)
        {
            _dashboard.Runtime.SetMariaDbStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Failed, exception.Message);
            _dashboard.DatabasesPage.SetStatus(_dashboard.Text.MariaDbInitializationFailed(exception.Message));
        }
        finally
        {
            _dashboard.Runtime.SetMariaDbOperationInProgress(false);
        }
    }

    private async void CreateDatabase_Click(object sender, RoutedEventArgs e)
    {
        if (!_dashboard.Runtime.MariaDbIsRunning)
        {
            return;
        }

        var databaseName = _dashboard.DatabasesPage.NewDatabaseName.Trim();
        _dashboard.DatabasesPage.SetStatus(_dashboard.Text.CreatingDatabase);
        try
        {
            var result = await _databaseCatalog.CreateAsync(_mariaDbOptions, databaseName, _applicationLifetime.Token);
            if (!result.IsSuccess)
            {
                _dashboard.DatabasesPage.SetStatus(_dashboard.Text.DatabaseCreateFailed(result.Detail));
                return;
            }

            _dashboard.DatabasesPage.ClearDatabaseName();
            await RefreshDatabasesAsync();
            _dashboard.DatabasesPage.SetStatus(_dashboard.Text.DatabaseCreated(databaseName));
        }
        catch (OperationCanceledException)
        {
            _dashboard.DatabasesPage.SetStatus(_dashboard.Text.OperationCanceled);
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException or UnauthorizedAccessException)
        {
            _dashboard.DatabasesPage.SetStatus(_dashboard.Text.DatabaseCreateFailed(exception.Message));
        }
    }

    private async void RefreshDatabases_Click(object sender, RoutedEventArgs e) => await RefreshDatabasesAsync();

    private async void DatabasesPage_DatabaseActionRequested(object? sender, DatabaseActionRequestedEventArgs e)
    {
        e.Handled = true;
        switch (e.Action)
        {
            case DatabaseAction.Manage:
                OpenPhpMyAdmin(e.DatabaseName);
                break;
            case DatabaseAction.Delete:
                await DeleteDatabaseAsync(e.DatabaseName);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e.Action), e.Action, null);
        }
    }

    private async Task DeleteDatabaseAsync(string databaseName)
    {
        if (!_dashboard.Runtime.MariaDbIsRunning ||
            string.Equals(databaseName, "portable_dev", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!ConfirmationDialog.Show(
                this,
                _dashboard.Text.DeleteDatabaseTitle,
                _dashboard.Text.DeleteDatabaseQuestion(databaseName),
                _dashboard.Text.DeleteDatabase,
                _dashboard.Text.Cancel))
        {
            return;
        }

        _dashboard.Runtime.SetMariaDbOperationInProgress(true);
        _dashboard.DatabasesPage.SetStatus(_dashboard.Text.DeletingDatabase(databaseName));
        try
        {
            var result = await _databaseCatalog.DeleteAsync(
                _mariaDbOptions,
                databaseName,
                _applicationLifetime.Token);
            if (!result.IsSuccess)
            {
                _dashboard.DatabasesPage.SetStatus(_dashboard.Text.DatabaseDeleteFailed(result.Detail));
                return;
            }

            await RefreshDatabasesAsync();
            _dashboard.DatabasesPage.SetStatus(_dashboard.Text.DatabaseDeleted(databaseName));
        }
        catch (OperationCanceledException)
        {
            _dashboard.DatabasesPage.SetStatus(_dashboard.Text.OperationCanceled);
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException or UnauthorizedAccessException)
        {
            _dashboard.DatabasesPage.SetStatus(_dashboard.Text.DatabaseDeleteFailed(exception.Message));
        }
        finally
        {
            _dashboard.Runtime.SetMariaDbOperationInProgress(false);
        }
    }

    private async Task RefreshDatabasesAsync()
    {
        if (!_dashboard.Runtime.MariaDbIsRunning)
        {
            return;
        }

        try
        {
            var databases = await _databaseCatalog.ListAsync(_mariaDbOptions, _applicationLifetime.Token);
            _dashboard.DatabasesPage.SetDatabases(databases);
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException or UnauthorizedAccessException)
        {
            _dashboard.DatabasesPage.SetStatus(_dashboard.Text.DatabaseOverviewFailed(exception.Message));
        }
    }

    private async void ChangeRootPassword_Click(object? sender, PasswordChangeRequestedEventArgs e)
    {
        if (!_dashboard.Runtime.MariaDbIsRunning)
        {
            return;
        }

        var newPassword = e.Password;
        if (!string.Equals(newPassword, e.Confirmation, StringComparison.Ordinal))
        {
            _dashboard.DatabasesPage.SetStatus(_dashboard.Text.PasswordMismatch);
            return;
        }

        _dashboard.Runtime.SetMariaDbOperationInProgress(true);
        _dashboard.DatabasesPage.SetStatus(_dashboard.Text.PasswordChanging);
        try
        {
            var result = await _mariaDbAccount.ChangeRootPasswordAsync(
                _mariaDbOptions,
                newPassword,
                _applicationLifetime.Token);
            if (!result.IsSuccess)
            {
                _dashboard.DatabasesPage.SetStatus(_dashboard.Text.PasswordChangeFailed(result.Detail));
                return;
            }

            e.ClearInputs();
            _dashboard.Runtime.SetRootPasswordState(true);
            _dashboard.DatabasesPage.SetStatus(_dashboard.Text.PasswordChanged);
            await RefreshDatabasesAsync();
        }
        catch (OperationCanceledException)
        {
            _dashboard.DatabasesPage.SetStatus(_dashboard.Text.OperationCanceled);
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException or UnauthorizedAccessException)
        {
            _dashboard.DatabasesPage.SetStatus(_dashboard.Text.PasswordChangeFailed(exception.Message));
        }
        finally
        {
            _dashboard.Runtime.SetMariaDbOperationInProgress(false);
        }
    }

    private void OpenPhpMyAdmin_Click(object sender, RoutedEventArgs e) => OpenPhpMyAdmin();

    private void OpenPhpMyAdmin(string? databaseName = null)
    {
        if (!_dashboard.Runtime.PhpMyAdminActionEnabled)
        {
            _dashboard.DatabasesPage.SetStatus(_dashboard.Runtime.PhpMyAdminDependencyState);
            return;
        }

        _dashboard.DatabasesPage.SetStatus(_dashboard.Text.OpeningPhpMyAdmin);
        try
        {
            var url = string.IsNullOrWhiteSpace(databaseName)
                ? _dashboard.Runtime.PhpMyAdminUrl
                : $"{_dashboard.Runtime.PhpMyAdminUrl.TrimEnd('/')}/index.php?route=%2Fdatabase%2Fstructure&db={Uri.EscapeDataString(databaseName)}";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            _dashboard.DatabasesPage.SetStatus(url);
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            _dashboard.DatabasesPage.SetStatus(exception.Message);
        }
    }
}
