using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Projects;

namespace PortableDeveloper.Infrastructure.Projects;

public sealed partial class JsonWebProjectCatalog : IWebProjectCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IPortablePathResolver _paths;
    private CatalogState _state;

    public JsonWebProjectCatalog(IPortablePathResolver paths)
    {
        _paths = paths;
        _state = Load();
        EnsureProjectDirectories(_state.Projects);
    }

    public IReadOnlyList<WebProject> Projects => _state.Projects;

    public WebProject ActiveProject => _state.Projects.First(project =>
        string.Equals(project.Id, _state.ActiveProjectId, StringComparison.OrdinalIgnoreCase));

    public WebProject Create(string name, string webRootRelativePath = "public")
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 80 || name.Any(char.IsControl))
        {
            throw new ArgumentException("Enter a project name with at most 80 characters.", nameof(name));
        }

        var id = CreateId(name);
        if (_state.Projects.Any(project => string.Equals(project.Id, id, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"A project with the ID '{id}' already exists.");
        }

        webRootRelativePath = NormalizeWebRoot(webRootRelativePath);
        var project = new WebProject(
            id,
            name,
            Path.Combine("instances", "default", "projects", id),
            webRootRelativePath);
        var projects = _state.Projects.Append(project).ToArray();
        _state = new CatalogState(id, projects);
        EnsureProjectDirectories([project]);
        Save();
        return project;
    }

    public void SetActive(string projectId)
    {
        var project = Find(projectId);
        _state = _state with { ActiveProjectId = project.Id };
        Save();
    }

    public void SetHtaccess(string projectId, bool allowHtaccess) =>
        Update(Find(projectId) with { AllowHtaccess = allowHtaccess });

    public void SetEnabled(string projectId, bool isEnabled)
    {
        var project = Find(projectId);
        if (project.Id == WebProjectCatalogDefaults.DefaultProjectId && !isEnabled)
        {
            throw new InvalidOperationException("The default localhost project cannot be disabled.");
        }

        Update(project with { IsEnabled = isEnabled });
    }

    public void Remove(string projectId)
    {
        var project = Find(projectId);
        if (project.Id == WebProjectCatalogDefaults.DefaultProjectId)
        {
            throw new InvalidOperationException("The default localhost project cannot be removed.");
        }

        var projects = _state.Projects.Where(item => item.Id != project.Id).ToArray();
        var activeId = _state.ActiveProjectId == project.Id
            ? WebProjectCatalogDefaults.DefaultProjectId
            : _state.ActiveProjectId;
        _state = new CatalogState(activeId, projects);
        Save();
    }

    private CatalogState Load()
    {
        var defaultState = new CatalogState(
            WebProjectCatalogDefaults.DefaultProjectId,
            [WebProjectCatalogDefaults.DefaultProject]);
        var path = GetCatalogPath();
        if (!File.Exists(path))
        {
            return defaultState;
        }

        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
        {
            return defaultState;
        }

        try
        {
            var loaded = JsonSerializer.Deserialize<CatalogState>(File.ReadAllText(path), SerializerOptions);
            return Normalize(loaded ?? defaultState);
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException or NullReferenceException)
        {
            return defaultState;
        }
    }

    private CatalogState Normalize(CatalogState state)
    {
        var projects = (state.Projects ?? [])
            .Where(IsValidStoredProject)
            .GroupBy(project => project.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First() with
            {
                WebRootRelativePath = NormalizeWebRoot(group.First().WebRootRelativePath)
            })
            .ToList();
        var defaultIndex = projects.FindIndex(project => project.Id == WebProjectCatalogDefaults.DefaultProjectId);
        if (defaultIndex < 0)
        {
            projects.Insert(0, WebProjectCatalogDefaults.DefaultProject);
        }
        else
        {
            var defaultProject = WebProjectCatalogDefaults.DefaultProject with
            {
                AllowHtaccess = projects[defaultIndex].AllowHtaccess
            };
            projects.RemoveAt(defaultIndex);
            projects.Insert(0, defaultProject);
        }

        var activeId = projects.Any(project => string.Equals(project.Id, state.ActiveProjectId, StringComparison.OrdinalIgnoreCase))
            ? projects.First(project => string.Equals(project.Id, state.ActiveProjectId, StringComparison.OrdinalIgnoreCase)).Id
            : WebProjectCatalogDefaults.DefaultProjectId;
        return new CatalogState(activeId, projects);
    }

    private void Update(WebProject updated)
    {
        _state = _state with
        {
            Projects = _state.Projects.Select(project => project.Id == updated.Id ? updated : project).ToArray()
        };
        Save();
    }

    private WebProject Find(string projectId) => _state.Projects.FirstOrDefault(project =>
        string.Equals(project.Id, projectId?.Trim(), StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException("The web project does not exist.", nameof(projectId));

    private void EnsureProjectDirectories(IEnumerable<WebProject> projects)
    {
        foreach (var project in projects)
        {
            var projectRoot = EnsureManagedDirectory(project.ProjectRootRelativePath);
            var documentRoot = EnsureManagedDirectory(project.DocumentRootRelativePath);
            EnsureManagedDirectory(Path.Combine(project.ProjectRootRelativePath, "seldownloads"));
            WebStarterPage.EnsureCreated(documentRoot, project.Name);
        }
    }

    private void Save()
    {
        var path = GetCatalogPath();
        var temporaryPath = path + ".part";
        if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
        {
            throw new InvalidDataException("The web project catalog must not be a link or reparse point.");
        }

        if (File.Exists(temporaryPath))
        {
            if ((File.GetAttributes(temporaryPath) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            {
                throw new InvalidDataException("The temporary web project catalog must not be a link or reparse point.");
            }

            File.Delete(temporaryPath);
        }

        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(_state, SerializerOptions), new UTF8Encoding(false));
        File.Move(temporaryPath, path, overwrite: true);
    }

    private string GetCatalogPath()
    {
        EnsureManagedDirectory(Path.Combine("instances", "default", "config"));
        return _paths.Resolve(Path.Combine("instances", "default", "config", "web-projects.json"));
    }

    private static bool IsValidStoredProject(WebProject project) =>
        ProjectIdRegex().IsMatch(project.Id ?? string.Empty) &&
        !string.IsNullOrWhiteSpace(project.Name) &&
        project.Name.Length <= 80 &&
        IsExpectedProjectRoot(project);

    private static bool IsExpectedProjectRoot(WebProject project)
    {
        var expected = project.Id == WebProjectCatalogDefaults.DefaultProjectId
            ? Path.Combine("instances", "default", "www")
            : Path.Combine("instances", "default", "projects", project.Id);
        return string.Equals(
            project.ProjectRootRelativePath.Replace('/', Path.DirectorySeparatorChar),
            expected,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeWebRoot(string value)
    {
        value = string.IsNullOrWhiteSpace(value) ? "." : value.Trim().Replace('\\', '/').Trim('/');
        if (value.Length == 0 || value == ".")
        {
            return ".";
        }

        if (value.Length > 120 || Path.IsPathRooted(value) ||
            value.Split('/').Any(segment => segment is "" or "." or ".." ||
                segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
        {
            throw new ArgumentException("The web root must be a relative directory inside the project.", nameof(value));
        }

        return value;
    }

    private static string CreateId(string name)
    {
        var normalized = name.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (character is >= 'a' and <= 'z' or >= '0' and <= '9')
            {
                builder.Append(character);
            }
            else if (character is >= 'A' and <= 'Z')
            {
                builder.Append(char.ToLowerInvariant(character));
            }
            else if (builder.Length > 0 && builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        var id = builder.ToString().Trim('-');
        if (id.Length > 63)
        {
            id = id[..63].TrimEnd('-');
        }

        if (!ProjectIdRegex().IsMatch(id) || id == WebProjectCatalogDefaults.DefaultProjectId)
        {
            throw new ArgumentException("The project name must contain at least one ASCII letter or number and form a unique localhost name.", nameof(name));
        }

        return id;
    }

    private string EnsureManagedDirectory(string relativePath)
    {
        var target = _paths.Resolve(relativePath);
        var relative = Path.GetRelativePath(_paths.RootPath, target);
        var current = _paths.RootPath;
        foreach (var segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);
            if (File.Exists(current) && !Directory.Exists(current))
            {
                throw new IOException("A web project directory path is occupied by a file.");
            }

            if (Directory.Exists(current))
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                {
                    throw new InvalidDataException("Web project directories must not use links or reparse points.");
                }
            }
            else
            {
                Directory.CreateDirectory(current);
            }
        }

        return target;
    }

    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$")]
    private static partial Regex ProjectIdRegex();

    private sealed record CatalogState(string ActiveProjectId, IReadOnlyList<WebProject> Projects);
}
