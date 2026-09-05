using System.Text.Json;
using PortableDeveloper.Application.Projects;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Projects;

namespace PortableDeveloper.Tests;

public sealed class ProjectContractTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void Versioned_fixture_deserializes_and_validates()
    {
        var document = JsonSerializer.Deserialize<ProjectCatalogDocument>(
            File.ReadAllText(GetFixturePath("projects-v2.json")),
            JsonOptions);

        var validated = ProjectCatalogValidator.Validate(Assert.IsType<ProjectCatalogDocument>(document));

        Assert.Equal(ProjectCatalogDefaults.CurrentSchemaVersion, validated.SchemaVersion);
        Assert.Equal("automation-lab", validated.ActiveProjectId);
        Assert.Equal(2, validated.Projects.Count);
        Assert.Null(validated.Projects[1].Web);
    }

    [Fact]
    public void Legacy_fixture_preserves_the_shapes_required_by_migration()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GetFixturePath("legacy-web-projects.json")));
        var root = document.RootElement;

        Assert.Equal("automation-lab", root.GetProperty("activeProjectId").GetString());
        var projects = root.GetProperty("projects").EnumerateArray().ToArray();
        Assert.Equal(2, projects.Length);
        Assert.Equal("instances/default/projects/automation-lab", projects[1].GetProperty("projectRootRelativePath").GetString());
        Assert.False(projects[1].GetProperty("isEnabled").GetBoolean());
        Assert.False(projects[1].GetProperty("allowHtaccess").GetBoolean());
    }

    [Theory]
    [InlineData("Uppercase")]
    [InlineData("-leading")]
    [InlineData("trailing-")]
    [InlineData("contains space")]
    [InlineData("")]
    public void Project_ids_must_be_safe_stable_slugs(string projectId)
    {
        var project = new PortableProject(
            projectId,
            "Invalid ID",
            Path.Combine("instances", "default", "projects", projectId));

        Assert.Throws<InvalidDataException>(() => ProjectCatalogValidator.ValidateProject(project));
    }

    [Fact]
    public void Project_name_is_required_bounded_and_printable()
    {
        Assert.Throws<InvalidDataException>(() => ProjectCatalogValidator.ValidateProject(CreateProject(name: "")));
        Assert.Throws<InvalidDataException>(() => ProjectCatalogValidator.ValidateProject(CreateProject(name: new string('x', 81))));
        Assert.Throws<InvalidDataException>(() => ProjectCatalogValidator.ValidateProject(CreateProject(name: "line\nbreak")));
    }

    [Theory]
    [InlineData("Český projekt", "cesky-projekt")]
    [InlineData("Node.js Lab", "node-js-lab")]
    [InlineData("Browser  Automation", "browser-automation")]
    public void Project_id_is_stable_ascii_slug(string name, string expected) =>
        Assert.Equal(expected, ProjectCatalogValidator.CreateProjectId(name));

    [Theory]
    [InlineData("---")]
    [InlineData("Default")]
    [InlineData("")]
    public void Project_id_creation_rejects_unusable_or_reserved_names(string name) =>
        Assert.Throws<ArgumentException>(() => ProjectCatalogValidator.CreateProjectId(name));

    [Theory]
    [InlineData("../outside")]
    [InlineData("C:/outside")]
    [InlineData("instances/default/projects/other")]
    [InlineData("instances/default/projects/safe/nested")]
    public void Project_root_must_match_the_managed_layout(string rootRelativePath)
    {
        Assert.Throws<InvalidDataException>(() =>
            ProjectCatalogValidator.ValidateProject(CreateProject(rootRelativePath: rootRelativePath)));
    }

    [Theory]
    [InlineData("../public")]
    [InlineData("public/../private")]
    [InlineData("public//nested")]
    [InlineData("C:/public")]
    public void Web_root_refuses_unsafe_paths(string webRoot)
    {
        var project = CreateProject(web: new ProjectWebSettings(true, webRoot));

        Assert.Throws<InvalidDataException>(() => ProjectCatalogValidator.ValidateProject(project));
    }

    [Fact]
    public void Catalog_refuses_duplicates_and_missing_active_project()
    {
        var project = CreateProject();
        var duplicate = new ProjectCatalogDocument(2, project.Id, [project, project]);
        var missingActive = new ProjectCatalogDocument(2, "missing", [project]);

        Assert.Throws<InvalidDataException>(() => ProjectCatalogValidator.Validate(duplicate));
        Assert.Throws<InvalidDataException>(() => ProjectCatalogValidator.Validate(missingActive));
    }

    [Fact]
    public void Catalog_requires_the_default_compatibility_project()
    {
        var project = CreateProject();
        var document = new ProjectCatalogDocument(2, project.Id, [project]);

        Assert.Throws<InvalidDataException>(() => ProjectCatalogValidator.Validate(document));
    }

    [Fact]
    public void Malformed_fixture_text_is_rejected_by_json_reader()
    {
        const string malformed = "{ \"schemaVersion\": 2, \"projects\": [";

        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ProjectCatalogDocument>(malformed, JsonOptions));
    }

    [Fact]
    public void Physical_root_validation_rejects_a_reparse_point()
    {
        var paths = new PortablePathResolver(_testRoot);
        var projectsRoot = paths.EnsureDirectory(Path.Combine("instances", "default", "projects"));
        var outside = paths.EnsureDirectory("outside");
        var project = CreateProject();
        try
        {
            Directory.CreateSymbolicLink(Path.Combine(projectsRoot, project.Id), outside);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }

        var validator = new ProjectRootPathValidator(paths);

        Assert.Throws<InvalidDataException>(() => validator.ResolveManagedRoot(project));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private static PortableProject CreateProject(
        string name = "Safe",
        string? rootRelativePath = null,
        ProjectWebSettings? web = null) =>
        new(
            "safe",
            name,
            rootRelativePath ?? Path.Combine("instances", "default", "projects", "safe"),
            web);

    private static string GetFixturePath(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Projects", name);
}
