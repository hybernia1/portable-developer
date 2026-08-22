namespace PortableDeveloper.Application.Ports;

public sealed record PortSettings(
    int ApachePort = 8080,
    int PhpFastCgiPort = 9000,
    int MariaDbPort = 3307,
    int SeleniumPort = 4444)
{
    public static PortSettings Default { get; } = new();
}
