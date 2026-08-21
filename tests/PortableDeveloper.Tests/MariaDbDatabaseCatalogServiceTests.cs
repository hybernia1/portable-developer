using System.Text.Json;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.MariaDb;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;
using PortableDeveloper.Infrastructure.MariaDb;
using PortableDeveloper.Infrastructure.Paths;

namespace PortableDeveloper.Tests;

public sealed class MariaDbDatabaseCatalogServiceTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task ListAsync_returns_user_databases_and_approximate_sizes()
    {
        var runner = new RecordingRunner(new(0, "portable_dev\t16384\r\nshop\t1048576\r\n", string.Empty));
        var service = CreateService(runner);

        var databases = await service.ListAsync(new MariaDbInstanceOptions());

        Assert.Collection(
            databases,
            database => Assert.Equal(new DatabaseInfo("portable_dev", 16384), database),
            database => Assert.Equal(new DatabaseInfo("shop", 1048576), database));
        Assert.Contains(runner.Definition!.Arguments, argument => argument.Contains("information_schema", StringComparison.Ordinal));
        Assert.DoesNotContain(runner.Definition.Arguments, argument => argument.StartsWith("--password", StringComparison.Ordinal));
    }

    [Fact]
    public async Task CreateAsync_rejects_unsafe_identifier_without_running_client()
    {
        var runner = new RecordingRunner(new(0, string.Empty, string.Empty));
        var service = CreateService(runner);

        var result = await service.CreateAsync(new MariaDbInstanceOptions(), "bad`; DROP DATABASE mysql;--");

        Assert.False(result.IsSuccess);
        Assert.Null(runner.Definition);
    }

    [Fact]
    public async Task CreateAsync_uses_utf8mb4_for_valid_database()
    {
        var runner = new RecordingRunner(new(0, string.Empty, string.Empty));
        var service = CreateService(runner);

        var result = await service.CreateAsync(new MariaDbInstanceOptions(), "project_2026");

        Assert.True(result.IsSuccess);
        Assert.Contains(
            runner.Definition!.Arguments,
            argument => argument.Contains("CREATE DATABASE `project_2026` CHARACTER SET utf8mb4", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RemoveGeneratedTestDatabaseAsync_targets_only_standard_test_schema()
    {
        var runner = new RecordingRunner(new(0, string.Empty, string.Empty));
        var service = CreateService(runner);

        var result = await service.RemoveGeneratedTestDatabaseAsync(new MariaDbInstanceOptions());

        Assert.True(result.IsSuccess);
        Assert.Contains(runner.Definition!.Arguments, argument => argument == "--execute=DROP DATABASE IF EXISTS `test`;");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private MariaDbDatabaseCatalogService CreateService(IPortableCommandRunner runner)
    {
        var moduleRoot = Path.Combine(_testRoot, "modules", "mariadb", "12.3.2");
        Directory.CreateDirectory(Path.Combine(moduleRoot, "bin"));
        File.WriteAllText(Path.Combine(moduleRoot, "bin", "mariadb.exe"), "test client");
        var installation = new ModuleInstallation(
            ModuleKind.MariaDb,
            "12.3.2",
            Path.Combine("modules", "mariadb", "12.3.2"),
            Path.Combine("modules", "mariadb", "12.3.2", "bin", "mariadbd.exe"));
        var statePath = Path.Combine(_testRoot, "instances", "default", "state");
        Directory.CreateDirectory(statePath);
        File.WriteAllText(
            Path.Combine(statePath, "mariadb-credentials.json"),
            JsonSerializer.Serialize(new { userName = "root", password = "", port = 3307, createdAtUtc = DateTimeOffset.UtcNow }));
        var paths = new PortablePathResolver(_testRoot);
        return new(new VerifiedModule(installation), runner, paths);
    }

    private sealed class VerifiedModule(ModuleInstallation installation) : IModuleInstallationVerifier
    {
        public ModuleInstallationVerification Verify(ModuleKind kind, string displayName) => new(installation, string.Empty);
    }

    private sealed class RecordingRunner(PortableCommandResult result) : IPortableCommandRunner
    {
        public PortableCommandDefinition? Definition { get; private set; }

        public Task<PortableCommandResult> RunAsync(
            PortableCommandDefinition definition,
            CancellationToken cancellationToken = default)
        {
            Definition = definition;
            return Task.FromResult(result);
        }
    }
}
