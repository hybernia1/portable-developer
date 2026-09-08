using System.IO;
using System.Text.Json;
using System.Windows;
using PortableDeveloper.App.Controls;
using PortableDeveloper.App.ViewModels;
using PortableDeveloper.Application.Packages;
using PortableDeveloper.Application.ProjectTools;

namespace PortableDeveloper.App;

public partial class MainWindow
{
    private void PackageManager_OpenProjectRequested(object? sender, EventArgs e)
    {
        if (GetPackageManagerView(sender, e) is not { Page: { } page })
        {
            return;
        }

        var service = GetPackageManagerService(page.Kind);
        OpenProjectDirectory(service.ProjectRelativePath, page);
    }

    private async void PackageManager_RefreshRequested(object? sender, EventArgs e)
    {
        if (GetPackageManagerView(sender, e) is { Page: { } page })
        {
            await RefreshPackageManagerAsync(GetPackageManagerService(page.Kind), page);
        }
    }

    private void PackageManager_HelpRequested(object? sender, EventArgs e)
    {
        if (GetPackageManagerView(sender, e) is not { } view)
        {
            return;
        }

        InformationDialog.Show(
            this,
            _dashboard.Text.AddPackage,
            view.HelpText,
            _dashboard.Text.Close);
    }

    private async Task RefreshPackageManagerAsync(
        IProjectPackageManagerService service,
        PackageManagerPageViewModel page)
    {
        if (page.IsBusy || _dashboard.GlobalOperation.IsBusy)
        {
            return;
        }

        page.ClearOperation();
        page.SetRuntime(service.GetRuntime());
        if (!page.RuntimeReady)
        {
            SetPackageStatus(page, page.RuntimeDetail);
            return;
        }

        page.SetBusy(true);
        var progress = CreatePackageProgress(page);
        SetPackageStatus(page, _dashboard.Text.LoadingPackages);
        _dashboard.GlobalOperation.Begin();
        try
        {
            var packages = await Task.Run(
                () => service.ListPackagesAsync(_applicationLifetime.Token, progress),
                _applicationLifetime.Token);
            page.SetPackages(packages);
            SetPackageStatus(page, page.ProjectRelativePath);
            page.ClearOperation();
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

    private async Task EnsurePackageManagerLoadedAsync(
        IProjectPackageManagerService service,
        PackageManagerPageViewModel page)
    {
        if (page.InventoryLoaded)
        {
            return;
        }

        await RefreshPackageManagerAsync(service, page);
    }

    private async void PackageManager_InstallRequested(object? sender, PackageInstallRequestedEventArgs e)
    {
        if (GetPackageManagerView(sender, e) is not { Page: { } page } view)
        {
            return;
        }

        var succeeded = await InstallPackageAsync(
            GetPackageManagerService(page.Kind),
            page,
            e.PackageName,
            e.VersionConstraint);
        if (succeeded)
        {
            view.ClearPackageInput();
        }
    }

    private async Task<bool> InstallPackageAsync(
        IProjectPackageManagerService service,
        PackageManagerPageViewModel page,
        string packageName,
        string versionConstraint)
    {
        if (!page.CanOperate || _dashboard.GlobalOperation.IsBusy)
        {
            return false;
        }

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
        _dashboard.GlobalOperation.Begin();
        try
        {
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
                return false;
            }

            var packages = await Task.Run(
                () => service.ListPackagesAsync(_applicationLifetime.Token, progress),
                _applicationLifetime.Token);
            page.SetPackages(packages);
            var success = _dashboard.Text.PackageOperationSucceeded(packageName, result.Outcome);
            SetPackageStatus(page, success);
            page.SetOperationResult(success, isSuccess: true);
            return true;
        }
        catch (OperationCanceledException)
        {
            SetPackageStatus(page, _dashboard.Text.OperationCanceled);
            page.SetOperationResult(_dashboard.Text.OperationCanceled, isSuccess: false);
            return false;
        }
        catch (Exception exception) when (exception is IOException or JsonException or InvalidOperationException or UnauthorizedAccessException)
        {
            var failure = _dashboard.Text.PackageOperationFailed(exception.Message);
            SetPackageStatus(page, failure);
            page.SetOperationResult(failure, isSuccess: false);
            return false;
        }
        finally
        {
            page.SetBusy(false);
            _dashboard.GlobalOperation.End();
        }
    }

    private async void PackageManager_RemoveRequested(object? sender, PackageRemoveRequestedEventArgs e)
    {
        if (GetPackageManagerView(sender, e) is { Page: { } page })
        {
            await RemovePackageAsync(GetPackageManagerService(page.Kind), page, e.PackageName);
        }
    }

    private IProjectPackageManagerService GetPackageManagerService(PackageManagerKind kind) => kind switch
    {
        PackageManagerKind.Composer => _composerPackageManager,
        PackageManagerKind.Node => _nodePackageManager,
        PackageManagerKind.Python => _pythonPackageManager,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    private static PackageManagerView? GetPackageManagerView(object? sender, EventArgs e) =>
        e is RoutedEventArgs { OriginalSource: PackageManagerView source }
            ? source
            : sender as PackageManagerView;

    private async Task RemovePackageAsync(
        IProjectPackageManagerService service,
        PackageManagerPageViewModel page,
        string packageName)
    {
        if (!page.CanOperate || _dashboard.GlobalOperation.IsBusy)
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
        _dashboard.GlobalOperation.Begin();
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

    private void SetPackageStatus(PackageManagerPageViewModel page, string status) =>
        page.SetStatus(status);
}
