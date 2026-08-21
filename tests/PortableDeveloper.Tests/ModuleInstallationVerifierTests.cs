using System.Text.Json;
using System.Text.Json.Serialization;
using PortableDeveloper.Application.Packages;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Packages;
using PortableDeveloper.Infrastructure.Modules;
using PortableDeveloper.Infrastructure.Paths;

namespace PortableDeveloper.Tests;

public sealed class ModuleInstallationVerifierTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void Verify_accepts_installation_matching_catalog_and_metadata()
    {
        var package = CreateMariaDbPackage();
        WriteInstallation(package, package.EntrypointSha256, "test executable");
        var paths = new PortablePathResolver(_testRoot);
        var verifier = new ModuleInstallationVerifier(
            new FileModuleInventory(paths),
            new StaticCatalog(package),
            paths);

        var result = verifier.Verify(ModuleKind.MariaDb, "MariaDB");

        Assert.True(result.IsVerified);
        Assert.Equal("12.3.2", result.Installation!.Version);
    }

    [Fact]
    public void Verify_rejects_metadata_not_matching_catalog_hash()
    {
        var package = CreateMariaDbPackage();
        WriteInstallation(package, new string('0', 64), "test executable");
        var paths = new PortablePathResolver(_testRoot);
        var verifier = new ModuleInstallationVerifier(
            new FileModuleInventory(paths),
            new StaticCatalog(package),
            paths);

        var result = verifier.Verify(ModuleKind.MariaDb, "MariaDB");

        Assert.False(result.IsVerified);
        Assert.Contains("does not match", result.Detail, StringComparison.Ordinal);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private void WriteInstallation(ModulePackageManifest package, string metadataHash, string entrypointContents)
    {
        var moduleRoot = Path.Combine(_testRoot, "modules", "mariadb", package.Version);
        Directory.CreateDirectory(Path.Combine(moduleRoot, "bin"));
        File.WriteAllText(Path.Combine(moduleRoot, "bin", "mariadbd.exe"), entrypointContents);
        var metadata = new InstalledModuleMetadata(
            package.Kind,
            package.Version,
            package.SourceUrl,
            metadataHash,
            package.EntrypointRelativePath);
        var serializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        serializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        File.WriteAllText(
            Path.Combine(moduleRoot, ".portable-developer-module.json"),
            JsonSerializer.Serialize(metadata, serializerOptions));
    }

    private static ModulePackageManifest CreateMariaDbPackage() => new(
        ModuleKind.MariaDb,
        "12.3.2",
        "https://packages.example.test/mariadb-12.3.2.zip",
        Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes("test executable"))).ToLowerInvariant(),
        "bin/mariadbd.exe",
        "https://mariadb.com/kb/en/mariadb-licenses/");

    private sealed class StaticCatalog(ModulePackageManifest package) : IModulePackageCatalog
    {
        public ModulePackageCatalog Load() => new(1, [package]);
    }
}
