using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Scheduling;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Scheduling;

namespace PortableDeveloper.Tests;

public sealed class PortableTaskSchedulerTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task Application_start_task_runs_once_and_records_sanitized_history()
    {
        var paths = new PortablePathResolver(_testRoot);
        var catalog = new JsonScheduledTaskCatalog(paths);
        catalog.Add(CreateTask() with
        {
            Schedule = new ScheduledTaskSchedule(ScheduledTaskScheduleKind.ApplicationStart)
        });
        var executor = new RecordingExecutor(new(0, "token=secret-value\nok", string.Empty));
        await using var scheduler = new PortableTaskScheduler(
            catalog,
            new JsonScheduledTaskHistoryStore(paths),
            executor,
            new NullLogger());

        scheduler.Start();
        await executor.Executed.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await WaitForHistoryAsync(scheduler);

        var record = Assert.Single(scheduler.GetHistory("default"));
        Assert.Equal(ScheduledTaskTrigger.ApplicationStart, record.Trigger);
        Assert.Equal(ScheduledTaskOutcome.Succeeded, record.Outcome);
        Assert.Contains("token=[redacted]", record.Output);
        Assert.DoesNotContain("secret-value", record.Output);
        Assert.Null(Assert.Single(scheduler.GetTasks("default")).NextRunUtc);
    }

    [Fact]
    public async Task Manual_run_does_not_require_enabled_task_and_rejects_overlap()
    {
        var paths = new PortablePathResolver(_testRoot);
        var catalog = new JsonScheduledTaskCatalog(paths);
        catalog.Add(CreateTask() with { IsEnabled = false });
        var executor = new BlockingExecutor();
        await using var scheduler = new PortableTaskScheduler(
            catalog,
            new JsonScheduledTaskHistoryStore(paths),
            executor,
            new NullLogger());

        var running = scheduler.RunNowAsync("job");
        await executor.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));
        void StartOverlappingRun() => _ = scheduler.RunNowAsync("job");
        Assert.Throws<InvalidOperationException>(StartOverlappingRun);
        executor.Release.TrySetResult();

        var record = await running;
        Assert.Equal(ScheduledTaskTrigger.Manual, record.Trigger);
        Assert.Equal(ScheduledTaskOutcome.Succeeded, record.Outcome);
    }

    [Fact]
    public async Task Newly_added_application_start_task_waits_for_the_next_application_start()
    {
        var paths = new PortablePathResolver(_testRoot);
        var executor = new RecordingExecutor(new(0, "ok", string.Empty));
        await using var scheduler = new PortableTaskScheduler(
            new JsonScheduledTaskCatalog(paths),
            new JsonScheduledTaskHistoryStore(paths),
            executor,
            new NullLogger());
        scheduler.Start();

        scheduler.Add(CreateTask() with
        {
            Schedule = new ScheduledTaskSchedule(ScheduledTaskScheduleKind.ApplicationStart)
        });
        await Task.Delay(TimeSpan.FromMilliseconds(1_200));

        Assert.False(executor.Executed.Task.IsCompleted);
        Assert.Null(Assert.Single(scheduler.GetTasks("default")).NextRunUtc);
    }

    [Fact]
    public async Task Stop_cancels_a_manual_task_owned_by_the_running_scheduler()
    {
        var paths = new PortablePathResolver(_testRoot);
        var catalog = new JsonScheduledTaskCatalog(paths);
        catalog.Add(CreateTask());
        var executor = new BlockingExecutor();
        await using var scheduler = new PortableTaskScheduler(
            catalog,
            new JsonScheduledTaskHistoryStore(paths),
            executor,
            new NullLogger());
        scheduler.Start();
        var running = scheduler.RunNowAsync("job");
        await executor.Started.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await scheduler.StopAsync();

        var record = await running;
        Assert.Equal(ScheduledTaskOutcome.Canceled, record.Outcome);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private static async Task WaitForHistoryAsync(IPortableTaskScheduler scheduler)
    {
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (scheduler.GetHistory("default").Count == 0 && DateTime.UtcNow < deadline)
        {
            await Task.Delay(20);
        }
    }

    private static PortableScheduledTask CreateTask() => new(
        "job",
        "default",
        "Job",
        ScheduledTaskCommandKind.PythonScript,
        "job.py",
        string.Empty,
        new ScheduledTaskSchedule(ScheduledTaskScheduleKind.Interval, IntervalMinutes: 10));

    private sealed class RecordingExecutor(ScheduledTaskExecutionResult result) : IScheduledTaskExecutor
    {
        public TaskCompletionSource Executed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<ScheduledTaskExecutionResult> ExecuteAsync(PortableScheduledTask task, CancellationToken cancellationToken = default)
        {
            Executed.TrySetResult();
            return Task.FromResult(result);
        }
    }

    private sealed class BlockingExecutor : IScheduledTaskExecutor
    {
        public TaskCompletionSource Started { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public async Task<ScheduledTaskExecutionResult> ExecuteAsync(PortableScheduledTask task, CancellationToken cancellationToken = default)
        {
            Started.TrySetResult();
            await Release.Task.WaitAsync(cancellationToken);
            return new(0, "ok", string.Empty);
        }
    }

    private sealed class NullLogger : IApplicationLogger
    {
        public ValueTask LogAsync(ApplicationLogLevel level, string component, string eventName, string message, CancellationToken cancellationToken = default) =>
            ValueTask.CompletedTask;
    }
}
