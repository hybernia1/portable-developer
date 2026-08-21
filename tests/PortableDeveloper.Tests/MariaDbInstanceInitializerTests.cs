using System.Text.Json;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.MariaDb;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;
using PortableDeveloper.Infrastructure.MariaDb;
using PortableDeveloper.Infrastructure.Paths;

namespace PortableDeveloper.Tests;

public sealed class MariaDbInstanceInitializerTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task InitializeAsync_moves_only_complete_data_and_stores_portable_credentials()
    {
        var installation = CreateVerifiedInstallation();
        var runner = new InitializingCommandRunner();
        var initializer = CreateInitializer(installation, runner);

        var result = await initializer.InitializeAsync(new MariaDbInstanceOptions());

        Assert.Equal(MariaDbInitializationStatus.Initialized, result.Status);
        var dataPath = Path.Combine(_testRoot, "instances", "default", "data", "mariadb");
        Assert.True(Directory.Exists(Path.Combine(dataPath, "mysql")));
        Assert.False(File.Exists(Path.Combine(dataPath, "my.ini")));
        var credentialsPath = Path.Combine(_testRoot, "instances", "default", "state", "mariadb-credentials.json");
        using var credentials = JsonDocument.Parse(await File.ReadAllTextAsync(credentialsPath));
        Assert.Equal("root", credentials.RootElement.GetProperty("userName").GetString());
        Assert.Equal(3307, credentials.RootElement.GetProperty("port").GetInt32());
        Assert.True(credentials.RootElement.GetProperty("password").GetString()!.Length >= 32);
        Assert.DoesNotContain(runner.Definition!.Arguments, argument => argument.StartsWith("--service", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(MariaDbInstanceState.Initialized, initializer.GetState(new MariaDbInstanceOptions()));
    }

    [Fact]
    public async Task InitializeAsync_preserves_incomplete_existing_data()
    {
        var installation = CreateVerifiedInstallation();
        var existingData = Path.Combine(_testRoot, "instances", "default", "data", "mariadb");
        Directory.CreateDirectory(existingData);
        await File.WriteAllTextAsync(Path.Combine(existingData, "user-file.txt"), "keep me");
        var runner = new InitializingCommandRunner();
        var initializer = CreateInitializer(installation, runner);

        var result = await initializer.InitializeAsync(new MariaDbInstanceOptions());

        Assert.Equal(MariaDbInitializationStatus.Failed, result.Status);
        Assert.True(File.Exists(Path.Combine(existingData, "user-file.txt")));
        Assert.Null(runner.Definition);
        Assert.Equal(MariaDbInstanceState.Incomplete, initializer.GetState(new MariaDbInstanceOptions()));
    }

    [Fact]
    public async Task InitializeAsync_does_not_publish_failed_staging_data()
    {
        var installation = CreateVerifiedInstallation();
        var initializer = CreateInitializer(installation, new InitializingCommandRunner(exitCode: 1));

        var result = await initializer.InitializeAsync(new MariaDbInstanceOptions());

        Assert.Equal(MariaDbInitializationStatus.Failed, result.Status);
        Assert.False(Directory.Exists(Path.Combine(_testRoot, "instances", "default", "data", "mariadb")));
        Assert.Equal(MariaDbInstanceState.NotInitialized, initializer.GetState(new MariaDbInstanceOptions()));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private ModuleInstallation CreateVerifiedInstallation()
    {
        var moduleRoot = Path.Combine(_testRoot, "modules", "mariadb", "12.3.2");
        Directory.CreateDirectory(Path.Combine(moduleRoot, "bin"));
        File.WriteAllText(Path.Combine(moduleRoot, "bin", "mariadbd.exe"), "test server");
        File.WriteAllText(Path.Combine(moduleRoot, "bin", "mariadb-install-db.exe"), "test initializer");
        return new(
            ModuleKind.MariaDb,
            "12.3.2",
            Path.Combine("modules", "mariadb", "12.3.2"),
            Path.Combine("modules", "mariadb", "12.3.2", "bin", "mariadbd.exe"));
    }

    private MariaDbInstanceInitializer CreateInitializer(
        ModuleInstallation installation,
        IPortableCommandRunner runner)
    {
        var paths = new PortablePathResolver(_testRoot);
        return new(
            new VerifiedModule(installation),
            paths,
            runner,
            new SilentLogger());
    }

    private sealed class VerifiedModule(ModuleInstallation installation) : IModuleInstallationVerifier
    {
        public ModuleInstallationVerification Verify(ModuleKind kind, string displayName) => new(installation, string.Empty);
    }

    private sealed class InitializingCommandRunner(int exitCode = 0) : IPortableCommandRunner
    {
        public PortableCommandDefinition? Definition { get; private set; }

        public Task<PortableCommandResult> RunAsync(
            PortableCommandDefinition definition,
            CancellationToken cancellationToken = default)
        {
            Definition = definition;
            if (exitCode == 0)
            {
                var dataArgument = definition.Arguments.Single(argument => argument.StartsWith("--datadir=", StringComparison.Ordinal));
                var dataPath = dataArgument["--datadir=".Length..];
                Directory.CreateDirectory(Path.Combine(dataPath, "mysql"));
                File.WriteAllText(Path.Combine(dataPath, "my.ini"), "absolute paths generated by MariaDB");
            }

            return Task.FromResult(new PortableCommandResult(exitCode, "", ""));
        }
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
