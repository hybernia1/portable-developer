using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Application.ApachePhp;

public sealed record ApachePhpStackSnapshot(
    ManagedProcessState State,
    string Detail,
    int? ApacheProcessId = null,
    int? PhpProcessId = null);
