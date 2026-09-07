using System.IO;
using System.Windows;
using System.Windows.Controls;
using PortableDeveloper.App.Views;
using PortableDeveloper.Application.Settings;
using PortableDeveloper.Application.Storage;

namespace PortableDeveloper.App;

public partial class MainWindow
{

    private async void RefreshStorageUsage_Click(object sender, RoutedEventArgs e)
    {
        if (StorageMaintenanceIsBusy())
        {
            _dashboard.SettingsPage.SetStorageStatus(_dashboard.Text.StorageBusy);
            return;
        }

        await RefreshStorageUsageAsync();
    }


    private async void ClearStorageCache_Click(object? sender, StorageCacheRequestedEventArgs e)
    {
        var cache = e.Cache;

        if (StorageMaintenanceIsBusy())
        {
            _dashboard.SettingsPage.SetStorageStatus(_dashboard.Text.StorageBusy);
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

        _dashboard.SettingsPage.SetStorageActionsEnabled(false);
        var status = _dashboard.Text.ClearingCache(label);
        _dashboard.SettingsPage.SetStorageStatus(status);
        _dashboard.GlobalOperation.Begin();
        try
        {
            var result = await _storageMaintenance.ClearCacheAsync(cache, _applicationLifetime.Token);
            var completionStatus = result.Success
                ? _dashboard.Text.CacheCleared(label, FormatStorageSize(result.RemovedBytes))
                : _dashboard.Text.CacheClearFailed(label, result.Detail);
            await RefreshStorageUsageAsync(completionStatus);
        }
        catch (OperationCanceledException)
        {
            // Application shutdown cancels background storage work.
        }
        finally
        {
            _dashboard.GlobalOperation.End();
            _dashboard.SettingsPage.SetStorageActionsEnabled(!StorageMaintenanceIsBusy());
        }
    }

    private async void ClearAllStorageCaches_Click(object sender, RoutedEventArgs e)
    {
        if (StorageMaintenanceIsBusy())
        {
            _dashboard.SettingsPage.SetStorageStatus(_dashboard.Text.StorageBusy);
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

        _dashboard.SettingsPage.SetStorageActionsEnabled(false);
        _dashboard.GlobalOperation.Begin();
        long removedBytes = 0;
        try
        {
            foreach (var cache in Enum.GetValues<StorageCacheKind>())
            {
                var result = await _storageMaintenance.ClearCacheAsync(cache, _applicationLifetime.Token);
                if (!result.Success)
                {
                    _dashboard.SettingsPage.SetStorageStatus(_dashboard.Text.CacheClearFailed(
                        _dashboard.Text.StorageCacheName(cache),
                        result.Detail));
                    return;
                }

                removedBytes += result.RemovedBytes;
            }

            await RefreshStorageUsageAsync(_dashboard.Text.AllCachesCleared(FormatStorageSize(removedBytes)));
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _dashboard.GlobalOperation.End();
            _dashboard.SettingsPage.SetStorageActionsEnabled(!StorageMaintenanceIsBusy());
        }
    }

    private async Task RefreshStorageUsageAsync(string? completionStatus = null)
    {
        _dashboard.SettingsPage.SetStorageActionsEnabled(false);
        _dashboard.SettingsPage.SetStorageStatus(_dashboard.Text.MeasuringStorage);
        _dashboard.GlobalOperation.Begin();
        try
        {
            var usage = await _storageMaintenance.InspectAsync(_applicationLifetime.Token);
            _dashboard.SettingsPage.ApplyStorageUsage(usage, FormatStorageSize);
            _dashboard.SettingsPage.SetStorageStatus(completionStatus ?? _dashboard.Text.StorageMeasured);
        }
        catch (OperationCanceledException)
        {
            // Application shutdown cancels background storage work.
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _dashboard.SettingsPage.SetStorageStatus(_dashboard.Text.StorageMeasureFailed(exception.Message));
        }
        finally
        {
            _dashboard.GlobalOperation.End();
            _dashboard.SettingsPage.SetStorageActionsEnabled(!StorageMaintenanceIsBusy());
        }
    }

    private bool StorageMaintenanceIsBusy() =>
        _runtimePackageInstallationInProgress
        || _dashboard.GlobalOperation.IsBusy
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
}
