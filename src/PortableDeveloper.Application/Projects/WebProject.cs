using System.Text.Json.Serialization;

namespace PortableDeveloper.Application.Projects;

public sealed record WebProject(
    string Id,
    string Name,
    string ProjectRootRelativePath,
    string WebRootRelativePath,
    bool AllowHtaccess = true,
    bool IsEnabled = true)
{
    [JsonIgnore]
    public string HostName => Id == WebProjectCatalogDefaults.DefaultProjectId
        ? "localhost"
        : $"{Id}.localhost";

    [JsonIgnore]
    public string DocumentRootRelativePath => WebRootRelativePath == "."
        ? ProjectRootRelativePath
        : Path.Combine(ProjectRootRelativePath, WebRootRelativePath);
}
