namespace PortableDeveloper.Domain.Processes;

public enum ManagedProcessState
{
    Stopped,
    Starting,
    Running,
    Stopping,
    Failed
}
