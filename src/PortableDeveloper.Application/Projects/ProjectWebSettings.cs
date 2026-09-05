namespace PortableDeveloper.Application.Projects;

public sealed record ProjectWebSettings(
    bool IsEnabled,
    string RootRelativePath = "public",
    bool AllowHtaccess = true);
