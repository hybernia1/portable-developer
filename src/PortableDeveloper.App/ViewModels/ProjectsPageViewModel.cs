using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using PortableDeveloper.App.Shell;
using PortableDeveloper.Application.Projects;

namespace PortableDeveloper.App.ViewModels;

public sealed class ProjectsPageViewModel : INotifyPropertyChanged
{
    private readonly string _portableRoot;
    private readonly WorkspaceShellViewModel _shell;
    private IReadOnlyDictionary<string, ProjectCapabilitySnapshot> _capabilitySnapshots =
        new Dictionary<string, ProjectCapabilitySnapshot>(StringComparer.OrdinalIgnoreCase);
    private IReadOnlyList<PortableProject> _projectSnapshot = [];
    private ProjectsPageRuntimeState _runtimeState = ProjectsPageRuntimeState.Default;
    private string _existingProjectName = string.Empty;
    private string? _selectedExistingDirectoryId;
    private ProjectViewModel? _selectedProject;
    private ProjectTemplateKind _selectedTemplateKind = ProjectTemplateKind.Empty;
    private string _newProjectName = string.Empty;
    private string _statusText = string.Empty;

    public ProjectsPageViewModel(UiText text, WorkspaceShellViewModel shell, string portableRoot)
    {
        Text = text;
        _shell = shell;
        _portableRoot = portableRoot;
        ProjectTemplates = [];
        RegistrableProjectDirectories = [];
        RefreshProjectTemplates();
        _shell.PropertyChanged += Shell_PropertyChanged;
        Text.PropertyChanged += Text_PropertyChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UiText Text { get; }

    public NavigationSection SelectedSection => _shell.SelectedSection;

    public ObservableCollection<ProjectViewModel> Projects => _shell.Projects;

    public ObservableCollection<ProjectTemplateChoiceViewModel> ProjectTemplates { get; }

    public ObservableCollection<ManagedProjectDirectoryCandidate> RegistrableProjectDirectories { get; }

    public bool NoRegistrableProjectDirectories => RegistrableProjectDirectories.Count == 0;

    public bool WebConfigurationRestartPromptVisible => _runtimeState.WebConfigurationRestartPromptVisible;

    public bool WebConfigurationApplyEnabled => _runtimeState.WebConfigurationApplyEnabled;

    public bool ApacheIsRunning => _runtimeState.ApacheIsRunning;

    public ProjectViewModel? SelectedProject
    {
        get => _selectedProject;
        set => SetField(ref _selectedProject, value);
    }

    public string NewProjectName
    {
        get => _newProjectName;
        set => SetField(ref _newProjectName, value);
    }

    public ProjectTemplateKind SelectedTemplateKind
    {
        get => _selectedTemplateKind;
        set => SetField(ref _selectedTemplateKind, value);
    }

    public string? SelectedExistingDirectoryId
    {
        get => _selectedExistingDirectoryId;
        set => SetField(ref _selectedExistingDirectoryId, value);
    }

    public string ExistingProjectName
    {
        get => _existingProjectName;
        set => SetField(ref _existingProjectName, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public void SetRuntimeState(ProjectsPageRuntimeState state)
    {
        if (_runtimeState == state)
        {
            return;
        }

        var projectReadinessChanged = _runtimeState.ApacheReady != state.ApacheReady
            || _runtimeState.PhpReady != state.PhpReady
            || _runtimeState.NodeReady != state.NodeReady
            || _runtimeState.PythonReady != state.PythonReady
            || _runtimeState.SeleniumReady != state.SeleniumReady;
        _runtimeState = state;
        OnPropertyChanged(nameof(WebConfigurationRestartPromptVisible));
        OnPropertyChanged(nameof(WebConfigurationApplyEnabled));
        OnPropertyChanged(nameof(ApacheIsRunning));
        if (projectReadinessChanged)
        {
            RefreshProjects();
        }
    }

    public void SetProjects(
        IEnumerable<PortableProject> projects,
        string activeProjectId,
        IReadOnlyDictionary<string, ProjectCapabilitySnapshot>? capabilities = null)
    {
        _projectSnapshot = projects.ToArray();
        _capabilitySnapshots = capabilities?.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.OrdinalIgnoreCase)
            ?? new Dictionary<string, ProjectCapabilitySnapshot>(StringComparer.OrdinalIgnoreCase);
        RefreshProjects(activeProjectId);
    }

    public void SetRegistrableProjectDirectories(IEnumerable<ManagedProjectDirectoryCandidate> directories)
    {
        RegistrableProjectDirectories.Clear();
        foreach (var directory in directories)
        {
            RegistrableProjectDirectories.Add(directory);
        }

        OnPropertyChanged(nameof(NoRegistrableProjectDirectories));
    }

    public void ResetCreateForm()
    {
        NewProjectName = string.Empty;
        SelectedTemplateKind = ProjectTemplateKind.Empty;
    }

    public void ResetRegistrationForm() => ExistingProjectName = string.Empty;

    public void SetStatus(string status) => StatusText = status;

    private void RefreshProjects(string? activeProjectId = null)
    {
        activeProjectId ??= _shell.ActiveProjectId;
        var selectedProjectId = SelectedProject?.Id;
        if (!string.Equals(_shell.ActiveProjectId, activeProjectId, StringComparison.OrdinalIgnoreCase))
        {
            selectedProjectId = activeProjectId;
        }

        Projects.Clear();
        foreach (var project in _projectSnapshot)
        {
            _capabilitySnapshots.TryGetValue(project.Id, out var capabilitySnapshot);
            Projects.Add(ProjectViewModel.From(
                project,
                activeProjectId,
                _portableRoot,
                Text,
                capabilitySnapshot,
                GetMissingSharedRuntimes(capabilitySnapshot)));
        }

        _shell.SetActiveProjectId(activeProjectId);
        SelectedProject = Projects.FirstOrDefault(project =>
                              string.Equals(project.Id, selectedProjectId, StringComparison.OrdinalIgnoreCase))
                          ?? Projects.FirstOrDefault(project => project.IsActive)
                          ?? Projects.FirstOrDefault();
    }

    private void RefreshProjectTemplates()
    {
        ProjectTemplates.Clear();
        foreach (var kind in Enum.GetValues<ProjectTemplateKind>())
        {
            ProjectTemplates.Add(new ProjectTemplateChoiceViewModel(
                kind,
                Text.ProjectTemplateName(kind),
                Text.ProjectTemplateDescription(kind)));
        }
    }

    private IReadOnlyList<string> GetMissingSharedRuntimes(ProjectCapabilitySnapshot? snapshot)
    {
        if (snapshot is null)
        {
            return [];
        }

        var missing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var capability in snapshot.Capabilities.Select(capability => capability.Kind))
        {
            switch (capability)
            {
                case ProjectCapabilityKind.Web when !_runtimeState.ApacheReady:
                    missing.Add("Apache");
                    break;
                case ProjectCapabilityKind.Php when !_runtimeState.PhpReady:
                    missing.Add("PHP");
                    break;
                case ProjectCapabilityKind.NodeJs when !_runtimeState.NodeReady:
                    missing.Add("Node.js");
                    break;
                case ProjectCapabilityKind.Python when !_runtimeState.PythonReady:
                    missing.Add("Python");
                    break;
                case ProjectCapabilityKind.BrowserAutomation when !_runtimeState.SeleniumReady:
                    missing.Add("Selenium");
                    break;
            }
        }

        return missing.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private void Shell_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName)
            || e.PropertyName == nameof(WorkspaceShellViewModel.SelectedSection))
        {
            OnPropertyChanged(nameof(SelectedSection));
        }
    }

    private void Text_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RefreshProjectTemplates();
        RefreshProjects();
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record ProjectsPageRuntimeState(
    bool WebConfigurationRestartPromptVisible,
    bool WebConfigurationApplyEnabled,
    bool ApacheIsRunning,
    bool ApacheReady,
    bool PhpReady,
    bool NodeReady,
    bool PythonReady,
    bool SeleniumReady)
{
    public static ProjectsPageRuntimeState Default { get; } = new(
        WebConfigurationRestartPromptVisible: false,
        WebConfigurationApplyEnabled: false,
        ApacheIsRunning: false,
        ApacheReady: false,
        PhpReady: false,
        NodeReady: false,
        PythonReady: false,
        SeleniumReady: false);
}

public sealed record ProjectViewModel(
    string Id,
    string Name,
    string RootRelativePath,
    string WebStatus,
    string WebDetail,
    string Availability,
    string Capabilities,
    string RuntimeReadiness,
    bool IsActive,
    bool IsDefault,
    bool IsDirectoryAvailable,
    bool HasWebConfiguration,
    bool IsWebEnabled,
    bool AllowHtaccess,
    string HostName,
    string HtaccessStatus,
    string HtaccessAction,
    string ApacheAction)
{
    public bool CanUnregister => !IsDefault;

    public bool CanToggleWeb => HasWebConfiguration && !IsDefault;

    public static ProjectViewModel From(
        PortableProject project,
        string activeProjectId,
        string portableRoot,
        UiText text,
        ProjectCapabilitySnapshot? snapshot,
        IReadOnlyList<string> missingSharedRuntimes)
    {
        var isDefault = string.Equals(project.Id, ProjectCatalogDefaults.DefaultProjectId, StringComparison.OrdinalIgnoreCase);
        var webStatus = project.Web switch
        {
            null => text.WebNotConfigured,
            { IsEnabled: true } => text.WebEnabled,
            _ => text.WebDisabled
        };
        var webDetail = project.Web is null
            ? text.WebNotConfiguredDetail
            : text.WebRootSummary(project.Web.RootRelativePath);
        var available = Directory.Exists(Path.Combine(portableRoot, project.RootRelativePath));
        var capabilityNames = snapshot?.Capabilities
            .Select(capability => text.ProjectCapability(capability.Kind))
            .ToArray() ?? [];
        return new ProjectViewModel(
            project.Id,
            isDefault ? text.DefaultProjectName : project.Name,
            project.RootRelativePath,
            webStatus,
            webDetail,
            available ? string.Empty : text.ProjectDirectoryMissing,
            capabilityNames.Length == 0 ? text.NoCapabilitiesDetected : string.Join(" · ", capabilityNames),
            capabilityNames.Length == 0
                ? text.CapabilityDetectionHint
                : missingSharedRuntimes.Count == 0
                    ? text.SharedRuntimesReady
                    : text.MissingSharedRuntimes(missingSharedRuntimes),
            string.Equals(project.Id, activeProjectId, StringComparison.OrdinalIgnoreCase),
            isDefault,
            available,
            project.Web is not null,
            project.Web?.IsEnabled == true,
            project.Web?.AllowHtaccess == true,
            isDefault ? "localhost" : $"{project.Id}.localhost",
            text.HtaccessStatus(project.Web?.AllowHtaccess == true),
            project.Web?.AllowHtaccess == true ? text.DisableHtaccess : text.EnableHtaccess,
            project.Web?.IsEnabled == true ? text.DisableInApache : text.EnableInApache);
    }
}

public sealed record ProjectTemplateChoiceViewModel(
    ProjectTemplateKind Kind,
    string Name,
    string Description);
