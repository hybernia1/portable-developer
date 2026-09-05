namespace PortableDeveloper.Application.Projects;

public sealed record ProjectCatalogDocument(
    int SchemaVersion,
    string ActiveProjectId,
    IReadOnlyList<PortableProject> Projects);
