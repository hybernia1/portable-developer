namespace PortableDeveloper.Application.Workspace;

public enum WorkspaceConflictAction
{
    Overwrite,
    Rename,
    Skip,
    Cancel
}

public sealed record WorkspaceConflict(
    string Name,
    string DestinationRelativePath,
    bool IsDirectory);

public sealed record WorkspaceConflictDecision(
    WorkspaceConflictAction Action,
    bool ApplyToRemaining = false);
