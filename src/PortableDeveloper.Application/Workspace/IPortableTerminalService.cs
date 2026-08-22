namespace PortableDeveloper.Application.Workspace;

public interface IPortableTerminalService
{
    string InitialWorkingDirectory { get; }

    Task<PortableTerminalResult> ExecuteAsync(
        string commandLine,
        string workingDirectory,
        CancellationToken cancellationToken = default);
}
