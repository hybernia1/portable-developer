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

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }

        return ValueTask.CompletedTask;
    }
}
