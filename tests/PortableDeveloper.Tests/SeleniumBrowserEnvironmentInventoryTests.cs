using System.Security.Cryptography;
using PortableDeveloper.Application.Packages;
using PortableDeveloper.Application.Selenium;
using PortableDeveloper.Domain.Packages;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Selenium;

namespace PortableDeveloper.Tests;

public sealed class SeleniumBrowserEnvironmentInventoryTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void Scan_pairs_verified_portable_chrome_with_matching_driver()
    {
        var browserRelativePath = Path.Combine("modules", "browsers", "chrome-for-testing", "152.0.7977.54", "chrome.exe");
        var browserPath = Path.Combine(_testRoot, browserRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(browserPath)!);
        File.WriteAllText(browserPath, "verified browser");
        var component = BrowserComponent(Sha256(browserPath));
        var inventory = new SeleniumBrowserEnvironmentInventory(
            new PortablePathResolver(_testRoot),
            new FixedCatalog(component),
            new FixedDrivers("152.0.7977.54"));

        var environment = Assert.Single(inventory.Scan(), item => item.Id == "portable-chrome-for-testing");

        Assert.True(environment.IsReady);
        Assert.Equal(SeleniumBrowserSource.Portable, environment.Source);
        Assert.Equal("152.0.7977.54", environment.Driver!.Version);
    }

    [Fact]
    public void Scan_reports_version_mismatch_instead_of_starting_unsafe_pair()
    {
        var browserPath = Path.Combine(_testRoot, "modules", "browsers", "chrome-for-testing", "152.0.7977.54", "chrome.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(browserPath)!);
        File.WriteAllText(browserPath, "verified browser");
        var inventory = new SeleniumBrowserEnvironmentInventory(
            new PortablePathResolver(_testRoot),
            new FixedCatalog(BrowserComponent(Sha256(browserPath))),
            new FixedDrivers("151.0.0.0"));

        var environment = Assert.Single(inventory.Scan(), item => item.Id == "portable-chrome-for-testing");

        Assert.False(environment.IsReady);
        Assert.Equal(SeleniumBrowserEnvironmentState.VersionMismatch, environment.State);
    }

    [Fact]
    public void Scan_selects_matching_driver_even_when_a_newer_incompatible_version_exists()
    {
        var browserPath = Path.Combine(_testRoot, "modules", "browsers", "chrome-for-testing", "152.0.7977.54", "chrome.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(browserPath)!);
        File.WriteAllText(browserPath, "verified browser");
        var inventory = new SeleniumBrowserEnvironmentInventory(
            new PortablePathResolver(_testRoot),
            new FixedCatalog(BrowserComponent(Sha256(browserPath))),
            new FixedDrivers("153.0.0.0", "152.0.7977.54"));

        var environment = Assert.Single(inventory.Scan(), item => item.Id == "portable-chrome-for-testing");

        Assert.True(environment.IsReady);
        Assert.Equal("152.0.7977.54", environment.Driver!.Version);
    }

    private static DependencyLockComponent BrowserComponent(string hash) => new(
        "chrome-for-testing",
        "Chrome for Testing",
        "152.0.7977.54",
        "chrome.zip",
        new string('a', 64),
        ["https://storage.googleapis.com/chrome.zip"],
        "https://chromium.org/license",
        NormalizedEntrypointRelativePath: "chrome.exe",
        NormalizedEntrypointSha256: hash);

    private static string Sha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private sealed class FixedCatalog(DependencyLockComponent component) : IDependencyLockCatalog
    {
        public DependencyLockCatalog Load() => new(1, [component]);
    }

    private sealed class FixedDrivers : ISeleniumDriverInventory
    {
        private readonly IReadOnlyList<SeleniumDriverInfo> _drivers;

        public FixedDrivers(params string[] versions)
        {
            _drivers = versions.Select(version =>
                new SeleniumDriverInfo("chrome", "Chrome", version, $"drivers/{version}/chromedriver.exe", true)).ToArray();
        }

        public string DriversRelativePath => "drivers";

        public IReadOnlyList<SeleniumDriverInfo> Scan() => _drivers.Take(1).ToArray();

        public IReadOnlyList<SeleniumDriverInfo> ScanAll() => _drivers;
    }
}
