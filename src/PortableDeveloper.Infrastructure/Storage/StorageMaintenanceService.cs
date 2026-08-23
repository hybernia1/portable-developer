using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Storage;

namespace PortableDeveloper.Infrastructure.Storage;

public sealed class StorageMaintenanceService : IStorageMaintenanceService
{
    private static readonly IReadOnlyDictionary<StorageCacheKind, string> CachePaths =
        new Dictionary<StorageCacheKind, string>
        {
            [StorageCacheKind.RuntimePackages] = Path.Combine("downloads", "packages"),
            [StorageCacheKind.Composer] = Path.Combine("cache", "composer"),
            [StorageCacheKind.Pip] = Path.Combine("cache", "pip")
        };

    private static readonly string[] InstalledRuntimePaths = ["modules", "drivers", "tools"];
    private static readonly string[] PersistentDataPaths = ["instances", "profiles"];

    private readonly IPortablePathResolver _paths;
    private readonly IApplicationLogger _logger;
    private readonly SemaphoreSlim _operationLock = new(1, 1);

    public StorageMaintenanceService(IPortablePathResolver paths, IApplicationLogger logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public Task<StorageUsageSnapshot> InspectAsync(CancellationToken cancellationToken = default) =>
        Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            return new StorageUsageSnapshot(
                Measure(CachePaths[StorageCacheKind.RuntimePackages], cancellationToken),
                Measure(CachePaths[StorageCacheKind.Composer], cancellationToken),
                Measure(CachePaths[StorageCacheKind.Pip], cancellationToken),
                InstalledRuntimePaths.Sum(path => Measure(path, cancellationToken)),
                PersistentDataPaths.Sum(path => Measure(path, cancellationToken)));
        }, cancellationToken);

    public async Task<StorageCleanupResult> ClearCacheAsync(
        StorageCacheKind cache,
        CancellationToken cancellationToken = default)
    {
        if (!CachePaths.TryGetValue(cache, out var relativePath))
        {
            throw new ArgumentOutOfRangeException(nameof(cache));
        }

        await _operationLock.WaitAsync(cancellationToken);
        try
        {
            var result = await Task.Run(() => ClearKnownCache(cache, relativePath, cancellationToken), cancellationToken);
            await _logger.LogAsync(
                result.Success ? ApplicationLogLevel.Information : ApplicationLogLevel.Warning,
                "storage",
                result.Success ? "storage.cache.cleared" : "storage.cache.clear.failed",
                $"cache={cache}; files={result.RemovedFiles}; bytes={result.RemovedBytes}; detail={result.Detail}",
                cancellationToken);
            return result;
        }
        finally
        {
            _operationLock.Release();
        }
    }

    private StorageCleanupResult ClearKnownCache(
        StorageCacheKind cache,
        string relativePath,
        CancellationToken cancellationToken)
    {
        var root = _paths.Resolve(relativePath);
        if (!Directory.Exists(root))
        {
            return new(true, cache, 0, 0, "The cache is already empty.");
        }

        var removedFiles = 0;
        long removedBytes = 0;
        try
        {
            var (files, directories) = CollectSafeTree(root, cancellationToken);
            foreach (var file in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var length = file.Exists ? file.Length : 0L;
                file.Attributes = FileAttributes.Normal;
                file.Delete();
                removedFiles++;
                removedBytes += length;
            }

            foreach (var directory in directories.OrderByDescending(path => path.Length))
            {
                cancellationToken.ThrowIfCancellationRequested();
                Directory.Delete(directory);
            }

            return new(true, cache, removedFiles, removedBytes, "The cache was cleared.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new(false, cache, removedFiles, removedBytes, exception.Message);
        }
    }

    private long Measure(string relativePath, CancellationToken cancellationToken)
    {
        var root = _paths.Resolve(relativePath);
        if (!Directory.Exists(root) || IsReparsePoint(root))
        {
            return 0;
        }

        long total = 0;
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.TryPop(out var directory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                foreach (var path in Directory.EnumerateFileSystemEntries(directory))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var attributes = File.GetAttributes(path);
                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                    {
                        continue;
                    }

                    if ((attributes & FileAttributes.Directory) != 0)
                    {
                        pending.Push(path);
                    }
                    else
                    {
                        total += new FileInfo(path).Length;
                    }
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // A transiently locked or inaccessible cache entry does not block the rest of the overview.
            }
        }

        return total;
    }

    private static (FileInfo[] Files, string[] Directories) CollectSafeTree(
        string root,
        CancellationToken cancellationToken)
    {
        if (IsReparsePoint(root))
        {
            throw new IOException("The cache root is a reparse point and cannot be cleared safely.");
        }

        var files = new List<FileInfo>();
        var directories = new List<string>();
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.TryPop(out var directory))
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var path in Directory.EnumerateFileSystemEntries(directory))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) != 0)
                {
                    throw new IOException("The cache contains a reparse point and cannot be cleared safely.");
                }

                if ((attributes & FileAttributes.Directory) != 0)
                {
                    directories.Add(path);
                    pending.Push(path);
                }
                else
                {
                    files.Add(new FileInfo(path));
                }
            }
        }

        return (files.ToArray(), directories.ToArray());
    }

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0;
}
