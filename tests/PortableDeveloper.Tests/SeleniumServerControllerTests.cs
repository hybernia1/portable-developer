using System.Net;
using System.Net.Sockets;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Application.Selenium;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Selenium;

namespace PortableDeveloper.Tests;

public sealed class SeleniumServerControllerTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task StartAsync_uses_bundled_java_explicit_driver_config_and_local_port()
    {
        var seleniumRoot = Path.Combine(_testRoot, "modules", "selenium", "4.47.0");
        Directory.CreateDirectory(seleniumRoot);
        File.WriteAllText(Path.Combine(seleniumRoot, "selenium-server.jar"), "jar");
        var javaPath = Path.Combine(_testRoot, "modules", "jre", "25.0.3", "bin", "java.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(javaPath)!);
        File.WriteAllText(javaPath, "java");
        var driverPath = Path.Combine(_testRoot, "drivers", "bundled", "chrome", "152.0.7977.54", "chromedriver.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(driverPath)!);
        File.WriteAllText(driverPath, "driver");
        var browserPath = Path.Combine(_testRoot, "modules", "browsers", "chrome", "chrome.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(browserPath)!);
        File.WriteAllText(browserPath, "browser");
        var installation = new ModuleInstallation(
            ModuleKind.Selenium,
            "4.47.0",
            Path.Combine("modules", "selenium", "4.47.0"),
            Path.Combine("modules", "selenium", "4.47.0", "selenium-server.jar"));
        var supervisor = new RecordingSupervisor();
        var paths = new PortablePathResolver(_testRoot);
        var controller = new SeleniumServerController(
            new VerifiedModule(installation),
            new FixedEnvironments([new SeleniumBrowserEnvironmentInfo(
                "portable-chrome",
                "chrome",
                "Chrome",
                "152.0.7977.54",
                Path.Combine("modules", "browsers", "chrome", "chrome.exe"),
                true,
                SeleniumBrowserSource.Managed,
                new SeleniumDriverInfo("chrome", "Chrome", "152.0.7977.54", Path.Combine("drivers", "bundled", "chrome", "152.0.7977.54", "chromedriver.exe"), true),
                SeleniumBrowserEnvironmentState.Ready,
                "Ready")]),
            new SeleniumConfigurationGenerator(paths),
            new ReadyGrid(),
            new FixedProfileNodeExtension(_testRoot),
            supervisor,
            paths,
            new SilentLogger());

        var port = GetAvailablePort();
        var result = await controller.StartAsync(new SeleniumServerOptions(
            Port: port,
            MaxSessions: 3,
            DownloadsEnabled: true)
        {
            DownloadDirectoryRelativePath = Path.Combine("instances", "default", "www", "seldownloads")
        });

        Assert.Equal(ManagedProcessState.Running, result.State);
        Assert.EndsWith(Path.Combine("bin", "java.exe"), supervisor.Definition!.ExecutableRelativePath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("selenium-server.jar", supervisor.Definition.Arguments, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("--config", supervisor.Definition.Arguments, StringComparison.Ordinal);
        Assert.Contains("--node-implementation portabledeveloper.selenium.PortableProfileNode", supervisor.Definition.Arguments, StringComparison.Ordinal);
        Assert.Equal("true", supervisor.Definition.Environment!["SE_AVOID_BROWSER_DOWNLOAD"]);
        Assert.Equal("true", supervisor.Definition.Environment["SE_AVOID_STATS"]);
        Assert.Equal("1", supervisor.Definition.Environment["MOZ_CRASHREPORTER_DISABLE"]);
        Assert.Equal("1", supervisor.Definition.Environment["MOZ_CRASHREPORTER_NO_REPORT"]);
        Assert.Equal("true", supervisor.Definition.Environment["PORTABLE_DEVELOPER_DOWNLOADS_ENABLED"]);
        Assert.Equal(
            Path.Combine(_testRoot, "instances", "default", "www", "seldownloads"),
            supervisor.Definition.Environment["PORTABLE_DEVELOPER_DOWNLOADS"]);
        Assert.True(Directory.Exists(supervisor.Definition.Environment["PORTABLE_DEVELOPER_DOWNLOADS"]));
        var config = File.ReadAllText(Path.Combine(_testRoot, "temp", "generated", "default", "selenium", "selenium.toml"));
        Assert.Contains("host = \"127.0.0.1\"", config, StringComparison.Ordinal);
        Assert.Contains("max-sessions = 3", config, StringComparison.Ordinal);
    }

    private static int GetAvailablePort()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        return ((IPEndPoint)listener.LocalEndpoint).Port;
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private sealed class VerifiedModule(ModuleInstallation installation) : IModuleInstallationVerifier
    {
        public ModuleInstallationVerification Verify(ModuleKind kind, string displayName) => new(installation, string.Empty);
    }

    private sealed class FixedEnvironments(IReadOnlyList<SeleniumBrowserEnvironmentInfo> environments) : ISeleniumBrowserEnvironmentInventory
    {
        public IReadOnlyList<SeleniumBrowserEnvironmentInfo> Scan() => environments;
    }

    private sealed class ReadyGrid : ISeleniumGridClient
    {
        public Task<bool> IsReadyAsync(int port, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<IReadOnlyList<SeleniumSessionInfo>> ListSessionsAsync(int port, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<SeleniumSessionInfo>>([]);
        public Task<SeleniumOperationResult> TerminateSessionAsync(int port, string sessionId, CancellationToken cancellationToken = default) =>
            Task.FromResult(SeleniumOperationResult.Success());
    }

    private sealed class FixedProfileNodeExtension(string root) : ISeleniumProfileNodeExtension
    {
        public Task<string> EnsureBuiltAsync(
            string javaRuntimeRelativePath,
            string seleniumJarRelativePath,
            CancellationToken cancellationToken = default)
        {
            var relativePath = Path.Combine("temp", "profile-node.jar");
            var fullPath = Path.Combine(root, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            File.WriteAllText(fullPath, "extension");
            return Task.FromResult(relativePath);
        }
    }

    private sealed class RecordingSupervisor : IManagedProcessSupervisor
    {
        private ManagedProcessSnapshot? _snapshot;
        public ManagedProcessDefinition? Definition { get; private set; }
        public IReadOnlyCollection<ManagedProcessSnapshot> GetSnapshots() => _snapshot is null ? [] : [_snapshot];
        public Task<ManagedProcessSnapshot> StartAsync(ManagedProcessDefinition definition, CancellationToken cancellationToken = default)
        {
            Definition = definition;
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

    private sealed class SilentLogger : IApplicationLogger
    {
        public ValueTask LogAsync(ApplicationLogLevel level, string component, string eventName, string message, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;
    }
}
