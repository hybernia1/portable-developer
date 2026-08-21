using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.MariaDb;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Infrastructure.MariaDb;

/// <summary>
/// Initializes MariaDB in a private staging directory and moves only a complete data directory into an instance.
/// </summary>
public sealed class MariaDbInstanceInitializer : IMariaDbInstanceInitializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IModuleInstallationVerifier _moduleVerifier;
    private readonly IPortablePathResolver _paths;
    private readonly IPortableCommandRunner _commandRunner;
    private readonly IApplicationLogger _logger;

    public MariaDbInstanceInitializer(
        IModuleInstallationVerifier moduleVerifier,
        IPortablePathResolver paths,
        IPortableCommandRunner commandRunner,
        IApplicationLogger logger)
    {
        _moduleVerifier = moduleVerifier;
        _paths = paths;
        _commandRunner = commandRunner;
        _logger = logger;
    }

    public MariaDbInstanceState GetState(MariaDbInstanceOptions options)
    {
        Validate(options);
        var dataPath = _paths.Resolve(Path.Combine("instances", options.InstanceId, "data", "mariadb"));
        var credentialsPath = _paths.Resolve(Path.Combine("instances", options.InstanceId, "state", "mariadb-credentials.json"));

        if (!Directory.Exists(dataPath))
        {
            return File.Exists(credentialsPath)
                ? MariaDbInstanceState.Incomplete
                : MariaDbInstanceState.NotInitialized;
        }

        if (!Directory.EnumerateFileSystemEntries(dataPath).Any())
        {
            return MariaDbInstanceState.NotInitialized;
        }

        return Directory.Exists(Path.Combine(dataPath, "mysql")) && File.Exists(credentialsPath)
            ? MariaDbInstanceState.Initialized
            : MariaDbInstanceState.Incomplete;
    }

    public async Task<MariaDbInitializationResult> InitializeAsync(
        MariaDbInstanceOptions options,
        CancellationToken cancellationToken = default)
    {
        Validate(options);
        cancellationToken.ThrowIfCancellationRequested();

        var verification = _moduleVerifier.Verify(ModuleKind.MariaDb, "MariaDB");
        if (!verification.IsVerified)
        {
            return await FailAsync(verification.Detail);
        }

        var installation = verification.Installation!;
        var initializerRelativePath = Path.Combine(installation.ModuleRootRelativePath, "bin", "mariadb-install-db.exe");
        if (!File.Exists(_paths.Resolve(initializerRelativePath)))
        {
            return await FailAsync("The verified MariaDB package does not contain bin/mariadb-install-db.exe.");
        }

        var targetDataRelativePath = Path.Combine("instances", options.InstanceId, "data", "mariadb");
        var targetDataPath = _paths.Resolve(targetDataRelativePath);
        var credentialsRelativePath = Path.Combine("instances", options.InstanceId, "state", "mariadb-credentials.json");
        var credentialsPath = _paths.Resolve(credentialsRelativePath);
        var existing = InspectExistingInstance(targetDataPath, credentialsPath);
        if (existing is not null)
        {
            return existing;
        }

        var stagingRelativePath = Path.Combine("temp", "initialize", $"mariadb-{options.InstanceId}-{Guid.NewGuid():N}");
        var stagingPath = _paths.EnsureDirectory(stagingRelativePath);
        var stagingDataPath = Path.Combine(stagingPath, "data");
        var templatePath = Path.Combine(stagingPath, "initialization.ini");
        var stagedCredentialsPath = Path.Combine(stagingPath, "mariadb-credentials.json");
        var rootPassword = CreatePassword();
        var dataMoved = false;

        try
        {
            File.WriteAllText(templatePath, BuildInitializationTemplate(), new UTF8Encoding(false));
            var command = new PortableCommandDefinition(
                $"mariadb.initialize.{options.InstanceId}",
                initializerRelativePath,
                installation.ModuleRootRelativePath,
                [
                    $"--datadir={stagingDataPath}",
                    $"--port={options.Port}",
                    $"--password={rootPassword}",
                    $"--config={templatePath}",
                    "--silent"
                ],
                Timeout: TimeSpan.FromMinutes(2));
            var commandResult = await _commandRunner.RunAsync(command, cancellationToken);
            if (!commandResult.IsSuccess)
            {
                var detail = commandResult.TimedOut
                    ? "MariaDB initialization exceeded the two-minute time limit."
                    : $"MariaDB initialization exited with code {commandResult.ExitCode?.ToString() ?? "unknown"}.";
                return await FailAsync(detail);
            }

            if (!Directory.Exists(Path.Combine(stagingDataPath, "mysql")))
            {
                return await FailAsync("MariaDB initialization did not create the required system tables.");
            }

            var generatedIniPath = Path.Combine(stagingDataPath, "my.ini");
            if (File.Exists(generatedIniPath))
            {
                File.Delete(generatedIniPath);
            }

            var credentials = new MariaDbCredentials("root", rootPassword, options.Port, DateTimeOffset.UtcNow);
            File.WriteAllText(stagedCredentialsPath, JsonSerializer.Serialize(credentials, SerializerOptions), new UTF8Encoding(false));

            _paths.EnsureDirectory(Path.GetDirectoryName(targetDataRelativePath)!);
            _paths.EnsureDirectory(Path.GetDirectoryName(credentialsRelativePath)!);
            if (Directory.Exists(targetDataPath))
            {
                Directory.Delete(targetDataPath);
            }

            Directory.Move(stagingDataPath, targetDataPath);
            dataMoved = true;
            File.Move(stagedCredentialsPath, credentialsPath);
            await LogSafelyAsync(ApplicationLogLevel.Information, "mariadb.initialized", $"instance={options.InstanceId}; port={options.Port}");
            return new(MariaDbInitializationStatus.Initialized, "MariaDB data directory was initialized inside the portable instance.");
        }
        catch (OperationCanceledException)
        {
            if (dataMoved && Directory.Exists(targetDataPath))
            {
                Directory.Delete(targetDataPath, recursive: true);
            }

            throw;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            if (dataMoved && Directory.Exists(targetDataPath))
            {
                Directory.Delete(targetDataPath, recursive: true);
            }

            return await FailAsync(exception.Message);
        }
        finally
        {
            if (Directory.Exists(stagingPath))
            {
                Directory.Delete(stagingPath, recursive: true);
            }
        }
    }

    private static MariaDbInitializationResult? InspectExistingInstance(string dataPath, string credentialsPath)
    {
        if (!Directory.Exists(dataPath))
        {
            return File.Exists(credentialsPath)
                ? new(MariaDbInitializationStatus.Failed, "MariaDB credentials exist but the data directory is missing.")
                : null;
        }

        var hasContents = Directory.EnumerateFileSystemEntries(dataPath).Any();
        if (!hasContents)
        {
            return null;
        }

        if (Directory.Exists(Path.Combine(dataPath, "mysql")) && File.Exists(credentialsPath))
        {
            return new(MariaDbInitializationStatus.AlreadyInitialized, "MariaDB data directory is already initialized.");
        }

        return new(MariaDbInitializationStatus.Failed, "MariaDB data directory is incomplete; existing files were left untouched.");
    }

    private static string BuildInitializationTemplate() =>
        """
        [mysqld]
        bind-address=127.0.0.1
        skip-name-resolve
        character-set-server=utf8mb4
        collation-server=utf8mb4_unicode_ci
        max-connections=50
        [client]
        protocol=tcp
        """;

    private static string CreatePassword() =>
        Convert.ToHexString(RandomNumberGenerator.GetBytes(24)).ToLowerInvariant();

    private static void Validate(MariaDbInstanceOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.InstanceId)
            || options.InstanceId.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
        {
            throw new ArgumentException("Instance ID may only contain ASCII letters, digits, hyphens, and underscores.", nameof(options));
        }

        if (options.Port is < 1024 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "Only non-privileged TCP ports from 1024 to 65535 are supported.");
        }
    }

    private async Task<MariaDbInitializationResult> FailAsync(string detail)
    {
        await LogSafelyAsync(ApplicationLogLevel.Error, "mariadb.initialize.failed", detail);
        return new(MariaDbInitializationStatus.Failed, detail);
    }

    private async Task LogSafelyAsync(ApplicationLogLevel level, string eventName, string message)
    {
        try
        {
            await _logger.LogAsync(level, "mariadb", eventName, message);
        }
        catch
        {
            // Initialization rollback must not depend on diagnostic logging.
        }
    }

    private sealed record MariaDbCredentials(
        string UserName,
        string Password,
        int Port,
        DateTimeOffset CreatedAtUtc);
}
