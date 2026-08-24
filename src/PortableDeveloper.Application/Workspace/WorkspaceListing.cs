namespace PortableDeveloper.Application.Workspace;

public enum WorkspaceFileKind
{
    Folder,
    File,
    Php,
    Python,
    JavaScript,
    StyleSheet,
    Html,
    Xml,
    Json,
    Yaml,
    Markdown,
    Text,
    Document,
    Spreadsheet,
    Configuration,
    Image,
    Archive,
    Database,
    Executable
}

public enum WorkspaceSortColumn
{
    Name,
    Type,
    Size,
    Modified
}

public enum WorkspaceSortDirection
{
    Ascending,
    Descending
}

public sealed record WorkspacePageRequest(
    string RelativeDirectory,
    int PageNumber = 1,
    int PageSize = 50,
    WorkspaceSortColumn SortColumn = WorkspaceSortColumn.Name,
    WorkspaceSortDirection SortDirection = WorkspaceSortDirection.Ascending);

public sealed record WorkspacePage(
    IReadOnlyList<WorkspaceEntry> Entries,
    int PageNumber,
    int PageSize,
    int TotalCount)
{
    public int TotalPages => TotalCount == 0 ? 1 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
