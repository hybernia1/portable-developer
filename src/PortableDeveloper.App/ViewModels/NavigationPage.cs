using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PortableDeveloper.App.ViewModels;

public enum NavigationPage
{
    Projects,
    Modules,
    Php,
    Apache,
    Databases,
    Selenium,
    Ports,
    Composer,
    Node,
    Python,
    Scheduler,
    Terminal,
    Files,
    Guides,
    Settings
}

public enum NavigationSection
{
    None,
    ProjectsOverview,
    ProjectsCreate,
    ProjectsAdd,
    PhpSettings,
    PhpExtensions,
    DatabasesOverview,
    DatabasesAdministration,
    DatabasesCatalog,
    SeleniumSettings,
    SeleniumDrivers,
    SeleniumBrowserProfiles,
    SeleniumCookieVaults,
    SeleniumSessions,
    PortsApplication,
    PortsListeners,
    SchedulerTasks,
    SchedulerHistory,
    SettingsGeneral,
    SettingsStorage,
    SettingsAbout
}

public sealed record NavigationSectionViewModel(
    NavigationSection Section,
    string Label);

public enum NavigationIconKind
{
    Folder,
    Package,
    Ports,
    Code,
    Database,
    Gear,
    Schedule,
    Terminal,
    Guide
}

public sealed class NavigationItemViewModel : INotifyPropertyChanged
{
    private bool? _serviceIsRunning;
    private string _statusText = string.Empty;

    public NavigationItemViewModel(
        NavigationPage page,
        string label,
        string group,
        int groupOrder,
        int itemOrder)
    {
        Page = page;
        Label = label;
        Group = group;
        GroupOrder = groupOrder;
        ItemOrder = itemOrder;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public NavigationPage Page { get; }

    public string Label { get; }

    public string Group { get; }

    public int GroupOrder { get; }

    public int ItemOrder { get; }

    public string? BrandLogo => Page switch
    {
        NavigationPage.Apache => "apache",
        NavigationPage.Php => "php",
        NavigationPage.Databases => "mariadb",
        NavigationPage.Selenium => "selenium",
        NavigationPage.Composer => "composer",
        NavigationPage.Node => "nodejs",
        NavigationPage.Python => "python",
        _ => null
    };

    public bool UsesBrandLogo => BrandLogo is not null;

    public NavigationIconKind IconKind => Page switch
    {
        NavigationPage.Projects or NavigationPage.Files => NavigationIconKind.Folder,
        NavigationPage.Ports => NavigationIconKind.Ports,
        NavigationPage.Php or NavigationPage.Python => NavigationIconKind.Code,
        NavigationPage.Databases => NavigationIconKind.Database,
        NavigationPage.Selenium or NavigationPage.Settings => NavigationIconKind.Gear,
        NavigationPage.Scheduler => NavigationIconKind.Schedule,
        NavigationPage.Terminal => NavigationIconKind.Terminal,
        NavigationPage.Guides => NavigationIconKind.Guide,
        _ => NavigationIconKind.Package
    };

    public bool HasStatus => _serviceIsRunning.HasValue;

    public bool IsRunning => _serviceIsRunning == true;

    public string StatusText => _statusText;

    public void SetServiceStatus(bool isRunning, string statusText)
    {
        _serviceIsRunning = isRunning;
        _statusText = statusText;
        OnPropertyChanged(nameof(HasStatus));
        OnPropertyChanged(nameof(IsRunning));
        OnPropertyChanged(nameof(StatusText));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
