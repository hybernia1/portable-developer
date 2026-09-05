using System.Text;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Projects;

namespace PortableDeveloper.Infrastructure.Projects;

public sealed class ProjectTemplateService : IProjectTemplateService
{
    private readonly IPortablePathResolver _paths;
    private readonly IProjectCatalog _catalog;
    private readonly IProjectContext _context;
    private readonly ProjectRootPathValidator _rootValidator;

    public ProjectTemplateService(
        IPortablePathResolver paths,
        IProjectCatalog catalog,
        IProjectContext context)
    {
        _paths = paths;
        _catalog = catalog;
        _context = context;
        _rootValidator = new ProjectRootPathValidator(paths);
    }

    public async Task<ProjectTemplateResult> CreateAsync(
        ProjectTemplateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        EnsureSwitchAvailable();
        var name = request.Name?.Trim() ?? string.Empty;
        var id = ProjectCatalogValidator.CreateProjectId(name);
        if (_catalog.Projects.Any(project => string.Equals(project.Id, id, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"A project with the ID '{id}' already exists.");
        }

        var web = request.Template == ProjectTemplateKind.Web
            ? new ProjectWebSettings(true, ProjectCatalogValidator.NormalizeWebRoot(request.WebRootRelativePath))
            : null;
        var project = new PortableProject(id, name, ProjectCatalogValidator.GetExpectedRootRelativePath(id), web);
        ProjectCatalogValidator.ValidateProject(project);
        var finalRoot = _rootValidator.ResolveManagedRoot(project);
        if (Directory.Exists(finalRoot) || File.Exists(finalRoot))
        {
            throw new InvalidOperationException("The managed project directory already exists. Register it instead of replacing it.");
        }

        var projectsRoot = EnsureManagedDirectory(Path.Combine("instances", "default", "projects"));
        var stagingRoot = Path.Combine(projectsRoot, $".stage-{id}-{Guid.NewGuid():N}");
        var movedToFinal = false;
        var registered = false;
        try
        {
            Directory.CreateDirectory(stagingRoot);
            var created = await WriteTemplateAsync(
                stagingRoot,
                project.Name,
                request.Template,
                project.Web?.RootRelativePath ?? ".",
                cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            Directory.Move(stagingRoot, finalRoot);
            movedToFinal = true;
            _catalog.Add(project, makeActive: false);
            registered = true;
            EnsureActivated(project.Id);
            return new ProjectTemplateResult(project, created);
        }
        finally
        {
            if (!registered && movedToFinal && Directory.Exists(finalRoot))
            {
                DeleteOwnedCreationDirectory(finalRoot, projectsRoot);
            }

            if (Directory.Exists(stagingRoot))
            {
                DeleteOwnedCreationDirectory(stagingRoot, projectsRoot);
            }
        }
    }

    public IReadOnlyList<ManagedProjectDirectoryCandidate> GetRegistrableDirectories()
    {
        var projectsRoot = EnsureManagedDirectory(Path.Combine("instances", "default", "projects"));
        var registeredIds = _catalog.Projects.Select(project => project.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return Directory.EnumerateDirectories(projectsRoot)
            .Where(path => (File.GetAttributes(path) & FileAttributes.ReparsePoint) == 0)
            .Select(Path.GetFileName)
            .Where(id => !string.IsNullOrWhiteSpace(id) && !id.StartsWith(".stage-", StringComparison.OrdinalIgnoreCase))
            .Where(id => !registeredIds.Contains(id!))
            .Where(id => IsValidProjectDirectoryId(id!))
            .OrderBy(id => id, StringComparer.OrdinalIgnoreCase)
            .Select(id => new ManagedProjectDirectoryCandidate(
                id!,
                ProjectCatalogValidator.GetExpectedRootRelativePath(id!)))
            .ToArray();
    }

    public Task<PortableProject> RegisterExistingAsync(
        string directoryId,
        string displayName,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureSwitchAvailable();
        directoryId = directoryId?.Trim() ?? string.Empty;
        var project = new PortableProject(
            directoryId,
            displayName?.Trim() ?? string.Empty,
            ProjectCatalogValidator.GetExpectedRootRelativePath(directoryId));
        ProjectCatalogValidator.ValidateProject(project);
        var root = _rootValidator.ResolveManagedRoot(project);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException("The managed project directory does not exist.");
        }

        _catalog.Add(project, makeActive: false);
        EnsureActivated(project.Id);
        return Task.FromResult(project);
    }

    private void EnsureActivated(string projectId)
    {
        var activation = _context.Activate(projectId);
        if (!activation.IsSuccess)
        {
            throw new InvalidOperationException("The project was registered, but cannot become active while another project operation is running.");
        }
    }

    private void EnsureSwitchAvailable()
    {
        if (_context.IsSwitchBlocked)
        {
            throw new InvalidOperationException("A project cannot be created or registered while another project operation is running.");
        }
    }

    private async Task<IReadOnlyList<string>> WriteTemplateAsync(
        string stagingRoot,
        string projectName,
        ProjectTemplateKind template,
        string webRootRelativePath,
        CancellationToken cancellationToken)
    {
        if (template == ProjectTemplateKind.Web)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativeIndexPath = webRootRelativePath == "."
                ? WebStarterPage.FileName
                : Path.Combine(webRootRelativePath, WebStarterPage.FileName);
            var indexPath = ResolveBelow(stagingRoot, relativeIndexPath);
            WebStarterPage.EnsureCreated(Path.GetDirectoryName(indexPath)!, projectName);
            return [relativeIndexPath];
        }

        var files = template switch
        {
            ProjectTemplateKind.Empty => Array.Empty<(string Path, string Content)>(),
            ProjectTemplateKind.Python =>
            [
                ("main.py", "def main() -> None:\n    print(\"Portable Developer is ready\")\n\n\nif __name__ == \"__main__\":\n    main()\n"),
                ("requirements.txt", string.Empty)
            ],
            ProjectTemplateKind.BrowserAutomation =>
            [
                ("selenium_example.py", "from selenium import webdriver\nfrom selenium.webdriver.chrome.options import Options\n\noptions = Options()\ndriver = webdriver.Remote(command_executor=\"http://127.0.0.1:4444\", options=options)\ntry:\n    driver.get(\"https://example.com\")\n    print(driver.title)\nfinally:\n    driver.quit()\n"),
                ("README.md", "# Browser automation\n\nStart the shared Selenium Server in Portable Developer, then run `selenium_example.py` from the project terminal. Install project dependencies explicitly; this template does not download or execute anything.\n")
            ],
            ProjectTemplateKind.NodeJs =>
            [
                ("package.json", "{\n  \"name\": \"portable-project\",\n  \"private\": true,\n  \"version\": \"0.1.0\",\n  \"scripts\": {\n    \"start\": \"node src/index.js\"\n  }\n}\n"),
                (Path.Combine("src", "index.js"), "console.log(\"Portable Developer is ready\");\n")
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(template))
        };

        var created = new List<string>(files.Length);
        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var path = ResolveBelow(stagingRoot, file.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, file.Content, new UTF8Encoding(false), cancellationToken);
            created.Add(file.Path);
        }

        return created;
    }

    private string EnsureManagedDirectory(string relativePath)
    {
        Directory.CreateDirectory(_paths.RootPath);
        var target = _paths.Resolve(relativePath);
        var current = _paths.RootPath;
        foreach (var segment in Path.GetRelativePath(_paths.RootPath, target)
                     .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);
            if (File.Exists(current) && !Directory.Exists(current))
            {
                throw new IOException("A managed project path is occupied by a file.");
            }

            if (Directory.Exists(current) &&
                (File.GetAttributes(current) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            {
                throw new InvalidDataException("Managed project directories must not use links or reparse points.");
            }

            Directory.CreateDirectory(current);
        }

        return target;
    }

    private static string ResolveBelow(string root, string relativePath)
    {
        var path = Path.GetFullPath(Path.Combine(root, relativePath));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("A template file escapes its staging directory.");
        }

        return path;
    }

    private static void DeleteOwnedCreationDirectory(string target, string projectsRoot)
    {
        var fullTarget = Path.GetFullPath(target);
        var prefix = Path.GetFullPath(projectsRoot) + Path.DirectorySeparatorChar;
        if (!fullTarget.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
            (File.GetAttributes(fullTarget) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
        {
            throw new InvalidDataException("Refusing to clean an unsafe project-creation directory.");
        }

        Directory.Delete(fullTarget, recursive: true);
    }

    private static bool IsValidProjectDirectoryId(string id)
    {
        try
        {
            ProjectCatalogValidator.ValidateProject(new PortableProject(
                id,
                id,
                ProjectCatalogValidator.GetExpectedRootRelativePath(id)));
            return !string.Equals(id, ProjectCatalogDefaults.DefaultProjectId, StringComparison.OrdinalIgnoreCase);
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }
}
