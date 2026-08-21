namespace PortableDeveloper.Domain.Modules;

/// <summary>
/// A module detected in the portable modules directory.
/// Detection does not mean the module has been verified for execution yet.
/// </summary>
public sealed record ModuleInstallation(
    ModuleKind Kind,
    string Version,
    string ModuleRootRelativePath,
    string EntrypointRelativePath);
