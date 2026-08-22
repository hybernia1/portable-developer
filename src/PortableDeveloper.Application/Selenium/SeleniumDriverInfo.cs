namespace PortableDeveloper.Application.Selenium;

public sealed record SeleniumDriverInfo(
    string BrowserName,
    string DisplayName,
    string Version,
    string RelativePath,
    bool IsBundled);
