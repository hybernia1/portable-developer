using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Application.Workspace;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;
using PortableDeveloper.Infrastructure.Paths;
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

    private string WorkspaceRoot => Path.Combine(_testRoot, "instances", "default", "www");

    private PortableTerminalService CreateService(RecordingRunner? runner = null)
    {
        Directory.CreateDirectory(_testRoot);
        return new PortableTerminalService(
            new ReadyModules(),
            new ReadyTools(),
            runner ?? new RecordingRunner(),
            new PortablePathResolver(_testRoot));
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
}
