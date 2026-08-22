using System.Net;
using System.Net.Sockets;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Application.Selenium;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Infrastructure.Selenium;

public sealed class SeleniumServerController : ISeleniumServerController
{
    private const string ProcessId = "selenium-default";
    private readonly IModuleInstallationVerifier _moduleVerifier;
    private readonly ISeleniumDriverInventory _driverInventory;
    private readonly ISeleniumConfigurationGenerator _configurationGenerator;
    private readonly ISeleniumGridClient _gridClient;
    private readonly IManagedProcessSupervisor _supervisor;
    private readonly IPortablePathResolver _paths;
    private readonly IApplicationLogger _logger;
    private SeleniumServerOptions _options = SeleniumServerOptions.Default;

    public SeleniumServerController(
        IModuleInstallationVerifier moduleVerifier,
        ISeleniumDriverInventory driverInventory,
        ISeleniumConfigurationGenerator configurationGenerator,
        ISeleniumGridClient gridClient,
        IManagedProcessSupervisor supervisor,
        IPortablePathResolver paths,
        IApplicationLogger logger)
    {
        _moduleVerifier = moduleVerifier;
        _driverInventory = driverInventory;
        _configurationGenerator = configurationGenerator;
        _gridClient = gridClient;
        _supervisor = supervisor;
        _paths = paths;
        _logger = logger;
    }

    public SeleniumServerSnapshot GetSnapshot()
    {
        var process = _supervisor.GetSnapshots().FirstOrDefault(snapshot =>
            string.Equals(snapshot.Id, ProcessId, StringComparison.OrdinalIgnoreCase));
        return process?.State == ManagedProcessState.Running
            ? new(ManagedProcessState.Running, $"Selenium is running on 127.0.0.1:{_options.Port}.", process.ProcessId)
            : new(ManagedProcessState.Stopped, "Selenium is stopped.");
    }

    public async Task<SeleniumServerSnapshot> StartAsync(
        SeleniumServerOptions options,
        CancellationToken cancellationToken = default)
    {
        SeleniumConfigurationGenerator.Validate(options);
        cancellationToken.ThrowIfCancellationRequested();
        _options = options;
        var current = GetSnapshot();
        if (current.State == ManagedProcessState.Running)
        {
            return current;
        }

        var verification = _moduleVerifier.Verify(ModuleKind.Selenium, "Selenium");
        if (!verification.IsVerified)
        {
            return await FailAsync(verification.Detail);
        }

        var javaRelativePath = FindJavaRelativePath();
        if (javaRelativePath is null)
        {
            return await FailAsync("The bundled Java runtime was not found.");
        }

        var drivers = _driverInventory.Scan();
        if (drivers.Count == 0)
        {
            return await FailAsync("No supported driver was found under the portable drivers folder.");
        }

        if (!IsPortAvailable(options.Port))
        {
            return await FailAsync($"Port {options.Port} is already in use.");
        }

        var configPath = _paths.Resolve(_configurationGenerator.Generate(options, drivers));
        var installation = verification.Installation!;
        var jarPath = _paths.Resolve(installation.EntrypointRelativePath);
        var javaWorkingDirectory = Path.GetDirectoryName(javaRelativePath)!;
        var started = await _supervisor.StartAsync(
            new ManagedProcessDefinition(
                ProcessId,
                javaRelativePath,
                javaWorkingDirectory,
                $"-jar {Quote(jarPath)} standalone --config {Quote(configPath)}",
                new Dictionary<string, string>
                {
                    ["SE_AVOID_BROWSER_DOWNLOAD"] = "true",
                    ["SE_AVOID_STATS"] = "true"
                }),
            cancellationToken);
        if (started.State != ManagedProcessState.Running)
        {
            return await FailAsync($"Selenium could not start: {started.Detail}");
        }

        for (var attempt = 0; attempt < 60; attempt++)
        {
            if (await _gridClient.IsReadyAsync(options.Port, cancellationToken))
            {
                await LogAsync(
                    ApplicationLogLevel.Information,
                    "selenium.started",
                    $"instance={options.InstanceId}; port={options.Port}; maxSessions={options.MaxSessions}; drivers={drivers.Count}");
                return new(ManagedProcessState.Running, $"Selenium is running on 127.0.0.1:{options.Port}.", started.ProcessId);
            }

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }

        await _supervisor.StopAsync(ProcessId, cancellationToken);
        return await FailAsync("Selenium did not become ready before the startup timeout.");
    }

    public async Task<SeleniumServerSnapshot> StopAsync(CancellationToken cancellationToken = default)
    {
        await _supervisor.StopAsync(ProcessId, cancellationToken);
        await LogAsync(ApplicationLogLevel.Information, "selenium.stopped", $"instance={_options.InstanceId}");
        return new(ManagedProcessState.Stopped, "Selenium is stopped.");
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        await _supervisor.DisposeAsync();
    }

    private string? FindJavaRelativePath()
    {
        var jreRoot = _paths.EnsureDirectory(Path.Combine("modules", "jre"));
        foreach (var versionDirectory in Directory.EnumerateDirectories(jreRoot)
                     .Where(path => !IsReparsePoint(path))
                     .OrderByDescending(path => ParseVersion(Path.GetFileName(path))))
        {
            var javaPath = Path.Combine(versionDirectory, "bin", "java.exe");
            if (File.Exists(javaPath) && !IsReparsePoint(javaPath))
            {
                var relativePath = Path.GetRelativePath(_paths.RootPath, javaPath);
                _paths.Resolve(relativePath);
                return relativePath;
            }
        }

        return null;
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

    private async Task<SeleniumServerSnapshot> FailAsync(string detail)
    {
        await LogAsync(ApplicationLogLevel.Error, "selenium.start.failed", detail);
        return new(ManagedProcessState.Failed, detail);
    }

    private async Task LogAsync(ApplicationLogLevel level, string eventName, string message)
    {
        try
        {
            await _logger.LogAsync(level, "selenium", eventName, message);
        }
        catch
        {
            // Server ownership and cleanup must not depend on diagnostic logging.
        }
    }

    private static Version ParseVersion(string version) =>
        Version.TryParse(version, out var parsed) ? parsed : new Version(0, 0);

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;

    private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
}
