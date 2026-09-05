using PortableDeveloper.Application.Projects;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Projects;

namespace PortableDeveloper.Tests;

public sealed class ProjectCapabilityDetectorTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task Detects_capabilities_from_bounded_names_and_small_content_without_execution()
    {
        var paths = new PortablePathResolver(_testRoot);
        var project = CreateProject("mixed");
        var root = paths.EnsureDirectory(project.RootRelativePath);
        Directory.CreateDirectory(Path.Combine(root, "public"));
        File.WriteAllText(Path.Combine(root, "public", "index.html"), "<h1>safe</h1>");
        File.WriteAllText(Path.Combine(root, "package.json"), "{\"private\":true}");
        File.WriteAllText(Path.Combine(root, "automation.py"), "from selenium import webdriver\nraise RuntimeError('must not run')");

        var snapshot = await new ProjectCapabilityDetector(paths).DetectAsync(project);
        var kinds = snapshot.Capabilities.Select(capability => capability.Kind).ToHashSet();

        Assert.Contains(ProjectCapabilityKind.Web, kinds);
        Assert.Contains(ProjectCapabilityKind.NodeJs, kinds);
        Assert.Contains(ProjectCapabilityKind.Python, kinds);
        Assert.Contains(ProjectCapabilityKind.BrowserAutomation, kinds);
    }

    [Fact]
    public async Task Dependency_directories_and_large_content_are_not_inspected()
    {
        var paths = new PortablePathResolver(_testRoot);
        var project = CreateProject("bounded");
        var root = paths.EnsureDirectory(project.RootRelativePath);
        var dependencies = Directory.CreateDirectory(Path.Combine(root, "node_modules", "hidden"));
        File.WriteAllText(Path.Combine(dependencies.FullName, "selenium.py"), "from selenium import webdriver");
        File.WriteAllBytes(Path.Combine(root, "large.py"), new byte[130 * 1024]);

        var snapshot = await new ProjectCapabilityDetector(paths).DetectAsync(project);

        Assert.Contains(snapshot.Capabilities, capability => capability.Kind == ProjectCapabilityKind.Python);
        Assert.DoesNotContain(snapshot.Capabilities, capability => capability.Kind == ProjectCapabilityKind.BrowserAutomation);
        Assert.DoesNotContain(snapshot.Capabilities.SelectMany(capability => capability.Evidence),
            value => value.Contains("node_modules", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Missing_project_directory_returns_only_persisted_web_capability()
    {
        var paths = new PortablePathResolver(_testRoot);
        var project = CreateProject("missing") with { Web = new ProjectWebSettings(false) };

        var snapshot = await new ProjectCapabilityDetector(paths).DetectAsync(project);

        var capability = Assert.Single(snapshot.Capabilities);
        Assert.Equal(ProjectCapabilityKind.Web, capability.Kind);
    }

    [Fact]
    public async Task Cancellation_is_observed_before_scanning()
    {
        var paths = new PortablePathResolver(_testRoot);
        var project = CreateProject("cancelled");
        paths.EnsureDirectory(project.RootRelativePath);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            new ProjectCapabilityDetector(paths).DetectAsync(project, cancellation.Token));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private static PortableProject CreateProject(string id) => new(
        id,
        id,
        ProjectCatalogValidator.GetExpectedRootRelativePath(id));
}
