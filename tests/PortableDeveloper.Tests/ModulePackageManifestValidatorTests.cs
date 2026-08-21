using PortableDeveloper.Application.Packages;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Packages;

namespace PortableDeveloper.Tests;

public sealed class ModulePackageManifestValidatorTests
{
    [Fact]
    public void ValidatePackage_accepts_https_manifest_with_safe_paths()
    {
        ModulePackageManifestValidator.ValidatePackage(CreatePackage());
    }

    [Fact]
    public void ValidatePackage_rejects_non_https_source()
    {
        var package = CreatePackage() with { SourceUrl = "http://example.test/php.zip" };

        Assert.Throws<InvalidDataException>(() => ModulePackageManifestValidator.ValidatePackage(package));
    }

    [Fact]
    public void ValidatePackage_rejects_entrypoint_path_traversal()
    {
        var package = CreatePackage() with { EntrypointRelativePath = "../php-cgi.exe" };

        Assert.Throws<InvalidDataException>(() => ModulePackageManifestValidator.ValidatePackage(package));
    }

    private static ModulePackageManifest CreatePackage() => new(
        ModuleKind.Php,
        "8.4.16",
        "https://example.test/php.zip",
        new string('a', 64),
        "php-cgi.exe",
        "https://www.php.net/license/");
}
