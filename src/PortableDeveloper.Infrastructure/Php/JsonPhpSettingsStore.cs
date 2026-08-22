using System.Text;
using System.Text.Json;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Php;

namespace PortableDeveloper.Infrastructure.Php;

public sealed class JsonPhpSettingsStore : IPhpSettingsStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly IPortablePathResolver _paths;

    public JsonPhpSettingsStore(IPortablePathResolver paths)
    {
        _paths = paths;
    }

    public PhpSettings Load(string instanceId = "default")
    {
        var path = GetPath(instanceId);
        if (!File.Exists(path))
        {
            return PhpSettings.Default;
        }

        try
        {
            var settings = JsonSerializer.Deserialize<PhpSettings>(File.ReadAllText(path), SerializerOptions)
                ?? PhpSettings.Default;
            return PhpSettingsValidator.Normalize(settings);
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException)
        {
            return PhpSettings.Default;
        }
    }

    public void Save(PhpSettings settings, string instanceId = "default")
    {
        var normalized = PhpSettingsValidator.Normalize(settings);
        var path = GetPath(instanceId);
        var stagingPath = path + $".{Guid.NewGuid():N}.tmp";
        try
        {
            File.WriteAllText(stagingPath, JsonSerializer.Serialize(normalized, SerializerOptions), new UTF8Encoding(false));
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

    private string GetPath(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId) ||
            instanceId.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
        {
            throw new ArgumentException("Instance ID may only contain ASCII letters, digits, hyphens, and underscores.", nameof(instanceId));
        }

        var directory = Path.Combine("instances", instanceId, "config");
        _paths.EnsureDirectory(directory);
        return _paths.Resolve(Path.Combine(directory, "php-settings.json"));
    }
}
