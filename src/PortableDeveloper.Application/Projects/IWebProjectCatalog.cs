namespace PortableDeveloper.Application.Projects;

public interface IWebProjectCatalog
{
    IReadOnlyList<WebProject> Projects { get; }

    WebProject ActiveProject { get; }

    WebProject Create(string name, string webRootRelativePath = "public");

    void SetActive(string projectId);

    void SetHtaccess(string projectId, bool allowHtaccess);

    void SetEnabled(string projectId, bool isEnabled);

    void Remove(string projectId);
}
