using PortableDeveloper.Application.Projects;

namespace PortableDeveloper.App.ViewModels;

public sealed record WebProjectViewModel(
    string Id,
    string Name,
    string HostName,
    string ProjectRootRelativePath,
    string WebRootRelativePath,
    bool AllowHtaccess,
    bool IsEnabled,
    bool IsActive,
    bool IsDefault,
    string Url,
    string WebRootDisplay,
    string ActiveState,
    string HtaccessState,
    string ApacheState,
    string HtaccessAction,
    string ApacheAction)
{
    public bool CanRemove => !IsDefault;

    public bool CanSelect => !IsActive;

    public bool CanToggleApache => !IsDefault;

    public static WebProjectViewModel From(WebProject project, string activeProjectId, int apachePort, UiText text)
    {
        var isActive = string.Equals(project.Id, activeProjectId, StringComparison.OrdinalIgnoreCase);
        return new(
            project.Id,
            project.Id == WebProjectCatalogDefaults.DefaultProjectId ? text.DefaultProjectName : project.Name,
            project.HostName,
            project.ProjectRootRelativePath,
            project.WebRootRelativePath,
            project.AllowHtaccess,
            project.IsEnabled,
            isActive,
            project.Id == WebProjectCatalogDefaults.DefaultProjectId,
            $"http://{project.HostName}:{apachePort}/",
            $"Web root: {project.WebRootRelativePath}",
            isActive ? text.ActiveProjectBadge : string.Empty,
            $".htaccess: {(project.AllowHtaccess ? text.Enabled : text.Disabled)}",
            $"Apache: {(project.IsEnabled ? text.Enabled : text.Disabled)}",
            project.AllowHtaccess ? text.DisableHtaccess : text.EnableHtaccess,
            project.IsEnabled ? text.DisableInApache : text.EnableInApache);
    }
}
