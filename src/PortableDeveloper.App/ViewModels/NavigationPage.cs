namespace PortableDeveloper.App.ViewModels;

public enum NavigationPage
{
    Dashboard,
    Php,
    Apache,
    Databases,
    Selenium,
    Settings
}

public sealed record NavigationItemViewModel(NavigationPage Page, string Label);
