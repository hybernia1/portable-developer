using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Application.Settings;
using PortableDeveloper.Application.Workspace;

namespace PortableDeveloper.Infrastructure.Workspace;

public sealed class PortableFileLauncher : IPortableFileLauncher
{
    private static readonly HashSet<string> ExecutableExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".exe", ".com", ".scr", ".msi", ".msp", ".bat", ".cmd", ".ps1", ".psm1",
        ".reg", ".lnk", ".url", ".hta", ".cpl", ".jar", ".vbs", ".vbe", ".js", ".jse", ".wsf"
    };

    private readonly IPortablePathResolver _paths;
    private readonly IPortableEditorService _editor;
    private readonly IApplicationLogger _logger;
    private readonly Func<ProcessStartInfo, Process?> _processStarter;

    public PortableFileLauncher(
        IPortablePathResolver paths,
        IPortableEditorService editor,
        IApplicationLogger logger,
        Func<ProcessStartInfo, Process?>? processStarter = null)
    {
        _paths = paths;
        _editor = editor;
        _logger = logger;
        _processStarter = processStarter ?? Process.Start;
    }

    public async Task<PortableFileLaunchResult> LaunchAsync(
        string relativeFilePath,
        string allowedRootRelativePath,
        PortableFileLaunchIntent intent,
        ApplicationLanguage language,
        string? initialContent = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var filePath = ResolveSafeFile(relativeFilePath, allowedRootRelativePath);
            PrepareFile(filePath, initialContent);
            var extension = Path.GetExtension(filePath);
            if (ExecutableExtensions.Contains(extension))
            {
                if (intent == PortableFileLaunchIntent.Edit && _editor.GetRuntime().IsReady)
                {
                    return await OpenPortableEditorAsync(language, relativeFilePath, initialContent, cancellationToken);
                }

                return new(false, "Executable and script file types are not opened through Windows associations.");
            }

            try
            {
                var process = _processStarter(new ProcessStartInfo(filePath)
                {
                    WorkingDirectory = Path.GetDirectoryName(filePath),
                    UseShellExecute = true
                });
                if (process is null)
                {
                    return new(false, "Windows did not start an associated application.");
                }

                process.Dispose();
                await LogSafelyAsync("file.system-opened", extension, cancellationToken);
                return new(true, "The file was opened with its Windows default application.");
            }
            catch (Win32Exception exception) when (exception.NativeErrorCode == 1155)
            {
                if (_editor.GetRuntime().IsReady)
                {
                    return await OpenPortableEditorAsync(language, relativeFilePath, initialContent, cancellationToken);
                }

                var process = _processStarter(new ProcessStartInfo(filePath)
                {
                    Verb = "openas",
                    WorkingDirectory = Path.GetDirectoryName(filePath),
                    UseShellExecute = true
                });
                process?.Dispose();
                return process is null
                    ? new(false, "No Windows application is associated with this file type.")
                    : new(true, "Windows application selection was opened.");
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or Win32Exception)
        {
            await LogSafelyAsync("file.open-failed", Path.GetExtension(relativeFilePath), cancellationToken);
            return new(false, exception.Message);
        }
    }

    private async Task<PortableFileLaunchResult> OpenPortableEditorAsync(
        ApplicationLanguage language,
        string relativeFilePath,
        string? initialContent,
        CancellationToken cancellationToken)
    {
        var result = await _editor.OpenAsync(language, relativeFilePath, initialContent, cancellationToken);
        return new(result.IsSuccess, result.Detail, result.IsSuccess);
    }

    private string ResolveSafeFile(string relativeFilePath, string allowedRootRelativePath)
    {
        if (Path.IsPathRooted(relativeFilePath) || Path.IsPathRooted(allowedRootRelativePath))
        {
            throw new ArgumentException("Only portable relative paths are allowed.");
        }

        var root = _paths.Resolve(allowedRootRelativePath);
        var file = _paths.Resolve(relativeFilePath);
        var rootPrefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                         + Path.DirectorySeparatorChar;
        if (!file.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The file is outside the allowed portable directory.", nameof(relativeFilePath));
        }

        var current = root;
        if (Directory.Exists(current) && IsReparsePoint(current))
        {
            throw new IOException("The allowed file root cannot be a link or reparse point.");
        }

        foreach (var segment in Path.GetRelativePath(root, file)
                     .Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);
            if ((File.Exists(current) || Directory.Exists(current)) && IsReparsePoint(current))
            {
                throw new IOException("Files reached through links or reparse points cannot be opened.");
            }
        }

        return file;
    }

    private static void PrepareFile(string filePath, string? initialContent)
    {
        if (Directory.Exists(filePath))
        {
            throw new IOException("The requested file target is a directory.");
        }

        var directory = Path.GetDirectoryName(filePath)
            ?? throw new IOException("The requested file has no parent directory.");
        Directory.CreateDirectory(directory);
        if (!File.Exists(filePath))
        {
            File.WriteAllText(filePath, initialContent ?? string.Empty, new UTF8Encoding(false));
        }
    }

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;

    private async Task LogSafelyAsync(string eventName, string extension, CancellationToken cancellationToken)
    {
        try
        {
            await _logger.LogAsync(
                eventName.EndsWith("failed", StringComparison.Ordinal) ? ApplicationLogLevel.Warning : ApplicationLogLevel.Information,
                "file-launcher",
                eventName,
                $"extension={extension}",
                cancellationToken);
        }
        catch
        {
            // Opening a user-facing file must not depend on diagnostic logging.
        }
    }
}
