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
    Node,
    Python,
    Terminal,
    Files,
    Tools,
    Guides,
    Settings
}

public sealed record NavigationItemViewModel(
    NavigationPage Page,
    string Label,
    string Group,
    int GroupOrder,
    int ItemOrder)
{
    public string? BrandLogo => Page switch
    {
        NavigationPage.Apache => "apache",
        NavigationPage.Php => "php",
        NavigationPage.Databases => "mariadb",
        NavigationPage.Selenium => "selenium",
        NavigationPage.Composer => "composer",
        NavigationPage.Node => "nodejs",
        NavigationPage.Python => "python",
        NavigationPage.Tools => "notepadplusplus",
        _ => null
    };

    public bool UsesBrandLogo => BrandLogo is not null;
}
