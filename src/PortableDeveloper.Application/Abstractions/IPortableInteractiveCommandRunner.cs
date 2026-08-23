using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Application.Abstractions;

public sealed record PortableProcessOutput(string Text, bool IsError = false);

public sealed record PortableInteractiveProcessResult(
    int? ExitCode,
    bool WasStopped = false,
    bool TimedOut = false)
{
    public bool IsSuccess => ExitCode == 0 && !WasStopped && !TimedOut;
}

public interface IPortableProcessSession : IAsyncDisposable
{
    bool IsRunning { get; }

    Task<PortableInteractiveProcessResult> Completion { get; }

    ValueTask WriteLineAsync(string input, CancellationToken cancellationToken = default);

    Task StopAsync(CancellationToken cancellationToken = default);
}

public interface IPortableInteractiveCommandRunner
{
    Task<IPortableProcessSession> StartAsync(
        PortableCommandDefinition definition,
        IProgress<PortableProcessOutput> output,
        CancellationToken cancellationToken = default);
}
