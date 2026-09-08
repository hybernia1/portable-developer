using PortableDeveloper.Application.Scheduling;

namespace PortableDeveloper.App.ViewModels;

public sealed record ScheduledTaskViewModel(
    string Id,
    string Name,
    string Command,
    string Brand,
    string Target,
    string Schedule,
    string NextRun,
    string LastRun,
    string LastRunResult,
    bool LastRunSucceeded,
    string Status,
    bool IsRunning,
    bool IsEnabled)
{
    public bool CanRun => !IsRunning;

    public bool HasLastRunResult => !string.IsNullOrWhiteSpace(LastRunResult);
}

public sealed record ScheduledTaskRunViewModel(
    string Id,
    string TaskName,
    string Command,
    string Brand,
    string Target,
    string Started,
    string Duration,
    string Trigger,
    string Result,
    string Output,
    bool IsSuccess);

public sealed record ScheduledTaskChoice<T>(T Value, string Label);
