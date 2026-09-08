using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using PortableDeveloper.Application.Projects;

namespace PortableDeveloper.App.ViewModels;

public sealed class ApachePageViewModel : INotifyPropertyChanged
{
    private IReadOnlyList<WebProject> _webProjectSnapshot = [];
    private ApachePageRuntimeState _runtimeState;
    private string _activeWebProjectId = WebProjectCatalogDefaults.DefaultProjectId;
    private string _activeDocumentRoot;
    private string _activeWebProjectName;

    public ApachePageViewModel(UiText text)
    {
        Text = text;
        _runtimeState = ApachePageRuntimeState.CreateDefault(text);
        _activeDocumentRoot = Text.NotServedByApache;
        _activeWebProjectName = Text.DefaultProjectName;
        Text.PropertyChanged += Text_PropertyChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UiText Text { get; }

    public ServiceCardViewModel ApacheService => _runtimeState.ApacheService;

    public string ApacheHeaderDetail => ApacheService.State == Text.Failed
        ? ApacheService.Detail
        : string.Empty;

    public bool ApacheActionEnabled => _runtimeState.ApacheActionEnabled;

    public bool ApacheIsRunning => _runtimeState.ApacheIsRunning;

    public string ApacheActionLabel => _runtimeState.ApacheActionLabel;

    public int ApachePort => _runtimeState.ApachePort;

    public string ActiveDocumentRoot
    {
        get => _activeDocumentRoot;
        private set => SetField(ref _activeDocumentRoot, value);
    }

    public string ActiveWebProjectName
    {
        get => _activeWebProjectName;
        private set => SetField(ref _activeWebProjectName, value);
    }

    public void SetRuntimeState(ApachePageRuntimeState state)
    {
        if (_runtimeState == state)
        {
            return;
        }

        _runtimeState = state;
        OnPropertyChanged(nameof(ApacheService));
        OnPropertyChanged(nameof(ApacheHeaderDetail));
        OnPropertyChanged(nameof(ApacheActionEnabled));
        OnPropertyChanged(nameof(ApacheIsRunning));
        OnPropertyChanged(nameof(ApacheActionLabel));
        OnPropertyChanged(nameof(ApachePort));
    }

    public void SetWebProjects(IEnumerable<WebProject> projects, string activeProjectId)
    {
        _webProjectSnapshot = projects.ToArray();
        _activeWebProjectId = activeProjectId;
        RefreshActiveWebProject();
    }

    private void RefreshActiveWebProject()
    {
        var activeProject = _webProjectSnapshot.FirstOrDefault(project =>
            string.Equals(project.Id, _activeWebProjectId, StringComparison.OrdinalIgnoreCase));
        ActiveWebProjectName = activeProject is null
            ? Text.DefaultProjectName
            : activeProject.Id == WebProjectCatalogDefaults.DefaultProjectId
                ? Text.DefaultProjectName
                : activeProject.Name;
        ActiveDocumentRoot = activeProject is not { IsEnabled: true }
            ? Text.NotServedByApache
            : activeProject.WebRootRelativePath == "."
                ? activeProject.ProjectRootRelativePath
                : Path.Combine(activeProject.ProjectRootRelativePath, activeProject.WebRootRelativePath);
    }

    private void Text_PropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshActiveWebProject();

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

public sealed record ApachePageRuntimeState(
    ServiceCardViewModel ApacheService,
    bool ApacheActionEnabled,
    bool ApacheIsRunning,
    string ApacheActionLabel,
    int ApachePort)
{
    public static ApachePageRuntimeState CreateDefault(UiText text) => new(
        new ServiceCardViewModel("Apache", string.Empty, text.ModuleNotFound, text.NotInstalled),
        ApacheActionEnabled: false,
        ApacheIsRunning: false,
        string.Empty,
        ApachePort: 0);
}
