namespace PortableDeveloper.Application.Scheduling;

public interface IScheduledTaskExecutor
{
    Task<ScheduledTaskExecutionResult> ExecuteAsync(
        PortableScheduledTask task,
        CancellationToken cancellationToken = default);
}
