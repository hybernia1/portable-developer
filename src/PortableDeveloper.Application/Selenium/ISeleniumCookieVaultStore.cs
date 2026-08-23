namespace PortableDeveloper.Application.Selenium;

public interface ISeleniumCookieVaultStore
{
    IReadOnlyList<SeleniumCookieVaultInfo> GetVaults();

    SeleniumCookieVaultOperationResult ImportJson(
        string name,
        byte[] json);

    SeleniumCookieVaultOperationResult Remove(string id);
}
