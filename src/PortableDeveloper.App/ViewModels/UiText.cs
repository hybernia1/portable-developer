using System.ComponentModel;
using System.Runtime.CompilerServices;
using PortableDeveloper.Application.Settings;

namespace PortableDeveloper.App.ViewModels;

public sealed partial class UiText : INotifyPropertyChanged
{
    private readonly IApplicationSettingsStore _settingsStore;
    private ApplicationLanguage _currentLanguage;

    public UiText(IApplicationSettingsStore settingsStore)
    {
        _settingsStore = settingsStore;
        _currentLanguage = settingsStore.Load().Language;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ApplicationLanguage CurrentLanguage => _currentLanguage;

    public string Close => IsCzech ? "Zavřít" : "Close";

    public string MoreInformation => IsCzech ? "Více informací" : "More information";

    public string NavigationLabel(NavigationPage page) => page switch
    {
        NavigationPage.Projects => IsCzech ? "Projekty" : "Projects",
        NavigationPage.Modules => IsCzech ? "Moduly" : "Modules",
        NavigationPage.Php => "PHP",
        NavigationPage.Apache => "Apache",
        NavigationPage.Databases => IsCzech ? "Databáze" : "Databases",
        NavigationPage.Selenium => "Selenium",
        NavigationPage.Ports => IsCzech ? "Porty" : "Ports",
        NavigationPage.Composer => "Composer",
        NavigationPage.Node => "Node.js",
        NavigationPage.Python => "Python",
        NavigationPage.Scheduler => IsCzech ? "Plánovač" : "Scheduler",
        NavigationPage.Terminal => IsCzech ? "Terminál" : "Terminal",
        NavigationPage.Files => IsCzech ? "Soubory" : "Files",
        NavigationPage.Guides => IsCzech ? "Návody" : "Guides",
        NavigationPage.Settings => IsCzech ? "Nastavení" : "Settings",
        _ => page.ToString()
    };

    public string PageTitle(NavigationPage page) => page switch
    {
        NavigationPage.Projects => IsCzech ? "Správa projektů" : "Project management",
        NavigationPage.Modules => IsCzech ? "Správce modulů" : "Module manager",
        NavigationPage.Php => IsCzech ? "PHP runtime" : "PHP runtime",
        NavigationPage.Apache => IsCzech ? "Apache server" : "Apache server",
        NavigationPage.Databases => IsCzech ? "Databáze" : "Databases",
        NavigationPage.Selenium => "Selenium Server",
        NavigationPage.Ports => IsCzech ? "Správce portů" : "Port manager",
        NavigationPage.Composer => IsCzech ? "Composer balíčky" : "Composer packages",
        NavigationPage.Node => IsCzech ? "Node.js balíčky" : "Node.js packages",
        NavigationPage.Python => IsCzech ? "Python balíčky" : "Python packages",
        NavigationPage.Scheduler => IsCzech ? "Plánovač úloh" : "Task scheduler",
        NavigationPage.Terminal => IsCzech ? "Portable terminál" : "Portable terminal",
        NavigationPage.Files => IsCzech ? "Soubory projektu" : "Project files",
        NavigationPage.Guides => IsCzech ? "Návody a ukázky" : "Guides and examples",
        NavigationPage.Settings => IsCzech ? "Nastavení aplikace" : "Application settings",
        _ => page.ToString()
    };

    public string NavigationSectionLabel(NavigationSection section) => section switch
    {
        NavigationSection.ProjectsOverview => ProjectsTab,
        NavigationSection.ProjectsCreate => CreateProjectTab,
        NavigationSection.ProjectsAdd => AddProjectTab,
        NavigationSection.PhpSettings => SettingsTab,
        NavigationSection.PhpExtensions => ExtensionsTab,
        NavigationSection.DatabasesOverview => OverviewTab,
        NavigationSection.DatabasesAdministration => AdministrationTab,
        NavigationSection.DatabasesCatalog => DatabasesTab,
        NavigationSection.SeleniumSettings => SeleniumSettings,
        NavigationSection.SeleniumDrivers => SeleniumDrivers,
        NavigationSection.SeleniumBrowserProfiles => SeleniumBrowserProfiles,
        NavigationSection.SeleniumCookieVaults => SeleniumCookieVaults,
        NavigationSection.SeleniumSessions => SeleniumSessions,
        NavigationSection.PortsApplication => ApplicationPortsTab,
        NavigationSection.PortsListeners => ListeningPortsTab,
        NavigationSection.SchedulerTasks => SchedulerTasksTab,
        NavigationSection.SchedulerHistory => SchedulerHistoryTab,
        NavigationSection.SettingsGeneral => SettingsGeneralTab,
        NavigationSection.SettingsStorage => SettingsStorageTab,
        NavigationSection.SettingsAbout => SettingsAboutTab,
        _ => string.Empty
    };

    public string NavigationGroup(int groupOrder) => groupOrder switch
    {
        0 => IsCzech ? "PROSTŘEDÍ" : "ENVIRONMENT",
        1 => IsCzech ? "SERVERY" : "SERVERS",
        2 => IsCzech ? "VÝVOJ" : "DEVELOPMENT",
        _ => IsCzech ? "APLIKACE" : "APPLICATION"
    };

    public void SetLanguage(ApplicationLanguage language)
    {
        if (_currentLanguage == language)
        {
            return;
        }

        _currentLanguage = language;
        _settingsStore.Save(_settingsStore.Load() with { Language = language });
        OnPropertyChanged(string.Empty);
    }

    private bool IsCzech => _currentLanguage == ApplicationLanguage.Czech;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
