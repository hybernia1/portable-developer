namespace PortableDeveloper.Application.MariaDb;

public interface IMariaDbAccountService
{
    bool HasRootPassword(MariaDbInstanceOptions options);

    Task<DatabaseOperationResult> ChangeRootPasswordAsync(
        MariaDbInstanceOptions options,
        string newPassword,
        CancellationToken cancellationToken = default);
}
