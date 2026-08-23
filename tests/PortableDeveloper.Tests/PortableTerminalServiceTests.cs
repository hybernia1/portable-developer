using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Application.Workspace;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Projects;
using PortableDeveloper.Infrastructure.Workspace;

namespace PortableDeveloper.Tests;

public sealed class PortableTerminalServiceTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task Internal_navigation_cannot_leave_www()
    {
        var service = CreateService();

        var result = await service.ExecuteAsync("cd ..", string.Empty);

        Assert.True(result.IsError);
        Assert.Equal(string.Empty, result.WorkingDirectory);
        Assert.Contains("leaves the project workspace", result.Output);
    }

    [Fact]
    public async Task Shell_chaining_is_rejected_without_starting_a_process()
    {
        var runner = new RecordingRunner();
        var service = CreateService(runner);

        var result = await service.ExecuteAsync("php -v & del app.exe", string.Empty);

        Assert.True(result.IsError);
        Assert.Null(runner.Definition);
        Assert.Contains("shell chaining", result.Output);
    }

    [Fact]
    public async Task Service_command_returns_typed_request_for_existing_controllers()
    {
        var service = CreateService();

        var result = await service.ExecuteAsync("service restart selenium", string.Empty);

        Assert.NotNull(result.ServiceRequest);
        Assert.Equal(PortableTerminalServiceOperation.Restart, result.ServiceRequest.Operation);
        Assert.Equal(PortableServiceTarget.Selenium, result.ServiceRequest.Service);
    }

    [Fact]
    public async Task Php_uses_exact_bundled_executable_and_clean_path()
    {
        var runner = new RecordingRunner();
        var service = CreateService(runner);

        var result = await service.ExecuteAsync("php -v", string.Empty);

        Assert.False(result.IsError);
        Assert.NotNull(runner.Definition);
        Assert.Equal(Path.Combine("modules", "php", "8.4.12", "php.exe"), runner.Definition.ExecutableRelativePath);
        Assert.Equal(Path.Combine("instances", "default", "www"), runner.Definition.WorkingDirectoryRelativePath);
        Assert.DoesNotContain(Environment.GetEnvironmentVariable("PATH") ?? string.Empty, runner.Definition.Environment!["PATH"]);
    }

    [Fact]
    public async Task Mkdir_creates_nested_directory_with_spaces_without_external_process()
    {
        var runner = new RecordingRunner();
        var service = CreateService(runner);

        var result = await service.ExecuteAsync("mkdir \"storage/cache data\"", string.Empty);

        Assert.False(result.IsError);
        Assert.Null(runner.Definition);
        Assert.True(Directory.Exists(Path.Combine(WorkspaceRoot, "storage", "cache data")));
        Assert.Contains("www:/storage/cache data", result.Output);
    }

    [Fact]
    public async Task Mkdir_rejects_existing_directory_and_file_collision()
    {
        var service = CreateService();
        Directory.CreateDirectory(Path.Combine(WorkspaceRoot, "existing"));
        File.WriteAllText(Path.Combine(WorkspaceRoot, "occupied"), "test");

        var existing = await service.ExecuteAsync("mkdir existing", string.Empty);
        var occupied = await service.ExecuteAsync("mkdir occupied", string.Empty);

        Assert.True(existing.IsError);
        Assert.Contains("already exists", existing.Output);
        Assert.True(occupied.IsError);
        Assert.Contains("file already exists", occupied.Output);
    }

    [Fact]
    public async Task Mkdir_rejects_absolute_path_and_workspace_escape()
    {
        var service = CreateService();

        var absolute = await service.ExecuteAsync($"mkdir \"{Path.GetPathRoot(_testRoot)}outside\"", string.Empty);
        var escape = await service.ExecuteAsync("mkdir ../outside", string.Empty);

        Assert.True(absolute.IsError);
        Assert.Contains("Absolute paths", absolute.Output);
        Assert.True(escape.IsError);
        Assert.Contains("leaves the project workspace", escape.Output);
    }

    [Fact]
    public async Task Mkdir_rejects_reparse_point_in_existing_path()
    {
        var service = CreateService();
        var outside = Path.Combine(_testRoot, "outside");
        Directory.CreateDirectory(outside);
        try
        {
            Directory.CreateSymbolicLink(Path.Combine(WorkspaceRoot, "linked"), outside);
        }
        catch (IOException)
        {
            // Windows without Developer Mode cannot create the test link.
            return;
        }
        catch (UnauthorizedAccessException)
        {
            return;
        }

        var result = await service.ExecuteAsync("mkdir linked/nested", string.Empty);

        Assert.True(result.IsError);
        Assert.Contains("reparse points", result.Output);
        Assert.False(Directory.Exists(Path.Combine(outside, "nested")));
    }

    [Fact]
    public async Task Help_is_generated_from_command_registry_and_supports_aliases()
    {
        var service = CreateService();

        var overview = await service.ExecuteAsync("help", string.Empty);
        var detail = await service.ExecuteAsync("help dir", string.Empty);

        Assert.Contains(service.Commands, command => command.Name == "mkdir");
        Assert.Contains("mkdir <relative-directory>", overview.Output);
        Assert.Contains("Usage: ls [relative-directory]", detail.Output);
        Assert.Contains("Aliases: dir", detail.Output);
    }

    [Fact]
    public async Task Python_session_uses_utf8_unbuffered_portable_environment()
    {
        var interactiveRunner = new RecordingInteractiveRunner();
        var service = CreateService(interactiveRunner: interactiveRunner);

        var result = await service.TryStartSessionAsync(
            "python translate.py",
            "scripts",
            new Progress<PortableProcessOutput>());

        Assert.True(result.IsSuccess);
        Assert.NotNull(interactiveRunner.Definition);
        Assert.Equal("terminal.python.interactive", interactiveRunner.Definition.Id);
        Assert.Equal(Path.Combine("instances", "default", "www", "scripts"), interactiveRunner.Definition.WorkingDirectoryRelativePath);
        Assert.Equal("1", interactiveRunner.Definition.Environment!["PYTHONUTF8"]);
        Assert.Equal("utf-8", interactiveRunner.Definition.Environment["PYTHONIOENCODING"]);
        Assert.Equal("1", interactiveRunner.Definition.Environment["PYTHONUNBUFFERED"]);
    }

    [Fact]
    public async Task Interactive_session_still_rejects_shell_chaining()
    {
        var interactiveRunner = new RecordingInteractiveRunner();
        var service = CreateService(interactiveRunner: interactiveRunner);

        var result = await service.TryStartSessionAsync(
            "python test.py & cmd.exe",
            string.Empty,
            new Progress<PortableProcessOutput>());

        Assert.True(result.IsRuntimeCommand);
        Assert.False(result.IsSuccess);
        Assert.Contains("shell chaining", result.Error);
        Assert.Null(interactiveRunner.Definition);
    }

    [Fact]
    public async Task Built_in_command_is_not_started_as_an_interactive_process()
    {
        var interactiveRunner = new RecordingInteractiveRunner();
        var service = CreateService(interactiveRunner: interactiveRunner);

        var result = await service.TryStartSessionAsync(
            "ls",
            string.Empty,
            new Progress<PortableProcessOutput>());

        Assert.False(result.IsRuntimeCommand);
        Assert.Null(interactiveRunner.Definition);
    }

    [Fact]
    public async Task Safe_file_commands_remain_inside_the_project_and_do_not_overwrite()
    {
        var service = CreateService();

        Assert.False((await service.ExecuteAsync("mkdir notes", string.Empty)).IsError);
        Assert.False((await service.ExecuteAsync("touch notes/input.txt", string.Empty)).IsError);
        await File.WriteAllTextAsync(Path.Combine(WorkspaceRoot, "notes", "input.txt"), "Příliš žluťoučký kůň");

        var contents = await service.ExecuteAsync("cat notes/input.txt", string.Empty);
        var copied = await service.ExecuteAsync("cp notes/input.txt notes/copy.txt", string.Empty);
        var overwrite = await service.ExecuteAsync("cp notes/input.txt notes/copy.txt", string.Empty);
        var moved = await service.ExecuteAsync("mv notes/copy.txt notes/moved.txt", string.Empty);
        var removed = await service.ExecuteAsync("rm notes/moved.txt", string.Empty);
        await service.ExecuteAsync("rm notes/input.txt", string.Empty);
        var removedDirectory = await service.ExecuteAsync("rmdir notes", string.Empty);

        Assert.Equal("Příliš žluťoučký kůň", contents.Output);
        Assert.False(copied.IsError);
        Assert.True(overwrite.IsError);
        Assert.Contains("already exists", overwrite.Output);
        Assert.False(moved.IsError);
        Assert.False(removed.IsError);
        Assert.False(removedDirectory.IsError);
        Assert.False(Directory.Exists(Path.Combine(WorkspaceRoot, "notes")));
    }

    [Fact]
    public async Task Remove_commands_cannot_delete_project_root_or_nonempty_directory()
    {
        var service = CreateService();
        Directory.CreateDirectory(Path.Combine(WorkspaceRoot, "protected"));
        await File.WriteAllTextAsync(Path.Combine(WorkspaceRoot, "protected", "data.txt"), "data");

        var root = await service.ExecuteAsync("rmdir .", string.Empty);
        var nonempty = await service.ExecuteAsync("rmdir protected", string.Empty);

        Assert.True(root.IsError);
        Assert.Contains("project root", root.Output);
        Assert.True(nonempty.IsError);
        Assert.True(Directory.Exists(Path.Combine(WorkspaceRoot, "protected")));
    }

    private string WorkspaceRoot => Path.Combine(_testRoot, "instances", "default", "www");

    private PortableTerminalService CreateService(
        RecordingRunner? runner = null,
        IPortableInteractiveCommandRunner? interactiveRunner = null)
    {
        Directory.CreateDirectory(_testRoot);
        var paths = new PortablePathResolver(_testRoot);
        return new PortableTerminalService(
            new ReadyModules(),
            new ReadyTools(),
            runner ?? new RecordingRunner(),
            paths,
            new JsonWebProjectCatalog(paths),
            interactiveRunner);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private sealed class ReadyModules : IModuleInstallationVerifier
    {
        public ModuleInstallationVerification Verify(ModuleKind kind, string displayName) => kind == ModuleKind.Php
            ? new(new ModuleInstallation(
                kind,
                "8.4.12",
                Path.Combine("modules", "php", "8.4.12"),
                Path.Combine("modules", "php", "8.4.12", "php-cgi.exe")), string.Empty)
            : new(null, "Not available in test.");
    }

    private sealed class ReadyTools : IPortableToolRuntimeInventory
    {
        public PortableToolRuntimeInfo GetRuntime(PortableToolKind kind) => kind switch
        {
            PortableToolKind.Python => new(kind, true, "3.13.0", Path.Combine("modules", "python", "3.13.0", "python.exe"), string.Empty),
            PortableToolKind.Composer => new(kind, true, "2.10.2", Path.Combine("modules", "composer", "2.10.2", "composer.phar"), string.Empty),
            _ => new(kind, false, string.Empty, string.Empty, "Unavailable in test.")
        };
    }

    private sealed class RecordingRunner : IPortableCommandRunner
    {
        public PortableCommandDefinition? Definition { get; private set; }

        public Task<PortableCommandResult> RunAsync(
            PortableCommandDefinition definition,
            CancellationToken cancellationToken = default)
        {
            Definition = definition;
            return Task.FromResult(new PortableCommandResult(0, "ok", string.Empty));
        }
    }

    private sealed class RecordingInteractiveRunner : IPortableInteractiveCommandRunner
    {
        public PortableCommandDefinition? Definition { get; private set; }

        public Task<IPortableProcessSession> StartAsync(
            PortableCommandDefinition definition,
            IProgress<PortableProcessOutput> output,
            CancellationToken cancellationToken = default)
        {
            Definition = definition;
            return Task.FromResult<IPortableProcessSession>(new CompletedSession());
        }
    }

    private sealed class CompletedSession : IPortableProcessSession
    {
        public bool IsRunning => false;

        public Task<PortableInteractiveProcessResult> Completion { get; } =
            Task.FromResult(new PortableInteractiveProcessResult(0));

        public ValueTask WriteLineAsync(string input, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
