using PortableDeveloper.Application.Ports;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Ports;

namespace PortableDeveloper.Tests;

public sealed class JsonPortSettingsStoreTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void Load_uses_valid_fallback_when_central_settings_do_not_exist()
    {
        var store = CreateStore();
        var legacyCompatibleFallback = PortSettings.Default with { SeleniumPort = 4555 };

        Assert.Equal(legacyCompatibleFallback, store.Load(legacyCompatibleFallback));
    }

    [Fact]
    public void Save_persists_all_ports_inside_portable_state()
    {
        var store = CreateStore();
        var settings = new PortSettings(8081, 9001, 3308, 4445);

        store.Save(settings);

        Assert.Equal(settings, store.Load(PortSettings.Default));
        Assert.True(File.Exists(Path.Combine(_testRoot, "state", "port-settings.json")));
    }

    [Fact]
    public void Load_returns_fallback_for_duplicate_ports()
    {
        Directory.CreateDirectory(Path.Combine(_testRoot, "state"));
        File.WriteAllText(
            Path.Combine(_testRoot, "state", "port-settings.json"),
            """{"apachePort":8080,"phpFastCgiPort":8080,"mariaDbPort":3307,"seleniumPort":4444}""");
        var store = CreateStore();

        Assert.Equal(PortSettings.Default, store.Load(PortSettings.Default));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private JsonPortSettingsStore CreateStore() => new(new PortablePathResolver(_testRoot));
}
