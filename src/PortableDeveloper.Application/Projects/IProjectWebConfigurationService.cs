namespace PortableDeveloper.Application.Projects;

public interface IProjectWebConfigurationService
{
    ProjectWebConfigurationResult Configure(
        string projectId,
        ProjectWebSettings settings);
}

public sealed record ProjectWebConfigurationResult(
    PortableProject Project,
    bool WebRootDirectoryCreated,
    bool StarterFileCreated);
