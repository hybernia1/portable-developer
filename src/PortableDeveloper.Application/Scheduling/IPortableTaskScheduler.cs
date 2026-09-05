namespace PortableDeveloper.Application.Scheduling;

public interface IPortableTaskScheduler : IAsyncDisposable
{
    event EventHandler? Changed;

    IReadOnlyList<ScheduledTaskSnapshot> GetTasks(string projectId);

    IReadOnlyList<ScheduledTaskRunRecord> GetHistory(string projectId, int maximumCount = 200);

    void Add(PortableScheduledTask task);

    void Update(PortableScheduledTask task);

    void Remove(string taskId);

    void Start();

    Task<ScheduledTaskRunRecord> RunNowAsync(string taskId, CancellationToken cancellationToken = default);

    Task StopAsync();
}
