namespace PortableDeveloper.Application.Workspace;

public sealed record WorkspaceEntry(
    string Name,
    string RelativePath,
    bool IsDirectory,
    long? SizeBytes,
    DateTime LastWriteTime,
    bool IsSafe);
