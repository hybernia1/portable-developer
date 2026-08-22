namespace PortableDeveloper.Application.ProjectTools;

public sealed record ProjectPackageInfo(
    string Name,
    string Version,
    string Description = "",
    bool IsDirectDependency = false);
