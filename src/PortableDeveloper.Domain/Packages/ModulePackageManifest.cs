using PortableDeveloper.Domain.Modules;

namespace PortableDeveloper.Domain.Packages;

/// <summary>
/// Immutable package data from the trusted, bundled catalog.
/// </summary>
public sealed record ModulePackageManifest(
    ModuleKind Kind,
    string Version,
    string SourceUrl,
    string EntrypointSha256,
    string EntrypointRelativePath,
    string LicenseUrl);
