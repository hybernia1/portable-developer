namespace PortableDeveloper.Application.Storage;

public enum StorageCacheKind
{
    RuntimePackages,
    Composer,
    Pip
}

public sealed record StorageUsageSnapshot(
    long RuntimePackageCacheBytes,
    long ComposerCacheBytes,
    long PipCacheBytes,
    long InstalledRuntimeBytes,
    long PersistentDataBytes)
{
    public long TotalCacheBytes => RuntimePackageCacheBytes + ComposerCacheBytes + PipCacheBytes;
}

public sealed record StorageCleanupResult(
    bool Success,
    StorageCacheKind Cache,
    int RemovedFiles,
    long RemovedBytes,
    string Detail);
