using System.Security.Cryptography;
using System.Text.Json;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.ProjectTools;

namespace PortableDeveloper.Infrastructure.ProjectTools;

public sealed class PortableToolRuntimeInventory : IPortableToolRuntimeInventory
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly IPortablePathResolver _paths;

    public PortableToolRuntimeInventory(IPortablePathResolver paths)
    {
        _paths = paths;
    }

    public PortableToolRuntimeInfo GetRuntime(PortableToolKind kind)
    {
        var kindName = kind.ToString().ToLowerInvariant();
        var rootRelativePath = Path.Combine("modules", kindName);
        var root = _paths.EnsureDirectory(rootRelativePath);
        var candidates = Directory.EnumerateDirectories(root)
            .Where(path => !IsReparsePoint(path))
            .OrderByDescending(path => ParseVersion(Path.GetFileName(path)))
            .ToArray();
        if (candidates.Length == 0)
        {
            return Failed(kind, $"Portable {kind} runtime was not found.");
        }

        var failure = $"No verified portable {kind} runtime was found.";
        foreach (var directory in candidates)
        {
            var result = VerifyCandidate(kind, kindName, directory);
            if (result.IsReady)
            {
                return result;
            }

            failure = result.Detail;
        }

        return Failed(kind, failure);
    }

    private PortableToolRuntimeInfo VerifyCandidate(PortableToolKind kind, string kindName, string directory)
    {
        var manifestPath = Path.Combine(directory, ".portable-developer-tool.json");
        if (!File.Exists(manifestPath) || IsReparsePoint(manifestPath))
        {
            return Failed(kind, $"The {kind} runtime metadata is missing.");
        }

        try
        {
            var manifest = JsonSerializer.Deserialize<ToolRuntimeManifest>(
                File.ReadAllText(manifestPath),
                SerializerOptions);
            var version = Path.GetFileName(directory);
            if (manifest is null ||
                manifest.SchemaVersion != 1 ||
                !string.Equals(manifest.Kind, kindName, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(manifest.Version, version, StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(manifest.Kind) ||
                string.IsNullOrWhiteSpace(manifest.Version) ||
                string.IsNullOrWhiteSpace(manifest.EntrypointRelativePath) ||
                string.IsNullOrWhiteSpace(manifest.EntrypointSha256) ||
                Path.IsPathRooted(manifest.EntrypointRelativePath) ||
                !IsSha256(manifest.EntrypointSha256))
            {
                return Failed(kind, $"The {kind} runtime metadata is invalid.");
            }

            var moduleRelativePath = Path.GetRelativePath(_paths.RootPath, directory);
            var entrypointRelativePath = Path.Combine(moduleRelativePath, manifest.EntrypointRelativePath);
            var entrypoint = _paths.Resolve(entrypointRelativePath);
            var directoryPrefix = Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!entrypoint.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase) ||
                !File.Exists(entrypoint) ||
                IsReparsePoint(entrypoint))
            {
                return Failed(kind, $"The {kind} runtime entrypoint is missing or unsafe.");
            }

            var actualSha256 = ComputeSha256(entrypoint);
            if (!string.Equals(actualSha256, manifest.EntrypointSha256, StringComparison.OrdinalIgnoreCase))
            {
                return Failed(kind, $"The {kind} runtime integrity check failed.");
            }

            return new(kind, true, version, entrypointRelativePath, $"Verified portable {kind} {version}.");
        }
        catch (Exception exception) when (exception is IOException or JsonException or UnauthorizedAccessException or ArgumentException)
        {
            return Failed(kind, $"The {kind} runtime could not be verified: {exception.Message}");
        }
    }

    private static PortableToolRuntimeInfo Failed(PortableToolKind kind, string detail) =>
        new(kind, false, string.Empty, string.Empty, detail);

    private static Version ParseVersion(string value) =>
        Version.TryParse(value, out var version) ? version : new Version(0, 0);

    private static bool IsSha256(string value) =>
        value.Length == 64 && value.All(Uri.IsHexDigit);

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;

    private sealed record ToolRuntimeManifest(
        int SchemaVersion,
        string? Kind,
        string? Version,
        string? EntrypointRelativePath,
        string? EntrypointSha256);
}
