using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using PortableDeveloper.App.ViewModels;
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
        _taskScheduler.Start();
        RefreshScheduledTaskBindings();
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
}
