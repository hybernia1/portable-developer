using System.Security.Cryptography;
using System.Text.Json;
using PortableDeveloper.Application.NativeRuntime;

namespace PortableDeveloper.Infrastructure.NativeRuntime;

internal static class NativeRuntimeMetadataValidator
{
    private static readonly Version MinimumRuntimeVersion = new(14, 50, 0, 0);
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static IReadOnlyList<string> FindIssues(
        string moduleRootPath,
        IReadOnlyList<string> requiredRuntimeRelativePaths)
    {
        var missingFiles = requiredRuntimeRelativePaths
            .Where(relativePath => !File.Exists(Path.Combine(moduleRootPath, relativePath)))
            .ToArray();
        if (missingFiles.Length > 0)
        {
            return missingFiles;
        }

        var metadataPath = Path.Combine(moduleRootPath, ".portable-developer-runtime.json");
        if (!File.Exists(metadataPath))
        {
            return [".portable-developer-runtime.json"];
        }

        try
        {
            var metadata = JsonSerializer.Deserialize<NativeRuntimeFileMetadata[]>(File.ReadAllText(metadataPath), SerializerOptions) ?? [];
            var issues = new List<string>();
            foreach (var relativePath in requiredRuntimeRelativePaths)
            {
                var fileName = Path.GetFileName(relativePath);
                var item = metadata.FirstOrDefault(candidate => string.Equals(candidate.FileName, fileName, StringComparison.OrdinalIgnoreCase));
                if (item is null
                    || !string.Equals(item.Signer, "Microsoft Corporation", StringComparison.Ordinal)
                    || !Version.TryParse(item.FileVersion, out var version)
                    || version < MinimumRuntimeVersion)
                {
                    issues.Add($"{relativePath} (invalid metadata)");
                    continue;
                }

                using var stream = File.OpenRead(Path.Combine(moduleRootPath, relativePath));
                var actualHash = Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
                if (!string.Equals(actualHash, item.Sha256, StringComparison.OrdinalIgnoreCase))
                {
                    issues.Add($"{relativePath} (SHA-256 mismatch)");
                }
            }

            return issues;
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return [".portable-developer-runtime.json (invalid)"];
        }
    }
}
