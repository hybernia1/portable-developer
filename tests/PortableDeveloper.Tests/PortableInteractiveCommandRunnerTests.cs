using System.Collections.Concurrent;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Domain.Processes;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Processes;

namespace PortableDeveloper.Tests;

public sealed class PortableInteractiveCommandRunnerTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperInteractive-{Guid.NewGuid():N}");

    [Fact]
    public async Task Streams_utf8_prompt_accepts_input_and_preserves_czech_output()
    {
        Directory.CreateDirectory(_testRoot);
        Directory.CreateDirectory(Path.Combine(_testRoot, "project"));
        var fixtureRelativePath = CopyFixtureToPortableRoot();
        var output = new RecordingProgress();
        var runner = new PortableInteractiveCommandRunner(
            new PortablePathResolver(_testRoot),
            new NullLogger());

        await using var session = await runner.StartAsync(
            new PortableCommandDefinition(
                "test.interactive",
                fixtureRelativePath,
                "project",
                [],
                Timeout: TimeSpan.FromSeconds(20)),
            output);

        await output.PromptSeen.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.True(session.IsRunning);
        await session.WriteLineAsync("žluťoučký kůň");
        var result = await session.Completion.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.True(result.IsSuccess);
        Assert.Contains("CZ: ", output.Text, StringComparison.Ordinal);
        Assert.Contains("Překlad: Dobrý den — žluťoučký kůň", output.Text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Stop_terminates_the_owned_process()
    {
        Directory.CreateDirectory(_testRoot);
        Directory.CreateDirectory(Path.Combine(_testRoot, "project"));
        var fixtureRelativePath = CopyFixtureToPortableRoot();
        var output = new RecordingProgress("READY");
        var runner = new PortableInteractiveCommandRunner(
            new PortablePathResolver(_testRoot),
            new NullLogger());

        await using var session = await runner.StartAsync(
            new PortableCommandDefinition(
                "test.stop",
                fixtureRelativePath,
                "project",
                ["--wait"],
                Timeout: TimeSpan.FromSeconds(20)),
            output);

        await output.PromptSeen.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await session.StopAsync().WaitAsync(TimeSpan.FromSeconds(10));
        var result = await session.Completion;

        Assert.False(session.IsRunning);
        Assert.True(result.WasStopped);
        Assert.False(result.TimedOut);
    }

    [Fact]
    public async Task One_shot_runner_uses_utf8_without_a_bom_for_standard_input()
    {
        Directory.CreateDirectory(_testRoot);
        Directory.CreateDirectory(Path.Combine(_testRoot, "project"));
        var fixtureRelativePath = CopyFixtureToPortableRoot();
        var runner = new PortableCommandRunner(
            new PortablePathResolver(_testRoot),
            new NullLogger());

        var result = await runner.RunAsync(new PortableCommandDefinition(
            "test.oneshot",
            fixtureRelativePath,
            "project",
            [],
            StandardInput: "žluťoučký kůň" + Environment.NewLine,
            Timeout: TimeSpan.FromSeconds(20)));

        Assert.True(result.IsSuccess);
        Assert.Contains("Překlad: Dobrý den — žluťoučký kůň", result.StandardOutput, StringComparison.Ordinal);
        Assert.DoesNotContain('\uFEFF', result.StandardOutput);
    }

    private string CopyFixtureToPortableRoot()
    {
        var repositoryRoot = FindRepositoryRoot();
        var configuration = new DirectoryInfo(AppContext.BaseDirectory)
            .Parent?.Name ?? "Debug";
        var source = Path.Combine(
            repositoryRoot,
            "tests",
            "PortableDeveloper.ProcessFixture",
            "bin",
            configuration,
            "net10.0");
        var destination = Path.Combine(_testRoot, "tools", "fixture");
        Directory.CreateDirectory(destination);
        foreach (var file in Directory.EnumerateFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
        }

        return Path.Combine("tools", "fixture", "PortableDeveloper.ProcessFixture.exe");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "PortableDeveloper.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("PortableDeveloper.slnx was not found above the test output directory.");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private sealed class RecordingProgress(string expectedPrompt = "CZ: ") : IProgress<PortableProcessOutput>
    {
        private readonly ConcurrentQueue<string> _chunks = new();

        public TaskCompletionSource PromptSeen { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public string Text => string.Concat(_chunks);

        public void Report(PortableProcessOutput value)
        {
            _chunks.Enqueue(value.Text);
            if (Text.Contains(expectedPrompt, StringComparison.Ordinal))
            {
                PromptSeen.TrySetResult();
            }
        }
    }

    private sealed class NullLogger : IApplicationLogger
    {
        public ValueTask LogAsync(
            ApplicationLogLevel level,
            string component,
            string eventName,
            string message,
            CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
