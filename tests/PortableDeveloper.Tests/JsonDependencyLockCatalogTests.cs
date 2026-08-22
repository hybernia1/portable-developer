using PortableDeveloper.Infrastructure.Packages;
using PortableDeveloper.Infrastructure.Paths;

namespace PortableDeveloper.Tests;

public sealed class JsonDependencyLockCatalogTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void Load_accepts_trusted_component_with_normalized_entrypoint()
    {
        WriteCatalog(
            """
            {
              "schemaVersion": 1,
              "components": [
                {
                  "id": "python",
                  "displayName": "Python",
                  "version": "3.13.0",
                  "fileName": "python.zip",
                  "archiveSha256": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                  "archiveRoot": "tools",
                  "normalizedEntrypointRelativePath": "python.exe",
                  "normalizedEntrypointSha256": "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                  "sources": ["https://api.nuget.org/v3-flatcontainer/python/package.zip"],
                  "licenseUrl": "https://docs.python.org/3/license.html"
                }
              ]
            }
            """);

        var catalog = new JsonDependencyLockCatalog(new PortablePathResolver(_testRoot)).Load();

        var component = Assert.Single(catalog.Components);
        Assert.Equal("python.exe", component.NormalizedEntrypointRelativePath);
    }

    [Fact]
    public void Bundled_release_lock_is_valid_and_complete()
    {
        var catalog = new JsonDependencyLockCatalog(new PortablePathResolver(AppContext.BaseDirectory)).Load();

        Assert.Equal(14, catalog.Components.Count);
        Assert.All(catalog.Components, component => Assert.Equal(64, component.ArchiveSha256.Length));
    }

    [Fact]
    public void Load_rejects_untrusted_download_host()
    {
        WriteCatalog(CreateBasicCatalog("https://untrusted.example.test/component.zip"));

        Assert.Throws<InvalidDataException>(() =>
            new JsonDependencyLockCatalog(new PortablePathResolver(_testRoot)).Load());
    }

    [Fact]
    public void Load_rejects_unsafe_validation_file_path()
    {
        WriteCatalog(
            CreateBasicCatalog(
                "https://github.com/example/component.zip",
                "\"validationFiles\": { \"../outside.txt\": \"bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb\" },"));

        Assert.Throws<InvalidDataException>(() =>
            new JsonDependencyLockCatalog(new PortablePathResolver(_testRoot)).Load());
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private void WriteCatalog(string json)
    {
        var directory = Path.Combine(_testRoot, "catalog");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, "dependencies.lock.json"), json);
    }

    private static string CreateBasicCatalog(string source, string additionalProperty = "") => $$"""
        {
          "schemaVersion": 1,
          "components": [
            {
              "id": "component",
              "displayName": "Component",
              "version": "1.0.0",
              "fileName": "component.zip",
              "archiveSha256": "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
              {{additionalProperty}}
              "sources": ["{{source}}"],
              "licenseUrl": "https://example.test/license"
            }
          ]
        }
        """;
}
