using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.MariaDb;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Infrastructure.MariaDb;

public sealed class MariaDbAccountService : IMariaDbAccountService
{
    private readonly IModuleInstallationVerifier _moduleVerifier;
    private readonly IPortableCommandRunner _commandRunner;
    private readonly IPortablePathResolver _paths;
    private readonly IApplicationLogger _logger;

    public MariaDbAccountService(
        IModuleInstallationVerifier moduleVerifier,
        IPortableCommandRunner commandRunner,
        IPortablePathResolver paths,
        IApplicationLogger logger)
    {
        _moduleVerifier = moduleVerifier;
        _commandRunner = commandRunner;
        _paths = paths;
        _logger = logger;
    }

    public bool HasRootPassword(MariaDbInstanceOptions options) =>
        !string.IsNullOrEmpty(new MariaDbCredentialStore(_paths).Read(options.InstanceId).Password);

    public async Task<DatabaseOperationResult> ChangeRootPasswordAsync(
        MariaDbInstanceOptions options,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        if (newPassword.Length is < 8 or > 128 || newPassword.Contains('\0'))
        {
            return DatabaseOperationResult.Failure("Password must contain 8 to 128 characters and cannot contain a null character.");
        }

        var verification = _moduleVerifier.Verify(ModuleKind.MariaDb, "MariaDB");
        if (!verification.IsVerified)
        {
            return DatabaseOperationResult.Failure(verification.Detail);
        }

        var installation = verification.Installation!;
        var clientRelativePath = Path.Combine(installation.ModuleRootRelativePath, "bin", "mariadb.exe");
        if (!File.Exists(_paths.Resolve(clientRelativePath)))
        {
            return DatabaseOperationResult.Failure("The verified MariaDB package does not contain bin/mariadb.exe.");
        }

        var store = new MariaDbCredentialStore(_paths);
        var current = store.Read(options.InstanceId);
        var changed = await SetPasswordAsync(
            installation.ModuleRootRelativePath,
            clientRelativePath,
            current,
            options.Port,
            newPassword,
            cancellationToken);
        if (!changed.IsSuccess)
        {
            return changed;
        }

        try
        {
            store.Write(options.InstanceId, current with { Password = newPassword });
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            var rollbackCredentials = current with { Password = newPassword };
            _ = await SetPasswordAsync(
                installation.ModuleRootRelativePath,
                clientRelativePath,
                rollbackCredentials,
                options.Port,
                current.Password,
                CancellationToken.None);
            return DatabaseOperationResult.Failure($"The credential file could not be updated: {exception.Message}");
        }

        await LogSafelyAsync("mariadb.root_password.changed", $"instance={options.InstanceId}; passwordSet=true");
        return DatabaseOperationResult.Success();
    }

    private async Task<DatabaseOperationResult> SetPasswordAsync(
        string moduleRootRelativePath,
        string clientRelativePath,
        MariaDbStoredCredentials connectionCredentials,
        int port,
        string newPassword,
        CancellationToken cancellationToken)
    {
        using var defaultsFile = new MariaDbClientDefaultsFile(_paths, connectionCredentials, port);
        var result = await _commandRunner.RunAsync(
            new PortableCommandDefinition(
                "mariadb.root-password.change",
                clientRelativePath,
                moduleRootRelativePath,
                [defaultsFile.Argument, "--batch", "--skip-column-names"],
                Timeout: TimeSpan.FromSeconds(15),
                StandardInput: $"SET PASSWORD = PASSWORD('{EscapeSqlLiteral(newPassword)}');{Environment.NewLine}"),
            cancellationToken);
        return result.IsSuccess
            ? DatabaseOperationResult.Success()
            : DatabaseOperationResult.Failure(string.IsNullOrWhiteSpace(result.StandardError)
                ? $"MariaDB command exited with code {result.ExitCode?.ToString() ?? "unknown"}."
                : result.StandardError.Trim());
    }

    private async Task LogSafelyAsync(string eventName, string message)
    {
        try
        {
            await _logger.LogAsync(ApplicationLogLevel.Information, "mariadb", eventName, message);
        }
        catch
        {
            // The account change has already completed and must not depend on logging.
        }
    }

    private static string EscapeSqlLiteral(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("'", "''", StringComparison.Ordinal);
}
