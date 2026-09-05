namespace PortableDeveloper.Application.Projects;

public interface IProjectCatalog
{
    IReadOnlyList<PortableProject> Projects { get; }

    string ActiveProjectId { get; }

    PortableProject GetRequired(string projectId);

    void Add(PortableProject project, bool makeActive = true);

    void Update(PortableProject project);

    void SetActive(string projectId);

    void Remove(string projectId);
}
