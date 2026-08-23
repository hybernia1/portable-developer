using PortableDeveloper.Application.Selenium;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Selenium;

namespace PortableDeveloper.Tests;

public sealed class SeleniumConfigurationGeneratorTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void Generate_writes_localhost_config_and_explicit_portable_drivers()
    {
        var driverRelativePath = Path.Combine("drivers", "bundled", "chrome", "152.0.7977.54", "chromedriver.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(Path.Combine(_testRoot, driverRelativePath))!);
        File.WriteAllText(Path.Combine(_testRoot, driverRelativePath), "driver");
        var browserRelativePath = Path.Combine("modules", "browsers", "chrome", "chrome.exe");
        Directory.CreateDirectory(Path.Combine(_testRoot, "modules", "browsers", "chrome"));
        File.WriteAllText(Path.Combine(_testRoot, browserRelativePath), "browser");
        var paths = new PortablePathResolver(_testRoot);
        var generator = new SeleniumConfigurationGenerator(paths);

        var relativeConfig = generator.Generate(
            new SeleniumServerOptions(MaxSessions: 3, SessionTimeoutSeconds: 900),
            [ReadyChrome(browserRelativePath, driverRelativePath)]);

        var config = File.ReadAllText(paths.Resolve(relativeConfig));
        Assert.Contains("host = \"127.0.0.1\"", config, StringComparison.Ordinal);
        Assert.Contains("port = 4444", config, StringComparison.Ordinal);
        Assert.Contains("detect-drivers = false", config, StringComparison.Ordinal);
        Assert.Contains("selenium-manager = false", config, StringComparison.Ordinal);
        Assert.Contains("delete-session-on-ui = true", config, StringComparison.Ordinal);
        Assert.Contains("max-sessions = 3", config, StringComparison.Ordinal);
        Assert.Contains("session-timeout = 900", config, StringComparison.Ordinal);
        Assert.Contains("chromedriver.exe", config, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\\\"browserName\\\":\\\"chrome\\\"", config, StringComparison.Ordinal);
        Assert.Contains("goog:chromeOptions", config, StringComparison.Ordinal);
        Assert.Contains("chrome.exe", config, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData(0, 300)]
    [InlineData(33, 300)]
    [InlineData(2, 29)]
    [InlineData(2, 86401)]
    public void Generate_rejects_unsafe_limits(int maxSessions, int sessionTimeout)
    {
        var generator = new SeleniumConfigurationGenerator(new PortablePathResolver(_testRoot));

        Assert.Throws<ArgumentOutOfRangeException>(() => generator.Generate(
            new SeleniumServerOptions(MaxSessions: maxSessions, SessionTimeoutSeconds: sessionTimeout),
            [ReadyChrome("modules/browsers/chrome.exe", "drivers/chromedriver.exe")]));
    }

    private static SeleniumBrowserEnvironmentInfo ReadyChrome(string browserPath, string driverPath) => new(
        "portable-chrome",
        "chrome",
        "Chrome",
        "152.0.7977.54",
        browserPath,
        true,
        SeleniumBrowserSource.Managed,
        new SeleniumDriverInfo("chrome", "Chrome", "152.0.7977.54", driverPath, true),
        SeleniumBrowserEnvironmentState.Ready,
        "Ready");

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }
}
