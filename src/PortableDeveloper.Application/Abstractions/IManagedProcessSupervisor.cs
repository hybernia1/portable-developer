using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Application.Abstractions;

public interface IManagedProcessSupervisor : IAsyncDisposable
{
    Task<ManagedProcessSnapshot> StartAsync(
        ManagedProcessDefinition definition,
        CancellationToken cancellationToken = default);

    Task StopAsync(string processId, CancellationToken cancellationToken = default);

    IReadOnlyCollection<ManagedProcessSnapshot> GetSnapshots();
}
