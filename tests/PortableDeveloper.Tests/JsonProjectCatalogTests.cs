using System.Text.Json;
using PortableDeveloper.Application.Projects;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Projects;

namespace PortableDeveloper.Tests;

public sealed class JsonProjectCatalogTests : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void Fresh_catalog_persists_default_without_creating_project_content()
    {
        var paths = new PortablePathResolver(_testRoot);

        var catalog = new JsonProjectCatalog(paths);

        Assert.Equal(ProjectCatalogLoadOutcome.DefaultCreated, catalog.LoadOutcome);
        Assert.Equal(ProjectCatalogDefaults.DefaultProjectId, catalog.ActiveProjectId);
        Assert.Single(catalog.Projects);
        Assert.True(File.Exists(ProjectCatalogPath));
        Assert.False(Directory.Exists(paths.Resolve(ProjectCatalogDefaults.DefaultProject.RootRelativePath)));
    }

    [Fact]
    public void Legacy_catalog_migrates_all_valid_settings_without_modifying_legacy_file()
    {
        var paths = new PortablePathResolver(_testRoot);
        var legacyPath = WriteLegacyFixture(paths);
        var originalLegacy = File.ReadAllText(legacyPath);

        var catalog = new JsonProjectCatalog(paths);

        Assert.Equal(ProjectCatalogLoadOutcome.LegacyMigrated, catalog.LoadOutcome);
        Assert.Equal("automation-lab", catalog.ActiveProjectId);
        var migrated = Assert.Single(catalog.Projects, project => project.Id == "automation-lab");
        Assert.NotNull(migrated.Web);
        Assert.False(migrated.Web.IsEnabled);
        Assert.False(migrated.Web.AllowHtaccess);
        Assert.Equal(".", migrated.Web.RootRelativePath);
        Assert.Equal(originalLegacy, File.ReadAllText(legacyPath));
        Assert.False(Directory.Exists(paths.Resolve(migrated.RootRelativePath)));
    }

    [Fact]
    public void Existing_versioned_catalog_wins_over_changed_legacy_state()
    {
        var paths = new PortablePathResolver(_testRoot);
        var legacyPath = WriteLegacyFixture(paths);
        _ = new JsonProjectCatalog(paths);
        File.WriteAllText(legacyPath, "{\"activeProjectId\":\"default\",\"projects\":[]}");

        var reloaded = new JsonProjectCatalog(paths);

        Assert.Equal(ProjectCatalogLoadOutcome.Current, reloaded.LoadOutcome);
        Assert.Equal("automation-lab", reloaded.ActiveProjectId);
        Assert.Equal(2, reloaded.Projects.Count);
    }

    [Fact]
    public void Corrupt_current_catalog_is_restored_from_valid_backup()
    {
        var paths = new PortablePathResolver(_testRoot);
        WriteVersionedFixture(paths, ProjectCatalogPath + ".bak");
        Directory.CreateDirectory(Path.GetDirectoryName(ProjectCatalogPath)!);
        File.WriteAllText(ProjectCatalogPath, "{ broken");

        var catalog = new JsonProjectCatalog(paths);

        Assert.Equal(ProjectCatalogLoadOutcome.RecoveredBackup, catalog.LoadOutcome);
        Assert.Equal("automation-lab", catalog.ActiveProjectId);
        var restored = JsonSerializer.Deserialize<ProjectCatalogDocument>(File.ReadAllText(ProjectCatalogPath), JsonOptions);
        Assert.Equal("automation-lab", Assert.IsType<ProjectCatalogDocument>(restored).ActiveProjectId);
    }

    [Fact]
    public void Interrupted_staging_file_is_replaced_without_becoming_authoritative()
    {
        var paths = new PortablePathResolver(_testRoot);
        var partPath = ProjectCatalogPath + ".part";
        Directory.CreateDirectory(Path.GetDirectoryName(partPath)!);
        File.WriteAllText(partPath, "{ interrupted");

        var catalog = new JsonProjectCatalog(paths);

        Assert.Equal(ProjectCatalogLoadOutcome.DefaultCreated, catalog.LoadOutcome);
        Assert.False(File.Exists(partPath));
        Assert.Equal(ProjectCatalogDefaults.DefaultProjectId, catalog.ActiveProjectId);
    }

    [Fact]
    public void Invalid_legacy_records_are_skipped_and_missing_active_falls_back_to_default()
    {
        var paths = new PortablePathResolver(_testRoot);
        var config = paths.EnsureDirectory(Path.Combine("instances", "default", "config"));
        File.WriteAllText(
            Path.Combine(config, "web-projects.json"),
            """
            {
              "activeProjectId": "unsafe",
              "projects": [
                {
                  "id": "unsafe",
                  "name": "Unsafe",
                  "projectRootRelativePath": "../outside",
                  "webRootRelativePath": "public"
                }
              ]
            }
            """);

        var catalog = new JsonProjectCatalog(paths);

        Assert.Equal(ProjectCatalogLoadOutcome.LegacyMigrated, catalog.LoadOutcome);
        Assert.Equal(ProjectCatalogDefaults.DefaultProjectId, catalog.ActiveProjectId);
        Assert.Single(catalog.Projects);
    }

    [Fact]
    public void Catalog_mutations_are_persisted_and_unregistering_keeps_files()
    {
        var paths = new PortablePathResolver(_testRoot);
        var catalog = new JsonProjectCatalog(paths);
        var project = new PortableProject(
            "sample",
            "Sample",
            Path.Combine("instances", "default", "projects", "sample"));
        var root = paths.Resolve(project.RootRelativePath);
        Directory.CreateDirectory(root);
        var marker = Path.Combine(root, "keep.txt");
        File.WriteAllText(marker, "keep");

        catalog.Add(project);
        catalog.Update(project with { Name = "Renamed" });
        var reloaded = new JsonProjectCatalog(paths);

        Assert.Equal("sample", reloaded.ActiveProjectId);
        Assert.Equal("Renamed", reloaded.GetRequired("sample").Name);
        reloaded.Remove("sample");
        Assert.Equal(ProjectCatalogDefaults.DefaultProjectId, reloaded.ActiveProjectId);
        Assert.True(File.Exists(marker));
        Assert.True(File.Exists(ProjectCatalogPath + ".bak"));
    }

    [Fact]
    public void Missing_registered_project_directory_is_valid_and_not_created_on_load()
    {
        var paths = new PortablePathResolver(_testRoot);
        WriteVersionedFixture(paths, ProjectCatalogPath);

        var catalog = new JsonProjectCatalog(paths);

        Assert.Equal(2, catalog.Projects.Count);
        Assert.False(Directory.Exists(paths.Resolve(Path.Combine(
            "instances",
            "default",
            "projects",
            "automation-lab"))));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private string ProjectCatalogPath => Path.Combine(
        _testRoot,
        "instances",
        "default",
        "config",
        "projects.json");

    private static string WriteLegacyFixture(PortablePathResolver paths)
    {
        var config = paths.EnsureDirectory(Path.Combine("instances", "default", "config"));
        var target = Path.Combine(config, "web-projects.json");
        File.Copy(GetFixturePath("legacy-web-projects.json"), target);
        return target;
    }

    private static void WriteVersionedFixture(PortablePathResolver paths, string target)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
        File.Copy(GetFixturePath("projects-v2.json"), target);
    }

    private static string GetFixturePath(string name) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", "Projects", name);
}
