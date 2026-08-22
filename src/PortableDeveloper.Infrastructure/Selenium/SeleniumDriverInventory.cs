using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Selenium;

namespace PortableDeveloper.Infrastructure.Selenium;

public sealed class SeleniumDriverInventory : ISeleniumDriverInventory
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyDictionary<string, DriverSpecification> Specifications =
        new Dictionary<string, DriverSpecification>(StringComparer.OrdinalIgnoreCase)
        {
            ["geckodriver.exe"] = new("firefox", "Firefox"),
            ["chromedriver.exe"] = new("chrome", "Chrome"),
            ["msedgedriver.exe"] = new("MicrosoftEdge", "Microsoft Edge")
        };

    private readonly IPortablePathResolver _paths;

    public SeleniumDriverInventory(IPortablePathResolver paths)
    {
        _paths = paths;
    }

    public string DriversRelativePath => "drivers";

    public IReadOnlyList<SeleniumDriverInfo> Scan()
    {
        var root = _paths.EnsureDirectory(DriversRelativePath);
        _paths.EnsureDirectory(Path.Combine(DriversRelativePath, "custom"));
        var bundledManifest = LoadBundledManifest();
        var drivers = EnumerateFiles(root)
            .Select(path => CreateDriverInfo(path, bundledManifest))
            .Where(driver => driver is not null)
            .Cast<SeleniumDriverInfo>()
            .GroupBy(driver => driver.BrowserName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group
                .OrderByDescending(driver => ParseVersion(driver.Version))
                .ThenBy(driver => driver.RelativePath, StringComparer.OrdinalIgnoreCase)
                .First())
            .OrderBy(driver => driver.DisplayName, StringComparer.CurrentCultureIgnoreCase)
            .ToArray();
        return drivers;
    }

    private SeleniumDriverInfo? CreateDriverInfo(
        string path,
        IReadOnlyDictionary<string, BundledDriverManifestItem> bundledManifest)
    {
        if (!Specifications.TryGetValue(Path.GetFileName(path), out var specification))
        {
            return null;
        }

        var relativePath = Path.GetRelativePath(_paths.RootPath, path);
        _paths.Resolve(relativePath);
        var isBundled = relativePath.StartsWith(
            Path.Combine("drivers", "bundled") + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);
        var version = ReadVersion(path);
        if (isBundled)
        {
            var manifestKey = Normalize(relativePath);
            if (!bundledManifest.TryGetValue(manifestKey, out var manifestItem) ||
                !string.Equals(manifestItem.BrowserName, specification.BrowserName, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(ComputeSha256(path), manifestItem.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            version = manifestItem.Version;
        }

        return new(
            specification.BrowserName,
            specification.DisplayName,
            version,
            relativePath,
            isBundled);
    }

    private IReadOnlyDictionary<string, BundledDriverManifestItem> LoadBundledManifest()
    {
        var path = _paths.Resolve(Path.Combine("drivers", "bundled", "drivers.json"));
        if (!File.Exists(path))
        {
            return new Dictionary<string, BundledDriverManifestItem>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            var manifest = JsonSerializer.Deserialize<BundledDriverManifest>(File.ReadAllText(path), SerializerOptions);
            return manifest?.Drivers
                .Where(item => !string.IsNullOrWhiteSpace(item.RelativePath))
                .ToDictionary(item => Normalize(item.RelativePath), StringComparer.OrdinalIgnoreCase)
                ?? new Dictionary<string, BundledDriverManifestItem>(StringComparer.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return new Dictionary<string, BundledDriverManifestItem>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static IEnumerable<string> EnumerateFiles(string root)
    {
        var pending = new Stack<string>();
        pending.Push(root);
        while (pending.Count > 0)
        {
            var directory = pending.Pop();
            foreach (var child in Directory.EnumerateDirectories(directory))
            {
                if (!IsReparsePoint(child))
                {
                    pending.Push(child);
                }
            }

            foreach (var file in Directory.EnumerateFiles(directory, "*.exe"))
            {
                if (!IsReparsePoint(file))
                {
                    yield return file;
                }
            }
        }
    }

    private static string ReadVersion(string path)
    {
        var fileVersion = FileVersionInfo.GetVersionInfo(path).FileVersion;
        if (!string.IsNullOrWhiteSpace(fileVersion))
        {
            var normalized = fileVersion.Split(' ', '-', '+')[0];
            if (Version.TryParse(normalized, out var parsed))
            {
                return parsed.ToString();
            }
        }

        var parentName = Path.GetFileName(Path.GetDirectoryName(path));
        return Version.TryParse(parentName, out var parentVersion) ? parentVersion.ToString() : "unknown";
    }

    private static Version ParseVersion(string version) =>
        Version.TryParse(version, out var parsed) ? parsed : new Version(0, 0);

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static string Normalize(string path) => path.Replace('\\', '/');

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;

    private sealed record DriverSpecification(string BrowserName, string DisplayName);

    private sealed record BundledDriverManifest(int SchemaVersion, IReadOnlyList<BundledDriverManifestItem> Drivers);

    private sealed record BundledDriverManifestItem(
        string BrowserName,
        string Version,
        string RelativePath,
        string Sha256);
}
