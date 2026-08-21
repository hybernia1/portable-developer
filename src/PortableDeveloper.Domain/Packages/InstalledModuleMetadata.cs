using PortableDeveloper.Domain.Modules;

namespace PortableDeveloper.Domain.Packages;

/// <summary>
/// Portable proof that an installed module came from a specific verified catalog archive.
/// </summary>
public sealed record InstalledModuleMetadata(
    ModuleKind Kind,
    string Version,
    string SourceUrl,
    string EntrypointSha256,
    string EntrypointRelativePath);
