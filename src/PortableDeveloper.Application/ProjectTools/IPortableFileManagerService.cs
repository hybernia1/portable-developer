using PortableDeveloper.Application.Settings;

namespace PortableDeveloper.Application.ProjectTools;

public interface IPortableFileManagerService
{
    PortableToolRuntimeInfo GetRuntime();

    Task<PortableFileManagerLaunchResult> OpenAsync(
        ApplicationLanguage language,
        CancellationToken cancellationToken = default);
}
