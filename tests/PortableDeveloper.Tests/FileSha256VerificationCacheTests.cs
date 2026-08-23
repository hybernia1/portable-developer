using System.Security.Cryptography;
using PortableDeveloper.Infrastructure.Security;

namespace PortableDeveloper.Tests;

public sealed class FileSha256VerificationCacheTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperHashCacheTests-{Guid.NewGuid():N}");

    [Fact]
    public void Matches_hashes_an_unchanged_verified_file_only_once()
    {
        Directory.CreateDirectory(_testRoot);
        var path = Path.Combine(_testRoot, "runtime.bin");
        File.WriteAllText(path, "verified runtime");
        var expected = Sha256(path);
        var computations = 0;
        var cache = new FileSha256VerificationCache(file =>
        {
            computations++;
            return Sha256(file);
        });

        Assert.True(cache.Matches(path, expected));
        Assert.True(cache.Matches(path, expected));
        Assert.Equal(1, computations);
    }

    [Fact]
    public void Matches_rehashes_when_the_file_changes()
    {
        Directory.CreateDirectory(_testRoot);
        var path = Path.Combine(_testRoot, "runtime.bin");
        File.WriteAllText(path, "first");
        var expected = Sha256(path);
        var computations = 0;
        var cache = new FileSha256VerificationCache(file =>
        {
            computations++;
            return Sha256(file);
        });

        Assert.True(cache.Matches(path, expected));
        File.WriteAllText(path, "changed content");
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow.AddSeconds(2));

        Assert.False(cache.Matches(path, expected));
        Assert.Equal(2, computations);
    }

    [Fact]
    public void Matches_does_not_cache_failed_verification()
    {
        Directory.CreateDirectory(_testRoot);
        var path = Path.Combine(_testRoot, "runtime.bin");
        File.WriteAllText(path, "unexpected");
        var computations = 0;
        var cache = new FileSha256VerificationCache(file =>
        {
            computations++;
            return Sha256(file);
        });
        var wrongHash = new string('0', 64);

        Assert.False(cache.Matches(path, wrongHash));
        Assert.False(cache.Matches(path, wrongHash));
        Assert.Equal(2, computations);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
