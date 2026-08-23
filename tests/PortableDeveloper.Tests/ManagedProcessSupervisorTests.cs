using System.Diagnostics;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Domain.Processes;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Processes;

namespace PortableDeveloper.Tests;

public sealed class ManagedProcessSupervisorTests : IAsyncDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task StartAsync_attaches_a_child_process_and_dispose_stops_its_tree()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var executableDirectory = Path.Combine(_testRoot, "tools", "test-shell");
        Directory.CreateDirectory(executableDirectory);
        var sourceExecutable = Environment.GetEnvironmentVariable("ComSpec")
            ?? Path.Combine(Environment.SystemDirectory, "cmd.exe");
        var executablePath = Path.Combine(executableDirectory, "cmd.exe");
        File.Copy(sourceExecutable, executablePath);
        var paths = new PortablePathResolver(_testRoot);
        var supervisor = new ManagedProcessSupervisor(paths, new SilentLogger());

        var snapshot = await supervisor.StartAsync(new ManagedProcessDefinition(
            "test-process",
            Path.Combine("tools", "test-shell", "cmd.exe"),
            Path.Combine("temp", "test-process"),
            "/d /c ping 127.0.0.1 -t"));

        Assert.Equal(ManagedProcessState.Running, snapshot.State);
        Assert.NotNull(snapshot.ProcessId);
        var processId = snapshot.ProcessId.Value;

        await supervisor.DisposeAsync();

        Assert.True(await HasExitedAsync(processId));
    }

    public ValueTask DisposeAsync()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }

        return ValueTask.CompletedTask;
    }

    private static async Task<bool> HasExitedAsync(int processId)
    {
        try
        {
            using var process = Process.GetProcessById(processId);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await process.WaitForExitAsync(timeout.Token);
            return process.HasExited;
        }
        catch (ArgumentException)
        {
            return true;
        }
    }

    private sealed class SilentLogger : IApplicationLogger
    {
        public ValueTask LogAsync(
            ApplicationLogLevel level,
            string component,
            string eventName,
            string message,
            CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
