using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Domain.Processes;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.ProjectTools;
using PortableDeveloper.Infrastructure.Projects;

namespace PortableDeveloper.Tests;

public sealed class NpmProjectPackageManagerTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task ListPackagesAsync_uses_verified_node_and_marks_root_dependencies()
    {
        var service = CreateService(out var runner);
        var project = Path.Combine(_testRoot, "instances", "default", "www");
        Directory.CreateDirectory(project);
        File.WriteAllText(Path.Combine(project, "package.json"), "{\"dependencies\":{\"lodash\":\"^4.17.21\"}}");
        runner.Result = new PortableCommandResult(
            0,
            "{\"dependencies\":{\"lodash\":{\"version\":\"4.17.21\",\"dependencies\":{\"dep\":{\"version\":\"1.0.0\"}}}}}",
            string.Empty);

        var packages = await service.ListPackagesAsync();

        Assert.Equal(2, packages.Count);
        Assert.Equal("lodash", packages[0].Name);
        Assert.True(packages[0].IsDirectDependency);
        var definition = Assert.IsType<PortableCommandDefinition>(runner.Definition);
        Assert.Contains("ls", definition.Arguments);
        Assert.Contains("--ignore-scripts", definition.Arguments);
        Assert.EndsWith(Path.Combine("node", "24.19.0", "node.exe"), definition.ExecutableRelativePath, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(Path.Combine("instances", "default", "www"), definition.WorkingDirectoryRelativePath);
        var environment = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(definition.Environment);
        Assert.Equal("true", environment["NPM_CONFIG_IGNORE_SCRIPTS"]);
    }

    [Fact]
    public async Task InstallPackageAsync_rejects_urls_without_starting_npm()
    {
        var service = CreateService(out var runner);

        var result = await service.InstallPackageAsync("https://example.test/package.tgz", string.Empty);

        Assert.False(result.IsSuccess);
        Assert.Null(runner.Definition);
    }

    [Fact]
    public async Task InstallPackageAsync_uses_save_and_disables_package_scripts()
    {
        var service = CreateService(out var runner);

        var result = await service.InstallPackageAsync("@types/node", "^24.0.0");

        Assert.True(result.IsSuccess);
        var definition = Assert.IsType<PortableCommandDefinition>(runner.Definition);
        Assert.Contains("install", definition.Arguments);
        Assert.Contains("@types/node@^24.0.0", definition.Arguments);
        Assert.Contains("--save", definition.Arguments);
        Assert.Contains("--ignore-scripts", definition.Arguments);
        Assert.Contains("--no-audit", definition.Arguments);
    }

    [Fact]
    public async Task RemovePackageAsync_uses_non_interactive_script_free_npm_uninstall()
    {
        var service = CreateService(out var runner);

        var result = await service.RemovePackageAsync("lodash");

        Assert.True(result.IsSuccess);
        var definition = Assert.IsType<PortableCommandDefinition>(runner.Definition);
        Assert.Contains("uninstall", definition.Arguments);
        Assert.Contains("lodash", definition.Arguments);
        Assert.Contains("--ignore-scripts", definition.Arguments);
        var environment = Assert.IsAssignableFrom<IReadOnlyDictionary<string, string>>(definition.Environment);
        Assert.Equal("true", environment["NPM_CONFIG_YES"]);
    }

    [Fact]
    public async Task Active_web_project_changes_npm_working_directory()
    {
        var service = CreateService(out var runner, out var projects);
        var project = projects.Create("Second app");

        await service.InstallPackageAsync("lodash", "^4.17.21");

        Assert.Equal(project.ProjectRootRelativePath, service.ProjectRelativePath);
        Assert.Equal(project.ProjectRootRelativePath, runner.Definition!.WorkingDirectoryRelativePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private NpmProjectPackageManager CreateService(out RecordingRunner runner) => CreateService(out runner, out _);

    private NpmProjectPackageManager CreateService(out RecordingRunner runner, out JsonWebProjectCatalog projects)
    {
        var nodeRoot = Path.Combine(_testRoot, "modules", "node", "24.19.0");
        Directory.CreateDirectory(Path.Combine(nodeRoot, "node_modules", "npm", "bin"));
        File.WriteAllText(Path.Combine(nodeRoot, "node.exe"), "node");
        File.WriteAllText(Path.Combine(nodeRoot, "node_modules", "npm", "bin", "npm-cli.js"), "npm");
        runner = new RecordingRunner();
        var paths = new PortablePathResolver(_testRoot);
        projects = new JsonWebProjectCatalog(paths);
        return new NpmProjectPackageManager(
            new ReadyTool(Path.Combine("modules", "node", "24.19.0", "node.exe")),
            runner,
            paths,
            new ProjectContext(new LegacyWebProjectCatalogAdapter(projects)));
    }

    private sealed class ReadyTool(string entrypoint) : IPortableToolRuntimeInventory
    {
        public PortableToolRuntimeInfo GetRuntime(PortableToolKind kind) =>
            new(kind, true, "24.19.0", entrypoint, "ready");
    }

    private sealed class RecordingRunner : IPortableCommandRunner
    {
        public PortableCommandDefinition? Definition { get; private set; }
        public PortableCommandResult Result { get; set; } = new(0, "{}", string.Empty);

        public Task<PortableCommandResult> RunAsync(PortableCommandDefinition definition, CancellationToken cancellationToken = default)
        {
            Definition = definition;
            return Task.FromResult(Result);
        }
    }
}
