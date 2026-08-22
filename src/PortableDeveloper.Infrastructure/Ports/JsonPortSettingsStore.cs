using System.Text;
using System.Text.Json;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Ports;

namespace PortableDeveloper.Infrastructure.Ports;

public sealed class JsonPortSettingsStore : IPortSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IPortablePathResolver _paths;

    public JsonPortSettingsStore(IPortablePathResolver paths)
    {
        _paths = paths;
    }

    public PortSettings Load(PortSettings fallback)
    {
        PortSettingsValidator.Validate(fallback);
        var path = GetPath();
        if (!File.Exists(path))
        {
            return fallback;
        }

        try
        {
            var settings = JsonSerializer.Deserialize<PortSettings>(File.ReadAllText(path), SerializerOptions)
                ?? fallback;
            return PortSettingsValidator.Validate(settings);
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            return fallback;
        }
    }

    public void Save(PortSettings settings)
    {
        PortSettingsValidator.Validate(settings);
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
        return _paths.Resolve(Path.Combine("state", "port-settings.json"));
    }
}
