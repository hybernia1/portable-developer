using PortableDeveloper.Application.Php;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Php;

namespace PortableDeveloper.Tests;

public sealed class JsonPhpSettingsStoreTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void Save_persists_normalized_settings_inside_instance_config()
    {
        var store = new JsonPhpSettingsStore(new PortablePathResolver(_testRoot));
        var settings = new PhpSettings
        {
            MemoryLimitMb = 512,
            UploadMaxFileSizeMb = 32,
            PostMaxSizeMb = 64,
            EnabledExtensions = ["sockets"]
        };

        store.Save(settings);
        var loaded = store.Load();

        Assert.Equal(512, loaded.MemoryLimitMb);
        Assert.Contains("sockets", loaded.EnabledExtensions);
        Assert.Contains("mysqli", loaded.EnabledExtensions);
        Assert.True(File.Exists(Path.Combine(_testRoot, "instances", "default", "config", "php-settings.json")));
    }

    [Fact]
    public void Load_returns_defaults_for_invalid_or_unsupported_settings()
    {
        var configDirectory = Path.Combine(_testRoot, "instances", "default", "config");
        Directory.CreateDirectory(configDirectory);
        File.WriteAllText(
            Path.Combine(configDirectory, "php-settings.json"),
            "{\"memoryLimitMb\":1,\"enabledExtensions\":[\"outside.dll\"]}");
        var store = new JsonPhpSettingsStore(new PortablePathResolver(_testRoot));

        var loaded = store.Load();

        Assert.Equal(PhpSettings.Default.MemoryLimitMb, loaded.MemoryLimitMb);
        Assert.Equal(PhpSettings.Default.EnabledExtensions, loaded.EnabledExtensions);
    }

    [Fact]
    public void Save_rejects_post_limit_smaller_than_upload_limit()
    {
        var store = new JsonPhpSettingsStore(new PortablePathResolver(_testRoot));
        var settings = new PhpSettings { UploadMaxFileSizeMb = 128, PostMaxSizeMb = 64 };

        Assert.Throws<ArgumentException>(() => store.Save(settings));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }
}
