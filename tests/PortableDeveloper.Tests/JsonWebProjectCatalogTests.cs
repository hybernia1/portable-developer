using PortableDeveloper.Application.Projects;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Projects;

namespace PortableDeveloper.Tests;

public sealed class JsonWebProjectCatalogTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void New_catalog_preserves_legacy_www_as_default_project()
    {
        var catalog = new JsonWebProjectCatalog(new PortablePathResolver(_testRoot));

        var project = Assert.Single(catalog.Projects);
        Assert.Equal(WebProjectCatalogDefaults.DefaultProjectId, project.Id);
        Assert.Equal(Path.Combine("instances", "default", "www"), project.ProjectRootRelativePath);
        Assert.Equal("localhost", project.HostName);
        Assert.True(Directory.Exists(Path.Combine(_testRoot, project.ProjectRootRelativePath)));
        Assert.True(Directory.Exists(Path.Combine(_testRoot, project.ProjectRootRelativePath, "seldownloads")));
    }

    [Fact]
    public void Create_uses_private_project_root_public_web_root_and_persists_selection()
    {
        var paths = new PortablePathResolver(_testRoot);
        var catalog = new JsonWebProjectCatalog(paths);

        var created = catalog.Create("Customer Portal");
        var reloaded = new JsonWebProjectCatalog(paths);

        Assert.Equal("customer-portal", created.Id);
        Assert.Equal("customer-portal.localhost", created.HostName);
        Assert.Equal(Path.Combine("instances", "default", "projects", "customer-portal"), created.ProjectRootRelativePath);
        Assert.Equal("public", created.WebRootRelativePath);
        Assert.Equal(created.Id, reloaded.ActiveProject.Id);
        Assert.True(File.Exists(paths.Resolve(Path.Combine(created.DocumentRootRelativePath, "index.php"))));
        Assert.True(Directory.Exists(paths.Resolve(Path.Combine(created.ProjectRootRelativePath, "seldownloads"))));
        var json = File.ReadAllText(paths.Resolve("instances/default/config/web-projects.json"));
        Assert.DoesNotContain("hostName", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("documentRootRelativePath", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Remove_keeps_project_files_and_returns_tools_to_default()
    {
        var paths = new PortablePathResolver(_testRoot);
        var catalog = new JsonWebProjectCatalog(paths);
        var created = catalog.Create("Temporary site");
        var marker = paths.Resolve(Path.Combine(created.ProjectRootRelativePath, "keep.txt"));
        File.WriteAllText(marker, "keep");

        catalog.Remove(created.Id);

        Assert.Equal(WebProjectCatalogDefaults.DefaultProjectId, catalog.ActiveProject.Id);
        Assert.DoesNotContain(catalog.Projects, project => project.Id == created.Id);
        Assert.True(File.Exists(marker));
    }

    [Fact]
    public void Create_refuses_web_root_escape()
    {
        var catalog = new JsonWebProjectCatalog(new PortablePathResolver(_testRoot));

        Assert.Throws<ArgumentException>(() => catalog.Create("Unsafe", "../outside"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }
}
