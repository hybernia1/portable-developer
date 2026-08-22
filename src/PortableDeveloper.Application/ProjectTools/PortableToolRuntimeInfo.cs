namespace PortableDeveloper.Application.ProjectTools;

public sealed record PortableToolRuntimeInfo(
    PortableToolKind Kind,
    bool IsReady,
    string Version,
    string EntrypointRelativePath,
    string Detail);
