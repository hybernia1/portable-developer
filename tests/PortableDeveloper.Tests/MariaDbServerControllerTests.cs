using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Health;
using PortableDeveloper.Application.MariaDb;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;
using PortableDeveloper.Infrastructure.MariaDb;
using PortableDeveloper.Infrastructure.Paths;

namespace PortableDeveloper.Tests;

public sealed class MariaDbServerControllerTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task StartAsync_generates_localhost_only_portable_configuration()
    {
        var port = GetAvailablePort();
        var installation = CreateInstallation(port);
        var supervisor = new RecordingSupervisor();
        var controller = CreateController(installation, supervisor);

        var result = await controller.StartAsync(new MariaDbInstanceOptions(Port: port));

        Assert.Equal(ManagedProcessState.Running, result.State);
        Assert.Contains("--defaults-file=", supervisor.StartedDefinition!.Arguments, StringComparison.Ordinal);
        Assert.DoesNotContain("--install", supervisor.StartedDefinition.Arguments, StringComparison.OrdinalIgnoreCase);
        var config = await File.ReadAllTextAsync(Path.Combine(_testRoot, "temp", "generated", "default", "mariadb", "my.ini"));
        Assert.Contains("bind-address=127.0.0.1", config, StringComparison.Ordinal);
        Assert.Contains("datadir=", config, StringComparison.Ordinal);
        Assert.DoesNotContain("skip-grant-tables", config, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private ModuleInstallation CreateInstallation(int port)
    {
        var moduleRoot = Path.Combine(_testRoot, "modules", "mariadb", "12.3.2");
        Directory.CreateDirectory(Path.Combine(moduleRoot, "bin"));
        File.WriteAllText(Path.Combine(moduleRoot, "bin", "mariadbd.exe"), "test server");
        File.WriteAllText(Path.Combine(moduleRoot, "bin", "mariadb-admin.exe"), "test admin");
        var dataPath = Path.Combine(_testRoot, "instances", "default", "data", "mariadb", "mysql");
        Directory.CreateDirectory(dataPath);
        var statePath = Path.Combine(_testRoot, "instances", "default", "state");
        Directory.CreateDirectory(statePath);
        File.WriteAllText(
            Path.Combine(statePath, "mariadb-credentials.json"),
            JsonSerializer.Serialize(new { userName = "root", password = "", port, createdAtUtc = DateTimeOffset.UtcNow }));
        return new(
            ModuleKind.MariaDb,
            "12.3.2",
            Path.Combine("modules", "mariadb", "12.3.2"),
            Path.Combine("modules", "mariadb", "12.3.2", "bin", "mariadbd.exe"));
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    private MariaDbServerController CreateController(ModuleInstallation installation, IManagedProcessSupervisor supervisor)
    {
        var paths = new PortablePathResolver(_testRoot);
        return new(
            new VerifiedModule(installation),
            supervisor,
            new SuccessfulRunner(),
            new HealthyPort(),
            paths,
            new SilentLogger());
    }

    private sealed class VerifiedModule(ModuleInstallation installation) : IModuleInstallationVerifier
    {
        public ModuleInstallationVerification Verify(ModuleKind kind, string displayName) => new(installation, string.Empty);
    }

    private sealed class RecordingSupervisor : IManagedProcessSupervisor
    {
        private ManagedProcessSnapshot? _snapshot;
        public ManagedProcessDefinition? StartedDefinition { get; private set; }

        public IReadOnlyCollection<ManagedProcessSnapshot> GetSnapshots() => _snapshot is null ? [] : [_snapshot];

        public Task<ManagedProcessSnapshot> StartAsync(ManagedProcessDefinition definition, CancellationToken cancellationToken = default)
        {
            StartedDefinition = definition;
            _snapshot = new(definition.Id, ManagedProcessState.Running, 1234);
            return Task.FromResult(_snapshot);
        }

        public Task StopAsync(string processId, CancellationToken cancellationToken = default)
        {
            _snapshot = null;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class SuccessfulRunner : IPortableCommandRunner
    {
        public Task<PortableCommandResult> RunAsync(PortableCommandDefinition definition, CancellationToken cancellationToken = default) =>
            Task.FromResult(new PortableCommandResult(0, string.Empty, string.Empty));
    }

    private sealed class HealthyPort : ITcpPortHealthCheck
    {
        public Task<HealthCheckResult> CheckAsync(string host, int port, TimeSpan timeout, CancellationToken cancellationToken = default) =>
            Task.FromResult(new HealthCheckResult(true, TimeSpan.Zero, "ready"));
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
