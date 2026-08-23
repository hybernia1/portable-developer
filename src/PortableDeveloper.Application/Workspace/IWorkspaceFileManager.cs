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

    void Delete(string relativePath);
}
