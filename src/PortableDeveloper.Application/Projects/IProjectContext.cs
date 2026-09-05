namespace PortableDeveloper.Application.Projects;

public interface IProjectContext
{
    PortableProject ActiveProject { get; }

    bool IsSwitchBlocked { get; }

    ProjectSwitchBlockReason SwitchBlockReason { get; }

    event EventHandler<ProjectContextChangedEventArgs>? Changed;

    ProjectActivationResult Activate(string projectId);
}

public sealed class ProjectContextChangedEventArgs : EventArgs
{
    public ProjectContextChangedEventArgs(PortableProject previousProject, PortableProject activeProject)
    {
        PreviousProject = previousProject;
        ActiveProject = activeProject;
    }

    public PortableProject PreviousProject { get; }

    public PortableProject ActiveProject { get; }
}

public sealed record ProjectActivationResult(
    bool IsSuccess,
    ProjectSwitchBlockReason FailureReason,
    PortableProject ActiveProject);

public enum ProjectSwitchBlockReason
{
    None,
    ProjectOperation,
    InteractiveTerminal
}
