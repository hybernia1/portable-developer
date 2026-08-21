using PortableDeveloper.Application.Settings;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Settings;

namespace PortableDeveloper.Tests;

public sealed class JsonApplicationSettingsStoreTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void Load_returns_czech_by_default()
    {
        var store = CreateStore();

        var settings = store.Load();

        Assert.Equal(ApplicationLanguage.Czech, settings.Language);
    }

    [Fact]
    public void Save_persists_language_inside_portable_root()
    {
        var store = CreateStore();

        store.Save(new ApplicationSettings(ApplicationLanguage.English));

        Assert.Equal(ApplicationLanguage.English, store.Load().Language);
        Assert.True(File.Exists(Path.Combine(_testRoot, "state", "settings.json")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private JsonApplicationSettingsStore CreateStore() => new(new PortablePathResolver(_testRoot));
}
