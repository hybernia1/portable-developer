namespace PortableDeveloper.Application.Projects;

public static class ProjectCatalogDefaults
{
    public const int CurrentSchemaVersion = 2;
    public const string DefaultProjectId = "default";

    public static PortableProject DefaultProject { get; } = new(
        DefaultProjectId,
        "Default",
        Path.Combine("instances", "default", "www"),
        new ProjectWebSettings(true));

    public static ProjectCatalogDocument DefaultDocument { get; } = new(
        CurrentSchemaVersion,
        DefaultProjectId,
        [DefaultProject]);
}
