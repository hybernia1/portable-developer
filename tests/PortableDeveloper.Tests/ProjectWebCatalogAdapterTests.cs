using PortableDeveloper.Application.Projects;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Projects;

namespace PortableDeveloper.Tests;

public sealed class ProjectWebCatalogAdapterTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void Create_registers_generic_project_and_prepares_static_web_starter()
    {
        var paths = new PortablePathResolver(_testRoot);
        var catalog = new JsonProjectCatalog(paths);
        var context = new ProjectContext(catalog);
        var adapter = new ProjectWebCatalogAdapter(catalog, context, paths);

        var created = adapter.Create("Český projekt", "public");

        Assert.Equal("cesky-projekt", created.Id);
        Assert.Equal(created.Id, catalog.ActiveProjectId);
        Assert.True(File.Exists(paths.Resolve(Path.Combine(created.DocumentRootRelativePath, "index.html"))));
        Assert.True(Directory.Exists(paths.Resolve(Path.Combine(created.ProjectRootRelativePath, "seldownloads"))));
        Assert.True(catalog.GetRequired(created.Id).Web?.IsEnabled);
    }

    [Fact]
    public void Apache_settings_are_persisted_in_generic_catalog()
    {
        var paths = new PortablePathResolver(_testRoot);
        var catalog = new JsonProjectCatalog(paths);
        var context = new ProjectContext(catalog);
        var adapter = new ProjectWebCatalogAdapter(catalog, context, paths);
        var created = adapter.Create("Static site");

        adapter.SetHtaccess(created.Id, false);
        adapter.SetEnabled(created.Id, false);

        var reloaded = new JsonProjectCatalog(paths).GetRequired(created.Id);
        Assert.False(reloaded.Web?.AllowHtaccess);
        Assert.False(reloaded.Web?.IsEnabled);
    }

    [Fact]
    public void Unregister_preserves_project_files()
    {
        var paths = new PortablePathResolver(_testRoot);
        var catalog = new JsonProjectCatalog(paths);
        var context = new ProjectContext(catalog);
        var adapter = new ProjectWebCatalogAdapter(catalog, context, paths);
        var created = adapter.Create("Keep files");
        var sourceFile = paths.Resolve(Path.Combine(created.ProjectRootRelativePath, "notes.txt"));
        File.WriteAllText(sourceFile, "preserve me");

        adapter.Remove(created.Id);

        Assert.DoesNotContain(catalog.Projects, project => project.Id == created.Id);
        Assert.True(File.Exists(sourceFile));
    }

    [Fact]
    public void Duplicate_project_is_rejected_before_missing_directory_is_created()
    {
        var paths = new PortablePathResolver(_testRoot);
        var catalog = new JsonProjectCatalog(paths);
        var context = new ProjectContext(catalog);
        var existing = new PortableProject(
            "duplicate",
            "Duplicate",
            ProjectCatalogValidator.GetExpectedRootRelativePath("duplicate"),
            new ProjectWebSettings(true, "public"));
        catalog.Add(existing, makeActive: false);
        var adapter = new ProjectWebCatalogAdapter(catalog, context, paths);
        var projectRoot = paths.Resolve(existing.RootRelativePath);

        Assert.Throws<InvalidOperationException>(() => adapter.Create("Duplicate"));

        Assert.False(Directory.Exists(projectRoot));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }
}
