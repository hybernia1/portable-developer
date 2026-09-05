namespace PortableDeveloper.Application.Projects;

public interface IProjectCapabilityDetector
{
    Task<ProjectCapabilitySnapshot> DetectAsync(
        PortableProject project,
        CancellationToken cancellationToken = default);
}

public sealed record ProjectCapabilitySnapshot(
    string ProjectId,
    IReadOnlyList<ProjectCapabilityEvidence> Capabilities);

public sealed record ProjectCapabilityEvidence(
    ProjectCapabilityKind Kind,
    IReadOnlyList<string> Evidence);

public enum ProjectCapabilityKind
{
    Web,
    Php,
    NodeJs,
    Python,
    BrowserAutomation
}
