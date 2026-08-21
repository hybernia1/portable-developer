namespace PortableDeveloper.Application.MariaDb;

public interface IDatabaseCatalogService
{
    Task<IReadOnlyList<DatabaseInfo>> ListAsync(
        MariaDbInstanceOptions options,
        CancellationToken cancellationToken = default);

    Task<DatabaseOperationResult> CreateAsync(
        MariaDbInstanceOptions options,
        string databaseName,
        CancellationToken cancellationToken = default);

    Task<DatabaseOperationResult> RemoveGeneratedTestDatabaseAsync(
        MariaDbInstanceOptions options,
        CancellationToken cancellationToken = default);
}
