using PortableDeveloper.Application.Abstractions;

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

public sealed record PortableTerminalCommandInfo(
    string Name,
    IReadOnlyList<string> Aliases,
    string Usage,
    string Description,
    string Category);

public sealed record PortableTerminalResult(
    string WorkingDirectory,
    string Output,
    bool ClearScreen = false,
    PortableTerminalServiceRequest? ServiceRequest = null,
    bool IsError = false);

public sealed record PortableTerminalSessionStartResult(
    bool IsRuntimeCommand,
    IPortableProcessSession? Session = null,
    string Error = "")
{
    public bool IsSuccess => IsRuntimeCommand && Session is not null && string.IsNullOrEmpty(Error);
}
