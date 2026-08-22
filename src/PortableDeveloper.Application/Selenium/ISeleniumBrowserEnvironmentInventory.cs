namespace PortableDeveloper.Application.Selenium;

public interface ISeleniumBrowserEnvironmentInventory
{
    IReadOnlyList<SeleniumBrowserEnvironmentInfo> Scan();
}
