namespace PortableDeveloper.Domain.Processes;

public sealed record ManagedProcessSnapshot(
    string Id,
    ManagedProcessState State,
    int? ProcessId = null,
    string? Detail = null);
