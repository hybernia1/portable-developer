using System.Text.Json;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.MariaDb;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;
using PortableDeveloper.Infrastructure.MariaDb;
using PortableDeveloper.Infrastructure.Paths;

namespace PortableDeveloper.Tests;

public sealed class MariaDbAccountServiceTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task ChangeRootPasswordAsync_keeps_password_out_of_arguments_and_updates_portable_state()
    {
        var installation = CreateInstallationAndCredentials();
        var runner = new InspectingRunner();
        var logger = new RecordingLogger();
        var service = new MariaDbAccountService(
            new VerifiedModule(installation),
            runner,
            new PortablePathResolver(_testRoot),
            logger);
        const string password = "Safe'Pass\\2026";

        var result = await service.ChangeRootPasswordAsync(new MariaDbInstanceOptions(), password);

        Assert.True(result.IsSuccess);
        Assert.NotNull(runner.Definition);
        Assert.DoesNotContain(runner.Definition.Arguments, argument => argument.Contains(password, StringComparison.Ordinal));
        Assert.Contains("SET PASSWORD", runner.Definition.StandardInput, StringComparison.Ordinal);
        Assert.Contains("Safe''Pass\\\\2026", runner.Definition.StandardInput, StringComparison.Ordinal);
        Assert.Contains("password=\"\"", runner.DefaultsFileContents, StringComparison.Ordinal);
        Assert.False(File.Exists(runner.DefaultsFilePath));
        using var credentials = JsonDocument.Parse(
            await File.ReadAllTextAsync(Path.Combine(_testRoot, "instances", "default", "state", "mariadb-credentials.json")));
        Assert.Equal(password, credentials.RootElement.GetProperty("password").GetString());
        Assert.DoesNotContain(logger.Messages, message => message.Contains(password, StringComparison.Ordinal));
        Assert.True(service.HasRootPassword(new MariaDbInstanceOptions()));
    }

    [Fact]
    public async Task ChangeRootPasswordAsync_rejects_short_password_without_running_client()
    {
        var installation = CreateInstallationAndCredentials();
        var runner = new InspectingRunner();
        var service = new MariaDbAccountService(
            new VerifiedModule(installation),
            runner,
            new PortablePathResolver(_testRoot),
            new RecordingLogger());

        var result = await service.ChangeRootPasswordAsync(new MariaDbInstanceOptions(), "short");

        Assert.False(result.IsSuccess);
        Assert.Null(runner.Definition);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private ModuleInstallation CreateInstallationAndCredentials()
    {
        var moduleRoot = Path.Combine(_testRoot, "modules", "mariadb", "12.3.2");
        Directory.CreateDirectory(Path.Combine(moduleRoot, "bin"));
        File.WriteAllText(Path.Combine(moduleRoot, "bin", "mariadb.exe"), "test client");
        var statePath = Path.Combine(_testRoot, "instances", "default", "state");
        Directory.CreateDirectory(statePath);
        File.WriteAllText(
            Path.Combine(statePath, "mariadb-credentials.json"),
            JsonSerializer.Serialize(new { userName = "root", password = "", port = 3307, createdAtUtc = DateTimeOffset.UtcNow }));
        return new(
            ModuleKind.MariaDb,
            "12.3.2",
            Path.Combine("modules", "mariadb", "12.3.2"),
            Path.Combine("modules", "mariadb", "12.3.2", "bin", "mariadbd.exe"));
    }

    private sealed class VerifiedModule(ModuleInstallation installation) : IModuleInstallationVerifier
    {
        public ModuleInstallationVerification Verify(ModuleKind kind, string displayName) => new(installation, string.Empty);
    }

    private sealed class InspectingRunner : IPortableCommandRunner
    {
        public PortableCommandDefinition? Definition { get; private set; }
        public string DefaultsFileContents { get; private set; } = string.Empty;
        public string DefaultsFilePath { get; private set; } = string.Empty;

        public Task<PortableCommandResult> RunAsync(
            PortableCommandDefinition definition,
            CancellationToken cancellationToken = default)
        {
            Definition = definition;
            DefaultsFilePath = definition.Arguments[0]["--defaults-extra-file=".Length..];
            DefaultsFileContents = File.ReadAllText(DefaultsFilePath);
            return Task.FromResult(new PortableCommandResult(0, string.Empty, string.Empty));
        }
    }

    private sealed class RecordingLogger : IApplicationLogger
    {
        public List<string> Messages { get; } = [];

        public ValueTask LogAsync(
            ApplicationLogLevel level,
            string component,
            string eventName,
            string message,
            CancellationToken cancellationToken = default)
        {
            Messages.Add(message);
            return ValueTask.CompletedTask;
        }
    }
}
