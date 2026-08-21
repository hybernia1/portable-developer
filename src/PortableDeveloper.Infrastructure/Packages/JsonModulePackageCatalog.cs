using System.Text.Json;
using System.Text.Json.Serialization;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Packages;
using PortableDeveloper.Domain.Packages;

namespace PortableDeveloper.Infrastructure.Packages;

/// <summary>
/// Loads the package catalog bundled below catalog/. Remote catalog updates are intentionally not trusted yet.
/// </summary>
public sealed class JsonModulePackageCatalog : IModulePackageCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly IPortablePathResolver _paths;

    public JsonModulePackageCatalog(IPortablePathResolver paths)
    {
        _paths = paths;
    }

    public ModulePackageCatalog Load()
    {
        var catalogPath = _paths.Resolve(Path.Combine("catalog", "modules.json"));
        if (!File.Exists(catalogPath))
        {
            throw new FileNotFoundException("The bundled module catalog was not found.", catalogPath);
        }

        using var stream = File.OpenRead(catalogPath);
        var catalog = JsonSerializer.Deserialize<ModulePackageCatalog>(stream, SerializerOptions)
            ?? throw new InvalidDataException("The module catalog is empty or invalid JSON.");
        ModulePackageManifestValidator.Validate(catalog);
        return catalog;
    }
}
