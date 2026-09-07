using System.Windows;
using System.Windows.Controls;

namespace PortableDeveloper.App.Views;

public partial class ProjectsPageView : UserControl
{
    public static readonly RoutedEvent ApplyWebConfigurationRequestedEvent = RegisterEvent(nameof(ApplyWebConfigurationRequested));
    public static readonly RoutedEvent CreateProjectRequestedEvent = RegisterEvent(nameof(CreateProjectRequested));
    public static readonly RoutedEvent RegisterExistingProjectRequestedEvent = RegisterEvent(nameof(RegisterExistingProjectRequested));
    public static readonly RoutedEvent ProjectActionRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(ProjectActionRequested),
        RoutingStrategy.Bubble,
        typeof(EventHandler<ProjectActionRequestedEventArgs>),
        typeof(ProjectsPageView));

    public ProjectsPageView()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler ApplyWebConfigurationRequested
    {
        add => AddHandler(ApplyWebConfigurationRequestedEvent, value);
        remove => RemoveHandler(ApplyWebConfigurationRequestedEvent, value);
    }

    public event RoutedEventHandler CreateProjectRequested
    {
        add => AddHandler(CreateProjectRequestedEvent, value);
        remove => RemoveHandler(CreateProjectRequestedEvent, value);
    }

    public event RoutedEventHandler RegisterExistingProjectRequested
    {
        add => AddHandler(RegisterExistingProjectRequestedEvent, value);
        remove => RemoveHandler(RegisterExistingProjectRequestedEvent, value);
    }

    public event EventHandler<ProjectActionRequestedEventArgs> ProjectActionRequested
    {
        add => AddHandler(ProjectActionRequestedEvent, value);
        remove => RemoveHandler(ProjectActionRequestedEvent, value);
    }

    private static RoutedEvent RegisterEvent(string name) => EventManager.RegisterRoutedEvent(
        name,
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(ProjectsPageView));

    private void ApplyWebConfiguration_Click(object sender, RoutedEventArgs e) =>
        RaiseEvent(new RoutedEventArgs(ApplyWebConfigurationRequestedEvent));

    private void CreateGeneralProject_Click(object sender, RoutedEventArgs e) =>
        RaiseEvent(new RoutedEventArgs(CreateProjectRequestedEvent));

    private void RegisterExistingProject_Click(object sender, RoutedEventArgs e) =>
        RaiseEvent(new RoutedEventArgs(RegisterExistingProjectRequestedEvent));

    private void OpenManagedProject_Click(object sender, RoutedEventArgs e) => RaiseProjectAction(sender, ProjectAction.OpenDirectory);

    private void OpenProjectFiles_Click(object sender, RoutedEventArgs e) => RaiseProjectAction(sender, ProjectAction.OpenFiles);

    private void OpenProjectTerminal_Click(object sender, RoutedEventArgs e) => RaiseProjectAction(sender, ProjectAction.OpenTerminal);

    private void OpenWebProjectUrl_Click(object sender, RoutedEventArgs e) => RaiseProjectAction(sender, ProjectAction.OpenWebUrl);

    private void ConfigureProjectWeb_Click(object sender, RoutedEventArgs e) => RaiseProjectAction(sender, ProjectAction.ConfigureWeb);

    private void RenameProject_Click(object sender, RoutedEventArgs e) => RaiseProjectAction(sender, ProjectAction.Rename);

    private void UnregisterProject_Click(object sender, RoutedEventArgs e) => RaiseProjectAction(sender, ProjectAction.Unregister);

    private void RaiseProjectAction(object sender, ProjectAction action)
    {
        if (sender is Button { Tag: string projectId })
        {
            RaiseEvent(new ProjectActionRequestedEventArgs(ProjectActionRequestedEvent, projectId, action));
        }
    }
}

public sealed class ProjectActionRequestedEventArgs(
    RoutedEvent routedEvent,
    string projectId,
    ProjectAction action) : RoutedEventArgs(routedEvent)
{
    public string ProjectId { get; } = projectId;

    public ProjectAction Action { get; } = action;
}

public enum ProjectAction
{
    OpenDirectory,
    OpenFiles,
    OpenTerminal,
    OpenWebUrl,
    ConfigureWeb,
    Rename,
    Unregister
}
