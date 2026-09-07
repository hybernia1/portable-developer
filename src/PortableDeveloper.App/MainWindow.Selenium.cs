using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using PortableDeveloper.App.ViewModels;
using PortableDeveloper.App.Views;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Selenium;

namespace PortableDeveloper.App;

public partial class MainWindow
{
    private void SetSeleniumStatus(string status) => _dashboard.SeleniumPage.SetStatus(status);

    private async void SeleniumPage_ActionRequested(object? sender, SeleniumActionRequestedEventArgs e)
    {
        switch (e.Action)
        {
            case SeleniumAction.ToggleServer:
                await ToggleSeleniumAsync();
                break;
            case SeleniumAction.OpenHub:
                OpenSeleniumHub();
                break;
            case SeleniumAction.SaveSettings:
                SaveSeleniumSettings();
                break;
            case SeleniumAction.ReloadDrivers:
                ReloadSeleniumDrivers();
                break;
            case SeleniumAction.InstallDriver when e.Payload is RuntimePackageViewModel package:
                await InstallRuntimePackageAsync(package);
                break;
            case SeleniumAction.CreateProfile:
                await CreateCleanSeleniumProfileAsync(e.View);
                break;
            case SeleniumAction.EditProfile when e.Payload is string profileId:
                await EditSeleniumProfileAsync(profileId);
                break;
            case SeleniumAction.CopyProfileId when e.Payload is string copiedProfileId:
                CopySeleniumIdentifier(copiedProfileId, _dashboard.Text.ProfileIdCopied);
                break;
            case SeleniumAction.RemoveProfile when e.Payload is string removedProfileId:
                RemoveSeleniumProfile(removedProfileId);
                break;
            case SeleniumAction.ChooseCookieFile:
                ChooseCookieFile();
                break;
            case SeleniumAction.ImportCookieVault:
                await ImportCookieVaultAsync();
                break;
            case SeleniumAction.CopyCookieVaultId when e.Payload is string copiedVaultId:
                CopySeleniumIdentifier(copiedVaultId, _dashboard.Text.CookieVaultIdCopied);
                break;
            case SeleniumAction.RemoveCookieVault when e.Payload is string removedVaultId:
                RemoveCookieVault(removedVaultId);
                break;
            case SeleniumAction.RefreshSessions:
                await RefreshSeleniumSessionsAsync();
                break;
            case SeleniumAction.TerminateSession when e.Payload is string sessionId:
                await TerminateSeleniumSessionAsync(sessionId);
                break;
        }
    }

    private async Task ToggleSeleniumAsync()
    {
        if (!_dashboard.Runtime.SeleniumActionEnabled)
        {
            return;
        }

        var shouldStop = _dashboard.Runtime.SeleniumIsRunning;
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

        _dashboard.Runtime.SetSeleniumOperationInProgress(true);
        _dashboard.Runtime.SetSeleniumStatus(
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
            _dashboard.Runtime.SetSeleniumStatus(snapshot.State, snapshot.Detail);
            if (snapshot.State == PortableDeveloper.Domain.Processes.ManagedProcessState.Running)
            {
                await RefreshSeleniumSessionsAsync();
                SetSeleniumStatus(_dashboard.Runtime.SeleniumHubUrl);
            }
            else
            {
                _dashboard.SeleniumPage.SetSessions([]);
            }
        }
        catch (OperationCanceledException)
        {
            SetSeleniumStatus(_dashboard.Text.OperationCanceled);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or UnauthorizedAccessException or HttpRequestException)
        {
            _dashboard.Runtime.SetSeleniumStatus(PortableDeveloper.Domain.Processes.ManagedProcessState.Failed, exception.Message);
            SetSeleniumStatus(_dashboard.Text.SeleniumOperationFailed(exception.Message));
        }
        finally
        {
            _dashboard.Runtime.SetSeleniumOperationInProgress(false);
        }
    }

    private void SaveSeleniumSettings()
    {
        var page = _dashboard.SeleniumPage;
        if (!_dashboard.Runtime.SeleniumSettingsEnabled ||
            !int.TryParse(page.MaximumSessionsText.Trim(), out var maxSessions) ||
            !int.TryParse(page.SessionTimeoutText.Trim(), out var sessionTimeout) ||
            maxSessions is < 1 or > 32 ||
            sessionTimeout is < 30 or > 86400)
        {
            SetSeleniumStatus(_dashboard.Text.SeleniumSettingsInvalid);
            return;
        }

        try
        {
            _seleniumOptions = _seleniumOptions with
            {
                Port = _portSettings.SeleniumPort,
                MaxSessions = maxSessions,
                SessionTimeoutSeconds = sessionTimeout,
                DownloadsEnabled = page.DownloadsEnabled
            };
            _seleniumSettingsStore.Save(_seleniumOptions);
            _dashboard.Runtime.SetSeleniumOptions(_seleniumOptions);
            SetSeleniumStatus(_dashboard.Text.SeleniumSettingsSaved);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException)
        {
            SetSeleniumStatus(_dashboard.Text.SeleniumOperationFailed(exception.Message));
        }
    }

    private void PopulateSeleniumSettingsFields()
    {
        _dashboard.SeleniumPage.PopulateSettings(
            _seleniumOptions.MaxSessions,
            _seleniumOptions.SessionTimeoutSeconds,
            _seleniumOptions.DownloadsEnabled);
    }

    private void ReloadSeleniumDrivers()
    {
        if (!_dashboard.Runtime.SeleniumSettingsEnabled)
        {
            return;
        }

        RefreshSeleniumEnvironments();
        SetSeleniumStatus(_dashboard.SeleniumPage.SeleniumDriverCount);
    }

    private void RefreshSeleniumEnvironments()
    {
        _seleniumEnvironments = _seleniumEnvironmentInventory.Scan();
        _dashboard.SeleniumPage.SetEnvironments(_seleniumEnvironments);
        _dashboard.Runtime.SetSeleniumEnvironmentAvailability(_dashboard.SeleniumPage.ReadyEnvironmentCount);
        _dashboard.SeleniumPage.SelectFirstBrowserEnvironmentIfNeeded();
    }

    private async Task CreateCleanSeleniumProfileAsync(SeleniumPageView view)
    {
        if (!_dashboard.Runtime.SeleniumProfileActionsEnabled)
        {
            return;
        }

        var page = _dashboard.SeleniumPage;
        if (!SeleniumProfileName.TryNormalize(page.CleanProfileName, out var profileName))
        {
            SetSeleniumStatus(_dashboard.Text.ProfileNameRequired);
            view.FocusProfileName();
            return;
        }

        if (page.SelectedBrowserEnvironmentId is not string environmentId
            || _seleniumEnvironments.FirstOrDefault(item => item.Id == environmentId) is not { } environment)
        {
            SetSeleniumStatus(_dashboard.Text.SelectBrowserEnvironment);
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
            SetSeleniumStatus(_dashboard.Text.UnsupportedBrowserEnvironment);
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

        _dashboard.Runtime.SetSeleniumOperationInProgress(true);
        SetSeleniumProfileProgress(true, _dashboard.Text.SeleniumProfileWaiting);
        var browserStopwatch = Stopwatch.StartNew();
        var sealingMilliseconds = 0L;
        try
        {
            SetSeleniumStatus(_dashboard.Text.ConfigureBrowserAndClose);
            var process = Process.Start(startInfo);
            if (process is null)
            {
                SetSeleniumStatus(_dashboard.Text.BrowserCouldNotStart);
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
                SetSeleniumStatus(_dashboard.Text.SeleniumProfileCreateFailed(result.Detail));
                return;
            }

            page.CleanProfileName = string.Empty;
            _dashboard.SeleniumPage.SetProfiles(_seleniumProfileStore.GetProfiles());
            SetSeleniumStatus(_dashboard.Text.SeleniumProfileCreated(result.Profile!.Name));
        }
        catch (OperationCanceledException) when (_applicationLifetime.IsCancellationRequested)
        {
            // Closing the application cancels profile enrollment without surfacing an unhandled UI exception.
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            SetSeleniumStatus(_dashboard.Text.SeleniumProfileCreateFailed(exception.Message));
        }
        finally
        {
            browserStopwatch.Stop();
            SetSeleniumProfileProgress(true, _dashboard.Text.SeleniumProfileCleaning);
            var cleanupStopwatch = Stopwatch.StartNew();
            await Task.Run(() => TryDeleteProfileDraft(draftPath));
            cleanupStopwatch.Stop();
            SetSeleniumProfileProgress(false, string.Empty);
            _dashboard.Runtime.SetSeleniumOperationInProgress(false);
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "selenium-profiles",
                "selenium.profile.enrollment.timing",
                $"browser={browser.Value}; browserOpenMs={browserStopwatch.ElapsedMilliseconds}; sealingMs={sealingMilliseconds}; cleanupMs={cleanupStopwatch.ElapsedMilliseconds}");
        }
    }

    private async Task EditSeleniumProfileAsync(string id)
    {
        if (!_dashboard.Runtime.SeleniumProfileActionsEnabled)
        {
            return;
        }

        var profile = _seleniumProfileStore.GetProfiles().FirstOrDefault(item => item.Id == id && item.IsVerified);
        if (profile is null)
        {
            SetSeleniumStatus(_dashboard.Text.SeleniumProfileUpdateFailed("The profile does not exist or is damaged."));
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
            SetSeleniumStatus(_dashboard.Text.SeleniumProfileUpdateFailed(_dashboard.Text.ProfileBrowserUnavailable));
            return;
        }

        var token = Guid.NewGuid().ToString("N");
        var draftRelativePath = Path.Combine("temp", "selenium-profile-creation", token);
        var draftPath = _paths.Resolve(draftRelativePath);
        _dashboard.Runtime.SetSeleniumOperationInProgress(true);
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

            SetSeleniumStatus(_dashboard.Text.SeleniumProfileEditing);
            SetSeleniumProfileProgress(true, _dashboard.Text.SeleniumProfileEditing);
            browserStopwatch.Start();
            var process = Process.Start(startInfo);
            if (process is null)
            {
                SetSeleniumStatus(_dashboard.Text.BrowserCouldNotStart);
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
                SetSeleniumStatus(_dashboard.Text.SeleniumProfileUpdateFailed(result.Detail));
                return;
            }

            _dashboard.SeleniumPage.SetProfiles(_seleniumProfileStore.GetProfiles());
            SetSeleniumStatus(_dashboard.Text.SeleniumProfileUpdated(profile.Name));
        }
        catch (OperationCanceledException) when (_applicationLifetime.IsCancellationRequested)
        {
            // Closing the application cancels editing and leaves the original master intact.
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            SetSeleniumStatus(_dashboard.Text.SeleniumProfileUpdateFailed(exception.Message));
        }
        finally
        {
            browserStopwatch.Stop();
            SetSeleniumProfileProgress(true, _dashboard.Text.SeleniumProfileCleaning);
            var cleanupStopwatch = Stopwatch.StartNew();
            await Task.Run(() => TryDeleteProfileDraft(draftPath));
            cleanupStopwatch.Stop();
            SetSeleniumProfileProgress(false, string.Empty);
            _dashboard.Runtime.SetSeleniumOperationInProgress(false);
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
        _dashboard.SeleniumPage.SetProfileProgress(visible, message);
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

    private void RemoveSeleniumProfile(string id)
    {
        if (!_dashboard.Runtime.SeleniumProfileActionsEnabled)
        {
            return;
        }

        var profile = _dashboard.SeleniumPage.SeleniumProfiles.FirstOrDefault(item => item.Id == id);
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
            SetSeleniumStatus(_dashboard.Text.SeleniumOperationFailed(result.Detail));
            return;
        }

        _dashboard.SeleniumPage.SetProfiles(_seleniumProfileStore.GetProfiles());
        SetSeleniumStatus(_dashboard.Text.SeleniumProfileRemoved);
    }

    private void CopySeleniumIdentifier(string id, string successMessage)
    {
        try
        {
            Clipboard.SetText(id);
            SetSeleniumStatus(successMessage);
        }
        catch (Exception exception) when (exception is System.Runtime.InteropServices.ExternalException)
        {
            SetSeleniumStatus(_dashboard.Text.CopyIdFailed(exception.Message));
        }
    }

    private void ChooseCookieFile()
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

        _dashboard.SeleniumPage.SetCookieFile(dialog.FileName);
    }

    private async Task ImportCookieVaultAsync()
    {
        if (!_dashboard.Runtime.SeleniumProfileActionsEnabled)
        {
            return;
        }

        var page = _dashboard.SeleniumPage;
        var selectedCookieFilePath = page.SelectedCookieFilePath;
        if (selectedCookieFilePath is null || !File.Exists(selectedCookieFilePath))
        {
            SetSeleniumStatus(_dashboard.Text.NoCookieFileSelected);
            return;
        }

        byte[]? json = null;
        var vaultName = page.CookieVaultName;
        _dashboard.Runtime.SetSeleniumOperationInProgress(true);
        try
        {
            var file = new FileInfo(selectedCookieFilePath);
            if (file.Length is < 1 or > 5 * 1024 * 1024)
            {
                SetSeleniumStatus(_dashboard.Text.CookieVaultImportFailed("The cookie export must be between 1 byte and 5 MiB."));
                return;
            }

            json = await File.ReadAllBytesAsync(selectedCookieFilePath, _applicationLifetime.Token);
            var result = await Task.Run(
                () => _seleniumCookieVaultStore.ImportJson(vaultName, json),
                _applicationLifetime.Token);
            if (!result.IsSuccess)
            {
                SetSeleniumStatus(_dashboard.Text.CookieVaultImportFailed(result.Detail));
                return;
            }

            page.CookieVaultName = string.Empty;
            page.ClearCookieFile();
            RefreshCookieVaults();
            SetSeleniumStatus(_dashboard.Text.CookieVaultImported(result.Vault!.Name, result.SkippedCookies));
        }
        catch (OperationCanceledException)
        {
            SetSeleniumStatus(_dashboard.Text.OperationCanceled);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            SetSeleniumStatus(_dashboard.Text.CookieVaultImportFailed(exception.Message));
        }
        finally
        {
            if (json is not null)
            {
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(json);
            }
            _dashboard.Runtime.SetSeleniumOperationInProgress(false);
        }
    }

    private void RemoveCookieVault(string id)
    {
        if (!_dashboard.Runtime.SeleniumProfileActionsEnabled)
        {
            return;
        }

        var vault = _dashboard.SeleniumPage.SeleniumCookieVaults.FirstOrDefault(item => item.Id == id);
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
        SetSeleniumStatus(result.IsSuccess
            ? _dashboard.Text.CookieVaultRemoved
            : _dashboard.Text.SeleniumOperationFailed(result.Detail));
        RefreshCookieVaults();
    }

    private void RefreshCookieVaults() =>
        _dashboard.SeleniumPage.SetCookieVaults(_seleniumCookieVaultStore.GetVaults());

    private void OpenSeleniumHub()
    {
        if (!_dashboard.Runtime.SeleniumIsRunning)
        {
            return;
        }

        Process.Start(new ProcessStartInfo(_dashboard.Runtime.SeleniumHubUrl) { UseShellExecute = true });
        SetSeleniumStatus(_dashboard.Runtime.SeleniumHubUrl);
    }

    private async Task RefreshSeleniumSessionsAsync()
    {
        if (!_dashboard.Runtime.SeleniumIsRunning)
        {
            _dashboard.SeleniumPage.SetSessions([]);
            return;
        }

        try
        {
            var sessions = await _seleniumGrid.ListSessionsAsync(_seleniumOptions.Port, _applicationLifetime.Token);
            _dashboard.SeleniumPage.SetSessions(sessions);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidDataException or JsonException or TaskCanceledException)
        {
            SetSeleniumStatus(_dashboard.Text.SeleniumSessionsFailed(exception.Message));
        }
    }

    private async Task TerminateSeleniumSessionAsync(string sessionId)
    {
        if (!_dashboard.Runtime.SeleniumSessionActionsEnabled)
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

        _dashboard.Runtime.SetSeleniumOperationInProgress(true);
        SetSeleniumStatus(_dashboard.Text.TerminatingSession);
        try
        {
            var result = await _seleniumGrid.TerminateSessionAsync(
                _seleniumOptions.Port,
                sessionId,
                _applicationLifetime.Token);
            if (!result.IsSuccess)
            {
                SetSeleniumStatus(_dashboard.Text.SeleniumOperationFailed(result.Detail));
                return;
            }

            await RefreshSeleniumSessionsAsync();
            SetSeleniumStatus(_dashboard.Text.SeleniumSessionTerminated);
        }
        catch (Exception exception) when (exception is HttpRequestException or InvalidDataException or TaskCanceledException)
        {
            SetSeleniumStatus(_dashboard.Text.SeleniumOperationFailed(exception.Message));
        }
        finally
        {
            _dashboard.Runtime.SetSeleniumOperationInProgress(false);
        }
    }
}
