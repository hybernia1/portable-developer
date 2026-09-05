using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Projects;

namespace PortableDeveloper.Infrastructure.Projects;

public sealed class ProjectRootPathValidator
{
    private readonly IPortablePathResolver _paths;

    public ProjectRootPathValidator(IPortablePathResolver paths)
    {
        _paths = paths;
    }

    public string ResolveManagedRoot(PortableProject project)
    {
        ProjectCatalogValidator.ValidateProject(project);
        var target = _paths.Resolve(project.RootRelativePath);
        var relative = Path.GetRelativePath(_paths.RootPath, target);
        var current = _paths.RootPath;
        foreach (var segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);
            if (File.Exists(current) && !Directory.Exists(current))
            {
                throw new IOException("A managed project root is occupied by a file.");
            }

            if (Directory.Exists(current) &&
                (File.GetAttributes(current) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            {
                throw new InvalidDataException("Managed project roots must not use links or reparse points.");
            }
        }

        return target;
    }
}
