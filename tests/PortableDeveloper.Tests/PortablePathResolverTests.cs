using PortableDeveloper.Infrastructure.Paths;

namespace PortableDeveloper.Tests;

public sealed class PortablePathResolverTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void Resolve_returns_path_inside_portable_root()
    {
        Directory.CreateDirectory(_testRoot);
        var resolver = new PortablePathResolver(_testRoot);

        var resolvedPath = resolver.Resolve("instances/default/config");

        Assert.Equal(
            Path.Combine(_testRoot, "instances", "default", "config"),
            resolvedPath);
    }

    [Fact]
    public void Resolve_rejects_path_that_escapes_portable_root()
    {
        Directory.CreateDirectory(_testRoot);
        var resolver = new PortablePathResolver(_testRoot);

        Assert.Throws<ArgumentException>(() => resolver.Resolve("../outside"));
    }

    [Fact]
    public void EnsureDirectory_creates_directory_inside_portable_root()
    {
        Directory.CreateDirectory(_testRoot);
        var resolver = new PortablePathResolver(_testRoot);

        var createdPath = resolver.EnsureDirectory("logs/application");

        Assert.True(Directory.Exists(createdPath));
        Assert.StartsWith(_testRoot, createdPath, StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }
}
