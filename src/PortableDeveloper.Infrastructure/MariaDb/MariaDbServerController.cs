using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Health;
using PortableDeveloper.Application.MariaDb;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Infrastructure.MariaDb;

/// <summary>
/// Owns one localhost-only MariaDB process without installing a Windows service.
/// </summary>
public sealed class MariaDbServerController : IMariaDbServerController
{
    private const string ProcessId = "mariadb-default";
    private readonly IModuleInstallationVerifier _moduleVerifier;
    private readonly IManagedProcessSupervisor _supervisor;
    private readonly IPortableCommandRunner _commandRunner;
    private readonly ITcpPortHealthCheck _healthCheck;
    private readonly IPortablePathResolver _paths;
    private readonly IApplicationLogger _logger;
    private MariaDbInstanceOptions _options = new();

    public MariaDbServerController(
        IModuleInstallationVerifier moduleVerifier,
        IManagedProcessSupervisor supervisor,
        IPortableCommandRunner commandRunner,
        ITcpPortHealthCheck healthCheck,
        IPortablePathResolver paths,
        IApplicationLogger logger)
    {
        _moduleVerifier = moduleVerifier;
        _supervisor = supervisor;
        _commandRunner = commandRunner;
        _healthCheck = healthCheck;
        _paths = paths;
        _logger = logger;
    }

    public MariaDbServerSnapshot GetSnapshot()
    {
        var process = _supervisor.GetSnapshots().FirstOrDefault(snapshot =>
            string.Equals(snapshot.Id, ProcessId, StringComparison.OrdinalIgnoreCase));
        return process?.State == ManagedProcessState.Running
            ? new(ManagedProcessState.Running, $"MariaDB is running on 127.0.0.1:{_options.Port}.", process.ProcessId)
            : new(ManagedProcessState.Stopped, "MariaDB is stopped.");
    }

    public async Task<MariaDbServerSnapshot> StartAsync(
        MariaDbInstanceOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();
        _options = options;

        var current = GetSnapshot();
        if (current.State == ManagedProcessState.Running)
        {
            return current;
        }

        var verification = _moduleVerifier.Verify(ModuleKind.MariaDb, "MariaDB");
        if (!verification.IsVerified)
        {
            return await FailAsync(verification.Detail);
        }

        var dataRelativePath = Path.Combine("instances", options.InstanceId, "data", "mariadb");
        var dataPath = _paths.Resolve(dataRelativePath);
        if (!Directory.Exists(Path.Combine(dataPath, "mysql")))
        {
            return await FailAsync("MariaDB data directory is not initialized.");
        }

        try
        {
            _ = new MariaDbCredentialStore(_paths).Read(options.InstanceId);
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            return await FailAsync(exception.Message);
        }

        if (!IsPortAvailable(options.Port))
        {
            return await FailAsync($"Port {options.Port} is already in use.");
        }

        var installation = verification.Installation!;
        var configRelativePath = Path.Combine("temp", "generated", options.InstanceId, "mariadb", "my.ini");
        var configPath = _paths.Resolve(configRelativePath);
        _paths.EnsureDirectory(Path.GetDirectoryName(configRelativePath)!);
        File.WriteAllText(
            configPath,
            BuildConfiguration(_paths.Resolve(installation.ModuleRootRelativePath), dataPath, options.Port),
            new UTF8Encoding(false));

        var started = await _supervisor.StartAsync(
            new ManagedProcessDefinition(
                ProcessId,
                installation.EntrypointRelativePath,
                installation.ModuleRootRelativePath,
                $"--defaults-file={Quote(configPath)} --console"),
            cancellationToken);
        if (started.State != ManagedProcessState.Running)
        {
            return await FailAsync($"MariaDB could not start: {started.Detail}");
        }

        var health = await WaitForPortAsync(options.Port, cancellationToken);
        if (!health.IsHealthy)
        {
            await _supervisor.StopAsync(ProcessId, cancellationToken);
            return await FailAsync($"MariaDB did not become ready: {health.Detail}");
        }

        await LogAsync(ApplicationLogLevel.Information, "mariadb.started", $"instance={options.InstanceId}; port={options.Port}");
        return new(ManagedProcessState.Running, $"MariaDB is running on 127.0.0.1:{options.Port}.", started.ProcessId);
    }

    public async Task<MariaDbServerSnapshot> StopAsync(CancellationToken cancellationToken = default)
    {
        var snapshot = GetSnapshot();
        if (snapshot.State == ManagedProcessState.Running)
        {
            await RequestGracefulShutdownAsync(cancellationToken);
            await WaitForExitAsync(cancellationToken);
        }

        await _supervisor.StopAsync(ProcessId, cancellationToken);
        await LogAsync(ApplicationLogLevel.Information, "mariadb.stopped", $"instance={_options.InstanceId}");
        return new(ManagedProcessState.Stopped, "MariaDB is stopped.");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        await _supervisor.DisposeAsync();
    }

    private async Task RequestGracefulShutdownAsync(CancellationToken cancellationToken)
    {
        var verification = _moduleVerifier.Verify(ModuleKind.MariaDb, "MariaDB");
        if (!verification.IsVerified)
        {
            return;
        }

        var installation = verification.Installation!;
        var adminRelativePath = Path.Combine(installation.ModuleRootRelativePath, "bin", "mariadb-admin.exe");
        if (!File.Exists(_paths.Resolve(adminRelativePath)))
        {
            return;
        }

        try
        {
            var credentials = new MariaDbCredentialStore(_paths).Read(_options.InstanceId);
            using var defaultsFile = new MariaDbClientDefaultsFile(_paths, credentials, _options.Port);
            var arguments = new[] { defaultsFile.Argument, "shutdown" };
            await _commandRunner.RunAsync(
                new PortableCommandDefinition(
                    "mariadb.shutdown",
                    adminRelativePath,
                    installation.ModuleRootRelativePath,
                    arguments,
                    Timeout: TimeSpan.FromSeconds(5)),
                cancellationToken);
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException)
        {
            await LogAsync(ApplicationLogLevel.Warning, "mariadb.shutdown.graceful_failed", exception.Message);
        }
    }

    private async Task WaitForExitAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 50; attempt++)
        {
            if (_supervisor.GetSnapshots().All(snapshot =>
                    !string.Equals(snapshot.Id, ProcessId, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(100), cancellationToken);
        }
    }

    private async Task<HealthCheckResult> WaitForPortAsync(int port, CancellationToken cancellationToken)
    {
        HealthCheckResult result = new(false, TimeSpan.Zero, "The server did not open its port.");
        for (var attempt = 0; attempt < 40; attempt++)
        {
            result = await _healthCheck.CheckAsync("127.0.0.1", port, TimeSpan.FromMilliseconds(250), cancellationToken);
            if (result.IsHealthy)
            {
                return result;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }

        return result;
    }

    private static bool IsPortAvailable(int port)
    {
        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }

    private static string BuildConfiguration(string moduleRoot, string dataPath, int port) =>
        $"""
        [mysqld]
        basedir={Normalize(moduleRoot)}
        datadir={Normalize(dataPath)}
        port={port}
        bind-address=127.0.0.1
        skip-name-resolve
        character-set-server=utf8mb4
        collation-server=utf8mb4_unicode_ci
        max-connections=50
        [client]
        protocol=tcp
        host=127.0.0.1
        port={port}
        """;

    private async Task<MariaDbServerSnapshot> FailAsync(string detail)
    {
        await LogAsync(ApplicationLogLevel.Error, "mariadb.start.failed", detail);
        return new(ManagedProcessState.Failed, detail);
    }

    private async Task LogAsync(ApplicationLogLevel level, string eventName, string message)
    {
        try
        {
            await _logger.LogAsync(level, "mariadb", eventName, message);
        }
        catch
        {
            // Process ownership and cleanup must not depend on diagnostic logging.
        }
    }

    private static string Normalize(string path) => path.Replace('\\', '/');

    private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"")}\"";
}
