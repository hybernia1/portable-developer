using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Application.Scheduling;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Projects;
using PortableDeveloper.Infrastructure.Scheduling;

namespace PortableDeveloper.Tests;

public sealed class PortableScheduledTaskExecutorTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task Python_task_uses_captured_project_and_literal_arguments()
    {
        var runner = new RecordingRunner();
        var executor = CreateExecutor(runner);
        var script = Path.Combine(_testRoot, "instances", "default", "www", "scripts", "job.py");
        Directory.CreateDirectory(Path.GetDirectoryName(script)!);
        File.WriteAllText(script, "print('ok')");
        var task = CreateTask() with { Arguments = "--name \"portable task\" & literal" };

        var result = await executor.ExecuteAsync(task);

        Assert.True(result.IsSuccess);
        Assert.NotNull(runner.Definition);
        Assert.Equal(Path.Combine("instances", "default", "www"), runner.Definition.WorkingDirectoryRelativePath);
        Assert.Equal([Path.Combine("scripts", "job.py"), "--name", "portable task", "&", "literal"], runner.Definition.Arguments);
        Assert.Equal("1", runner.Definition.Environment!["PYTHONNOUSERSITE"]);
        Assert.DoesNotContain(Environment.GetEnvironmentVariable("PATH") ?? string.Empty, runner.Definition.Environment["PATH"]);
    }

    [Fact]
    public async Task Missing_script_fails_without_starting_a_process()
    {
        var runner = new RecordingRunner();
        var executor = CreateExecutor(runner);

        await Assert.ThrowsAsync<FileNotFoundException>(() => executor.ExecuteAsync(CreateTask()));

        Assert.Null(runner.Definition);
    }

    [Fact]
    public async Task Npm_task_uses_verified_node_and_portable_cache()
    {
        var runner = new RecordingRunner();
        var executor = CreateExecutor(runner);
        var npmCli = Path.Combine(_testRoot, "modules", "node", "24.19.0", "node_modules", "npm", "bin", "npm-cli.js");
        Directory.CreateDirectory(Path.GetDirectoryName(npmCli)!);
        File.WriteAllText(npmCli, "test");
        var task = CreateTask() with
        {
            CommandKind = ScheduledTaskCommandKind.NpmScript,
            Target = "backup",
            Arguments = "--silent"
        };

        var result = await executor.ExecuteAsync(task);

        Assert.True(result.IsSuccess);
        Assert.NotNull(runner.Definition);
        Assert.Equal(Path.Combine("modules", "node", "24.19.0", "node.exe"), runner.Definition.ExecutableRelativePath);
        Assert.Equal("run", runner.Definition.Arguments[1]);
        Assert.Equal("backup", runner.Definition.Arguments[2]);
        Assert.StartsWith(_testRoot, runner.Definition.Environment!["NPM_CONFIG_CACHE"], StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private PortableScheduledTaskExecutor CreateExecutor(RecordingRunner runner)
    {
        Directory.CreateDirectory(Path.Combine(_testRoot, "instances", "default", "www"));
        var paths = new PortablePathResolver(_testRoot);
        return new PortableScheduledTaskExecutor(
            new JsonProjectCatalog(paths),
            new ReadyModules(),
            new ReadyTools(),
            runner,
            paths);
    }

    private static PortableScheduledTask CreateTask() => new(
        "job",
        "default",
        "Job",
        ScheduledTaskCommandKind.PythonScript,
        Path.Combine("scripts", "job.py"),
        string.Empty,
        new ScheduledTaskSchedule(ScheduledTaskScheduleKind.Interval, IntervalMinutes: 10));

    private sealed class ReadyModules : IModuleInstallationVerifier
    {
        public ModuleInstallationVerification Verify(ModuleKind kind, string displayName) => kind == ModuleKind.Php
            ? new(new ModuleInstallation(kind, "8.4.12", Path.Combine("modules", "php", "8.4.12"), Path.Combine("modules", "php", "8.4.12", "php-cgi.exe")), string.Empty)
            : new(null, "Unavailable in test.");
    }

    private sealed class ReadyTools : IPortableToolRuntimeInventory
    {
        public PortableToolRuntimeInfo GetRuntime(PortableToolKind kind) => kind switch
        {
            PortableToolKind.Python => new(kind, true, "3.13.0", Path.Combine("modules", "python", "3.13.0", "python.exe"), string.Empty),
            PortableToolKind.Node => new(kind, true, "24.19.0", Path.Combine("modules", "node", "24.19.0", "node.exe"), string.Empty),
            _ => new(kind, false, string.Empty, string.Empty, "Unavailable in test.")
        };
    }

    private sealed class RecordingRunner : IPortableCommandRunner
    {
        public PortableCommandDefinition? Definition { get; private set; }

        public Task<PortableCommandResult> RunAsync(PortableCommandDefinition definition, CancellationToken cancellationToken = default)
        {
            Definition = definition;
            return Task.FromResult(new PortableCommandResult(0, "ok", string.Empty));
        }
    }
}
