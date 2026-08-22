namespace PortableDeveloper.Application.ProjectTools;

public interface IProjectPackageManagerService
{
    PortableToolKind Kind { get; }

    string ProjectRelativePath { get; }

    PortableToolRuntimeInfo GetRuntime();

    Task<IReadOnlyList<ProjectPackageInfo>> ListPackagesAsync(
        CancellationToken cancellationToken = default);

    Task<PackageOperationResult> InstallPackageAsync(
        string packageName,
        string versionConstraint,
        CancellationToken cancellationToken = default);

    Task<PackageOperationResult> RemovePackageAsync(
        string packageName,
        CancellationToken cancellationToken = default);
}
