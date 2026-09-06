using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using PortableDeveloper.App.ViewModels;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Php;
using PortableDeveloper.Application.Projects;
using PortableDeveloper.Application.Workspace;

namespace PortableDeveloper.App;

public partial class MainWindow
{

    private void OpenComposerProject_Click(object sender, RoutedEventArgs e) =>
        OpenProjectDirectory(_composerPackageManager.ProjectRelativePath, _dashboard.Composer);

    private void OpenNodeProject_Click(object sender, RoutedEventArgs e) =>
        OpenProjectDirectory(_nodePackageManager.ProjectRelativePath, _dashboard.Node);

    private async void ProjectSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_changingWebProject || sender is not ComboBox { SelectedValue: string projectId } ||
            string.Equals(projectId, _projectContext.ActiveProject.Id, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        await SelectWebProjectAsync(projectId);
    }

    private async void SelectWebProject_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string projectId })
        {
            await SelectWebProjectAsync(projectId);
        }
    }

    private async Task<bool> SelectWebProjectAsync(string projectId)
    {
        if (_changingWebProject || string.Equals(projectId, _projectContext.ActiveProject.Id, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var requestedProject = _projects.GetRequired(projectId);
        if (!Directory.Exists(_paths.Resolve(requestedProject.RootRelativePath)))
        {
            RefreshWebProjectBindings();
            InstallationStatusText.Text = _dashboard.Text.ProjectDirectoryUnavailable;
            return false;
        }

        if (!CanChangeWebProject())
        {
            RefreshWebProjectBindings();
            InstallationStatusText.Text = _dashboard.Text.ProjectChangeBusy;
            return false;
        }

        _changingWebProject = true;
        try
        {
            var activation = _projectContext.Activate(projectId);
            if (!activation.IsSuccess)
            {
                RefreshWebProjectBindings();
                InstallationStatusText.Text = _dashboard.Text.ProjectChangeBusy;
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
            await RefreshPackageManagerAsync(_composerPackageManager, _dashboard.Composer);
            await RefreshPackageManagerAsync(_nodePackageManager, _dashboard.Node);
            InstallationStatusText.Text = _dashboard.Text.ProjectSelected(_dashboard.ActiveWebProjectName);
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "projects",
                "project.selected",
                $"project={_projectContext.ActiveProject.Id}");
            return true;
        }
        catch (Exception exception) when (exception is IOException or ArgumentException or InvalidOperationException or UnauthorizedAccessException)
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectOperationFailed(exception.Message);
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

    private async void OpenProjectFiles_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string projectId } || !await SelectWebProjectAsync(projectId))
        {
            return;
        }

        _dashboard.SelectedPage = NavigationPage.Files;
        RefreshWorkspaceFiles();
        InstallationStatusText.Text = DisplayTerminalPath(_workspaceDirectory);
    }

    private async void OpenProjectTerminal_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string projectId } || !await SelectWebProjectAsync(projectId))
        {
            return;
        }

        _dashboard.SelectedPage = NavigationPage.Terminal;
        TerminalConsoleTextBox.Focus();
    }

    private void OpenManagedProject_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string projectId })
        {
            OpenProjectDirectory(_projects.GetRequired(projectId).RootRelativePath);
        }
    }

    private void RenameProject_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string projectId })
        {
            return;
        }

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
            InstallationStatusText.Text = _dashboard.Text.ProjectRenamed(renamed.Name);
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "projects",
                "project.renamed",
                $"project={project.Id}");
        }
        catch (Exception exception) when (exception is IOException or InvalidDataException or ArgumentException or UnauthorizedAccessException)
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectOperationFailed(exception.Message);
        }
    }

    private async void UnregisterProject_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string projectId })
        {
            return;
        }

        var project = _projects.GetRequired(projectId);
        if (!CanChangeWebProject())
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectChangeBusy;
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
                !await SelectWebProjectAsync(ProjectCatalogDefaults.DefaultProjectId))
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
            _ = _logger.LogAsync(
                ApplicationLogLevel.Information,
                "projects",
                "project.unregistered",
                $"project={project.Id}; filesPreserved=true");
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or UnauthorizedAccessException)
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectOperationFailed(exception.Message);
        }
    }

    private async void CreateGeneralProject_Click(object sender, RoutedEventArgs e)
    {
        if (ProjectTemplateSelector.SelectedValue is not ProjectTemplateKind template)
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectOperationFailed(_dashboard.Text.ProjectTemplate);
            return;
        }

        if (!CanChangeWebProject())
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectChangeBusy;
            return;
        }

        try
        {
            var result = await _projectTemplateService.CreateAsync(
                new ProjectTemplateRequest(GeneralProjectNameTextBox.Text, template),
                _applicationLifetime.Token);
            GeneralProjectNameTextBox.Clear();
            ProjectTemplateSelector.SelectedValue = ProjectTemplateKind.Empty;
            ResetProjectTools();
            RefreshWebProjectBindings();
            await RefreshProjectCapabilitiesAsync();
            InstallationStatusText.Text = _dashboard.Text.ProjectCreatedWithoutDownloads(result.Project.Name);
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
            InstallationStatusText.Text = _dashboard.Text.ProjectOperationFailed(exception.Message);
        }
    }

    private async void RegisterExistingProject_Click(object sender, RoutedEventArgs e)
    {
        if (ExistingProjectDirectorySelector.SelectedValue is not string directoryId)
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectOperationFailed(_dashboard.Text.NoExistingProjectDirectories);
            return;
        }

        if (!CanChangeWebProject())
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectChangeBusy;
            return;
        }

        try
        {
            var displayName = string.IsNullOrWhiteSpace(ExistingProjectNameTextBox.Text)
                ? directoryId
                : ExistingProjectNameTextBox.Text;
            var project = await _projectTemplateService.RegisterExistingAsync(
                directoryId,
                displayName,
                _applicationLifetime.Token);
            ExistingProjectNameTextBox.Clear();
            ResetProjectTools();
            RefreshWebProjectBindings();
            await RefreshProjectCapabilitiesAsync();
            InstallationStatusText.Text = _dashboard.Text.ProjectRegistered(project.Name);
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
            InstallationStatusText.Text = _dashboard.Text.ProjectOperationFailed(exception.Message);
            RefreshWebProjectBindings();
        }
    }

    private void ConfigureProjectWeb_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string projectId })
        {
            return;
        }

        if (!_dashboard.PhpSettingsEnabled)
        {
            InstallationStatusText.Text = _dashboard.Text.ProjectChangeBusy;
            return;
        }

        var project = _projects.GetRequired(projectId);
        var initialSettings = project.Web ?? new ProjectWebSettings(true, "public", true);
        var dialog = new ProjectWebSettingsDialog(
            this,
            _dashboard.Text.ConfigureWebProject,
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
            InstallationStatusText.Text = _dashboard.Text.ProjectOperationFailed(exception.Message);
        }
    }

    private void OpenWebProjectUrl_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string projectId })
        {
            var hostName = string.Equals(projectId, ProjectCatalogDefaults.DefaultProjectId, StringComparison.OrdinalIgnoreCase)
                ? "localhost"
                : $"{projectId}.localhost";
            Process.Start(new ProcessStartInfo($"http://{hostName}:{_portSettings.ApachePort}/") { UseShellExecute = true });
        }
    }

    private void RecordWebConfigurationChange(bool affectsRunningConfiguration = true)
    {
        if (_dashboard.ApacheIsRunning &&
            (affectsRunningConfiguration || _dashboard.WebConfigurationRestartRequired))
        {
            _dashboard.SetWebConfigurationRestartRequired(true);
            InstallationStatusText.Text = _dashboard.Text.WebConfigurationRestartPending;
            return;
        }

        InstallationStatusText.Text = _dashboard.Text.WebConfigurationSavedForNextStart;
    }

    private async void ApplyWebConfiguration_Click(object sender, RoutedEventArgs e)
    {
        if (!_dashboard.WebConfigurationApplyEnabled || !await RestartApacheAsync(announce: false))
        {
            return;
        }

        InstallationStatusText.Text = _dashboard.Text.WebConfigurationApplied;
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
        _dashboard.SetProjects(_projects.Projects, _projectContext.ActiveProject.Id, _projectCapabilitySnapshots);
        _dashboard.SetRegistrableProjectDirectories(_projectTemplateService.GetRegistrableDirectories());
        _dashboard.SetWebProjects(_webProjects.Projects, _projectContext.ActiveProject.Id);
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

        return _dashboard.Composer.IsBusy || _dashboard.Node.IsBusy
            ? ProjectSwitchBlockReason.ProjectOperation
            : ProjectSwitchBlockReason.None;
    }

    private void OpenPythonProject_Click(object sender, RoutedEventArgs e) =>
        OpenProjectDirectory(_pythonPackageManager.ProjectRelativePath, _dashboard.Python);

    private async void EditCustomPhpIni_Click(object sender, RoutedEventArgs e) =>
        await OpenPortableFileAsync(
            PhpCustomIni.GetRelativePath("default"),
            Path.Combine("instances", "default", "config"),
            PortableFileLaunchIntent.Edit,
            PhpCustomIni.InitialContent);

    private void OpenProjectDirectory(string relativePath, PackageManagerPageViewModel page)
    {
        var folder = _paths.EnsureDirectory(relativePath);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
        SetPackageStatus(page, relativePath);
    }

    private void OpenProjectDirectory(string relativePath)
    {
        var folder = _paths.EnsureDirectory(relativePath);
        Process.Start(new ProcessStartInfo("explorer.exe", $"\"{folder}\"") { UseShellExecute = true });
        InstallationStatusText.Text = relativePath;
    }
}
