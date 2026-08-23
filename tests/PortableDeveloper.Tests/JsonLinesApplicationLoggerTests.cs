using System.Text.Json;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Infrastructure.Logging;
using PortableDeveloper.Infrastructure.Paths;

namespace PortableDeveloper.Tests;

public sealed class JsonLinesApplicationLoggerTests : IAsyncDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task LogAsync_writes_structured_event_below_portable_root()
    {
        Directory.CreateDirectory(_testRoot);
        var paths = new PortablePathResolver(_testRoot);
        await using var logger = new JsonLinesApplicationLogger(paths);

        await logger.LogAsync(
            ApplicationLogLevel.Information,
            "apache",
            "process.started",
            "Managed process started with PID 1234.");

        var logPath = Path.Combine(_testRoot, "logs", $"portable-developer-{DateTime.UtcNow:yyyy-MM-dd}.jsonl");
        var line = await File.ReadAllTextAsync(logPath);
        using var document = JsonDocument.Parse(line);

        Assert.Equal("information", document.RootElement.GetProperty("level").GetString());
        Assert.Equal("apache", document.RootElement.GetProperty("component").GetString());
        Assert.Equal("process.started", document.RootElement.GetProperty("eventName").GetString());
    }

    [Fact]
    public void LogAsync_can_be_waited_synchronously_during_wpf_startup()
    {
        Directory.CreateDirectory(_testRoot);
        Exception? threadException = null;
        using var completed = new ManualResetEventSlim();
        var thread = new Thread(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());
            try
            {
                var paths = new PortablePathResolver(_testRoot);
                var logger = new JsonLinesApplicationLogger(paths);
                logger.LogAsync(
                        ApplicationLogLevel.Information,
                        "application",
                        "application.started",
                        "Portable Developer started.")
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();
                logger.DisposeAsync().AsTask().GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                threadException = exception;
            }
            finally
            {
                completed.Set();
            }
        })
        {
            IsBackground = true
        };

        thread.Start();

        Assert.True(completed.Wait(TimeSpan.FromSeconds(5)), "Logger deadlocked on a synchronization context.");
        Assert.Null(threadException);
    }

    [Fact]
    public async Task LogAsync_rotates_files_that_reach_the_size_limit()
    {
        Directory.CreateDirectory(_testRoot);
        var paths = new PortablePathResolver(_testRoot);
        await using var logger = new JsonLinesApplicationLogger(
            paths,
            maximumFileBytes: 180,
            maximumTotalBytes: 2048,
            retentionDays: 14);

        await logger.LogAsync(ApplicationLogLevel.Information, "test", "first", new string('a', 100));
        await logger.LogAsync(ApplicationLogLevel.Information, "test", "second", new string('b', 100));

        var logs = Directory.GetFiles(Path.Combine(_testRoot, "logs"), "portable-developer-*.jsonl");
        Assert.Equal(2, logs.Length);
        Assert.Contains(logs, path => Path.GetFileName(path).EndsWith("-001.jsonl", StringComparison.Ordinal));
    }

    [Fact]
    public async Task LogAsync_removes_expired_log_files()
    {
        var logDirectory = Path.Combine(_testRoot, "logs");
        Directory.CreateDirectory(logDirectory);
        var expiredPath = Path.Combine(logDirectory, "portable-developer-2020-01-01.jsonl");
        await File.WriteAllTextAsync(expiredPath, "expired");
        File.SetLastWriteTimeUtc(expiredPath, DateTime.UtcNow.AddDays(-30));
        var paths = new PortablePathResolver(_testRoot);
        await using var logger = new JsonLinesApplicationLogger(paths, retentionDays: 14);

        await logger.LogAsync(ApplicationLogLevel.Information, "test", "retention", "new entry");

        Assert.False(File.Exists(expiredPath));
    }

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }

        return ValueTask.CompletedTask;
    }
}
