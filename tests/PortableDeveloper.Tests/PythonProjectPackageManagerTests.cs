using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Domain.Processes;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.ProjectTools;

namespace PortableDeveloper.Tests;

public sealed class PythonProjectPackageManagerTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task ListPackagesAsync_reads_only_project_target_directory()
    {
        var service = CreateService(out var runner);
        runner.Result = new PortableCommandResult(0, "[{\"name\":\"selenium\",\"version\":\"4.35.0\"}]", string.Empty);

        var packages = await service.ListPackagesAsync();

        var package = Assert.Single(packages);
        Assert.Equal("selenium", package.Name);
        Assert.True(package.IsDirectDependency);
        Assert.Contains("-I", runner.Definition!.Arguments);
        Assert.Contains("--path", runner.Definition.Arguments);
        Assert.Equal("NUL", runner.Definition.Environment!["PIP_CONFIG_FILE"]);
    }

    [Fact]
    public async Task InstallPackageAsync_builds_argument_list_without_a_shell()
    {
        var service = CreateService(out var runner);

        var result = await service.InstallPackageAsync("selenium", "==4.35.0");

        Assert.True(result.IsSuccess);
        Assert.Contains("selenium==4.35.0", runner.Definition!.Arguments);
        Assert.Contains("--target", runner.Definition.Arguments);
        Assert.Contains("--no-cache-dir", runner.Definition.Arguments);
        Assert.Equal("1", runner.Definition.Environment!["PIP_NO_CACHE_DIR"]);
        Assert.DoesNotContain("cmd.exe", runner.Definition.ExecutableRelativePath, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task InstallPackageAsync_rejects_direct_url()
    {
        var service = CreateService(out var runner);

        var result = await service.InstallPackageAsync("https://example.test/package.whl", string.Empty);

        Assert.False(result.IsSuccess);
        Assert.Null(runner.Definition);
    }

    [Fact]
    public async Task ListPackagesAsync_reports_refresh_and_completion()
    {
        var service = CreateService(out _);
        var progress = new RecordingProgress<ProjectPackageOperationProgress>();

        await service.ListPackagesAsync(progress: progress);

        Assert.Collection(
            progress.Values,
            item => Assert.Equal(ProjectPackageOperationPhase.RefreshingInventory, item.Phase),
            item =>
            {
                Assert.Equal(ProjectPackageOperationPhase.Completed, item.Phase);
                Assert.Equal(100, item.Percentage);
            });
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private PythonProjectPackageManager CreateService(out RecordingRunner runner)
    {
        var runtimeRoot = Path.Combine(_testRoot, "modules", "python", "3.13.0");
        Directory.CreateDirectory(Path.Combine(runtimeRoot, "Lib", "site-packages", "pip"));
        File.WriteAllText(Path.Combine(runtimeRoot, "python.exe"), "python");
        runner = new RecordingRunner();
        return new(
            new ReadyTool(Path.Combine("modules", "python", "3.13.0", "python.exe")),
            runner,
            new PortablePathResolver(_testRoot));
    }

    private sealed class ReadyTool(string entrypoint) : IPortableToolRuntimeInventory
    {
        public PortableToolRuntimeInfo GetRuntime(PortableToolKind kind) =>
            new(kind, true, "3.13.0", entrypoint, "ready");
    }

    private sealed class RecordingRunner : IPortableCommandRunner
    {
        public PortableCommandDefinition? Definition { get; private set; }
        public PortableCommandResult Result { get; set; } = new(0, "[]", string.Empty);

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
