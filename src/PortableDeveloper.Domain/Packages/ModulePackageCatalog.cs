namespace PortableDeveloper.Domain.Packages;

public sealed record ModulePackageCatalog(
    int SchemaVersion,
    IReadOnlyList<ModulePackageManifest> Packages);
