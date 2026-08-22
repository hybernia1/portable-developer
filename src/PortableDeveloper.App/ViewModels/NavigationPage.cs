namespace PortableDeveloper.App.ViewModels;

public enum NavigationPage
{
    Dashboard,
    Modules,
    Php,
    Apache,
    Databases,
    Selenium,
    Ports,
    Composer,
    Python,
    Terminal,
    Files,
    Tools,
    Settings
}

public sealed record NavigationItemViewModel(
    NavigationPage Page,
    string Label,
    string Group,
    int GroupOrder,
    int ItemOrder);
