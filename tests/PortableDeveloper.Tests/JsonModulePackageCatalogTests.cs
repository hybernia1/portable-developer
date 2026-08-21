using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Infrastructure.Packages;
using PortableDeveloper.Infrastructure.Paths;

namespace PortableDeveloper.Tests;

public sealed class JsonModulePackageCatalogTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void Load_deserializes_and_validates_bundled_catalog()
    {
        var catalogDirectory = Path.Combine(_testRoot, "catalog");
        Directory.CreateDirectory(catalogDirectory);
        File.WriteAllText(
            Path.Combine(catalogDirectory, "modules.json"),
            """
            {
              "schemaVersion": 1,
              "packages": [
                {
                  "kind": "php",
                  "version": "8.4.16",
                  "sourceUrl": "https://packages.example.test/php.zip",
                  "entrypointSha256": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                  "entrypointRelativePath": "php-cgi.exe",
                  "licenseUrl": "https://www.php.net/license/"
                }
              ]
            }
            """);
        var catalog = new JsonModulePackageCatalog(new PortablePathResolver(_testRoot));

        var loaded = catalog.Load();

        var package = Assert.Single(loaded.Packages);
        Assert.Equal(ModuleKind.Php, package.Kind);
        Assert.Equal("8.4.16", package.Version);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }
}
