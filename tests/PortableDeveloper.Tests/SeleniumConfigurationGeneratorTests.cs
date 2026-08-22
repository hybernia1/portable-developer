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
        var driverRelativePath = Path.Combine("drivers", "custom", "chromedriver.exe");
        Directory.CreateDirectory(Path.Combine(_testRoot, "drivers", "custom"));
        File.WriteAllText(Path.Combine(_testRoot, driverRelativePath), "driver");
        var paths = new PortablePathResolver(_testRoot);
        var generator = new SeleniumConfigurationGenerator(paths);

        var relativeConfig = generator.Generate(
            new SeleniumServerOptions(MaxSessions: 3, SessionTimeoutSeconds: 900),
            [new SeleniumDriverInfo("chrome", "Chrome", "unknown", driverRelativePath, false)]);

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
            [new SeleniumDriverInfo("firefox", "Firefox", "0.37.1", "drivers/geckodriver.exe", true)]));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }
}
