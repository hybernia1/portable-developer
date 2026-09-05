using System.Text.RegularExpressions;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Scheduling;

namespace PortableDeveloper.Infrastructure.Scheduling;

public sealed partial class PortableTaskScheduler : IPortableTaskScheduler
{
    private const int MaximumStoredOutputCharacters = 20_000;
    private readonly IScheduledTaskCatalog _catalog;
    private readonly IScheduledTaskHistoryStore _history;
    private readonly IScheduledTaskExecutor _executor;
    private readonly IApplicationLogger _logger;
    private readonly TimeProvider _timeProvider;
    private readonly object _sync = new();
    private readonly SemaphoreSlim _concurrency = new(2, 2);
    private readonly Dictionary<string, DateTimeOffset?> _nextRuns = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Task<ScheduledTaskRunRecord>> _running = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource? _lifetime;
    private Task? _loop;

    public PortableTaskScheduler(
        IScheduledTaskCatalog catalog,
        IScheduledTaskHistoryStore history,
        IScheduledTaskExecutor executor,
        IApplicationLogger logger,
        TimeProvider? timeProvider = null)
    {
        _catalog = catalog;
        _history = history;
        _executor = executor;
        _logger = logger;
        _timeProvider = timeProvider ?? TimeProvider.System;
    }

    public event EventHandler? Changed;

    public IReadOnlyList<ScheduledTaskSnapshot> GetTasks(string projectId)
    {
        var lastRuns = _history.ReadRecent().GroupBy(record => record.TaskId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.OrderByDescending(item => item.StartedAtUtc).First(), StringComparer.OrdinalIgnoreCase);
        lock (_sync)
        {
            return _catalog.Tasks
                .Where(task => string.Equals(task.ProjectId, projectId, StringComparison.OrdinalIgnoreCase))
                .OrderBy(task => task.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(task => new ScheduledTaskSnapshot(
                    task,
                    _nextRuns.GetValueOrDefault(task.Id),
                    _running.ContainsKey(task.Id),
                    lastRuns.GetValueOrDefault(task.Id)))
                .ToArray();
        }
    }

    public IReadOnlyList<ScheduledTaskRunRecord> GetHistory(string projectId, int maximumCount = 200) =>
        _history.ReadRecent(maximumCount)
            .Where(record => string.Equals(record.ProjectId, projectId, StringComparison.OrdinalIgnoreCase))
            .Take(maximumCount)
            .ToArray();

    public void Add(PortableScheduledTask task)
    {
        task = ScheduledTaskValidator.Validate(task);
        lock (_sync)
        {
            _catalog.Add(task);
            _nextRuns[task.Id] = CalculateNextRunAfterCatalogChange(task, _timeProvider.GetUtcNow());
        }

        OnChanged();
    }

    public void Update(PortableScheduledTask task)
    {
        task = ScheduledTaskValidator.Validate(task);
        lock (_sync)
        {
            _catalog.Update(task);
            _nextRuns[task.Id] = CalculateNextRunAfterCatalogChange(task, _timeProvider.GetUtcNow());
        }

        OnChanged();
    }

    public void Remove(string taskId)
    {
        lock (_sync)
        {
            if (_running.ContainsKey(taskId))
            {
                throw new InvalidOperationException("A running scheduled task cannot be removed.");
            }

            _catalog.Remove(taskId);
            _nextRuns.Remove(taskId);
        }

        OnChanged();
    }

    public void Start()
    {
        lock (_sync)
        {
            if (_lifetime is not null)
            {
                return;
            }

            _lifetime = new CancellationTokenSource();
            var now = _timeProvider.GetUtcNow();
            foreach (var task in _catalog.Tasks)
            {
                _nextRuns[task.Id] = CalculateInitialNextRun(task, now);
            }

            _loop = RunLoopAsync(_lifetime.Token);
        }

        OnChanged();
    }

    public Task<ScheduledTaskRunRecord> RunNowAsync(string taskId, CancellationToken cancellationToken = default)
    {
        PortableScheduledTask task;
        lock (_sync)
        {
            task = _catalog.GetRequired(taskId);
        }

        return StartRun(task, ScheduledTaskTrigger.Manual, cancellationToken);
    }

    public async Task StopAsync()
    {
        CancellationTokenSource? lifetime;
        Task? loop;
        lock (_sync)
        {
            lifetime = _lifetime;
            loop = _loop;
            _lifetime = null;
            _loop = null;
        }

        if (lifetime is null)
        {
            return;
        }

        lifetime.Cancel();
        try
        {
            if (loop is not null)
            {
                await loop;
            }
        }
        catch (OperationCanceledException)
        {
        }

        Task[] active;
        lock (_sync)
        {
            active = _running.Values.Cast<Task>().ToArray();
        }

        try
        {
            await Task.WhenAll(active);
        }
        catch (OperationCanceledException)
        {
        }

        lifetime.Dispose();
        OnChanged();
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _concurrency.Dispose();
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1), _timeProvider);
        await ProcessDueTasksAsync(cancellationToken);
        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            await ProcessDueTasksAsync(cancellationToken);
        }
    }

    private Task ProcessDueTasksAsync(CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var due = new List<(PortableScheduledTask Task, ScheduledTaskTrigger Trigger)>();
        lock (_sync)
        {
            foreach (var task in _catalog.Tasks.Where(task => task.IsEnabled))
            {
                if (!_nextRuns.TryGetValue(task.Id, out var nextRun) || nextRun is null || nextRun > now)
                {
                    continue;
                }

                var trigger = task.Schedule.Kind == ScheduledTaskScheduleKind.ApplicationStart
                    ? ScheduledTaskTrigger.ApplicationStart
                    : ScheduledTaskTrigger.Scheduled;
                _nextRuns[task.Id] = CalculateFollowingRun(task, nextRun.Value, now);
                if (!_running.ContainsKey(task.Id))
                {
                    due.Add((task, trigger));
                }
            }
        }

        foreach (var item in due)
        {
            try
            {
                _ = ObserveRunAsync(StartRun(item.Task, item.Trigger, cancellationToken));
            }
            catch (InvalidOperationException)
            {
                // A manual run won the race after the due snapshot was collected.
            }
        }

        if (due.Count > 0)
        {
            OnChanged();
        }

        return Task.CompletedTask;
    }

    private Task<ScheduledTaskRunRecord> StartRun(
        PortableScheduledTask task,
        ScheduledTaskTrigger trigger,
        CancellationToken cancellationToken)
    {
        lock (_sync)
        {
            if (_running.ContainsKey(task.Id))
            {
                throw new InvalidOperationException("The scheduled task is already running.");
            }

            var schedulerCancellation = _lifetime?.Token ?? CancellationToken.None;
            var run = ExecuteRunAsync(task, trigger, cancellationToken, schedulerCancellation);
            _running.Add(task.Id, run);
            OnChanged();
            return run;
        }
    }

    private async Task<ScheduledTaskRunRecord> ExecuteRunAsync(
        PortableScheduledTask task,
        ScheduledTaskTrigger trigger,
        CancellationToken cancellationToken,
        CancellationToken schedulerCancellation)
    {
        // Let StartRun register this task before even an immediately rejected execution can complete.
        await Task.Yield();
        using var linkedCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            schedulerCancellation);
        cancellationToken = linkedCancellation.Token;
        var startedAt = _timeProvider.GetUtcNow();
        ScheduledTaskExecutionResult? result = null;
        ScheduledTaskOutcome outcome;
        string output;
        try
        {
            await _concurrency.WaitAsync(cancellationToken);
            try
            {
                result = await _executor.ExecuteAsync(task, cancellationToken);
            }
            finally
            {
                _concurrency.Release();
            }

            outcome = result.TimedOut
                ? ScheduledTaskOutcome.TimedOut
                : result.IsSuccess ? ScheduledTaskOutcome.Succeeded : ScheduledTaskOutcome.Failed;
            output = CombineOutput(result);
        }
        catch (OperationCanceledException)
        {
            outcome = ScheduledTaskOutcome.Canceled;
            output = "The task was canceled.";
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or UnauthorizedAccessException)
        {
            outcome = ScheduledTaskOutcome.Failed;
            output = exception.Message;
        }

        var record = new ScheduledTaskRunRecord(
            Guid.NewGuid().ToString("N"),
            task.Id,
            task.Name,
            task.ProjectId,
            trigger,
            startedAt,
            _timeProvider.GetUtcNow(),
            outcome,
            result?.ExitCode,
            SanitizeAndBoundOutput(output));
        try
        {
            _history.Append(record);
            await LogOutcomeAsync(record);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            _ = exception;
            try
            {
                await _logger.LogAsync(
                    ApplicationLogLevel.Error,
                    $"scheduler.{record.TaskId}",
                    "task.history.failed",
                    "Scheduled task history could not be persisted.");
            }
            catch
            {
            }
        }
        finally
        {
            lock (_sync)
            {
                _running.Remove(task.Id);
            }

            OnChanged();
        }

        return record;
    }

    private static async Task ObserveRunAsync(Task<ScheduledTaskRunRecord> run)
    {
        try
        {
            await run;
        }
        catch (OperationCanceledException)
        {
        }
    }

    private DateTimeOffset? CalculateInitialNextRun(PortableScheduledTask task, DateTimeOffset now)
    {
        if (!task.IsEnabled)
        {
            return null;
        }

        return task.Schedule.Kind switch
        {
            ScheduledTaskScheduleKind.ApplicationStart => now,
            ScheduledTaskScheduleKind.Interval => now.AddMinutes(task.Schedule.IntervalMinutes),
            ScheduledTaskScheduleKind.Daily => NextLocalOccurrence(task.Schedule, now, weekly: false),
            ScheduledTaskScheduleKind.Weekly => NextLocalOccurrence(task.Schedule, now, weekly: true),
            _ => null
        };
    }

    private DateTimeOffset? CalculateNextRunAfterCatalogChange(PortableScheduledTask task, DateTimeOffset now) =>
        task.Schedule.Kind == ScheduledTaskScheduleKind.ApplicationStart
            ? null
            : CalculateInitialNextRun(task, now);

    private DateTimeOffset? CalculateFollowingRun(PortableScheduledTask task, DateTimeOffset scheduled, DateTimeOffset now) =>
        task.Schedule.Kind switch
        {
            ScheduledTaskScheduleKind.ApplicationStart => null,
            ScheduledTaskScheduleKind.Interval => now.AddMinutes(task.Schedule.IntervalMinutes),
            ScheduledTaskScheduleKind.Daily => NextLocalOccurrence(task.Schedule, now, weekly: false),
            ScheduledTaskScheduleKind.Weekly => NextLocalOccurrence(task.Schedule, now, weekly: true),
            _ => scheduled
        };

    private static DateTimeOffset NextLocalOccurrence(ScheduledTaskSchedule schedule, DateTimeOffset now, bool weekly)
    {
        var timeZone = TimeZoneInfo.Local;
        var localNow = TimeZoneInfo.ConvertTime(now, timeZone);
        var candidate = new DateTime(localNow.Year, localNow.Month, localNow.Day, schedule.Hour, schedule.Minute, 0, DateTimeKind.Unspecified);
        if (weekly)
        {
            var dayOffset = ((int)schedule.DayOfWeek - (int)candidate.DayOfWeek + 7) % 7;
            candidate = candidate.AddDays(dayOffset);
        }

        if (candidate <= localNow.DateTime)
        {
            candidate = candidate.AddDays(weekly ? 7 : 1);
        }

        if (timeZone.IsInvalidTime(candidate))
        {
            candidate = candidate.AddHours(1);
        }

        return new DateTimeOffset(candidate, timeZone.GetUtcOffset(candidate)).ToUniversalTime();
    }

    private static string CombineOutput(ScheduledTaskExecutionResult result)
    {
        var output = string.Join(
            Environment.NewLine,
            new[] { result.StandardOutput.TrimEnd(), result.StandardError.TrimEnd() }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
        return string.IsNullOrWhiteSpace(output)
            ? result.IsSuccess ? $"Process completed with exit code {result.ExitCode}." : "The process failed without output."
            : output;
    }

    private static string SanitizeAndBoundOutput(string output)
    {
        output = SecretAssignmentRegex().Replace(output, "$1=[redacted]");
        output = BearerTokenRegex().Replace(output, "$1[redacted]");
        if (output.Length > MaximumStoredOutputCharacters)
        {
            output = output[..MaximumStoredOutputCharacters] + Environment.NewLine + "[output truncated]";
        }

        return output;
    }

    private async Task LogOutcomeAsync(ScheduledTaskRunRecord record)
    {
        try
        {
            await _logger.LogAsync(
                record.Outcome == ScheduledTaskOutcome.Succeeded ? ApplicationLogLevel.Information : ApplicationLogLevel.Error,
                $"scheduler.{record.TaskId}",
                $"task.{record.Outcome.ToString().ToLowerInvariant()}",
                $"Scheduled task finished with exit code {record.ExitCode?.ToString() ?? "none"}.");
        }
        catch
        {
        }
    }

    private void OnChanged() => Changed?.Invoke(this, EventArgs.Empty);

    [GeneratedRegex(@"(?im)\b(password|passwd|secret|token|api[_-]?key)\s*[:=]\s*[^\r\n\s]+")]
    private static partial Regex SecretAssignmentRegex();

    [GeneratedRegex(@"(?im)\b(authorization\s*:\s*bearer\s+)[^\r\n\s]+")]
    private static partial Regex BearerTokenRegex();
}
