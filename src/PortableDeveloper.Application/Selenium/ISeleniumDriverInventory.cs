namespace PortableDeveloper.Application.Selenium;

public interface ISeleniumDriverInventory
{
    IReadOnlyList<SeleniumDriverInfo> Scan();

    IReadOnlyList<SeleniumDriverInfo> ScanAll() => Scan();
}
