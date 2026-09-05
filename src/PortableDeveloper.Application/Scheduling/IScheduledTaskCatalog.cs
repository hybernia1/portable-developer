namespace PortableDeveloper.Application.Scheduling;

public interface IScheduledTaskCatalog
{
    IReadOnlyList<PortableScheduledTask> Tasks { get; }

    PortableScheduledTask GetRequired(string taskId);

    void Add(PortableScheduledTask task);

    void Update(PortableScheduledTask task);

    void Remove(string taskId);
}
