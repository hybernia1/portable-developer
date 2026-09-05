using PortableDeveloper.Application.Projects;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Projects;

namespace PortableDeveloper.Tests;

public sealed class ProjectTemplateServiceTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Theory]
    [InlineData(ProjectTemplateKind.Empty, null)]
    [InlineData(ProjectTemplateKind.Web, "public/index.html")]
    [InlineData(ProjectTemplateKind.Python, "main.py")]
    [InlineData(ProjectTemplateKind.BrowserAutomation, "selenium_example.py")]
    [InlineData(ProjectTemplateKind.NodeJs, "package.json")]
    public async Task Templates_stage_content_and_register_only_after_completion(
        ProjectTemplateKind template,
        string? expectedFile)
    {
        var paths = new PortablePathResolver(_testRoot);
        var catalog = new JsonProjectCatalog(paths);
        var service = new ProjectTemplateService(paths, catalog, new ProjectContext(catalog));

        var result = await service.CreateAsync(new ProjectTemplateRequest($"{template} sample", template));

        Assert.Equal(result.Project.Id, catalog.ActiveProjectId);
        Assert.Equal(template == ProjectTemplateKind.Web, result.Project.Web?.IsEnabled == true);
        Assert.True(Directory.Exists(paths.Resolve(result.Project.RootRelativePath)));
        if (expectedFile is not null)
        {
            Assert.True(File.Exists(paths.Resolve(Path.Combine(result.Project.RootRelativePath, expectedFile))));
        }

        Assert.Empty(Directory.EnumerateDirectories(
            paths.Resolve(Path.Combine("instances", "default", "projects")),
            ".stage-*"));
    }

    [Fact]
    public async Task Existing_target_is_never_replaced_or_registered_as_partial_content()
    {
        var paths = new PortablePathResolver(_testRoot);
        var catalog = new JsonProjectCatalog(paths);
        var service = new ProjectTemplateService(paths, catalog, new ProjectContext(catalog));
        var root = paths.EnsureDirectory(Path.Combine("instances", "default", "projects", "occupied"));
        var marker = Path.Combine(root, "keep.txt");
        File.WriteAllText(marker, "keep");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CreateAsync(new ProjectTemplateRequest("Occupied", ProjectTemplateKind.Web)));

        Assert.DoesNotContain(catalog.Projects, project => project.Id == "occupied");
        Assert.Equal("keep", File.ReadAllText(marker));
    }

    [Fact]
    public async Task Web_template_writes_starter_to_the_requested_contained_web_root()
    {
        var paths = new PortablePathResolver(_testRoot);
        var catalog = new JsonProjectCatalog(paths);
        var service = new ProjectTemplateService(paths, catalog, new ProjectContext(catalog));

        var result = await service.CreateAsync(new ProjectTemplateRequest(
            "Root web",
            ProjectTemplateKind.Web,
            "."));

        Assert.True(File.Exists(paths.Resolve(Path.Combine(result.Project.RootRelativePath, "index.html"))));
        Assert.Equal(".", result.Project.Web?.RootRelativePath);
    }

    [Fact]
    public async Task Successful_creation_activates_through_the_shared_context_once()
    {
        var paths = new PortablePathResolver(_testRoot);
        var catalog = new JsonProjectCatalog(paths);
        var context = new ProjectContext(catalog);
        var service = new ProjectTemplateService(paths, catalog, context);
        var changes = 0;
        context.Changed += (_, _) => changes++;

        var result = await service.CreateAsync(new ProjectTemplateRequest("Observed", ProjectTemplateKind.Empty));

        Assert.Equal(result.Project.Id, context.ActiveProject.Id);
        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task Existing_managed_directory_is_registered_without_content_changes()
    {
        var paths = new PortablePathResolver(_testRoot);
        var catalog = new JsonProjectCatalog(paths);
        var service = new ProjectTemplateService(paths, catalog, new ProjectContext(catalog));
        var root = paths.EnsureDirectory(Path.Combine("instances", "default", "projects", "existing-work"));
        var marker = Path.Combine(root, "app.py");
        File.WriteAllText(marker, "print('unchanged')");

        var candidate = Assert.Single(service.GetRegistrableDirectories());
        var project = await service.RegisterExistingAsync(candidate.DirectoryId, "Existing work");

        Assert.Equal("existing-work", project.Id);
        Assert.Null(project.Web);
        Assert.Equal("print('unchanged')", File.ReadAllText(marker));
        Assert.Empty(service.GetRegistrableDirectories());
    }

    [Fact]
    public async Task Cancelled_creation_leaves_no_catalog_record_or_staging_directory()
    {
        var paths = new PortablePathResolver(_testRoot);
        var catalog = new JsonProjectCatalog(paths);
        var service = new ProjectTemplateService(paths, catalog, new ProjectContext(catalog));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.CreateAsync(new ProjectTemplateRequest("Cancelled", ProjectTemplateKind.NodeJs), cancellation.Token));

        Assert.DoesNotContain(catalog.Projects, project => project.Id == "cancelled");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }
}
