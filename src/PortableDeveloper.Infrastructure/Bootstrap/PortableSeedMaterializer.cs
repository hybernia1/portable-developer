using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using PortableDeveloper.Application.Abstractions;

namespace PortableDeveloper.Infrastructure.Bootstrap;

/// <summary>
/// Materializes immutable application-owned files from the embedded portable seed.
/// User-data roots are created when absent and are never removed or replaced.
/// </summary>
public sealed class PortableSeedMaterializer
{
    internal const string ManifestEntryName = "portable-seed-manifest.json";
    internal const int MaximumEntryCount = 128;
    internal const long MaximumManifestBytes = 1024 * 1024;
    internal const long MaximumFileBytes = 8 * 1024 * 1024;
    internal const long MaximumExtractedBytes = 16 * 1024 * 1024;
    internal const long MaximumArchiveBytes = 32 * 1024 * 1024;

    private static readonly string[] BaseDirectories =
    [
        "cache",
        "catalog",
        "docs",
        "downloads",
        "drivers",
        "instances",
        "logs",
        "modules",
        "profiles",
        "resources",
        "runtime",
        "state",
        "temp",
        "tools"
    ];

    private static readonly HashSet<string> ReservedWindowsNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    private readonly IPortablePathResolver _paths;

    public PortableSeedMaterializer(IPortablePathResolver paths)
    {
        _paths = paths ?? throw new ArgumentNullException(nameof(paths));
    }

    public void EnsureInitialized(Stream seedArchive)
    {
        ArgumentNullException.ThrowIfNull(seedArchive);
        if (!seedArchive.CanRead)
        {
            throw new InvalidDataException("The embedded portable seed cannot be read.");
        }

        if (seedArchive.CanSeek && seedArchive.Length > MaximumArchiveBytes)
        {
            throw new InvalidDataException("The embedded portable seed exceeds the allowed archive size.");
        }

        EnsureDirectoryIsSafe(_paths.RootPath);
        EnsurePortableDirectory(Path.Combine("temp", "bootstrap"));
        var bootstrapRoot = _paths.Resolve(Path.Combine("temp", "bootstrap"));
        var stagingRoot = Path.Combine(bootstrapRoot, Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(stagingRoot);

        try
        {
            using var archive = new ZipArchive(seedArchive, ZipArchiveMode.Read, leaveOpen: true);
            if (archive.Entries.Count is 0 or > MaximumEntryCount)
            {
                throw new InvalidDataException("The embedded portable seed has an invalid entry count.");
            }

            var manifestEntry = archive.Entries.SingleOrDefault(entry =>
                string.Equals(entry.FullName, ManifestEntryName, StringComparison.Ordinal));
            if (manifestEntry is null)
            {
                throw new InvalidDataException("The embedded portable seed manifest is missing.");
            }

            var manifestBytes = ReadBounded(manifestEntry, MaximumManifestBytes);
            PortableSeedManifest manifest;
            try
            {
                manifest = JsonSerializer.Deserialize<PortableSeedManifest>(manifestBytes, JsonOptions)
                    ?? throw new InvalidDataException("The embedded portable seed manifest is invalid.");
            }
            catch (JsonException exception)
            {
                throw new InvalidDataException("The embedded portable seed manifest is invalid.", exception);
            }
            ValidateManifest(manifest, archive);

            foreach (var file in manifest.Files)
            {
                var entry = archive.GetEntry(file.Path)
                    ?? throw new InvalidDataException($"The embedded portable seed file is missing: {file.Path}");
                var stagedPath = ResolveBelow(stagingRoot, file.Path);
                Directory.CreateDirectory(Path.GetDirectoryName(stagedPath)!);
                ExtractVerified(entry, stagedPath, file);
            }

            foreach (var directory in BaseDirectories)
            {
                EnsurePortableDirectory(directory);
            }

            foreach (var file in manifest.Files)
            {
                InstallVerifiedFile(stagingRoot, file);
            }

            WriteStateMarker(manifest, Convert.ToHexStringLower(SHA256.HashData(manifestBytes)));
        }
        finally
        {
            if (Directory.Exists(stagingRoot))
            {
                EnsureNotReparsePoint(stagingRoot);
                Directory.Delete(stagingRoot, recursive: true);
            }
        }
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static byte[] ReadBounded(ZipArchiveEntry entry, long maximumBytes)
    {
        ValidateRegularEntry(entry);
        if (entry.Length <= 0 || entry.Length > maximumBytes)
        {
            throw new InvalidDataException($"Seed entry has an invalid size: {entry.FullName}");
        }

        using var source = entry.Open();
        using var destination = new MemoryStream((int)entry.Length);
        CopyBounded(source, destination, maximumBytes, entry.FullName, hash: null);
        return destination.ToArray();
    }

    private static void ValidateManifest(PortableSeedManifest manifest, ZipArchive archive)
    {
        if (manifest.SchemaVersion != 1 || string.IsNullOrWhiteSpace(manifest.Version))
        {
            throw new InvalidDataException("The embedded portable seed manifest version is unsupported.");
        }

        if (manifest.Files is null || manifest.Files.Count is 0 || manifest.Files.Count >= MaximumEntryCount)
        {
            throw new InvalidDataException("The embedded portable seed manifest has an invalid file count.");
        }

        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        long totalBytes = 0;
        foreach (var file in manifest.Files)
        {
            if (file is null)
            {
                throw new InvalidDataException("The embedded portable seed manifest contains an empty file definition.");
            }

            ValidateRelativePath(file.Path);
            if (!paths.Add(file.Path))
            {
                throw new InvalidDataException($"The embedded portable seed contains a duplicate path: {file.Path}");
            }

            if (file.Length < 0 || file.Length > MaximumFileBytes)
            {
                throw new InvalidDataException($"The embedded portable seed file has an invalid size: {file.Path}");
            }

            totalBytes = checked(totalBytes + file.Length);
            if (totalBytes > MaximumExtractedBytes)
            {
                throw new InvalidDataException("The embedded portable seed exceeds the allowed extracted size.");
            }

            if (string.IsNullOrWhiteSpace(file.Sha256) || file.Sha256.Length != 64 || !file.Sha256.All(Uri.IsHexDigit))
            {
                throw new InvalidDataException($"The embedded portable seed file has an invalid SHA-256: {file.Path}");
            }
        }

        var archivePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in archive.Entries)
        {
            ValidateRegularEntry(entry);
            ValidateRelativePath(entry.FullName);
            if (!archivePaths.Add(entry.FullName))
            {
                throw new InvalidDataException($"The embedded portable seed contains a duplicate archive path: {entry.FullName}");
            }
        }

        var expectedPaths = new HashSet<string>(paths, StringComparer.OrdinalIgnoreCase)
        {
            ManifestEntryName
        };
        if (!archivePaths.SetEquals(expectedPaths))
        {
            throw new InvalidDataException("The embedded portable seed contains unexpected or missing entries.");
        }
    }

    private static void ValidateRegularEntry(ZipArchiveEntry entry)
    {
        var unixFileType = (entry.ExternalAttributes >> 16) & 0xF000;
        var hasWindowsReparseAttribute = (entry.ExternalAttributes & (int)FileAttributes.ReparsePoint) != 0;
        var hasWindowsDirectoryAttribute = (entry.ExternalAttributes & (int)FileAttributes.Directory) != 0;
        if (string.IsNullOrEmpty(entry.Name) ||
            unixFileType == 0xA000 ||
            hasWindowsReparseAttribute ||
            hasWindowsDirectoryAttribute)
        {
            throw new InvalidDataException($"The embedded portable seed contains a non-regular entry: {entry.FullName}");
        }
    }

    private static void ValidateRelativePath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath) ||
            Path.IsPathRooted(relativePath) ||
            relativePath.Contains('\\', StringComparison.Ordinal) ||
            relativePath.StartsWith("/", StringComparison.Ordinal) ||
            relativePath.EndsWith("/", StringComparison.Ordinal))
        {
            throw new InvalidDataException($"The embedded portable seed contains an invalid path: {relativePath}");
        }

        foreach (var segment in relativePath.Split('/'))
        {
            var baseName = segment.Split('.')[0];
            if (segment is "." or ".." ||
                segment.EndsWith(' ') ||
                segment.EndsWith('.') ||
                segment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
                ReservedWindowsNames.Contains(baseName))
            {
                throw new InvalidDataException($"The embedded portable seed contains an invalid path: {relativePath}");
            }
        }
    }

    private static string ResolveBelow(string root, string relativePath)
    {
        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var resolved = Path.GetFullPath(Path.Combine(root, normalized));
        var prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!resolved.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"The embedded portable seed path escapes its target: {relativePath}");
        }

        return resolved;
    }

    private static void ExtractVerified(ZipArchiveEntry entry, string stagedPath, PortableSeedFile file)
    {
        ValidateRegularEntry(entry);
        if (entry.Length != file.Length)
        {
            throw new InvalidDataException($"The embedded portable seed file length does not match its manifest: {file.Path}");
        }

        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using (var source = entry.Open())
        using (var destination = new FileStream(stagedPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            CopyBounded(source, destination, MaximumFileBytes, file.Path, hash);
        }

        var actualHash = Convert.ToHexStringLower(hash.GetHashAndReset());
        if (!string.Equals(actualHash, file.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"The embedded portable seed file failed SHA-256 verification: {file.Path}");
        }
    }

    private static void CopyBounded(
        Stream source,
        Stream destination,
        long maximumBytes,
        string name,
        IncrementalHash? hash)
    {
        var buffer = new byte[81920];
        long written = 0;
        int read;
        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            written = checked(written + read);
            if (written > maximumBytes)
            {
                throw new InvalidDataException($"The embedded portable seed entry is too large: {name}");
            }

            destination.Write(buffer, 0, read);
            hash?.AppendData(buffer, 0, read);
        }
    }

    private void InstallVerifiedFile(string stagingRoot, PortableSeedFile file)
    {
        var destinationPath = _paths.Resolve(file.Path.Replace('/', Path.DirectorySeparatorChar));
        var destinationDirectory = Path.GetDirectoryName(destinationPath)!;
        EnsurePortableDirectory(Path.GetRelativePath(_paths.RootPath, destinationDirectory));

        if (File.Exists(destinationPath))
        {
            EnsureNotReparsePoint(destinationPath);
            if (FileMatches(destinationPath, file))
            {
                return;
            }
        }
        else if (Directory.Exists(destinationPath))
        {
            throw new InvalidDataException($"A directory blocks a portable seed file: {file.Path}");
        }

        var stagedPath = ResolveBelow(stagingRoot, file.Path);
        File.Move(stagedPath, destinationPath, overwrite: true);
    }

    private static bool FileMatches(string path, PortableSeedFile file)
    {
        var info = new FileInfo(path);
        if (info.Length != file.Length)
        {
            return false;
        }

        var hash = Convert.ToHexStringLower(SHA256.HashData(File.ReadAllBytes(path)));
        return string.Equals(hash, file.Sha256, StringComparison.OrdinalIgnoreCase);
    }

    private void EnsurePortableDirectory(string relativePath)
    {
        var current = _paths.RootPath;
        EnsureDirectoryIsSafe(current);
        foreach (var segment in relativePath.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            current = Path.Combine(current, segment);
            if (File.Exists(current))
            {
                throw new InvalidDataException($"A file blocks a required portable directory: {relativePath}");
            }

            if (!Directory.Exists(current))
            {
                Directory.CreateDirectory(current);
            }

            EnsureDirectoryIsSafe(current);
        }
    }

    private static void EnsureDirectoryIsSafe(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }

        EnsureNotReparsePoint(path);
    }

    private static void EnsureNotReparsePoint(string path)
    {
        if ((File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
        {
            throw new InvalidDataException($"Portable seed initialization refuses a reparse point: {path}");
        }
    }

    private void WriteStateMarker(PortableSeedManifest manifest, string manifestHash)
    {
        var stateDirectory = _paths.Resolve("state");
        EnsureDirectoryIsSafe(stateDirectory);
        var markerPath = Path.Combine(stateDirectory, "portable-seed.json");
        if (File.Exists(markerPath))
        {
            EnsureNotReparsePoint(markerPath);
        }

        var temporaryPath = Path.Combine(stateDirectory, $"portable-seed-{Guid.NewGuid():N}.tmp");
        var state = new PortableSeedState(1, manifest.Version, manifestHash, DateTimeOffset.UtcNow);
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporaryPath, markerPath, overwrite: true);
        }
        finally
        {
            File.Delete(temporaryPath);
        }
    }

    private sealed record PortableSeedManifest(int SchemaVersion, string Version, IReadOnlyList<PortableSeedFile> Files);

    private sealed record PortableSeedFile(string Path, long Length, string Sha256);

    private sealed record PortableSeedState(
        int SchemaVersion,
        string SeedVersion,
        string ManifestSha256,
        DateTimeOffset InitializedAtUtc);
}
