using PortableDeveloper.App.ViewModels;

namespace PortableDeveloper.App.Shell;

public static class WorkspaceNavigationPolicy
{
    public static IReadOnlyList<NavigationSection> GetSections(NavigationPage page) => page switch
    {
        NavigationPage.Projects =>
        [
            NavigationSection.ProjectsOverview,
            NavigationSection.ProjectsCreate,
            NavigationSection.ProjectsAdd
        ],
        NavigationPage.Php => [NavigationSection.PhpSettings, NavigationSection.PhpExtensions],
        NavigationPage.Databases =>
        [
            NavigationSection.DatabasesOverview,
            NavigationSection.DatabasesAdministration,
            NavigationSection.DatabasesCatalog
        ],
        NavigationPage.Selenium =>
        [
            NavigationSection.SeleniumSettings,
            NavigationSection.SeleniumDrivers,
            NavigationSection.SeleniumBrowserProfiles,
            NavigationSection.SeleniumCookieVaults,
            NavigationSection.SeleniumSessions
        ],
        NavigationPage.Ports => [NavigationSection.PortsApplication, NavigationSection.PortsListeners],
        NavigationPage.Scheduler => [NavigationSection.SchedulerTasks, NavigationSection.SchedulerHistory],
        NavigationPage.Settings =>
        [
            NavigationSection.SettingsGeneral,
            NavigationSection.SettingsStorage,
            NavigationSection.SettingsAbout
        ],
        _ => []
    };

    public static NavigationSection ResolveSection(
        NavigationPage page,
        NavigationSection previousSection,
        bool preserveValidSelection)
    {
        var sections = GetSections(page);
        if (preserveValidSelection && sections.Contains(previousSection))
        {
            return previousSection;
        }

        return sections.FirstOrDefault();
    }
}
