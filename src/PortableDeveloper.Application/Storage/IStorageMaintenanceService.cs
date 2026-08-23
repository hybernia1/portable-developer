namespace PortableDeveloper.Application.Storage;

public interface IStorageMaintenanceService
{
    Task<StorageUsageSnapshot> InspectAsync(CancellationToken cancellationToken = default);

    Task<StorageCleanupResult> ClearCacheAsync(
        StorageCacheKind cache,
        CancellationToken cancellationToken = default);
}
