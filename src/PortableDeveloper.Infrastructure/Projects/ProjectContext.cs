using PortableDeveloper.Application.Projects;

namespace PortableDeveloper.Infrastructure.Projects;

public sealed class ProjectContext : IProjectContext
{
    private readonly IProjectCatalog _catalog;
    private Func<ProjectSwitchBlockReason> _blockReasonProvider = static () => ProjectSwitchBlockReason.None;

    public ProjectContext(IProjectCatalog catalog)
    {
        _catalog = catalog;
    }

    public PortableProject ActiveProject => _catalog.GetRequired(_catalog.ActiveProjectId);

    public bool IsSwitchBlocked => SwitchBlockReason != ProjectSwitchBlockReason.None;

    public ProjectSwitchBlockReason SwitchBlockReason => _blockReasonProvider();

    public event EventHandler<ProjectContextChangedEventArgs>? Changed;

    public void SetBlockReasonProvider(Func<ProjectSwitchBlockReason> blockReasonProvider)
    {
        _blockReasonProvider = blockReasonProvider ?? throw new ArgumentNullException(nameof(blockReasonProvider));
    }

    public ProjectActivationResult Activate(string projectId)
    {
        var requested = _catalog.GetRequired(projectId);
        var previous = ActiveProject;
        if (string.Equals(previous.Id, requested.Id, StringComparison.OrdinalIgnoreCase))
        {
            return new ProjectActivationResult(true, ProjectSwitchBlockReason.None, previous);
        }

        var blockReason = SwitchBlockReason;
        if (blockReason != ProjectSwitchBlockReason.None)
        {
            return new ProjectActivationResult(false, blockReason, previous);
        }

        _catalog.SetActive(requested.Id);
        var active = ActiveProject;
        Changed?.Invoke(this, new ProjectContextChangedEventArgs(previous, active));
        return new ProjectActivationResult(true, ProjectSwitchBlockReason.None, active);
    }
}
