using PortableDeveloper.Application.Abstractions;

namespace PortableDeveloper.Application.Workspace;

public interface IPortableTerminalService
{
    string InitialWorkingDirectory { get; }

    IReadOnlyList<PortableTerminalCommandInfo> Commands { get; }

    Task<PortableTerminalResult> ExecuteAsync(
        string commandLine,
        string workingDirectory,
        CancellationToken cancellationToken = default);

    Task<PortableTerminalSessionStartResult> TryStartSessionAsync(
        string commandLine,
        string workingDirectory,
        IProgress<PortableProcessOutput> output,
        CancellationToken cancellationToken = default);
}
