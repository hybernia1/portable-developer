namespace PortableDeveloper.Application.Selenium;

public interface ISeleniumDriverInventory
{
    string DriversRelativePath { get; }

    IReadOnlyList<SeleniumDriverInfo> Scan();
}
