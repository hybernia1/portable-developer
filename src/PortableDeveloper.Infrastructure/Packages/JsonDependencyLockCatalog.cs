using System.Text.Json;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Packages;
using PortableDeveloper.Domain.Packages;

namespace PortableDeveloper.Infrastructure.Packages;

public sealed class JsonDependencyLockCatalog : IDependencyLockCatalog
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IPortablePathResolver _paths;

    public JsonDependencyLockCatalog(IPortablePathResolver paths)
    {
        _paths = paths;
    }

    public DependencyLockCatalog Load()
    {
        var path = _paths.Resolve(Path.Combine("catalog", "dependencies.lock.json"));
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The bundled dependency lock was not found.", path);
        }

        using var stream = File.OpenRead(path);
        var catalog = JsonSerializer.Deserialize<DependencyLockCatalog>(stream, SerializerOptions)
            ?? throw new InvalidDataException("The dependency lock is empty or invalid JSON.");
        DependencyLockCatalogValidator.Validate(catalog);
        return catalog;
    }
}
