namespace PortableDeveloper.Application.Workspace;

public interface IPortableTerminalService
{
    string InitialWorkingDirectory { get; }

    IReadOnlyList<PortableTerminalCommandInfo> Commands { get; }

    Task<PortableTerminalResult> ExecuteAsync(
        string commandLine,
        string workingDirectory,
        CancellationToken cancellationToken = default);
}
