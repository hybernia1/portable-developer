namespace PortableDeveloper.Application.Scheduling;

public interface IScheduledTaskHistoryStore
{
    IReadOnlyList<ScheduledTaskRunRecord> ReadRecent(int maximumCount = 200);

    void Append(ScheduledTaskRunRecord record);

    bool Remove(string projectId, string recordId);

    int RemoveProject(string projectId);
}
