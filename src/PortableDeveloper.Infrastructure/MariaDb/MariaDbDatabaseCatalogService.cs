using System.Globalization;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.MariaDb;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Infrastructure.MariaDb;

public sealed class MariaDbDatabaseCatalogService : IDatabaseCatalogService
{
    private const string ListSql =
        "SELECT s.SCHEMA_NAME, COALESCE(SUM(t.DATA_LENGTH + t.INDEX_LENGTH), 0) " +
        "FROM information_schema.SCHEMATA s " +
        "LEFT JOIN information_schema.TABLES t ON t.TABLE_SCHEMA = s.SCHEMA_NAME " +
        "WHERE s.SCHEMA_NAME NOT IN ('information_schema','mysql','performance_schema','sys') " +
        "GROUP BY s.SCHEMA_NAME ORDER BY s.SCHEMA_NAME;";

    private readonly IModuleInstallationVerifier _moduleVerifier;
    private readonly IPortableCommandRunner _commandRunner;
    private readonly IPortablePathResolver _paths;

    public MariaDbDatabaseCatalogService(
        IModuleInstallationVerifier moduleVerifier,
        IPortableCommandRunner commandRunner,
        IPortablePathResolver paths)
    {
        _moduleVerifier = moduleVerifier;
        _commandRunner = commandRunner;
        _paths = paths;
    }

    public async Task<IReadOnlyList<DatabaseInfo>> ListAsync(
        MariaDbInstanceOptions options,
        CancellationToken cancellationToken = default)
    {
        var result = await ExecuteAsync(options, ListSql, cancellationToken);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(BuildFailureDetail(result));
        }

        return result.StandardOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(ParseRow)
            .ToArray();
    }

    public async Task<DatabaseOperationResult> CreateAsync(
        MariaDbInstanceOptions options,
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidDatabaseName(databaseName))
        {
            return DatabaseOperationResult.Failure(
                "Database name must start with a letter and contain only letters, digits, and underscores (maximum 64 characters).");
        }

        var result = await ExecuteAsync(
            options,
            $"CREATE DATABASE `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;",
            cancellationToken);
        return result.IsSuccess
            ? DatabaseOperationResult.Success()
            : DatabaseOperationResult.Failure(BuildFailureDetail(result));
    }

    public async Task<DatabaseOperationResult> RemoveGeneratedTestDatabaseAsync(
        MariaDbInstanceOptions options,
        CancellationToken cancellationToken = default)
    {
        var result = await ExecuteAsync(options, "DROP DATABASE IF EXISTS `test`;", cancellationToken);
        return result.IsSuccess
            ? DatabaseOperationResult.Success()
            : DatabaseOperationResult.Failure(BuildFailureDetail(result));
    }

    private async Task<PortableCommandResult> ExecuteAsync(
        MariaDbInstanceOptions options,
        string sql,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(options);
        var verification = _moduleVerifier.Verify(ModuleKind.MariaDb, "MariaDB");
        if (!verification.IsVerified)
        {
            return new(null, string.Empty, verification.Detail);
        }

        var installation = verification.Installation!;
        var clientRelativePath = Path.Combine(installation.ModuleRootRelativePath, "bin", "mariadb.exe");
        if (!File.Exists(_paths.Resolve(clientRelativePath)))
        {
            return new(null, string.Empty, "The verified MariaDB package does not contain bin/mariadb.exe.");
        }

        var credentials = new MariaDbCredentialStore(_paths).Read(options.InstanceId);
        var arguments = MariaDbServerController.BuildConnectionArguments(credentials, options.Port)
            .Concat(["--batch", "--skip-column-names", "--raw", $"--execute={sql}"])
            .ToArray();
        return await _commandRunner.RunAsync(
            new PortableCommandDefinition(
                "mariadb.query",
                clientRelativePath,
                installation.ModuleRootRelativePath,
                arguments,
                Timeout: TimeSpan.FromSeconds(15)),
            cancellationToken);
    }

    private static DatabaseInfo ParseRow(string row)
    {
        var columns = row.Split('\t');
        if (columns.Length != 2
            || !long.TryParse(columns[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var size))
        {
            throw new InvalidDataException("MariaDB returned an unexpected database overview row.");
        }

        return new DatabaseInfo(columns[0], size);
    }

    private static bool IsValidDatabaseName(string name) =>
        !string.IsNullOrWhiteSpace(name)
        && name.Length <= 64
        && char.IsAsciiLetter(name[0])
        && name.All(character => char.IsAsciiLetterOrDigit(character) || character == '_');

    private static string BuildFailureDetail(PortableCommandResult result)
    {
        var detail = string.IsNullOrWhiteSpace(result.StandardError)
            ? $"MariaDB command exited with code {result.ExitCode?.ToString() ?? "unknown"}."
            : result.StandardError.Trim();
        return detail;
    }
}
