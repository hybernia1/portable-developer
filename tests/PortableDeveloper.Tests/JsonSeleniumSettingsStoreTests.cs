using PortableDeveloper.Application.Selenium;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Selenium;

namespace PortableDeveloper.Tests;

public sealed class JsonSeleniumSettingsStoreTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void Save_persists_validated_settings_inside_portable_state()
    {
        var store = new JsonSeleniumSettingsStore(new PortablePathResolver(_testRoot));
        var settings = new SeleniumServerOptions(Port: 4555, MaxSessions: 4, SessionTimeoutSeconds: 1200, DownloadsEnabled: true);

        store.Save(settings);

        Assert.Equal(settings, store.Load());
        Assert.True(File.Exists(Path.Combine(_testRoot, "state", "selenium-settings.json")));
        Assert.Contains("\"downloadsEnabled\": true", File.ReadAllText(Path.Combine(_testRoot, "state", "selenium-settings.json")), StringComparison.Ordinal);
    }

    [Fact]
    public void Load_returns_defaults_for_invalid_file()
    {
        Directory.CreateDirectory(Path.Combine(_testRoot, "state"));
        File.WriteAllText(Path.Combine(_testRoot, "state", "selenium-settings.json"), "{ invalid");
        var store = new JsonSeleniumSettingsStore(new PortablePathResolver(_testRoot));

        Assert.Equal(SeleniumServerOptions.Default, store.Load());
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }
}
