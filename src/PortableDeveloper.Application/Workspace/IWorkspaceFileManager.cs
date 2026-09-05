namespace PortableDeveloper.Application.Workspace;

public interface IWorkspaceFileManager
{
    string RootRelativePath { get; }

    IReadOnlyList<WorkspaceEntry> List(string relativeDirectory);

    WorkspacePage ListPage(WorkspacePageRequest request);

    string NormalizeDirectory(string relativeDirectory);

    void CreateFile(string relativeDirectory, string name);

    void CreateDirectory(string relativeDirectory, string name);

    void Rename(string relativePath, string newName);

    bool Copy(
        string sourceRelativePath,
        string destinationRelativeDirectory,
        string copyNameSuffix,
        Func<WorkspaceConflict, WorkspaceConflictDecision> resolveConflict);

    bool Move(
        string sourceRelativePath,
        string destinationRelativeDirectory,
        string copyNameSuffix,
        Func<WorkspaceConflict, WorkspaceConflictDecision> resolveConflict);

    string GetExportPath(string relativePath);

    bool TryGetRelativePath(string absolutePath, out string relativePath);

    int Import(
        IReadOnlyList<string> sourcePaths,
        string destinationRelativeDirectory,
        string copyNameSuffix,
        Func<WorkspaceConflict, WorkspaceConflictDecision> resolveConflict);

    void Delete(string relativePath);
}
