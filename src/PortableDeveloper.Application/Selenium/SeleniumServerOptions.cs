namespace PortableDeveloper.Application.Selenium;

public sealed record SeleniumServerOptions(
    string InstanceId = "default",
    int Port = 4444,
    int MaxSessions = 2,
    int SessionTimeoutSeconds = 300)
{
    public static SeleniumServerOptions Default { get; } = new();
}
