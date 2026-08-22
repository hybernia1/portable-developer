namespace PortableDeveloper.Application.Packages;

public interface IRuntimePackageManager
{
    IReadOnlyList<RuntimePackageInfo> GetPackages();

    Task<RuntimePackageInstallResult> InstallAsync(
        RuntimePackageKind package,
        IProgress<RuntimePackageInstallProgress>? progress = null,
        CancellationToken cancellationToken = default);
}
