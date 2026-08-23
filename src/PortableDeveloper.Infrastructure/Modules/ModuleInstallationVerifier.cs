using System.Text.Json;
using System.Text.Json.Serialization;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Application.Packages;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Packages;
using PortableDeveloper.Infrastructure.Security;

namespace PortableDeveloper.Infrastructure.Modules;

/// <summary>
/// Matches a detected module and its portable installation record against the bundled catalog.
/// </summary>
public sealed class ModuleInstallationVerifier : IModuleInstallationVerifier
{
    private static readonly JsonSerializerOptions SerializerOptions = CreateSerializerOptions();
    private readonly IModuleInventory _inventory;
    private readonly IModulePackageCatalog _catalog;
    private readonly IPortablePathResolver _paths;
    private readonly FileSha256VerificationCache _entrypointHashes = new();

    public ModuleInstallationVerifier(
        IModuleInventory inventory,
        IModulePackageCatalog catalog,
        IPortablePathResolver paths)
    {
        _inventory = inventory;
        _catalog = catalog;
        _paths = paths;
    }

    public ModuleInstallationVerification Verify(ModuleKind kind, string displayName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(displayName);
        var installation = _inventory.GetInstalled(kind).FirstOrDefault();
        if (installation is null)
        {
            return new(null, $"{displayName} is not installed.");
        }

        var package = _catalog.Load().Packages.SingleOrDefault(candidate =>
            candidate.Kind == kind && string.Equals(candidate.Version, installation.Version, StringComparison.OrdinalIgnoreCase));
        if (package is null)
        {
            return new(null, $"{displayName} {installation.Version} is not in the bundled verified catalog.");
        }

        var metadataPath = _paths.Resolve(Path.Combine(installation.ModuleRootRelativePath, ".portable-developer-module.json"));
        if (!File.Exists(metadataPath))
        {
            return new(null, $"{displayName} {installation.Version} has no verified installation record.");
        }

        try
        {
            var metadata = JsonSerializer.Deserialize<InstalledModuleMetadata>(File.ReadAllText(metadataPath), SerializerOptions);
            if (metadata is null
                || metadata.Kind != package.Kind
                || !string.Equals(metadata.Version, package.Version, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(metadata.SourceUrl, package.SourceUrl, StringComparison.Ordinal)
                || !string.Equals(metadata.EntrypointSha256, package.EntrypointSha256, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(metadata.EntrypointRelativePath, package.EntrypointRelativePath, StringComparison.OrdinalIgnoreCase))
            {
                return new(null, $"{displayName} {installation.Version} does not match its verified catalog record.");
            }
        }
        catch (JsonException)
        {
            return new(null, $"{displayName} {installation.Version} has an invalid verification record.");
        }

        try
        {
            var entrypointPath = _paths.Resolve(installation.EntrypointRelativePath);
            if (!_entrypointHashes.Matches(entrypointPath, package.EntrypointSha256))
            {
                return new(null, $"{displayName} {installation.Version} entrypoint does not match its bundled SHA-256.");
            }
        }
        catch (IOException exception)
        {
            return new(null, $"{displayName} {installation.Version} could not be verified: {exception.Message}");
        }
        catch (UnauthorizedAccessException exception)
        {
            return new(null, $"{displayName} {installation.Version} could not be verified: {exception.Message}");
        }

        return new(installation, string.Empty);
    }

    private static JsonSerializerOptions CreateSerializerOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }
}
