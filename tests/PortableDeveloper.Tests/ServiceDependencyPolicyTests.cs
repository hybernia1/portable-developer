using PortableDeveloper.Application.Services;
using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Tests;

public sealed class ServiceDependencyPolicyTests
{
    [Theory]
    [InlineData(ManagedProcessState.Running, ManagedProcessState.Running, PhpMyAdminAvailability.Ready)]
    [InlineData(ManagedProcessState.Stopped, ManagedProcessState.Running, PhpMyAdminAvailability.NeedsWeb)]
    [InlineData(ManagedProcessState.Running, ManagedProcessState.Stopped, PhpMyAdminAvailability.NeedsDatabase)]
    [InlineData(ManagedProcessState.Starting, ManagedProcessState.Stopping, PhpMyAdminAvailability.NeedsWebAndDatabase)]
    public void PhpMyAdmin_requires_both_running_services(
        ManagedProcessState webState,
        ManagedProcessState databaseState,
        PhpMyAdminAvailability expected)
    {
        Assert.Equal(expected, ServiceDependencyPolicy.GetPhpMyAdminAvailability(webState, databaseState));
    }
}
