namespace PortableDeveloper.Application.Workspace;

public enum PortableServiceTarget
{
    Web,
    MariaDb,
    Selenium,
    All
}

public enum PortableTerminalServiceOperation
{
    Status,
    Start,
    Stop,
    Restart
}

public sealed record PortableTerminalServiceRequest(
    PortableTerminalServiceOperation Operation,
    PortableServiceTarget Service);

public sealed record PortableTerminalResult(
    string WorkingDirectory,
    string Output,
    bool ClearScreen = false,
    PortableTerminalServiceRequest? ServiceRequest = null,
    bool IsError = false);
