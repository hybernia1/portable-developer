using System.Collections.Concurrent;
using System.Diagnostics;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Infrastructure.Processes;

/// <summary>
/// Starts and stops child processes without relying on Windows services.
/// </summary>
public sealed class ManagedProcessSupervisor : IManagedProcessSupervisor
{
    private readonly ConcurrentDictionary<string, Process> _processes = new(StringComparer.OrdinalIgnoreCase);
    private readonly IApplicationLogger _logger;
    private readonly IPortablePathResolver _paths;

    public ManagedProcessSupervisor(IPortablePathResolver paths, IApplicationLogger logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public IReadOnlyCollection<ManagedProcessSnapshot> GetSnapshots() =>
        _processes
            .Select(pair => new ManagedProcessSnapshot(
                pair.Key,
                pair.Value.HasExited ? ManagedProcessState.Stopped : ManagedProcessState.Running,
                pair.Value.HasExited ? null : pair.Value.Id))
            .ToArray();

    public async Task<ManagedProcessSnapshot> StartAsync(
        ManagedProcessDefinition definition,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.Id);
        cancellationToken.ThrowIfCancellationRequested();

        if (_processes.ContainsKey(definition.Id))
        {
            await LogAsync(ApplicationLogLevel.Warning, definition.Id, "process.start.rejected", "The process is already managed.");
            return new ManagedProcessSnapshot(definition.Id, ManagedProcessState.Failed, Detail: "The process is already managed.");
        }

        var executablePath = _paths.Resolve(definition.ExecutableRelativePath);
        if (!File.Exists(executablePath))
        {
            var detail = $"Executable was not found: {definition.ExecutableRelativePath}";
            await LogAsync(ApplicationLogLevel.Error, definition.Id, "process.start.failed", detail);
            return new ManagedProcessSnapshot(definition.Id, ManagedProcessState.Failed, Detail: detail);
        }

        var workingDirectory = _paths.EnsureDirectory(definition.WorkingDirectoryRelativePath);
        var startInfo = new ProcessStartInfo(executablePath, definition.Arguments ?? string.Empty)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        ApplyPortableEnvironment(startInfo, definition.Environment);

        var process = new Process
        {
            StartInfo = startInfo,
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (_, eventArgs) => ForwardProcessOutput(definition.Id, "stdout", eventArgs.Data);
        process.ErrorDataReceived += (_, eventArgs) => ForwardProcessOutput(definition.Id, "stderr", eventArgs.Data);
        process.Exited += (_, _) => RemoveExitedProcess(definition.Id, process);

        await LogAsync(ApplicationLogLevel.Information, definition.Id, "process.start.requested", "Starting managed child process.");

        try
        {
            if (!process.Start())
            {
                process.Dispose();
                const string detail = "Process did not start.";
                await LogAsync(ApplicationLogLevel.Error, definition.Id, "process.start.failed", detail);
                return new ManagedProcessSnapshot(definition.Id, ManagedProcessState.Failed, Detail: detail);
            }

            if (!_processes.TryAdd(definition.Id, process))
            {
                process.Kill(entireProcessTree: true);
                process.Dispose();
                const string detail = "A process with this identifier already exists.";
                await LogAsync(ApplicationLogLevel.Error, definition.Id, "process.start.failed", detail);
                return new ManagedProcessSnapshot(definition.Id, ManagedProcessState.Failed, Detail: detail);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await LogAsync(ApplicationLogLevel.Information, definition.Id, "process.started", $"Managed process started with PID {process.Id}.");
            return new ManagedProcessSnapshot(definition.Id, ManagedProcessState.Running, process.Id);
        }
        catch (Exception exception) when (exception is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            process.Dispose();
            await LogAsync(ApplicationLogLevel.Error, definition.Id, "process.start.failed", exception.Message);
            return new ManagedProcessSnapshot(definition.Id, ManagedProcessState.Failed, Detail: exception.Message);
        }
    }

    public async Task StopAsync(string processId, CancellationToken cancellationToken = default)
    {
        if (!_processes.TryRemove(processId, out var process))
        {
            return;
        }

        await LogAsync(ApplicationLogLevel.Information, processId, "process.stop.requested", "Stopping managed child process.");

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync(cancellationToken);
            }

            await LogAsync(ApplicationLogLevel.Information, processId, "process.stopped", "Managed child process stopped.");
        }
        finally
        {
            process.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        var processIds = _processes.Keys.ToArray();
        foreach (var processId in processIds)
        {
            await StopAsync(processId);
        }
    }

    private void ApplyPortableEnvironment(ProcessStartInfo startInfo, IReadOnlyDictionary<string, string>? environment)
    {
        if (environment is not null)
        {
            foreach (var (key, value) in environment)
            {
                startInfo.Environment[key] = value;
            }
        }

        var temporaryDirectory = _paths.EnsureDirectory("temp");
        var homeDirectory = _paths.EnsureDirectory("state/home");

        startInfo.Environment["TEMP"] = temporaryDirectory;
        startInfo.Environment["TMP"] = temporaryDirectory;
        startInfo.Environment["HOME"] = homeDirectory;
    }

    private void ForwardProcessOutput(string processId, string stream, string? line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return;
        }

        _ = LogAsync(ApplicationLogLevel.Debug, processId, $"process.output.{stream}", line);
    }

    private void RemoveExitedProcess(string processId, Process process)
    {
        var processCollection = (ICollection<KeyValuePair<string, Process>>)_processes;
        if (processCollection.Remove(new KeyValuePair<string, Process>(processId, process)))
        {
            process.Dispose();
            _ = LogAsync(ApplicationLogLevel.Warning, processId, "process.exited", "Managed child process exited unexpectedly.");
        }
    }

    private async Task LogAsync(ApplicationLogLevel level, string component, string eventName, string message)
    {
        try
        {
            await _logger.LogAsync(level, component, eventName, message);
        }
        catch
        {
            // A log destination failure must not make a development server unavailable.
        }
    }
}
