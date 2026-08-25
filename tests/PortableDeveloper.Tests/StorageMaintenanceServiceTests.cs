using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Storage;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Storage;

namespace PortableDeveloper.Tests;

public sealed class StorageMaintenanceServiceTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task InspectAsync_separates_cache_from_installed_and_persistent_data()
    {
        WriteFile(Path.Combine("downloads", "packages", "runtime.zip"), 11);
        WriteFile(Path.Combine("cache", "composer", "package.zip"), 13);
        WriteFile(Path.Combine("cache", "npm", "package.tgz"), 15);
        WriteFile(Path.Combine("cache", "pip", "wheel.whl"), 17);
        WriteFile(Path.Combine("modules", "php", "php.exe"), 19);
        WriteFile(Path.Combine("drivers", "chrome", "driver.exe"), 23);
        WriteFile(Path.Combine("instances", "default", "projects", "app", "vendor", "library.php"), 29);
        WriteFile(Path.Combine("profiles", "selenium-vaults", "vault.json"), 31);
        var service = CreateService();

        var result = await service.InspectAsync();

        Assert.Equal(11, result.RuntimePackageCacheBytes);
        Assert.Equal(13, result.ComposerCacheBytes);
        Assert.Equal(15, result.NpmCacheBytes);
        Assert.Equal(17, result.PipCacheBytes);
        Assert.Equal(42, result.InstalledRuntimeBytes);
        Assert.Equal(60, result.PersistentDataBytes);
        Assert.Equal(56, result.TotalCacheBytes);
    }

    [Fact]
    public async Task ClearCacheAsync_removes_only_the_selected_cache()
    {
        WriteFile(Path.Combine("downloads", "packages", "component", "runtime.zip"), 11);
        WriteFile(Path.Combine("cache", "composer", "package.zip"), 13);
        WriteFile(Path.Combine("modules", "php", "php.exe"), 19);
        WriteFile(Path.Combine("instances", "default", "projects", "app", "index.php"), 23);
        var service = CreateService();

        var result = await service.ClearCacheAsync(StorageCacheKind.RuntimePackages);

        Assert.True(result.Success, result.Detail);
        Assert.Equal(1, result.RemovedFiles);
        Assert.Equal(11, result.RemovedBytes);
        Assert.Empty(Directory.EnumerateFileSystemEntries(Path.Combine(_testRoot, "downloads", "packages")));
        Assert.True(File.Exists(Path.Combine(_testRoot, "cache", "composer", "package.zip")));
        Assert.True(File.Exists(Path.Combine(_testRoot, "modules", "php", "php.exe")));
        Assert.True(File.Exists(Path.Combine(_testRoot, "instances", "default", "projects", "app", "index.php")));
    }

    [Fact]
    public async Task ClearCacheAsync_fails_closed_when_cache_contains_reparse_point()
    {
        var outside = Path.Combine(_testRoot, "outside");
        var cache = Path.Combine(_testRoot, "cache", "pip");
        Directory.CreateDirectory(outside);
        Directory.CreateDirectory(cache);
        WriteFile(Path.Combine("outside", "keep.txt"), 7);
        try
        {
            Directory.CreateSymbolicLink(Path.Combine(cache, "linked"), outside);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }

        var result = await CreateService().ClearCacheAsync(StorageCacheKind.Pip);

        Assert.False(result.Success);
        Assert.True(File.Exists(Path.Combine(outside, "keep.txt")));
        Assert.True(Directory.Exists(Path.Combine(cache, "linked")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private StorageMaintenanceService CreateService() =>
        new(new PortablePathResolver(_testRoot), new SilentLogger());

    private void WriteFile(string relativePath, int bytes)
    {
        var path = Path.Combine(_testRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, new byte[bytes]);
    }

    private sealed class SilentLogger : IApplicationLogger
    {
        public ValueTask LogAsync(
            ApplicationLogLevel level,
            string component,
            string eventName,
            string message,
            CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
