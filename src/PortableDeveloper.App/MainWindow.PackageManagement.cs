using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using PortableDeveloper.App.Controls;
using PortableDeveloper.App.ViewModels;
using PortableDeveloper.Application.Packages;
using PortableDeveloper.Application.ProjectTools;

namespace PortableDeveloper.App;

public partial class MainWindow
{
    private async void RefreshComposerPackages_Click(object sender, RoutedEventArgs e) =>
        await RefreshPackageManagerAsync(_composerPackageManager, _dashboard.Composer);

    private async void RefreshNodePackages_Click(object sender, RoutedEventArgs e) =>
        await RefreshPackageManagerAsync(_nodePackageManager, _dashboard.Node);

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
        _dashboard.GlobalOperation.Begin(_dashboard.Text.LoadingPackages);
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

    private async void InstallComposerPackage_Click(object sender, RoutedEventArgs e) =>
        await InstallPackageAsync(
            _composerPackageManager,
            _dashboard.Composer,
            ComposerPackageNameTextBox,
            ComposerVersionConstraintTextBox);

    private async void InstallNodePackage_Click(object sender, RoutedEventArgs e) =>
        await InstallPackageAsync(
            _nodePackageManager,
            _dashboard.Node,
            NodePackageNameTextBox,
            NodeVersionConstraintTextBox);

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
        _dashboard.GlobalOperation.Begin(initialStatus, detail: initialDetail);
        try
        {
            var versionConstraint = versionConstraintTextBox.Text.Trim();
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
                return;
            }

            var packages = await Task.Run(
                () => service.ListPackagesAsync(_applicationLifetime.Token, progress),
                _applicationLifetime.Token);
            page.SetPackages(packages);
            packageNameTextBox.Clear();
            versionConstraintTextBox.Clear();
            var success = _dashboard.Text.PackageOperationSucceeded(packageName, result.Outcome);
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

    private async void RemoveComposerPackage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string packageName })
        {
            await RemovePackageAsync(_composerPackageManager, _dashboard.Composer, packageName);
        }
    }

    private async void RemoveNodePackage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string packageName })
        {
            await RemovePackageAsync(_nodePackageManager, _dashboard.Node, packageName);
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
        _dashboard.GlobalOperation.Begin(initialStatus, detail: initialDetail);
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
            _dashboard.GlobalOperation.Update(status, progress.IsIndeterminate, progress.Percentage, detail);
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
