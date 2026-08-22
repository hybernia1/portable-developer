namespace PortableDeveloper.Application.ProjectTools;

using PortableDeveloper.Application.Settings;

public interface IPortableEditorService
{
    PortableToolRuntimeInfo GetRuntime();

    Task<PortableEditorLaunchResult> OpenAsync(
        ApplicationLanguage language,
        string? relativeFilePath = null,
        string? initialContent = null,
        CancellationToken cancellationToken = default);
}
