using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Infrastructure.Modules;
using PortableDeveloper.Infrastructure.Paths;

namespace PortableDeveloper.Tests;

public sealed class FileModuleInventoryTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void GetInstalled_returns_highest_detected_apache_version()
    {
        CreateFile("modules/apache/2.4.69/bin/httpd.exe");
        CreateFile("modules/apache/2.4.70/bin/httpd.exe");
        var inventory = new FileModuleInventory(new PortablePathResolver(_testRoot));

        var installations = inventory.GetInstalled(ModuleKind.Apache);

        Assert.Collection(
            installations,
            installation => Assert.Equal("2.4.70", installation.Version),
            installation => Assert.Equal("2.4.69", installation.Version));
        Assert.Equal(
            Path.Combine("modules", "apache", "2.4.70", "bin", "httpd.exe"),
            installations[0].EntrypointRelativePath);
    }

    [Fact]
    public void GetInstalled_requires_normalized_php_layout()
    {
        CreateFile("modules/php/8.4.16/php-cgi.exe");
        CreateFile("modules/php/8.5.0/nested/php-cgi.exe");
        var inventory = new FileModuleInventory(new PortablePathResolver(_testRoot));

        var installations = inventory.GetInstalled(ModuleKind.Php);

        var installation = Assert.Single(installations);
        Assert.Equal("8.4.16", installation.Version);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private void CreateFile(string relativePath)
    {
        var path = Path.Combine(_testRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "test module placeholder");
    }
}
