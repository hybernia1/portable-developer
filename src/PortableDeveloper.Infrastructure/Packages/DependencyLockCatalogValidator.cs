using System.Text.RegularExpressions;
using PortableDeveloper.Application.Packages;
using PortableDeveloper.Domain.Packages;

namespace PortableDeveloper.Infrastructure.Packages;

public static partial class DependencyLockCatalogValidator
{
    private static readonly HashSet<string> AllowedHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "www.apachelounge.com",
        "windows.php.net",
        "downloads.php.net",
        "downloads.mariadb.org",
        "archive.mariadb.org",
        "github.com",
        "objects.githubusercontent.com",
        "release-assets.githubusercontent.com",
        "storage.googleapis.com",
        "archive.mozilla.org",
        "aka.ms",
        "download.visualstudio.microsoft.com",
        "getcomposer.org",
        "api.nuget.org",
        "globalcdn.nuget.org",
        "files.phpmyadmin.net"
    };

    public static void Validate(DependencyLockCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if (catalog.SchemaVersion != 1 || catalog.Components is null)
        {
            throw new InvalidDataException($"Unsupported dependency lock schema: {catalog.SchemaVersion}.");
        }

        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var component in catalog.Components)
        {
            if (string.IsNullOrWhiteSpace(component.Id)
                || !SafeSegmentPattern().IsMatch(component.Id)
                || !ids.Add(component.Id)
                || string.IsNullOrWhiteSpace(component.DisplayName)
                || string.IsNullOrWhiteSpace(component.Version)
                || !SafeSegmentPattern().IsMatch(component.Version)
                || string.IsNullOrWhiteSpace(component.FileName)
                || Path.GetFileName(component.FileName) != component.FileName
                || !Sha256Pattern().IsMatch(component.ArchiveSha256)
                || component.Sources is null
                || component.Sources.Count == 0)
            {
                throw new InvalidDataException("The dependency lock contains an invalid or duplicate component.");
            }

            foreach (var source in component.Sources)
            {
                EnsureAllowedHttpsUri(source);
            }

            EnsureHttpsUri(component.LicenseUrl);
            ValidateOptionalHash(component.NormalizedEntrypointSha256);
            ValidateHashMap(component.ValidationFiles);
            ValidateHashMap(component.RuntimeFiles);
        }
    }

    public static bool IsAllowedDownloadUri(Uri uri) =>
        uri.Scheme == Uri.UriSchemeHttps && AllowedHosts.Contains(uri.Host);

    private static void EnsureAllowedHttpsUri(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || !IsAllowedDownloadUri(uri))
        {
            throw new InvalidDataException($"Dependency URL is not an allowed HTTPS source: {value}");
        }
    }

    private static void EnsureHttpsUri(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidDataException($"Dependency URL must use HTTPS: {value}");
        }
    }

    private static void ValidateOptionalHash(string? value)
    {
        if (value is not null && !Sha256Pattern().IsMatch(value))
        {
            throw new InvalidDataException("A dependency SHA-256 value is invalid.");
        }
    }

    private static void ValidateHashMap(IReadOnlyDictionary<string, string>? values)
    {
        if (values is null)
        {
            return;
        }

        foreach (var (path, hash) in values)
        {
            ModulePackageManifestValidator.EnsureSafeRelativePath(path, nameof(path), allowEmpty: false);
            if (!Sha256Pattern().IsMatch(hash))
            {
                throw new InvalidDataException("A dependency file SHA-256 value is invalid.");
            }
        }
    }

    [GeneratedRegex("^[a-zA-Z0-9._-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeSegmentPattern();

    [GeneratedRegex("^[a-fA-F0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Pattern();
}
