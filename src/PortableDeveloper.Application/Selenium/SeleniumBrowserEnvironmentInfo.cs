namespace PortableDeveloper.Application.Selenium;

public enum SeleniumBrowserSource
{
    Portable,
    System
}

public enum SeleniumBrowserEnvironmentState
{
    Ready,
    DriverMissing,
    VersionMismatch,
    BrowserUnavailable
}

public sealed record SeleniumBrowserEnvironmentInfo(
    string Id,
    string BrowserName,
    string DisplayName,
    string BrowserVersion,
    string BrowserExecutablePath,
    bool IsPortableBrowser,
    SeleniumBrowserSource Source,
    SeleniumDriverInfo? Driver,
    SeleniumBrowserEnvironmentState State,
    string Detail)
{
    public bool IsReady => State == SeleniumBrowserEnvironmentState.Ready && Driver is not null;
}
