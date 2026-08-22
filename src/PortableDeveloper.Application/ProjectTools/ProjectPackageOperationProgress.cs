namespace PortableDeveloper.Application.ProjectTools;

public enum ProjectPackageOperationKind
{
    Refresh,
    Install,
    Remove
}

public enum ProjectPackageOperationPhase
{
    Preparing,
    RunningPackageManager,
    RefreshingInventory,
    Completed
}

public sealed record ProjectPackageOperationProgress(
    ProjectPackageOperationKind Operation,
    ProjectPackageOperationPhase Phase,
    string PackageName = "",
    bool IsIndeterminate = true,
    int Percentage = 0);
