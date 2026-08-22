using System.Security.Cryptography;
using System.Text.Json;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Selenium;

namespace PortableDeveloper.Tests;

public sealed class SeleniumDriverInventoryTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void Scan_loads_verified_bundled_driver_and_supported_custom_driver()
    {
        var bundledPath = Path.Combine(_testRoot, "drivers", "bundled", "firefox", "0.37.1", "geckodriver.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(bundledPath)!);
        File.WriteAllText(bundledPath, "verified gecko");
        var relativePath = "drivers/bundled/firefox/0.37.1/geckodriver.exe";
        WriteManifest(relativePath, ComputeHash(bundledPath));
        var customPath = Path.Combine(_testRoot, "drivers", "custom", "chromedriver.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(customPath)!);
        File.WriteAllText(customPath, "custom chrome");

        var drivers = new SeleniumDriverInventory(new PortablePathResolver(_testRoot)).Scan();

        Assert.Equal(2, drivers.Count);
        Assert.Contains(drivers, driver => driver.BrowserName == "firefox" && driver.IsBundled && driver.Version == "0.37.1");
        Assert.Contains(drivers, driver => driver.BrowserName == "chrome" && !driver.IsBundled);
    }

    [Fact]
    public void Scan_rejects_modified_bundled_driver()
    {
        var bundledPath = Path.Combine(_testRoot, "drivers", "bundled", "firefox", "0.37.1", "geckodriver.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(bundledPath)!);
        File.WriteAllText(bundledPath, "modified");
        WriteManifest("drivers/bundled/firefox/0.37.1/geckodriver.exe", new string('0', 64));

        var drivers = new SeleniumDriverInventory(new PortablePathResolver(_testRoot)).Scan();

        Assert.Empty(drivers);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private void WriteManifest(string relativePath, string sha256)
    {
        var manifestPath = Path.Combine(_testRoot, "drivers", "bundled", "drivers.json");
        Directory.CreateDirectory(Path.GetDirectoryName(manifestPath)!);
        File.WriteAllText(
            manifestPath,
            JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                drivers = new[] { new { browserName = "firefox", version = "0.37.1", relativePath, sha256 } }
            }));
    }

    private static string ComputeHash(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
