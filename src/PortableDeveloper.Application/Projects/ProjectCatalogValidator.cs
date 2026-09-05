using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PortableDeveloper.Application.Projects;

public static partial class ProjectCatalogValidator
{
    public const int MaximumProjectNameLength = 80;
    public const int MaximumWebRootLength = 120;

    public static ProjectCatalogDocument Validate(ProjectCatalogDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.SchemaVersion != ProjectCatalogDefaults.CurrentSchemaVersion)
        {
            throw new InvalidDataException($"Unsupported project catalog schema version: {document.SchemaVersion}.");
        }

        if (document.Projects is null || document.Projects.Count == 0)
        {
            throw new InvalidDataException("The project catalog must contain at least one project.");
        }

        foreach (var project in document.Projects)
        {
            ValidateProject(project);
        }

        var duplicate = document.Projects
            .GroupBy(project => project.Id, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidDataException($"The project catalog contains duplicate ID '{duplicate.Key}'.");
        }

        if (!document.Projects.Any(project => string.Equals(
                project.Id,
                ProjectCatalogDefaults.DefaultProjectId,
                StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidDataException("The default compatibility project is missing from the project catalog.");
        }

        if (string.IsNullOrWhiteSpace(document.ActiveProjectId) ||
            !document.Projects.Any(project => string.Equals(
                project.Id,
                document.ActiveProjectId,
                StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidDataException("The active project does not exist in the project catalog.");
        }

        return document;
    }

    public static PortableProject ValidateProject(PortableProject project)
    {
        ArgumentNullException.ThrowIfNull(project);
        var projectId = project.Id ?? string.Empty;
        if (!ProjectIdPattern().IsMatch(projectId))
        {
            throw new InvalidDataException("A project ID must be a lowercase ASCII slug with at most 63 characters.");
        }

        if (string.IsNullOrWhiteSpace(project.Name) ||
            project.Name.Length > MaximumProjectNameLength ||
            project.Name.Any(char.IsControl))
        {
            throw new InvalidDataException($"A project name must contain 1-{MaximumProjectNameLength} printable characters.");
        }

        var expectedRoot = GetExpectedRootRelativePath(projectId);
        if (!PathsEqual(project.RootRelativePath, expectedRoot))
        {
            throw new InvalidDataException($"Project '{project.Id}' is outside its managed portable root.");
        }

        if (project.Web is not null)
        {
            ValidateWebRoot(project.Web.RootRelativePath);
        }

        return project;
    }

    public static string GetExpectedRootRelativePath(string projectId) =>
        string.Equals(projectId, ProjectCatalogDefaults.DefaultProjectId, StringComparison.OrdinalIgnoreCase)
            ? Path.Combine("instances", "default", "www")
            : Path.Combine("instances", "default", "projects", projectId);

    public static string CreateProjectId(string name)
    {
        name = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name) || name.Length > MaximumProjectNameLength || name.Any(char.IsControl))
        {
            throw new ArgumentException($"Enter a project name with at most {MaximumProjectNameLength} characters.", nameof(name));
        }

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

        if (!ProjectIdPattern().IsMatch(id) ||
            string.Equals(id, ProjectCatalogDefaults.DefaultProjectId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The project name must contain at least one ASCII letter or number and form a unique project ID.", nameof(name));
        }

        return id;
    }

    public static string NormalizeWebRoot(string value)
    {
        value = string.IsNullOrWhiteSpace(value) ? "." : value.Trim().Replace('\\', '/').Trim('/');
        if (value.Length == 0 || value == ".")
        {
            return ".";
        }

        ValidateWebRoot(value);
        return value;
    }

    private static void ValidateWebRoot(string value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > MaximumWebRootLength || Path.IsPathRooted(value))
        {
            throw new InvalidDataException("The web root must be a bounded relative directory inside the project.");
        }

        var normalized = value.Replace('\\', '/');
        if (normalized == ".")
        {
            return;
        }

        var segments = normalized.Split('/');
        if (segments.Any(segment => string.IsNullOrWhiteSpace(segment) || segment is "." or ".." ||
                segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0))
        {
            throw new InvalidDataException("The web root contains an unsafe path segment.");
        }
    }

    private static bool PathsEqual(string left, string right) =>
        string.Equals(
            (left ?? string.Empty).Replace('/', Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar),
            right.Replace('/', Path.DirectorySeparatorChar).TrimEnd(Path.DirectorySeparatorChar),
            StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$", RegexOptions.CultureInvariant)]
    private static partial Regex ProjectIdPattern();
}
