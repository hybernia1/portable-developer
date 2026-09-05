using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Projects;

namespace PortableDeveloper.Infrastructure.Projects;

/// <summary>
/// Temporary compatibility bridge for the Apache UI while the generic project catalog
/// becomes the single source of truth.
/// </summary>
public sealed class ProjectWebCatalogAdapter : IWebProjectCatalog
{
    private readonly IProjectCatalog _catalog;
    private readonly IProjectContext _context;
    private readonly IPortablePathResolver _paths;

    public ProjectWebCatalogAdapter(
        IProjectCatalog catalog,
        IProjectContext context,
        IPortablePathResolver paths)
    {
        _catalog = catalog;
        _context = context;
        _paths = paths;
        EnsureProjectDirectories(_catalog.GetRequired(ProjectCatalogDefaults.DefaultProjectId));
    }

    public IReadOnlyList<WebProject> Projects => _catalog.Projects.Select(ToWebProject).ToArray();

    public WebProject ActiveProject => ToWebProject(_context.ActiveProject);

    public WebProject Create(string name, string webRootRelativePath = "public")
    {
        if (_context.IsSwitchBlocked)
        {
            throw new InvalidOperationException("A project cannot be created while another project operation is running.");
        }

        name = name.Trim();
        var id = ProjectCatalogValidator.CreateProjectId(name);
        if (_catalog.Projects.Any(existing => string.Equals(existing.Id, id, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Project '{id}' already exists.");
        }

        var webRoot = ProjectCatalogValidator.NormalizeWebRoot(webRootRelativePath);
        var project = new PortableProject(
            id,
            name,
            ProjectCatalogValidator.GetExpectedRootRelativePath(id),
            new ProjectWebSettings(true, webRoot));

        EnsureProjectDirectories(project);
        _catalog.Add(project, makeActive: false);
        var activation = _context.Activate(project.Id);
        if (!activation.IsSuccess)
        {
            throw new InvalidOperationException("The project was created, but cannot become active while another project operation is running.");
        }
        return ToWebProject(project);
    }

    public void SetActive(string projectId)
    {
        var result = _context.Activate(projectId);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException("The project cannot be changed while another project operation is running.");
        }
    }

    public void SetHtaccess(string projectId, bool allowHtaccess)
    {
        var project = _catalog.GetRequired(projectId);
        var web = project.Web ?? new ProjectWebSettings(false);
        _catalog.Update(project with { Web = web with { AllowHtaccess = allowHtaccess } });
    }

    public void SetEnabled(string projectId, bool isEnabled)
    {
        var project = _catalog.GetRequired(projectId);
        if (string.Equals(project.Id, ProjectCatalogDefaults.DefaultProjectId, StringComparison.OrdinalIgnoreCase) && !isEnabled)
        {
            throw new InvalidOperationException("The default localhost project cannot be disabled.");
        }

        var web = project.Web ?? new ProjectWebSettings(false);
        _catalog.Update(project with { Web = web with { IsEnabled = isEnabled } });
    }

    public void Remove(string projectId) => _catalog.Remove(projectId);

    private static WebProject ToWebProject(PortableProject project)
    {
        var web = project.Web ?? new ProjectWebSettings(false, ".", false);
        return new WebProject(
            project.Id,
            project.Name,
            project.RootRelativePath,
            web.RootRelativePath,
            web.AllowHtaccess,
            web.IsEnabled);
    }

    private void EnsureProjectDirectories(PortableProject project)
    {
        var root = EnsureManagedDirectory(project.RootRelativePath);
        var webRoot = project.Web?.RootRelativePath ?? ".";
        var documentRoot = webRoot == "."
            ? root
            : EnsureManagedDirectory(Path.Combine(project.RootRelativePath, webRoot));
        EnsureManagedDirectory(Path.Combine(project.RootRelativePath, "seldownloads"));

        WebStarterPage.EnsureCreated(documentRoot, project.Name);
    }

    private string EnsureManagedDirectory(string relativePath)
    {
        Directory.CreateDirectory(_paths.RootPath);
        var target = _paths.Resolve(relativePath);
        var relative = Path.GetRelativePath(_paths.RootPath, target);
        var current = _paths.RootPath;
        foreach (var segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);
            if (File.Exists(current) && !Directory.Exists(current))
            {
                throw new IOException("A project directory path is occupied by a file.");
            }

            if (Directory.Exists(current))
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                {
                    throw new InvalidDataException("Project directories must not use links or reparse points.");
                }
            }
            else
            {
                Directory.CreateDirectory(current);
            }
        }

        return target;
    }

}
