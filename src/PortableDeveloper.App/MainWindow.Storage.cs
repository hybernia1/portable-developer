using System.IO;
using System.Windows;
using System.Windows.Controls;
using PortableDeveloper.Application.Settings;
using PortableDeveloper.Application.Storage;

namespace PortableDeveloper.App;

public partial class MainWindow
{

    private async void RefreshStorageUsage_Click(object sender, RoutedEventArgs e) =>
        await RefreshStorageUsageAsync();


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
}
