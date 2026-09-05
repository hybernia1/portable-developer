using System.Text;
using System.Text.Json;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Projects;

namespace PortableDeveloper.Infrastructure.Projects;

public sealed class JsonProjectCatalog : IProjectCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IPortablePathResolver _paths;
    private readonly ProjectRootPathValidator _rootValidator;
    private ProjectCatalogDocument _document;

    public JsonProjectCatalog(IPortablePathResolver paths)
    {
        _paths = paths;
        _rootValidator = new ProjectRootPathValidator(paths);
        _document = LoadOrMigrate();
    }

    public IReadOnlyList<PortableProject> Projects => _document.Projects;

    public string ActiveProjectId => _document.ActiveProjectId;

    public ProjectCatalogLoadOutcome LoadOutcome { get; private set; }

    public PortableProject GetRequired(string projectId) =>
        _document.Projects.FirstOrDefault(project => string.Equals(
            project.Id,
            projectId?.Trim(),
            StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException("The project does not exist.", nameof(projectId));

    public void Add(PortableProject project, bool makeActive = true)
    {
        ProjectCatalogValidator.ValidateProject(project);
        _rootValidator.ResolveManagedRoot(project);
        if (_document.Projects.Any(existing => string.Equals(existing.Id, project.Id, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"A project with the ID '{project.Id}' already exists.");
        }

        Save(_document with
        {
            ActiveProjectId = makeActive ? project.Id : _document.ActiveProjectId,
            Projects = _document.Projects.Append(project).ToArray()
        });
    }

    public void Update(PortableProject project)
    {
        ProjectCatalogValidator.ValidateProject(project);
        _rootValidator.ResolveManagedRoot(project);
        _ = GetRequired(project.Id);
        Save(_document with
        {
            Projects = _document.Projects
                .Select(existing => string.Equals(existing.Id, project.Id, StringComparison.OrdinalIgnoreCase)
                    ? project
                    : existing)
                .ToArray()
        });
    }

    public void SetActive(string projectId)
    {
        var project = GetRequired(projectId);
        if (string.Equals(_document.ActiveProjectId, project.Id, StringComparison.Ordinal))
        {
            return;
        }

        Save(_document with { ActiveProjectId = project.Id });
    }

    public void Remove(string projectId)
    {
        var project = GetRequired(projectId);
        if (string.Equals(project.Id, ProjectCatalogDefaults.DefaultProjectId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The default compatibility project cannot be unregistered.");
        }

        Save(_document with
        {
            ActiveProjectId = string.Equals(_document.ActiveProjectId, project.Id, StringComparison.OrdinalIgnoreCase)
                ? ProjectCatalogDefaults.DefaultProjectId
                : _document.ActiveProjectId,
            Projects = _document.Projects
                .Where(existing => !string.Equals(existing.Id, project.Id, StringComparison.OrdinalIgnoreCase))
                .ToArray()
        });
    }

    private ProjectCatalogDocument LoadOrMigrate()
    {
        var catalogPath = GetCatalogPath();
        RefuseReparsePoint(catalogPath, "The project catalog must not be a link or reparse point.");
        if (TryLoad(catalogPath, out var current))
        {
            LoadOutcome = ProjectCatalogLoadOutcome.Current;
            return current;
        }

        var backupPath = catalogPath + ".bak";
        RefuseReparsePoint(backupPath, "The project catalog backup must not be a link or reparse point.");
        if (TryLoad(backupPath, out var backup))
        {
            RestoreCurrent(backup, catalogPath);
            LoadOutcome = ProjectCatalogLoadOutcome.RecoveredBackup;
            return backup;
        }

        var migrated = TryMigrateLegacy() ?? ProjectCatalogDefaults.DefaultDocument;
        RestoreCurrent(migrated, catalogPath);
        LoadOutcome = migrated == ProjectCatalogDefaults.DefaultDocument
            ? ProjectCatalogLoadOutcome.DefaultCreated
            : ProjectCatalogLoadOutcome.LegacyMigrated;
        return migrated;
    }

    private ProjectCatalogDocument? TryMigrateLegacy()
    {
        var legacyPath = _paths.Resolve(Path.Combine("instances", "default", "config", "web-projects.json"));
        RefuseReparsePoint(legacyPath, "The legacy web project catalog must not be a link or reparse point.");
        if (!File.Exists(legacyPath))
        {
            return null;
        }

        try
        {
            var legacy = JsonSerializer.Deserialize<LegacyCatalogDocument>(File.ReadAllText(legacyPath), SerializerOptions);
            if (legacy?.Projects is null)
            {
                return null;
            }

            var projects = new List<PortableProject>();
            foreach (var legacyProject in legacy.Projects)
            {
                var project = ConvertLegacyProject(legacyProject);
                if (project is null || projects.Any(existing => string.Equals(
                        existing.Id,
                        project.Id,
                        StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                projects.Add(project);
            }

            var defaultIndex = projects.FindIndex(project => string.Equals(
                project.Id,
                ProjectCatalogDefaults.DefaultProjectId,
                StringComparison.OrdinalIgnoreCase));
            if (defaultIndex < 0)
            {
                _rootValidator.ResolveManagedRoot(ProjectCatalogDefaults.DefaultProject);
                projects.Insert(0, ProjectCatalogDefaults.DefaultProject);
            }
            else if (defaultIndex > 0)
            {
                var defaultProject = projects[defaultIndex];
                projects.RemoveAt(defaultIndex);
                projects.Insert(0, defaultProject);
            }

            var activeProjectId = projects.FirstOrDefault(project => string.Equals(
                project.Id,
                legacy.ActiveProjectId,
                StringComparison.OrdinalIgnoreCase))?.Id ?? ProjectCatalogDefaults.DefaultProjectId;
            return ProjectCatalogValidator.Validate(new ProjectCatalogDocument(
                ProjectCatalogDefaults.CurrentSchemaVersion,
                activeProjectId,
                projects));
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException or ArgumentException or IOException)
        {
            return null;
        }
    }

    private PortableProject? ConvertLegacyProject(LegacyWebProject? legacy)
    {
        if (legacy is null)
        {
            return null;
        }

        try
        {
            var project = new PortableProject(
                legacy.Id,
                legacy.Name,
                legacy.ProjectRootRelativePath,
                new ProjectWebSettings(
                    legacy.IsEnabled,
                    ProjectCatalogValidator.NormalizeWebRoot(legacy.WebRootRelativePath),
                    legacy.AllowHtaccess));
            ProjectCatalogValidator.ValidateProject(project);
            _rootValidator.ResolveManagedRoot(project);
            return project;
        }
        catch (Exception exception) when (exception is InvalidDataException or ArgumentException or IOException)
        {
            return null;
        }
    }

    private bool TryLoad(string path, out ProjectCatalogDocument document)
    {
        document = ProjectCatalogDefaults.DefaultDocument;
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            var loaded = JsonSerializer.Deserialize<ProjectCatalogDocument>(File.ReadAllText(path), SerializerOptions);
            document = ProjectCatalogValidator.Validate(loaded
                ?? throw new InvalidDataException("The project catalog is empty."));
            foreach (var project in document.Projects)
            {
                _rootValidator.ResolveManagedRoot(project);
            }

            return true;
        }
        catch (Exception exception) when (exception is JsonException or InvalidDataException or ArgumentException or IOException)
        {
            return false;
        }
    }

    private void Save(ProjectCatalogDocument document)
    {
        ProjectCatalogValidator.Validate(document);
        foreach (var project in document.Projects)
        {
            _rootValidator.ResolveManagedRoot(project);
        }

        var path = GetCatalogPath();
        var temporaryPath = path + ".part";
        var backupPath = path + ".bak";
        PrepareTemporaryPath(temporaryPath);
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(document, SerializerOptions), new UTF8Encoding(false));
        if (!TryLoad(temporaryPath, out var verified))
        {
            File.Delete(temporaryPath);
            throw new InvalidDataException("The staged project catalog failed validation.");
        }

        RefuseReparsePoint(path, "The project catalog must not be a link or reparse point.");
        RefuseReparsePoint(backupPath, "The project catalog backup must not be a link or reparse point.");
        if (File.Exists(path))
        {
            File.Replace(temporaryPath, path, backupPath, ignoreMetadataErrors: true);
        }
        else
        {
            File.Move(temporaryPath, path);
        }

        _document = verified;
    }

    private void RestoreCurrent(ProjectCatalogDocument document, string path)
    {
        ProjectCatalogValidator.Validate(document);
        foreach (var project in document.Projects)
        {
            _rootValidator.ResolveManagedRoot(project);
        }

        var temporaryPath = path + ".part";
        PrepareTemporaryPath(temporaryPath);
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(document, SerializerOptions), new UTF8Encoding(false));
        if (!TryLoad(temporaryPath, out _))
        {
            File.Delete(temporaryPath);
            throw new InvalidDataException("The staged project catalog failed validation.");
        }

        RefuseReparsePoint(path, "The project catalog must not be a link or reparse point.");
        File.Move(temporaryPath, path, overwrite: true);
    }

    private string GetCatalogPath()
    {
        var configDirectory = EnsureSafeDirectory(Path.Combine("instances", "default", "config"));
        return Path.Combine(configDirectory, "projects.json");
    }

    private string EnsureSafeDirectory(string relativePath)
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
                throw new IOException("The project catalog directory path is occupied by a file.");
            }

            if (Directory.Exists(current))
            {
                if ((File.GetAttributes(current) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                {
                    throw new InvalidDataException("Project catalog directories must not use links or reparse points.");
                }
            }
            else
            {
                Directory.CreateDirectory(current);
            }
        }

        return target;
    }

    private static void PrepareTemporaryPath(string path)
    {
        RefuseReparsePoint(path, "The staged project catalog must not be a link or reparse point.");
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static void RefuseReparsePoint(string path, string message)
    {
        if ((File.Exists(path) || Directory.Exists(path)) &&
            (File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
        {
            throw new InvalidDataException(message);
        }
    }

    private sealed record LegacyCatalogDocument(string? ActiveProjectId, IReadOnlyList<LegacyWebProject?>? Projects);

    private sealed record LegacyWebProject(
        string Id,
        string Name,
        string ProjectRootRelativePath,
        string WebRootRelativePath,
        bool AllowHtaccess = true,
        bool IsEnabled = true);
}

public enum ProjectCatalogLoadOutcome
{
    Current,
    LegacyMigrated,
    RecoveredBackup,
    DefaultCreated
}
