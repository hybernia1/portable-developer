using PortableDeveloper.Application.Projects;

namespace PortableDeveloper.Infrastructure.Projects;

/// <summary>
/// Keeps the existing Apache project UI usable while project-scoped consumers move to the
/// general context. It is removed when the Projects page adopts <see cref="JsonProjectCatalog"/>.
/// </summary>
public sealed class LegacyWebProjectCatalogAdapter : IProjectCatalog
{
    private readonly IWebProjectCatalog _legacy;

    public LegacyWebProjectCatalogAdapter(IWebProjectCatalog legacy)
    {
        _legacy = legacy;
    }

    public IReadOnlyList<PortableProject> Projects => _legacy.Projects.Select(Convert).ToArray();

    public string ActiveProjectId => _legacy.ActiveProject.Id;

    public PortableProject GetRequired(string projectId) => Convert(_legacy.Projects.FirstOrDefault(project =>
        string.Equals(project.Id, projectId?.Trim(), StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException("The project does not exist.", nameof(projectId)));

    public void SetActive(string projectId) => _legacy.SetActive(projectId);

    public void Remove(string projectId) => _legacy.Remove(projectId);

    public void Add(PortableProject project, bool makeActive = true) =>
        throw new NotSupportedException("Project creation remains owned by the legacy web-project UI during Stage 2.");

    public void Update(PortableProject project) =>
        throw new NotSupportedException("Project editing remains owned by the legacy web-project UI during Stage 2.");

    private static PortableProject Convert(WebProject project) => new(
        project.Id,
        project.Name,
        project.ProjectRootRelativePath,
        new ProjectWebSettings(project.IsEnabled, project.WebRootRelativePath, project.AllowHtaccess));
}
