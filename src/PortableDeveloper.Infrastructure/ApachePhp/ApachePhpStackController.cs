using System.Net;
using System.Net.Sockets;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.ApachePhp;
using PortableDeveloper.Application.Health;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Application.Php;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Infrastructure.ApachePhp;

/// <summary>
/// Owns the complete Apache/PHP FastCGI lifecycle for one portable instance.
/// </summary>
public sealed class ApachePhpStackController : IApachePhpStackController
{
    private const string ApacheProcessId = "apache-default";
    private const string PhpProcessId = "php-fastcgi-default";
    private readonly IApachePhpConfigurationGenerator _configurationGenerator;
    private readonly IApacheRuntimePreflight _apacheRuntimePreflight;
    private readonly ITcpPortHealthCheck _healthCheck;
    private readonly IApplicationLogger _logger;
    private readonly IModuleInstallationVerifier _moduleVerifier;
    private readonly IPortablePathResolver _paths;
    private readonly IPhpRuntimePreflight _phpRuntimePreflight;
    private readonly IManagedProcessSupervisor _supervisor;

    public ApachePhpStackController(
        IModuleInstallationVerifier moduleVerifier,
        IApacheRuntimePreflight apacheRuntimePreflight,
        IPhpRuntimePreflight phpRuntimePreflight,
        IApachePhpConfigurationGenerator configurationGenerator,
        IManagedProcessSupervisor supervisor,
        ITcpPortHealthCheck healthCheck,
        IPortablePathResolver paths,
        IApplicationLogger logger)
    {
        _moduleVerifier = moduleVerifier;
        _apacheRuntimePreflight = apacheRuntimePreflight;
        _phpRuntimePreflight = phpRuntimePreflight;
        _configurationGenerator = configurationGenerator;
        _supervisor = supervisor;
        _healthCheck = healthCheck;
        _paths = paths;
        _logger = logger;
    }

    public ApachePhpStackSnapshot GetSnapshot()
    {
        var snapshots = _supervisor.GetSnapshots().ToDictionary(snapshot => snapshot.Id, StringComparer.OrdinalIgnoreCase);
        var apache = snapshots.GetValueOrDefault(ApacheProcessId);
        var php = snapshots.GetValueOrDefault(PhpProcessId);

        if (apache?.State == ManagedProcessState.Running && php?.State == ManagedProcessState.Running)
        {
            return new ApachePhpStackSnapshot(ManagedProcessState.Running, "Apache and PHP FastCGI are running.", apache.ProcessId, php.ProcessId);
        }

        if (apache?.State == ManagedProcessState.Running || php?.State == ManagedProcessState.Running)
        {
            return new ApachePhpStackSnapshot(ManagedProcessState.Failed, "Only part of the Apache/PHP stack is running.", apache?.ProcessId, php?.ProcessId);
        }

        return new ApachePhpStackSnapshot(ManagedProcessState.Stopped, "Apache/PHP stack is stopped.");
    }

    public async Task<ApachePhpStackSnapshot> StartAsync(
        ApachePhpStackOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        cancellationToken.ThrowIfCancellationRequested();

        var current = GetSnapshot();
        if (current.State == ManagedProcessState.Running)
        {
            return current;
        }

        if (current.State == ManagedProcessState.Failed)
        {
            return await FailAsync("The previous stack is only partially running. Stop it before starting again.");
        }

        var apache = _moduleVerifier.Verify(ModuleKind.Apache, "Apache");
        if (!apache.IsVerified)
        {
            return await FailAsync(apache.Detail);
        }

        var apacheInstallation = apache.Installation!;
        var apacheReadiness = _apacheRuntimePreflight.Check(apacheInstallation.ModuleRootRelativePath);
        if (!apacheReadiness.IsReady)
        {
            return await FailAsync($"Apache app-local runtime is incomplete: {string.Join(", ", apacheReadiness.MissingFiles)}.");
        }

        var php = _moduleVerifier.Verify(ModuleKind.Php, "PHP");
        if (!php.IsVerified)
        {
            return await FailAsync(php.Detail);
        }

        var phpInstallation = php.Installation!;
        var readiness = _phpRuntimePreflight.Check(phpInstallation.ModuleRootRelativePath);
        if (!readiness.IsReady)
        {
            return await FailAsync($"PHP app-local runtime is incomplete: {string.Join(", ", readiness.MissingFiles)}.");
        }

        if (!IsPortAvailable(options.ApachePort) || !IsPortAvailable(options.PhpFastCgiPort))
        {
            return await FailAsync($"Port {options.ApachePort} or {options.PhpFastCgiPort} is already in use.");
        }

        var generated = _configurationGenerator.Generate(new ApachePhpInstanceConfiguration(
            options.InstanceId,
            apacheInstallation.ModuleRootRelativePath,
            phpInstallation.ModuleRootRelativePath,
            options.DocumentRootRelativePath,
            options.ApachePort,
            options.PhpFastCgiPort,
            options.MariaDbPort));
        var phpIniPath = _paths.Resolve(generated.PhpIniRelativePath);

        var phpStarted = await _supervisor.StartAsync(
            new ManagedProcessDefinition(
                PhpProcessId,
                phpInstallation.EntrypointRelativePath,
                phpInstallation.ModuleRootRelativePath,
                $"-b 127.0.0.1:{options.PhpFastCgiPort} -c {Quote(phpIniPath)}"),
            cancellationToken);
        if (phpStarted.State != ManagedProcessState.Running)
        {
            return await FailAsync($"PHP could not start: {phpStarted.Detail}");
        }

        var phpHealth = await WaitForPortAsync(options.PhpFastCgiPort, cancellationToken);
        if (!phpHealth.IsHealthy)
        {
            await _supervisor.StopAsync(PhpProcessId, cancellationToken);
            return await FailAsync($"PHP FastCGI did not become ready: {phpHealth.Detail}");
        }

        var apacheStarted = await _supervisor.StartAsync(
            new ManagedProcessDefinition(
                ApacheProcessId,
                apacheInstallation.EntrypointRelativePath,
                apacheInstallation.ModuleRootRelativePath,
                $"-f {Quote(_paths.Resolve(generated.ApacheConfigRelativePath))} -DFOREGROUND"),
            cancellationToken);
        if (apacheStarted.State != ManagedProcessState.Running)
        {
            await _supervisor.StopAsync(PhpProcessId, cancellationToken);
            return await FailAsync($"Apache could not start: {apacheStarted.Detail}");
        }

        var apacheHealth = await WaitForPortAsync(options.ApachePort, cancellationToken);
        if (!apacheHealth.IsHealthy)
        {
            await _supervisor.StopAsync(ApacheProcessId, cancellationToken);
            await _supervisor.StopAsync(PhpProcessId, cancellationToken);
            return await FailAsync($"Apache did not become ready: {apacheHealth.Detail}");
        }

        var result = new ApachePhpStackSnapshot(
            ManagedProcessState.Running,
            "Apache and PHP FastCGI are running.",
            apacheStarted.ProcessId,
            phpStarted.ProcessId);
        await LogAsync(ApplicationLogLevel.Information, "stack.started", $"apachePid={apacheStarted.ProcessId}; phpPid={phpStarted.ProcessId}");
        return result;
    }

    public async Task<ApachePhpStackSnapshot> StopAsync(CancellationToken cancellationToken = default)
    {
        await _supervisor.StopAsync(ApacheProcessId, cancellationToken);
        await _supervisor.StopAsync(PhpProcessId, cancellationToken);
        var result = new ApachePhpStackSnapshot(ManagedProcessState.Stopped, "Apache/PHP stack is stopped.");
        await LogAsync(ApplicationLogLevel.Information, "stack.stopped", "Apache/PHP stack was stopped.");
        return result;
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        await _supervisor.DisposeAsync();
    }

    private async Task<HealthCheckResult> WaitForPortAsync(int port, CancellationToken cancellationToken)
    {
        HealthCheckResult result = new(false, TimeSpan.Zero, "The server did not open its port.");
        for (var attempt = 0; attempt < 20; attempt++)
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

    private async Task<ApachePhpStackSnapshot> FailAsync(string detail)
    {
        await LogAsync(ApplicationLogLevel.Error, "stack.start.failed", detail);
        return new ApachePhpStackSnapshot(ManagedProcessState.Failed, detail);
    }

    private async Task LogAsync(ApplicationLogLevel level, string eventName, string message)
    {
        try
        {
            await _logger.LogAsync(level, "apache-php", eventName, message);
        }
        catch
        {
            // Lifecycle control must still clean up processes if portable logging is unavailable.
        }
    }

    private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"")}\"";

}
