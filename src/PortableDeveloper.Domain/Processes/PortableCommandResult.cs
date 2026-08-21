namespace PortableDeveloper.Domain.Processes;

public sealed record PortableCommandResult(
    int? ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut = false)
{
    public bool IsSuccess => ExitCode == 0 && !TimedOut;
}
