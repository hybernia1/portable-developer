namespace PortableDeveloper.Application.Projects;

public sealed record PortableProject(
    string Id,
    string Name,
    string RootRelativePath,
    ProjectWebSettings? Web = null);
