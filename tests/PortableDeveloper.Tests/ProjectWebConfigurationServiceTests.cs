using PortableDeveloper.Application.Projects;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Projects;

namespace PortableDeveloper.Tests;

public sealed class ProjectWebConfigurationServiceTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void Configure_enables_existing_project_without_rewriting_source_files()
    {
        var (paths, catalog, service, project) = CreateProject();
        var sourcePath = paths.Resolve(Path.Combine(project.RootRelativePath, "main.py"));
        File.WriteAllText(sourcePath, "print('preserve me')");

        var result = service.Configure(project.Id, new ProjectWebSettings(true, "public", false));

        Assert.True(result.WebRootDirectoryCreated);
        Assert.True(result.StarterFileCreated);
        Assert.True(Directory.Exists(paths.Resolve(Path.Combine(project.RootRelativePath, "public"))));
        Assert.True(File.Exists(paths.Resolve(Path.Combine(project.RootRelativePath, "public", "index.html"))));
        Assert.Equal("print('preserve me')", File.ReadAllText(sourcePath));
        Assert.Equal(new ProjectWebSettings(true, "public", false), catalog.GetRequired(project.Id).Web);
    }

    [Fact]
    public void Configure_preserves_an_existing_html_entry_point()
    {
        var (paths, _, service, project) = CreateProject();
        var webRoot = paths.EnsureDirectory(Path.Combine(project.RootRelativePath, "public"));
        var indexPath = Path.Combine(webRoot, "index.html");
        File.WriteAllText(indexPath, "preserve me");

        var result = service.Configure(project.Id, new ProjectWebSettings(true, "public", false));

        Assert.False(result.WebRootDirectoryCreated);
        Assert.False(result.StarterFileCreated);
        Assert.Equal("preserve me", File.ReadAllText(indexPath));
    }

    [Fact]
    public void Configure_does_not_create_a_starter_while_web_support_is_disabled()
    {
        var (paths, _, service, project) = CreateProject();

        var result = service.Configure(project.Id, new ProjectWebSettings(false, "public", false));

        Assert.True(result.WebRootDirectoryCreated);
        Assert.False(result.StarterFileCreated);
        Assert.False(File.Exists(paths.Resolve(Path.Combine(project.RootRelativePath, "public", "index.html"))));
    }

    [Fact]
    public void Configure_can_disable_web_without_removing_files_or_settings()
    {
        var (paths, catalog, service, project) = CreateProject();
        service.Configure(project.Id, new ProjectWebSettings(true, "site", true));
        var webFile = paths.Resolve(Path.Combine(project.RootRelativePath, "site", "index.html"));
        File.WriteAllText(webFile, "preserve me");

        var result = service.Configure(project.Id, new ProjectWebSettings(false, "site", true));

        Assert.False(result.Project.Web!.IsEnabled);
        Assert.Equal("site", result.Project.Web.RootRelativePath);
        Assert.True(result.Project.Web.AllowHtaccess);
        Assert.Equal("preserve me", File.ReadAllText(webFile));
        Assert.False(catalog.GetRequired(project.Id).Web!.IsEnabled);
    }

    [Fact]
    public void Configure_rejects_unsafe_or_file_occupied_web_roots_before_catalog_update()
    {
        var (paths, catalog, service, project) = CreateProject();
        var occupiedPath = paths.Resolve(Path.Combine(project.RootRelativePath, "occupied"));
        File.WriteAllText(occupiedPath, "file");

        Assert.Throws<InvalidDataException>(() =>
            service.Configure(project.Id, new ProjectWebSettings(true, "../outside")));
        Assert.Throws<IOException>(() =>
            service.Configure(project.Id, new ProjectWebSettings(true, "occupied/subdirectory")));

        Assert.Null(catalog.GetRequired(project.Id).Web);
        Assert.False(Directory.Exists(paths.Resolve(Path.Combine("instances", "default", "projects", "outside"))));
    }

    [Fact]
    public void Configure_does_not_allow_disabling_default_localhost()
    {
        var paths = new PortablePathResolver(_testRoot);
        var catalog = new JsonProjectCatalog(paths);
        var service = new ProjectWebConfigurationService(paths, catalog);

        Assert.Throws<InvalidOperationException>(() => service.Configure(
            ProjectCatalogDefaults.DefaultProjectId,
            new ProjectWebSettings(false, ".")));

        Assert.True(catalog.GetRequired(ProjectCatalogDefaults.DefaultProjectId).Web!.IsEnabled);
    }

    private (PortablePathResolver Paths, JsonProjectCatalog Catalog, ProjectWebConfigurationService Service, PortableProject Project)
        CreateProject()
    {
        var paths = new PortablePathResolver(_testRoot);
        var catalog = new JsonProjectCatalog(paths);
        var project = new PortableProject(
            "python-app",
            "Python app",
            ProjectCatalogValidator.GetExpectedRootRelativePath("python-app"));
        Directory.CreateDirectory(paths.Resolve(project.RootRelativePath));
        catalog.Add(project, makeActive: false);
        return (paths, catalog, new ProjectWebConfigurationService(paths, catalog), project);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }
}
