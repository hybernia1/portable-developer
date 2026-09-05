using PortableDeveloper.Application.Scheduling;

namespace PortableDeveloper.App.ViewModels;

public sealed record ScheduledTaskViewModel(
    string Id,
    string Name,
    string Command,
    string Target,
    string Schedule,
    string NextRun,
    string LastRun,
    string Status,
    bool IsRunning,
    bool IsEnabled)
{
    public bool CanRun => !IsRunning;
}

public sealed record ScheduledTaskRunViewModel(
    string TaskName,
    string Started,
    string Duration,
    string Trigger,
    string Result,
    string Output,
    bool IsSuccess);

public sealed record ScheduledTaskChoice<T>(T Value, string Label);
