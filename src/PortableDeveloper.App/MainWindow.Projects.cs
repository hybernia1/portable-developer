using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using PortableDeveloper.App.ViewModels;
using PortableDeveloper.App.Views;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Php;
using PortableDeveloper.Application.Projects;
using PortableDeveloper.Application.Workspace;

namespace PortableDeveloper.App;

public partial class MainWindow
{
    private void ProjectSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_changingWebProject || sender is not ComboBox { SelectedValue: string projectId } ||
            string.Equals(projectId, _projectContext.ActiveProject.Id, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        SelectWebProject(projectId, _dashboard.Shell.SetProjectContextStatus);
    }

    private bool SelectWebProject(string projectId, Action<string>? statusSink = null)
    {
        statusSink ??= _dashboard.Shell.SetProjectContextStatus;
        if (_changingWebProject || string.Equals(projectId, _projectContext.ActiveProject.Id, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var requestedProject = _projects.GetRequired(projectId);
        if (!Directory.Exists(_paths.Resolve(requestedProject.RootRelativePath)))
        {
            RefreshWebProjectBindings();
            statusSink(_dashboard.Text.ProjectDirectoryUnavailable);
            return false;
        }

        if (!CanChangeWebProject())
        {
            RefreshWebProjectBindings();
            statusSink(_dashboard.Text.ProjectChangeBusy);
            return false;
        }

        _changingWebProject = true;
        try
        {
            var activation = _projectContext.Activate(projectId);
            if (!activation.IsSuccess)
            {
                RefreshWebProjectBindings();
                statusSink(_dashboard.Text.ProjectChangeBusy);
                return false;
            }
            RefreshWebProjectBindings();
            _workspaceDirectory = string.Empty;
            _workspaceClipboard = null;
            _workspaceHistory.Clear();
            _workspacePageNumber = 1;
            _terminalWorkingDirectory = _terminalService.InitialWorkingDirectory;
            ResetTerminalConsole();
            RefreshWorkspaceFiles();
            statusSink(string.Empty);
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "projects",
                "project.selected",
                $"project={_projectContext.ActiveProject.Id}");
            return true;
        }
        catch (Exception exception) when (exception is IOException or ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        {
            statusSink(_dashboard.Text.ProjectOperationFailed(exception.Message));
            RefreshWebProjectBindings();
            return false;
        }
        finally
        {
            _changingWebProject = false;
        }
    }

    private void ManageProjects_Click(object sender, RoutedEventArgs e) =>
        _dashboard.SelectedPage = NavigationPage.Projects;

    private void ProjectsPage_ProjectActionRequested(object? sender, ProjectActionRequestedEventArgs e)
    {
        e.Handled = true;
        switch (e.Action)
        {
            case ProjectAction.OpenDirectory:
                OpenManagedProject(e.ProjectId);
                break;
            case ProjectAction.OpenFiles:
                OpenProjectFiles(e.ProjectId);
                break;
            case ProjectAction.OpenTerminal:
                OpenProjectTerminal(e.ProjectId);
                break;
            case ProjectAction.OpenWebUrl:
                OpenWebProjectUrl(e.ProjectId);
                break;
            case ProjectAction.ConfigureWeb:
                ConfigureProjectWeb(e.ProjectId);
                break;
            case ProjectAction.Rename:
                RenameProject(e.ProjectId);
                break;
            case ProjectAction.Unregister:
                UnregisterProject(e.ProjectId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e.Action), e.Action, null);
        }
    }

    private void OpenProjectFiles(string projectId)
    {
        if (!SelectWebProject(projectId, _dashboard.ProjectsPage.SetStatus))
        {
            return;
        }

        _dashboard.SelectedPage = NavigationPage.Files;
        RefreshWorkspaceFiles();
        _dashboard.ProjectsPage.SetStatus(string.Empty);
    }

    private void OpenProjectTerminal(string projectId)
    {
        if (!SelectWebProject(projectId, _dashboard.ProjectsPage.SetStatus))
        {
            return;
        }

        _dashboard.SelectedPage = NavigationPage.Terminal;
        _dashboard.TerminalPage.RequestFocus();
    }

    private void OpenManagedProject(string projectId)
    {
        OpenProjectDirectory(_projects.GetRequired(projectId).RootRelativePath);
    }

    private void RenameProject(string projectId)
    {
        var project = _projects.GetRequired(projectId);
        var dialog = new NamePromptDialog(
            this,
            _dashboard.Text.RenameProject,
            _dashboard.Text.RenameProjectPrompt,
            _dashboard.Text.RenameProject,
            _dashboard.Text.Cancel,
            _dashboard.Text.RenameProjectValidation,
            project.Name);
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var renamed = project with { Name = dialog.ItemName };
            ProjectCatalogValidator.ValidateProject(renamed);
            _projects.Update(renamed);
            RefreshWebProjectBindings();
            _dashboard.ProjectsPage.SetStatus(string.Empty);
            ShowTransientNotification(_dashboard.Text.ProjectRenamed(renamed.Name));
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "projects",
                "project.renamed",
                $"project={project.Id}");
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException or UnauthorizedAccessException)
        {
            _dashboard.ProjectsPage.SetStatus(_dashboard.Text.ProjectOperationFailed(exception.Message));
        }
    }

    private void UnregisterProject(string projectId)
    {
        var project = _projects.GetRequired(projectId);
        if (!CanChangeWebProject())
        {
            _dashboard.ProjectsPage.SetStatus(_dashboard.Text.ProjectChangeBusy);
            return;
        }

        if (!ConfirmationDialog.Show(
                this,
                _dashboard.Text.UnregisterProject,
                _dashboard.Text.UnregisterProjectQuestion(project.Name),
                _dashboard.Text.UnregisterProject,
                _dashboard.Text.Cancel))
        {
            return;
        }

        try
        {
            if (string.Equals(project.Id, _projectContext.ActiveProject.Id, StringComparison.OrdinalIgnoreCase) &&
                !SelectWebProject(ProjectCatalogDefaults.DefaultProjectId, _dashboard.ProjectsPage.SetStatus))
            {
                return;
            }

            foreach (var scheduledTask in _taskScheduler.GetTasks(project.Id)
                         .Where(snapshot => snapshot.Definition.IsEnabled))
            {
                _taskScheduler.Update(scheduledTask.Definition with { IsEnabled = false });
            }

            _projects.Remove(project.Id);
            RefreshWebProjectBindings();
            if (project.Web?.IsEnabled == true)
            {
                RecordWebConfigurationChange();
            }
            else
            {
                _dashboard.ProjectsPage.SetStatus(string.Empty);
                ShowTransientNotification(_dashboard.Text.ProjectUnregistered(project.Name));
            }
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "projects",
                "project.unregistered",
                $"project={project.Id}; filesPreserved=true");
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            _dashboard.ProjectsPage.SetStatus(_dashboard.Text.ProjectOperationFailed(exception.Message));
        }
    }

    private async void CreateGeneralProject_Click(object sender, RoutedEventArgs e)
    {
        var page = _dashboard.ProjectsPage;
        var template = page.SelectedTemplateKind;

        if (!CanChangeWebProject())
        {
            page.SetStatus(_dashboard.Text.ProjectChangeBusy);
            return;
        }

        try
        {
            var result = await _projectTemplateService.CreateAsync(
                new ProjectTemplateRequest(page.NewProjectName, template),
                _applicationLifetime.Token);
            page.ResetCreateForm();
            ResetProjectTools();
            RefreshWebProjectBindings();
            await RefreshProjectCapabilitiesAsync();
            page.SetStatus(string.Empty);
            ShowTransientNotification(_dashboard.Text.ProjectCreatedWithoutDownloads(result.Project.Name));
            if (result.Project.Web?.IsEnabled == true)
            {
                RecordWebConfigurationChange();
            }
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "projects",
                "project.template.created",
                $"project={result.Project.Id}; template={template}; files={result.CreatedRelativePaths.Count}");
        }
        catch (OperationCanceledException) when (_applicationLifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        {
            page.SetStatus(_dashboard.Text.ProjectOperationFailed(exception.Message));
        }
    }

    private async void RegisterExistingProject_Click(object sender, RoutedEventArgs e)
    {
        var page = _dashboard.ProjectsPage;
        if (page.SelectedExistingDirectoryId is not { } directoryId)
        {
            page.SetStatus(_dashboard.Text.ProjectOperationFailed(_dashboard.Text.NoExistingProjectDirectories));
            return;
        }

        if (!CanChangeWebProject())
        {
            page.SetStatus(_dashboard.Text.ProjectChangeBusy);
            return;
        }

        try
        {
            var displayName = string.IsNullOrWhiteSpace(page.ExistingProjectName)
                ? directoryId
                : page.ExistingProjectName;
            var project = await _projectTemplateService.RegisterExistingAsync(
                directoryId,
                displayName,
                _applicationLifetime.Token);
            page.ResetRegistrationForm();
            ResetProjectTools();
            RefreshWebProjectBindings();
            await RefreshProjectCapabilitiesAsync();
            page.SetStatus(string.Empty);
            ShowTransientNotification(_dashboard.Text.ProjectRegistered(project.Name));
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "projects",
                "project.existing.registered",
                $"project={project.Id}; contentModified=false");
        }
        catch (OperationCanceledException) when (_applicationLifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        {
            page.SetStatus(_dashboard.Text.ProjectOperationFailed(exception.Message));
            RefreshWebProjectBindings();
        }
    }

    private void ConfigureProjectWeb(string projectId)
    {
        if (!_dashboard.Runtime.PhpSettingsEnabled)
        {
            _dashboard.ProjectsPage.SetStatus(_dashboard.Text.ProjectChangeBusy);
            return;
        }

        var project = _projects.GetRequired(projectId);
        var initialSettings = project.Web ?? new ProjectWebSettings(true, "public", true);
        var dialog = new ProjectWebSettingsDialog(
            this,
            _dashboard.Text.ConfigureWebProject,
            string.Equals(project.Id, ProjectCatalogDefaults.DefaultProjectId, StringComparison.OrdinalIgnoreCase)
                ? _dashboard.Text.DefaultProjectName
                : project.Name,
            _dashboard.Text.ConfigureWebRootPrompt,
            _dashboard.Text.ServeProjectThroughApache,
            _dashboard.Text.AllowHtaccessLabel,
            _dashboard.Text.WebSettingsHelp,
            _dashboard.Text.DefaultProjectWebRequired,
            _dashboard.Text.SaveWebConfiguration,
            _dashboard.Text.Cancel,
            _dashboard.Text.ConfigureWebRootValidation,
            initialSettings,
            !string.Equals(project.Id, ProjectCatalogDefaults.DefaultProjectId, StringComparison.OrdinalIgnoreCase));
        if (dialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            var settings = dialog.Settings;
            var affectsRunningConfiguration = project.Web?.IsEnabled == true || settings.IsEnabled;
            var result = _projectWebConfigurationService.Configure(projectId, settings);
            RefreshWebProjectBindings();
            RecordWebConfigurationChange(affectsRunningConfiguration);
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "projects",
                "project.web.configured",
                $"project={projectId}; webRoot={result.Project.Web!.RootRelativePath}; enabled={settings.IsEnabled}; allowHtaccess={settings.AllowHtaccess}; directoryCreated={result.WebRootDirectoryCreated}; starterFileCreated={result.StarterFileCreated}");
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        {
            _dashboard.ProjectsPage.SetStatus(_dashboard.Text.ProjectOperationFailed(exception.Message));
        }
    }

    private void OpenWebProjectUrl(string projectId)
    {
        var hostName = string.Equals(projectId, ProjectCatalogDefaults.DefaultProjectId, StringComparison.OrdinalIgnoreCase)
            ? "localhost"
            : $"{projectId}.localhost";
        Process.Start(new ProcessStartInfo($"http://{hostName}:{_portSettings.ApachePort}/") { UseShellExecute = true });
    }

    private void RecordWebConfigurationChange(bool affectsRunningConfiguration = true)
    {
        if (_dashboard.Runtime.ApacheIsRunning &&
            (affectsRunningConfiguration || _dashboard.Runtime.WebConfigurationRestartRequired))
        {
            _dashboard.Runtime.SetWebConfigurationRestartRequired(true);
            _dashboard.ProjectsPage.SetStatus(_dashboard.Text.WebConfigurationRestartPending);
            return;
        }

        _dashboard.ProjectsPage.SetStatus(string.Empty);
        ShowTransientNotification(_dashboard.Text.WebConfigurationSavedForNextStart);
    }

    private async void ApplyWebConfiguration_Click(object sender, RoutedEventArgs e)
    {
        if (!_dashboard.Runtime.WebConfigurationApplyEnabled ||
            !await RestartApacheAsync(announce: false, _dashboard.ProjectsPage.SetStatus))
        {
            return;
        }

        _dashboard.ProjectsPage.SetStatus(string.Empty);
        ShowTransientNotification(_dashboard.Text.WebConfigurationApplied);
    }

    private void ResetProjectTools()
    {
        _workspaceDirectory = string.Empty;
        _workspaceHistory.Clear();
        _workspacePageNumber = 1;
        _terminalWorkingDirectory = _terminalService.InitialWorkingDirectory;
        RefreshWorkspaceFiles();
        ResetTerminalConsole();
    }

    private void RefreshWebProjectBindings()
    {
        _dashboard.ProjectsPage.SetProjects(_projects.Projects, _projectContext.ActiveProject.Id, _projectCapabilitySnapshots);
        _dashboard.ProjectsPage.SetRegistrableProjectDirectories(_projectTemplateService.GetRegistrableDirectories());
        _dashboard.ApachePage.SetWebProjects(_webProjects.Projects, _projectContext.ActiveProject.Id);
        _dashboard.Composer.SetProjectRelativePath(_composerPackageManager.ProjectRelativePath);
        _dashboard.Node.SetProjectRelativePath(_nodePackageManager.ProjectRelativePath);
        RefreshScheduledTaskBindings();
    }

    private async Task RefreshProjectCapabilitiesAsync()
    {
        var revision = ++_projectCapabilityRefreshRevision;
        var projects = _projects.Projects.ToArray();
        var tasks = projects.Select(async project =>
        {
            try
            {
                return await Task.Run(
                    () => _projectCapabilityDetector.DetectAsync(project, _applicationLifetime.Token),
                    _applicationLifetime.Token);
            }
            catch (Exception exception) when (exception is IOException or InvalidDataException or UnauthorizedAccessException)
            {
                await _logger.LogAsync(
                    ApplicationLogLevel.Warning,
                    "projects",
                    "project.capabilities.failed",
                    $"project={project.Id}; detail={exception.Message}");
                return new ProjectCapabilitySnapshot(project.Id, []);
            }
        });

        try
        {
            var snapshots = await Task.WhenAll(tasks);
            if (revision != _projectCapabilityRefreshRevision || _applicationLifetime.IsCancellationRequested)
            {
                return;
            }

            _projectCapabilitySnapshots = snapshots.ToDictionary(
                snapshot => snapshot.ProjectId,
                StringComparer.OrdinalIgnoreCase);
            RefreshWebProjectBindings();
        }
        catch (OperationCanceledException) when (_applicationLifetime.IsCancellationRequested)
        {
        }
    }

    private bool CanChangeWebProject() =>
        !_projectContext.IsSwitchBlocked;

    private ProjectSwitchBlockReason GetProjectSwitchBlockReason()
    {
        if (_terminalBusy)
        {
            return ProjectSwitchBlockReason.InteractiveTerminal;
        }

        return _dashboard.Composer.IsBusy || _dashboard.Node.IsBusy || _dashboard.Python.IsBusy
            ? ProjectSwitchBlockReason.ProjectOperation
            : ProjectSwitchBlockReason.None;
    }

    private async void EditCustomPhpIni_Click(object sender, RoutedEventArgs e) =>
        await OpenPortableFileAsync(
            PhpCustomIni.GetRelativePath("default"),
            Path.Combine("instances", "default", "config"),
            PortableFileLaunchIntent.Edit,
            PhpCustomIni.InitialContent,
            _dashboard.PhpPage.SetStatus);

    private void OpenProjectDirectory(string relativePath, PackageManagerPageViewModel page)
    {
        var folder = _paths.EnsureDirectory(relativePath);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
        SetPackageStatus(page, string.Empty);
    }

    private void OpenProjectDirectory(string relativePath)
    {
        var folder = _paths.EnsureDirectory(relativePath);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
        _dashboard.ProjectsPage.SetStatus(string.Empty);
    }
}
