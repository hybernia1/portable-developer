namespace PortableDeveloper.Application.Selenium;

public sealed record SeleniumSessionInfo(
    string Id,
    string BrowserName,
    string BrowserVersion,
    string PlatformName,
    DateTimeOffset? StartedAtUtc,
    TimeSpan Duration);
