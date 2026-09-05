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
    Terminal,
    Files,
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
        _ => null
    };

    public bool UsesBrandLogo => BrandLogo is not null;
}
