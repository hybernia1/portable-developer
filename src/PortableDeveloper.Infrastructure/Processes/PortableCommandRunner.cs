using System.Diagnostics;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Infrastructure.Processes;

/// <summary>
/// Runs a bounded one-shot tool with redirected output and portable environment directories.
/// </summary>
public sealed class PortableCommandRunner : IPortableCommandRunner
{
    private readonly IPortablePathResolver _paths;
    private readonly IApplicationLogger _logger;

    public PortableCommandRunner(IPortablePathResolver paths, IApplicationLogger logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public async Task<PortableCommandResult> RunAsync(
        PortableCommandDefinition definition,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentException.ThrowIfNullOrWhiteSpace(definition.Id);

        var executablePath = _paths.Resolve(definition.ExecutableRelativePath);
        if (!File.Exists(executablePath))
        {
            return new(null, string.Empty, $"Executable was not found: {definition.ExecutableRelativePath}");
        }

        var workingDirectory = _paths.Resolve(definition.WorkingDirectoryRelativePath);
        if (!Directory.Exists(workingDirectory))
        {
            return new(null, string.Empty, $"Working directory was not found: {definition.WorkingDirectoryRelativePath}");
        }

        var startInfo = new ProcessStartInfo(executablePath)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        foreach (var argument in definition.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        ApplyPortableEnvironment(startInfo, definition.Environment);
        using var process = new Process { StartInfo = startInfo };
        await LogSafelyAsync(ApplicationLogLevel.Information, definition.Id, "command.start.requested", "Starting portable command.");

        try
        {
            if (!process.Start())
            {
                return new(null, string.Empty, "Command process did not start.");
            }

            var standardOutput = process.StandardOutput.ReadToEndAsync();
            var standardError = process.StandardError.ReadToEndAsync();
            using var timeoutSource = definition.Timeout is { } timeout
                ? new CancellationTokenSource(timeout)
                : new CancellationTokenSource();
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);

            try
            {
                await process.WaitForExitAsync(linkedSource.Token);
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested && timeoutSource.IsCancellationRequested)
            {
                KillProcessTree(process);
                await process.WaitForExitAsync(CancellationToken.None);
                var timedOutResult = new PortableCommandResult(
                    process.ExitCode,
                    await standardOutput,
                    await standardError,
                    TimedOut: true);
                await LogSafelyAsync(ApplicationLogLevel.Error, definition.Id, "command.timed_out", "Portable command exceeded its time limit.");
                return timedOutResult;
            }
            catch (OperationCanceledException)
            {
                KillProcessTree(process);
                await process.WaitForExitAsync(CancellationToken.None);
                throw;
            }

            var result = new PortableCommandResult(process.ExitCode, await standardOutput, await standardError);
            await LogSafelyAsync(
                result.IsSuccess ? ApplicationLogLevel.Information : ApplicationLogLevel.Error,
                definition.Id,
                result.IsSuccess ? "command.completed" : "command.failed",
                $"Portable command exited with code {result.ExitCode}.");
            return result;
        }
        catch (System.ComponentModel.Win32Exception exception)
        {
            await LogSafelyAsync(ApplicationLogLevel.Error, definition.Id, "command.start.failed", exception.Message);
            return new(null, string.Empty, exception.Message);
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

        startInfo.Environment["TEMP"] = _paths.EnsureDirectory("temp");
        startInfo.Environment["TMP"] = _paths.EnsureDirectory("temp");
        startInfo.Environment["HOME"] = _paths.EnsureDirectory("state/home");
    }

    private static void KillProcessTree(Process process)
    {
        if (!process.HasExited)
        {
            process.Kill(entireProcessTree: true);
        }
    }

    private async Task LogSafelyAsync(ApplicationLogLevel level, string component, string eventName, string message)
    {
        try
        {
            await _logger.LogAsync(level, component, eventName, message);
        }
        catch
        {
            // Command ownership and cleanup must not depend on diagnostic logging.
        }
    }
}
