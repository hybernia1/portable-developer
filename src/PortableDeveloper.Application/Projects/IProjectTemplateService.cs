namespace PortableDeveloper.Application.Projects;

public interface IProjectTemplateService
{
    Task<ProjectTemplateResult> CreateAsync(
        ProjectTemplateRequest request,
        CancellationToken cancellationToken = default);

    IReadOnlyList<ManagedProjectDirectoryCandidate> GetRegistrableDirectories();

    Task<PortableProject> RegisterExistingAsync(
        string directoryId,
        string displayName,
        CancellationToken cancellationToken = default);
}

public sealed record ProjectTemplateRequest(
    string Name,
    ProjectTemplateKind Template,
    string WebRootRelativePath = "public");

public sealed record ProjectTemplateResult(
    PortableProject Project,
    IReadOnlyList<string> CreatedRelativePaths);

public sealed record ManagedProjectDirectoryCandidate(
    string DirectoryId,
    string RootRelativePath);

public enum ProjectTemplateKind
{
    Empty,
    Web,
    Python,
    BrowserAutomation,
    NodeJs
}
