using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Application.Settings;

namespace PortableDeveloper.Infrastructure.ProjectTools;

public sealed class PortableEditorService : IPortableEditorService
{
    private readonly IPortableToolRuntimeInventory _toolInventory;
    private readonly IPortablePathResolver _paths;
    private readonly IApplicationLogger _logger;
    private readonly Func<ProcessStartInfo, Process?> _processStarter;

    public PortableEditorService(
        IPortableToolRuntimeInventory toolInventory,
        IPortablePathResolver paths,
        IApplicationLogger logger,
        Func<ProcessStartInfo, Process?>? processStarter = null)
    {
        _toolInventory = toolInventory;
        _paths = paths;
        _logger = logger;
        _processStarter = processStarter ?? Process.Start;
    }

    public PortableToolRuntimeInfo GetRuntime() => _toolInventory.GetRuntime(PortableToolKind.Editor);

    public async Task<PortableEditorLaunchResult> OpenAsync(
        ApplicationLanguage language,
        string? relativeFilePath = null,
        string? initialContent = null,
        CancellationToken cancellationToken = default)
    {
        var runtime = GetRuntime();
        if (!runtime.IsReady)
        {
            return new(false, runtime.Detail);
        }

        try
        {
            var executablePath = _paths.Resolve(runtime.EntrypointRelativePath);
            var workingDirectory = Path.GetDirectoryName(executablePath)
                ?? throw new InvalidOperationException("The portable editor directory is invalid.");
            var startInfo = new ProcessStartInfo(executablePath)
            {
                WorkingDirectory = workingDirectory,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add("-multiInst");
            startInfo.ArgumentList.Add("-nosession");
            if (language == ApplicationLanguage.Czech)
            {
                startInfo.ArgumentList.Add("-Lcs");
            }

            startInfo.Environment["TEMP"] = _paths.EnsureDirectory("temp");
            startInfo.Environment["TMP"] = _paths.EnsureDirectory("temp");

            if (!string.IsNullOrWhiteSpace(relativeFilePath))
            {
                var filePath = PrepareFile(relativeFilePath, initialContent);
                startInfo.ArgumentList.Add(filePath);
            }

            using var process = _processStarter(startInfo);
            if (process is null)
            {
                return new(false, "The portable editor process did not start.");
            }

            await LogSafelyAsync(
                ApplicationLogLevel.Information,
                "editor.started",
                $"Portable editor started with PID {process.Id}.",
                cancellationToken);
            return new(true, "Portable editor started.");
        }
        catch (Exception exception) when (exception is Win32Exception or IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            await LogSafelyAsync(
                ApplicationLogLevel.Error,
                "editor.start.failed",
                exception.Message,
                cancellationToken);
            return new(false, exception.Message);
        }
    }

    private string PrepareFile(string relativeFilePath, string? initialContent)
    {
        var filePath = _paths.Resolve(relativeFilePath);
        if (Directory.Exists(filePath))
        {
            throw new IOException("The requested editor target is a directory.");
        }

        var relativeDirectory = Path.GetDirectoryName(relativeFilePath);
        if (!string.IsNullOrWhiteSpace(relativeDirectory))
        {
            _paths.EnsureDirectory(relativeDirectory);
        }

        if (File.Exists(filePath))
        {
            if ((File.GetAttributes(filePath) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            {
                throw new IOException("The requested editor file is a reparse point.");
            }
        }
        else
        {
            File.WriteAllText(filePath, initialContent ?? string.Empty, new UTF8Encoding(false));
        }

        return filePath;
    }

    private async Task LogSafelyAsync(
        ApplicationLogLevel level,
        string eventName,
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            await _logger.LogAsync(level, "editor", eventName, message, cancellationToken);
        }
        catch
        {
            // Launching a user-facing editor must not depend on diagnostic logging.
        }
    }
}
