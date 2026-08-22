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
