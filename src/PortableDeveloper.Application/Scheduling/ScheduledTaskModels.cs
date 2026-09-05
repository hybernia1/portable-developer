namespace PortableDeveloper.Application.Scheduling;

public enum ScheduledTaskCommandKind
{
    PhpScript,
    PythonScript,
    NodeScript,
    NpmScript
}

public enum ScheduledTaskScheduleKind
{
    ApplicationStart,
    Interval,
    Daily,
    Weekly
}

public enum ScheduledTaskTrigger
{
    Scheduled,
    Manual,
    ApplicationStart
}

public enum ScheduledTaskOutcome
{
    Succeeded,
    Failed,
    TimedOut,
    Canceled
}

public sealed record ScheduledTaskSchedule(
    ScheduledTaskScheduleKind Kind,
    int IntervalMinutes = 60,
    int Hour = 9,
    int Minute = 0,
    DayOfWeek DayOfWeek = DayOfWeek.Monday);

public sealed record PortableScheduledTask(
    string Id,
    string ProjectId,
    string Name,
    ScheduledTaskCommandKind CommandKind,
    string Target,
    string Arguments,
    ScheduledTaskSchedule Schedule,
    int TimeoutMinutes = 10,
    bool IsEnabled = true);

public sealed record ScheduledTaskExecutionResult(
    int? ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut = false)
{
    public bool IsSuccess => ExitCode == 0 && !TimedOut;
}

public sealed record ScheduledTaskRunRecord(
    string Id,
    string TaskId,
    string TaskName,
    string ProjectId,
    ScheduledTaskTrigger Trigger,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset FinishedAtUtc,
    ScheduledTaskOutcome Outcome,
    int? ExitCode,
    string Output);

public sealed record ScheduledTaskSnapshot(
    PortableScheduledTask Definition,
    DateTimeOffset? NextRunUtc,
    bool IsRunning,
    ScheduledTaskRunRecord? LastRun);
