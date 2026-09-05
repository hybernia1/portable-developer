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
            var settings = JsonSerializer.Deserialize<ApplicationSettings>(File.ReadAllText(settingsPath), SerializerOptions)
                ?? ApplicationSettings.Default;
            return Validate(settings);
        }
        catch (JsonException)
        {
            return ApplicationSettings.Default;
        }
    }

    public void Save(ApplicationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        settings = Validate(settings);

        var settingsPath = GetSettingsPath();
        var temporaryPath = settingsPath + ".part";
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(settings, SerializerOptions));
        File.Move(temporaryPath, settingsPath, overwrite: true);
    }

    private static ApplicationSettings Validate(ApplicationSettings settings)
    {
        var language = Enum.IsDefined(settings.Language)
            ? settings.Language
            : ApplicationSettings.Default.Language;
        var editorPreference = Enum.IsDefined(settings.EditorPreference)
            ? settings.EditorPreference
            : ApplicationSettings.Default.EditorPreference;
        return settings with { Language = language, EditorPreference = editorPreference };
    }

    private string GetSettingsPath()
    {
        _paths.EnsureDirectory("state");
        return _paths.Resolve("state/settings.json");
    }
}
