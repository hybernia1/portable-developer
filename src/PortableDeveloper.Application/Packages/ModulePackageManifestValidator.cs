using System.Text.RegularExpressions;
using PortableDeveloper.Domain.Packages;

namespace PortableDeveloper.Application.Packages;

public static partial class ModulePackageManifestValidator
{
    public const int CurrentSchemaVersion = 1;

    public static void Validate(ModulePackageCatalog catalog)
    {
        ArgumentNullException.ThrowIfNull(catalog);
        if (catalog.SchemaVersion != CurrentSchemaVersion)
        {
            throw new InvalidDataException($"Unsupported catalog schema version: {catalog.SchemaVersion}.");
        }

        if (catalog.Packages is null)
        {
            throw new InvalidDataException("Catalog packages cannot be null.");
        }

        var duplicatePackages = catalog.Packages
            .GroupBy(package => (package.Kind, package.Version), StringTupleComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicatePackages is not null)
        {
            throw new InvalidDataException($"Catalog contains a duplicate package: {duplicatePackages.Key.Kind} {duplicatePackages.Key.Version}.");
        }

        foreach (var package in catalog.Packages)
        {
            ValidatePackage(package);
        }
    }

    public static void ValidatePackage(ModulePackageManifest package)
    {
        ArgumentNullException.ThrowIfNull(package);
        ArgumentException.ThrowIfNullOrWhiteSpace(package.Version);
        EnsureSafePathSegment(package.Version, nameof(package.Version));
        EnsureHttpsUrl(package.SourceUrl, nameof(package.SourceUrl));
        EnsureHttpsUrl(package.LicenseUrl, nameof(package.LicenseUrl));

        if (!Sha256Pattern().IsMatch(package.EntrypointSha256))
        {
            throw new InvalidDataException("Module entrypoint SHA-256 must contain exactly 64 hexadecimal characters.");
        }

        EnsureSafeRelativePath(package.EntrypointRelativePath, nameof(package.EntrypointRelativePath), allowEmpty: false);
    }

    public static void EnsureSafeRelativePath(string path, string parameterName, bool allowEmpty)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            if (allowEmpty)
            {
                return;
            }

            throw new ArgumentException("A relative path must be provided.", parameterName);
        }

        if (Path.IsPathRooted(path))
        {
            throw new InvalidDataException($"{parameterName} must be relative.");
        }

        var segments = path.Replace('\\', '/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0 || segments.Any(segment => segment is "." or ".."))
        {
            throw new InvalidDataException($"{parameterName} contains an unsafe path traversal segment.");
        }

        foreach (var segment in segments)
        {
            EnsureSafePathSegment(segment, parameterName);
        }
    }

    private static void EnsureSafePathSegment(string value, string parameterName)
    {
        if (value.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '.' and not '-' and not '_'))
        {
            throw new InvalidDataException($"{parameterName} contains unsupported characters.");
        }
    }

    private static void EnsureHttpsUrl(string value, string parameterName)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
        {
            throw new InvalidDataException($"{parameterName} must be an absolute HTTPS URL.");
        }
    }

    [GeneratedRegex("^[a-fA-F0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex Sha256Pattern();

    private sealed class StringTupleComparer : IEqualityComparer<(PortableDeveloper.Domain.Modules.ModuleKind Kind, string Version)>
    {
        public static readonly StringTupleComparer OrdinalIgnoreCase = new();

        public bool Equals((PortableDeveloper.Domain.Modules.ModuleKind Kind, string Version) x, (PortableDeveloper.Domain.Modules.ModuleKind Kind, string Version) y) =>
            x.Kind == y.Kind && string.Equals(x.Version, y.Version, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((PortableDeveloper.Domain.Modules.ModuleKind Kind, string Version) value) =>
            HashCode.Combine(value.Kind, StringComparer.OrdinalIgnoreCase.GetHashCode(value.Version));
    }
}
