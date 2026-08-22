namespace PortableDeveloper.Domain.Packages;

/// <summary>
/// Versioned, trusted download catalog shipped with a Portable Developer release.
/// The running application never accepts an arbitrary remote catalog.
/// </summary>
public sealed record DependencyLockCatalog(
    int SchemaVersion,
    IReadOnlyList<DependencyLockComponent> Components);

public sealed record DependencyLockComponent(
    string Id,
    string DisplayName,
    string Version,
    string FileName,
    string ArchiveSha256,
    IReadOnlyList<string> Sources,
    string LicenseUrl,
    string? ArchiveRoot = null,
    string? Build = null,
    string? NormalizedEntrypointRelativePath = null,
    string? NormalizedEntrypointSha256 = null,
    IReadOnlyDictionary<string, string>? ValidationFiles = null,
    IReadOnlyDictionary<string, string>? RuntimeFiles = null,
    string? SignerSubjectContains = null);
