using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.ProjectTools;
using PortableDeveloper.Infrastructure.Projects;

namespace PortableDeveloper.Tests;

public sealed class ComposerProjectPackageManagerTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task ListPackagesAsync_uses_verified_php_and_marks_root_requirements()
    {
        var service = CreateService(out var runner);
        var project = Path.Combine(_testRoot, "instances", "default", "www");
        Directory.CreateDirectory(project);
        File.WriteAllText(Path.Combine(project, "composer.json"), "{\"require\":{\"php-webdriver/webdriver\":\"^1.15\"}}");
        runner.Result = new PortableCommandResult(
            0,
            "{\"installed\":[{\"name\":\"php-webdriver/webdriver\",\"version\":\"1.15.2\",\"description\":\"WebDriver client\"},{\"name\":\"symfony/process\",\"version\":\"v7.3.0\"}]}",
            string.Empty);

        var packages = await service.ListPackagesAsync();

        Assert.Equal(2, packages.Count);
        Assert.True(packages[0].IsDirectDependency);
        Assert.Equal("php-webdriver/webdriver", packages[0].Name);
        Assert.EndsWith(Path.Combine("php", "8.4.12", "php.exe"), runner.Definition!.ExecutableRelativePath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("-n", runner.Definition.Arguments);
        Assert.Contains("--no-plugins", runner.Definition.Arguments);
        Assert.Equal(Path.Combine("instances", "default", "www"), runner.Definition.WorkingDirectoryRelativePath);
    }

    [Fact]
    public async Task InstallPackageAsync_rejects_urls_without_starting_composer()
    {
        var service = CreateService(out var runner);

        var result = await service.InstallPackageAsync("https://example.test/package.zip", string.Empty);

        Assert.False(result.IsSuccess);
        Assert.Null(runner.Definition);
    }

    [Fact]
    public async Task ListPackagesAsync_accepts_array_root_after_last_requirement_is_removed()
    {
        var service = CreateService(out var runner);
        var project = Path.Combine(_testRoot, "instances", "default", "www");
        Directory.CreateDirectory(project);
        File.WriteAllText(Path.Combine(project, "composer.json"), "[]");
        runner.Result = new PortableCommandResult(
            0,
            "{\"installed\":[{\"name\":\"symfony/process\",\"version\":\"v8.1.0\"}]}",
            string.Empty);

        var packages = await service.ListPackagesAsync();

        var package = Assert.Single(packages);
        Assert.Equal("symfony/process", package.Name);
        Assert.False(package.IsDirectDependency);
    }

    [Fact]
    public async Task ListPackagesAsync_accepts_empty_array_from_composer_after_last_package_is_removed()
    {
        var service = CreateService(out var runner);
        var project = Path.Combine(_testRoot, "instances", "default", "www");
        Directory.CreateDirectory(project);
        File.WriteAllText(Path.Combine(project, "composer.json"), "{}");
        runner.Result = new PortableCommandResult(0, "[]", string.Empty);

        var packages = await service.ListPackagesAsync();

        Assert.Empty(packages);
    }

    [Fact]
    public async Task RemovePackageAsync_uses_non_interactive_composer_remove()
    {
        var service = CreateService(out var runner);

        var result = await service.RemovePackageAsync("php-webdriver/webdriver");

        Assert.True(result.IsSuccess);
        Assert.NotNull(runner.Definition);
        Assert.Contains("remove", runner.Definition.Arguments);
        Assert.Contains("php-webdriver/webdriver", runner.Definition.Arguments);
        Assert.Contains("--no-plugins", runner.Definition.Arguments);
        Assert.Contains("--no-scripts", runner.Definition.Arguments);
    }

    [Fact]
    public async Task Active_web_project_changes_composer_working_directory()
    {
        var service = CreateService(out var runner, out var projects);
        var project = projects.Create("Second app");

        await service.InstallPackageAsync("php-webdriver/webdriver", "^1.15");

        Assert.Equal(project.ProjectRootRelativePath, service.ProjectRelativePath);
        Assert.Equal(project.ProjectRootRelativePath, runner.Definition!.WorkingDirectoryRelativePath);
    }

    [Fact]
    public async Task InstallPackageAsync_reports_real_operation_phases()
    {
        var service = CreateService(out _);
        var progress = new RecordingProgress<ProjectPackageOperationProgress>();

        var result = await service.InstallPackageAsync(
            "php-webdriver/webdriver",
            "^1.15",
            progress: progress);

        Assert.True(result.IsSuccess);
        Assert.Collection(
            progress.Values,
            item => Assert.Equal(ProjectPackageOperationPhase.Preparing, item.Phase),
            item => Assert.Equal(ProjectPackageOperationPhase.RunningPackageManager, item.Phase),
            item =>
            {
                Assert.Equal(ProjectPackageOperationPhase.Completed, item.Phase);
                Assert.False(item.IsIndeterminate);
                Assert.Equal(100, item.Percentage);
            });
    }

    [Fact]
    public async Task InstallPackageAsync_returns_concise_not_found_error_with_suggestion()
    {
        var service = CreateService(out var runner);
        runner.Result = new PortableCommandResult(
            1,
            string.Empty,
            """
            Installation failed, deleting ./composer.json.
            Could not find package php-webriver/webdriver.
            Did you mean this?
                php-webdriver/webdriver
            require [--dev] [--dry-run] [--prefer-source] [many more options]
            """);

        var result = await service.InstallPackageAsync("php-webriver/webdriver", string.Empty);

        Assert.False(result.IsSuccess);
        Assert.Equal(
            "Composer could not find package php-webriver/webdriver. Did you mean php-webdriver/webdriver?",
            result.Detail);
        Assert.DoesNotContain("require [", result.Detail, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InstallPackageAsync_reports_promotion_of_existing_transitive_package()
    {
        var service = CreateService(out var runner);
        var project = Path.Combine(_testRoot, "instances", "default", "www");
        File.WriteAllText(Path.Combine(project, "composer.json"), "{}");
        File.WriteAllText(
            Path.Combine(project, "composer.lock"),
            "{\"packages\":[{\"name\":\"symfony/process\"}]}");

        var result = await service.InstallPackageAsync("symfony/process", "^8.0");

        Assert.True(result.IsSuccess);
        Assert.Equal(PackageOperationOutcome.PromotedToDirect, result.Outcome);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private ComposerProjectPackageManager CreateService(out RecordingRunner runner)
    {
        return CreateService(out runner, out _);
    }

    private ComposerProjectPackageManager CreateService(out RecordingRunner runner, out JsonWebProjectCatalog projects)
    {
        var phpRoot = Path.Combine(_testRoot, "modules", "php", "8.4.12");
        Directory.CreateDirectory(Path.Combine(phpRoot, "ext"));
        File.WriteAllText(Path.Combine(phpRoot, "php.exe"), "php");
        foreach (var extension in new[] { "php_curl.dll", "php_fileinfo.dll", "php_mbstring.dll", "php_openssl.dll", "php_zip.dll" })
        {
            File.WriteAllText(Path.Combine(phpRoot, "ext", extension), "extension");
        }

        var composerPath = Path.Combine(_testRoot, "modules", "composer", "2.10.2", "composer.phar");
        Directory.CreateDirectory(Path.GetDirectoryName(composerPath)!);
        File.WriteAllText(composerPath, "composer");
        var installation = new ModuleInstallation(
            ModuleKind.Php,
            "8.4.12",
            Path.Combine("modules", "php", "8.4.12"),
            Path.Combine("modules", "php", "8.4.12", "php-cgi.exe"));
        runner = new RecordingRunner();
        var paths = new PortablePathResolver(_testRoot);
        projects = new JsonWebProjectCatalog(paths);
        return new(
            new ReadyTool(PortableToolKind.Composer, "2.10.2", Path.Combine("modules", "composer", "2.10.2", "composer.phar")),
            new VerifiedModule(installation),
            runner,
            paths,
            projects);
    }

    private sealed class ReadyTool(PortableToolKind kind, string version, string entrypoint) : IPortableToolRuntimeInventory
    {
        public PortableToolRuntimeInfo GetRuntime(PortableToolKind requestedKind) =>
            new(kind, true, version, entrypoint, "ready");
    }

    private sealed class VerifiedModule(ModuleInstallation installation) : IModuleInstallationVerifier
    {
        public ModuleInstallationVerification Verify(ModuleKind kind, string displayName) => new(installation, string.Empty);
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

    private sealed class RecordingProgress<T> : IProgress<T>
    {
        public List<T> Values { get; } = [];

        public void Report(T value) => Values.Add(value);
    }
}
