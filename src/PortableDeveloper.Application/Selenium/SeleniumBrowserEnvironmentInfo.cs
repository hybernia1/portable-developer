namespace PortableDeveloper.Application.Selenium;

public enum SeleniumBrowserSource
{
    Managed
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
    bool IsManagedBrowser,
    SeleniumBrowserSource Source,
    SeleniumDriverInfo? Driver,
    SeleniumBrowserEnvironmentState State,
    string Detail)
{
    public bool IsReady => State == SeleniumBrowserEnvironmentState.Ready && Driver is not null;
}
