using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.ApachePhp;
using PortableDeveloper.Application.Health;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Application.Packages;
using PortableDeveloper.Application.Php;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Packages;
using PortableDeveloper.Domain.Processes;
using PortableDeveloper.Infrastructure.ApachePhp;
using PortableDeveloper.Infrastructure.Health;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Php;

namespace PortableDeveloper.Tests;

public sealed class ApachePhpStackControllerTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task StartAsync_refuses_when_apache_is_not_installed()
    {
        var controller = CreateController([]);

        var result = await controller.StartAsync(new ApachePhpStackOptions());

        Assert.Equal(ManagedProcessState.Failed, result.State);
        Assert.Equal("Apache is not installed.", result.Detail);
    }

    [Fact]
    public async Task StartAsync_refuses_module_without_catalog_entry_before_starting_process()
    {
        var apache = new ModuleInstallation(
            ModuleKind.Apache,
            "2.4.70",
            "modules/apache/2.4.70",
            "modules/apache/2.4.70/bin/httpd.exe");
        var controller = CreateController([apache]);

        var result = await controller.StartAsync(new ApachePhpStackOptions());

        Assert.Equal(ManagedProcessState.Failed, result.State);
        Assert.Contains("not in the bundled verified catalog", result.Detail, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private ApachePhpStackController CreateController(IReadOnlyList<ModuleInstallation> apacheInstallations)
    {
        var paths = new PortablePathResolver(_testRoot);
        var inventory = new TestInventory(apacheInstallations);
        var catalog = new EmptyCatalog();
        return new ApachePhpStackController(
            new PortableDeveloper.Infrastructure.Modules.ModuleInstallationVerifier(inventory, catalog, paths),
            new ApacheRuntimePreflight(paths),
            new PhpRuntimePreflight(paths),
            new ApachePhpConfigurationGenerator(paths),
            new NoProcessSupervisor(),
            new TcpPortHealthCheck(),
            paths,
            new SilentLogger());
    }

    private sealed class TestInventory(IReadOnlyList<ModuleInstallation> apacheInstallations) : IModuleInventory
    {
        public IReadOnlyList<ModuleInstallation> GetInstalled(ModuleKind kind) =>
            kind == ModuleKind.Apache ? apacheInstallations : [];
    }

    private sealed class EmptyCatalog : IModulePackageCatalog
    {
        public ModulePackageCatalog Load() => new(1, []);
    }

    private sealed class NoProcessSupervisor : IManagedProcessSupervisor
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        public IReadOnlyCollection<ManagedProcessSnapshot> GetSnapshots() => [];

        public Task<ManagedProcessSnapshot> StartAsync(ManagedProcessDefinition definition, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("No child process should start during this test.");

        public Task StopAsync(string processId, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class SilentLogger : IApplicationLogger
    {
        public ValueTask LogAsync(
            ApplicationLogLevel level,
            string component,
            string eventName,
            string message,
            CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
