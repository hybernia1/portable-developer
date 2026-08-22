using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Application.Services;

public static class ServiceDependencyPolicy
{
    public static PhpMyAdminAvailability GetPhpMyAdminAvailability(
        ManagedProcessState webState,
        ManagedProcessState databaseState)
    {
        var webReady = webState == ManagedProcessState.Running;
        var databaseReady = databaseState == ManagedProcessState.Running;
        return (webReady, databaseReady) switch
        {
            (true, true) => PhpMyAdminAvailability.Ready,
            (false, true) => PhpMyAdminAvailability.NeedsWeb,
            (true, false) => PhpMyAdminAvailability.NeedsDatabase,
            _ => PhpMyAdminAvailability.NeedsWebAndDatabase
        };
    }
}
