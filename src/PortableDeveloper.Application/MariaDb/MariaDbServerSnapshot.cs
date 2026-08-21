using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Application.MariaDb;

public sealed record MariaDbServerSnapshot(
    ManagedProcessState State,
    string Detail,
    int? ProcessId = null);
