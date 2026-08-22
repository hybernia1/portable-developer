using System.Text;
using System.Text.Json;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Selenium;

namespace PortableDeveloper.Infrastructure.Selenium;

public sealed class JsonSeleniumSettingsStore : ISeleniumSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IPortablePathResolver _paths;

    public JsonSeleniumSettingsStore(IPortablePathResolver paths)
    {
        _paths = paths;
    }

    public SeleniumServerOptions Load()
    {
        var path = GetPath();
        if (!File.Exists(path))
        {
            return SeleniumServerOptions.Default;
        }

        try
        {
            var settings = JsonSerializer.Deserialize<SeleniumServerOptions>(File.ReadAllText(path), SerializerOptions)
                ?? SeleniumServerOptions.Default;
            SeleniumConfigurationGenerator.Validate(settings);
            return settings;
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            return SeleniumServerOptions.Default;
        }
    }

    public void Save(SeleniumServerOptions settings)
    {
        SeleniumConfigurationGenerator.Validate(settings);
        var path = GetPath();
        var stagingPath = path + $".{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(stagingPath, JsonSerializer.Serialize(settings, SerializerOptions), new UTF8Encoding(false));
            File.Move(stagingPath, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(stagingPath))
            {
                File.Delete(stagingPath);
            }
        }
    }

    private string GetPath()
    {
        _paths.EnsureDirectory("state");
        return _paths.Resolve(Path.Combine("state", "selenium-settings.json"));
    }
}
