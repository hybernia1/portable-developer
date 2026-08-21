using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Application.Abstractions;

public interface IPortableCommandRunner
{
    Task<PortableCommandResult> RunAsync(
        PortableCommandDefinition definition,
        CancellationToken cancellationToken = default);
}
