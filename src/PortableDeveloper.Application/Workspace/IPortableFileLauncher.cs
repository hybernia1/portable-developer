using PortableDeveloper.Application.Settings;

namespace PortableDeveloper.Application.Workspace;

public enum PortableFileLaunchIntent
{
    Open,
    Edit
}

public sealed record PortableFileLaunchResult(bool IsSuccess, string Detail, bool UsedPortableEditor = false);

public interface IPortableFileLauncher
{
    Task<PortableFileLaunchResult> LaunchAsync(
        string relativeFilePath,
        string allowedRootRelativePath,
        PortableFileLaunchIntent intent,
        ApplicationLanguage language,
        string? initialContent = null,
        CancellationToken cancellationToken = default);
}
