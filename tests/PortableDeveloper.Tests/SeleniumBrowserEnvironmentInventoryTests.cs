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
    public void Scan_pairs_verified_managed_chrome_with_its_catalog_driver()
    {
        var browserRelativePath = Path.Combine("modules", "browsers", "chrome-for-testing", "152.0.7977.54", "chrome.exe");
        var browserPath = Path.Combine(_testRoot, browserRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(browserPath)!);
        File.WriteAllText(browserPath, "verified browser");
        var components = BrowserComponents(Sha256(browserPath));
        var inventory = new SeleniumBrowserEnvironmentInventory(
            new PortablePathResolver(_testRoot),
            new FixedCatalog(components),
            new FixedDrivers("chrome", true, "152.0.7977.54"));

        var environment = Assert.Single(inventory.Scan(), item => item.Id == "managed-chrome-for-testing");

        Assert.True(environment.IsReady);
        Assert.Equal(SeleniumBrowserSource.Managed, environment.Source);
        Assert.Equal("152.0.7977.54", environment.Driver!.Version);
    }

    [Fact]
    public void Scan_reports_missing_catalog_driver_instead_of_using_another_version()
    {
        var browserPath = Path.Combine(_testRoot, "modules", "browsers", "chrome-for-testing", "152.0.7977.54", "chrome.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(browserPath)!);
        File.WriteAllText(browserPath, "verified browser");
        var inventory = new SeleniumBrowserEnvironmentInventory(
            new PortablePathResolver(_testRoot),
            new FixedCatalog(BrowserComponents(Sha256(browserPath))),
            new FixedDrivers("chrome", true, "151.0.0.0"));

        var environment = Assert.Single(inventory.Scan(), item => item.Id == "managed-chrome-for-testing");

        Assert.False(environment.IsReady);
        Assert.Equal(SeleniumBrowserEnvironmentState.DriverMissing, environment.State);
    }

    [Fact]
    public void Scan_selects_matching_driver_even_when_a_newer_incompatible_version_exists()
    {
        var browserPath = Path.Combine(_testRoot, "modules", "browsers", "chrome-for-testing", "152.0.7977.54", "chrome.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(browserPath)!);
        File.WriteAllText(browserPath, "verified browser");
        var inventory = new SeleniumBrowserEnvironmentInventory(
            new PortablePathResolver(_testRoot),
            new FixedCatalog(BrowserComponents(Sha256(browserPath))),
            new FixedDrivers("chrome", true, "153.0.0.0", "152.0.7977.54"));

        var environment = Assert.Single(inventory.Scan(), item => item.Id == "managed-chrome-for-testing");

        Assert.True(environment.IsReady);
        Assert.Equal("152.0.7977.54", environment.Driver!.Version);
    }

    [Fact]
    public void Scan_ignores_custom_driver_for_a_managed_browser()
    {
        var browserPath = Path.Combine(_testRoot, "modules", "browsers", "chrome-for-testing", "152.0.7977.54", "chrome.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(browserPath)!);
        File.WriteAllText(browserPath, "verified browser");
        var inventory = new SeleniumBrowserEnvironmentInventory(
            new PortablePathResolver(_testRoot),
            new FixedCatalog(BrowserComponents(Sha256(browserPath))),
            new FixedDrivers("chrome", false, "152.0.7977.54"));

        var environment = Assert.Single(inventory.Scan());

        Assert.Equal(SeleniumBrowserEnvironmentState.DriverMissing, environment.State);
        Assert.Null(environment.Driver);
    }

    [Fact]
    public void Scan_pairs_verified_managed_firefox_with_geckodriver()
    {
        var browserRelativePath = Path.Combine("modules", "browsers", "firefox", "142.0", "firefox.exe");
        var browserPath = Path.Combine(_testRoot, browserRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(browserPath)!);
        File.WriteAllText(browserPath, "verified firefox");
        var components = new[]
        {
            new DependencyLockComponent(
                "firefox", "Firefox", "142.0", "firefox.exe", new string('a', 64),
                ["https://archive.mozilla.org/firefox.exe"], "https://mozilla.org/license",
                NormalizedEntrypointRelativePath: "firefox.exe", NormalizedEntrypointSha256: Sha256(browserPath)),
            new DependencyLockComponent(
                "geckodriver", "geckodriver", "0.37.1", "geckodriver.zip", new string('b', 64),
                ["https://github.com/geckodriver.zip"], "https://mozilla.org/license",
                NormalizedEntrypointRelativePath: "geckodriver.exe", NormalizedEntrypointSha256: new string('c', 64))
        };
        var inventory = new SeleniumBrowserEnvironmentInventory(
            new PortablePathResolver(_testRoot),
            new FixedCatalog(components),
            new FixedDrivers("firefox", true, "0.37.1"));

        var environment = Assert.Single(inventory.Scan());

        Assert.True(environment.IsReady, environment.Detail);
        Assert.Equal("firefox", environment.BrowserName);
        Assert.Equal("0.37.1", environment.Driver!.Version);
    }

    private static DependencyLockComponent[] BrowserComponents(string hash) =>
    [
        new(
            "chrome-for-testing",
            "Chrome for Testing",
            "152.0.7977.54",
            "chrome.zip",
            new string('a', 64),
            ["https://storage.googleapis.com/chrome.zip"],
            "https://chromium.org/license",
            NormalizedEntrypointRelativePath: "chrome.exe",
            NormalizedEntrypointSha256: hash),
        new(
            "chromedriver",
            "ChromeDriver",
            "152.0.7977.54",
            "driver.zip",
            new string('b', 64),
            ["https://storage.googleapis.com/driver.zip"],
            "https://chromium.org/license",
            NormalizedEntrypointRelativePath: "chromedriver.exe",
            NormalizedEntrypointSha256: new string('c', 64))
    ];

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

    private sealed class FixedCatalog(IReadOnlyList<DependencyLockComponent> components) : IDependencyLockCatalog
    {
        public DependencyLockCatalog Load() => new(1, components);
    }

    private sealed class FixedDrivers : ISeleniumDriverInventory
    {
        private readonly IReadOnlyList<SeleniumDriverInfo> _drivers;

        public FixedDrivers(string browserName, bool isBundled, params string[] versions)
        {
            _drivers = versions.Select(version =>
                new SeleniumDriverInfo(
                    browserName,
                    browserName == "firefox" ? "Firefox" : "Chrome",
                    version,
                    $"drivers/{version}/{(browserName == "firefox" ? "geckodriver.exe" : "chromedriver.exe")}",
                    isBundled)).ToArray();
        }

        public IReadOnlyList<SeleniumDriverInfo> Scan() => _drivers.Take(1).ToArray();

        public IReadOnlyList<SeleniumDriverInfo> ScanAll() => _drivers;
    }
}
