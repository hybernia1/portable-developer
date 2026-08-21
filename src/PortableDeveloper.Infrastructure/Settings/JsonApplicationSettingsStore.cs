using System.Text.Json;
using System.Text.Json.Serialization;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Settings;

namespace PortableDeveloper.Infrastructure.Settings;

public sealed class JsonApplicationSettingsStore : IApplicationSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IPortablePathResolver _paths;

    public JsonApplicationSettingsStore(IPortablePathResolver paths)
    {
        _paths = paths;
    }

    public ApplicationSettings Load()
    {
        var settingsPath = GetSettingsPath();
        if (!File.Exists(settingsPath))
        {
            return ApplicationSettings.Default;
        }

        try
        {
            return JsonSerializer.Deserialize<ApplicationSettings>(File.ReadAllText(settingsPath), SerializerOptions)
                ?? ApplicationSettings.Default;
        }
        catch (JsonException)
        {
            return ApplicationSettings.Default;
        }
    }

    public void Save(ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var settingsPath = GetSettingsPath();
        var temporaryPath = settingsPath + ".part";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, SerializerOptions));
        File.Move(temporaryPath, settingsPath, overwrite: true);
    }

    private string GetSettingsPath()
    {
        _paths.EnsureDirectory("state");
        return _paths.Resolve("state/settings.json");
    }
}
