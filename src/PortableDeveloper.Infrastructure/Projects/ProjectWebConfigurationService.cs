using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Projects;

namespace PortableDeveloper.Infrastructure.Projects;

public sealed class ProjectWebConfigurationService : IProjectWebConfigurationService
{
    private readonly IProjectCatalog _catalog;
    private readonly ProjectRootPathValidator _rootValidator;

    public ProjectWebConfigurationService(IPortablePathResolver paths, IProjectCatalog catalog)
    {
        _catalog = catalog;
        _rootValidator = new ProjectRootPathValidator(paths);
    }

    public ProjectWebConfigurationResult Configure(string projectId, ProjectWebSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var project = _catalog.GetRequired(projectId);
        if (string.Equals(project.Id, ProjectCatalogDefaults.DefaultProjectId, StringComparison.OrdinalIgnoreCase) &&
            !settings.IsEnabled)
        {
            throw new InvalidOperationException("The default localhost project cannot be disabled.");
        }

        var normalized = settings with
        {
            RootRelativePath = ProjectCatalogValidator.NormalizeWebRoot(settings.RootRelativePath)
        };
        var updated = ProjectCatalogValidator.ValidateProject(project with { Web = normalized });
        var projectRoot = _rootValidator.ResolveManagedRoot(updated);
        if (!Directory.Exists(projectRoot))
        {
            throw new DirectoryNotFoundException("The managed project directory does not exist.");
        }

        var webRoot = EnsureWebRoot(projectRoot, normalized.RootRelativePath);
        var created = !Directory.Exists(webRoot);
        Directory.CreateDirectory(webRoot);
        var starterFileCreated = false;
        try
        {
            if (normalized.IsEnabled)
            {
                starterFileCreated = WebStarterPage.EnsureCreated(webRoot, project.Name);
            }

            _catalog.Update(updated);
        }
        catch
        {
            if (starterFileCreated)
            {
                File.Delete(Path.Combine(webRoot, WebStarterPage.FileName));
            }

            if (created && Directory.Exists(webRoot) && !Directory.EnumerateFileSystemEntries(webRoot).Any())
            {
                Directory.Delete(webRoot);
            }

            throw;
        }

        return new ProjectWebConfigurationResult(updated, created, starterFileCreated);
    }

    private static string EnsureWebRoot(string projectRoot, string relativePath)
    {
        var target = relativePath == "."
            ? projectRoot
            : Path.GetFullPath(Path.Combine(projectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        if (!string.Equals(target, projectRoot, StringComparison.OrdinalIgnoreCase) &&
            !target.StartsWith(projectRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The web root escapes the managed project directory.");
        }

        var current = projectRoot;
        foreach (var segment in Path.GetRelativePath(projectRoot, target)
                     .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (segment == ".")
            {
                continue;
            }

            current = Path.Combine(current, segment);
            if (File.Exists(current) && !Directory.Exists(current))
            {
                throw new IOException("The web root path is occupied by a file.");
            }

            if (Directory.Exists(current) &&
                (File.GetAttributes(current) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            {
                throw new InvalidDataException("The web root must not use links or reparse points.");
            }
        }

        return target;
    }
}
