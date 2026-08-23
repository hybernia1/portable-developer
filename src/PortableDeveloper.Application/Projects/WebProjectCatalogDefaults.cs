namespace PortableDeveloper.Application.Projects;

public static class WebProjectCatalogDefaults
{
    public const string DefaultProjectId = "default";

    public static WebProject DefaultProject { get; } = new(
        DefaultProjectId,
        "Default",
        Path.Combine("instances", "default", "www"),
        "public");
}
