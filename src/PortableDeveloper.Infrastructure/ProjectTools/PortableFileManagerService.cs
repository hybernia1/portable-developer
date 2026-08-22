using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Linq;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Application.Settings;

namespace PortableDeveloper.Infrastructure.ProjectTools;

public sealed class PortableFileManagerService : IPortableFileManagerService
{
    private const string WorkspaceRelativePath = "instances/default/www";
    private const string ConfigurationRelativePath = "state/doublecmd";
    private readonly IPortableToolRuntimeInventory _toolInventory;
    private readonly IPortablePathResolver _paths;
    private readonly IApplicationLogger _logger;
    private readonly Func<ProcessStartInfo, Process?> _processStarter;

    public PortableFileManagerService(
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

    public PortableToolRuntimeInfo GetRuntime() =>
        _toolInventory.GetRuntime(PortableToolKind.FileManager);

    public async Task<PortableFileManagerLaunchResult> OpenAsync(
        ApplicationLanguage language,
        CancellationToken cancellationToken = default)
    {
        var runtime = GetRuntime();
        if (!runtime.IsReady)
        {
            return new(false, runtime.Detail);
        }

        var editor = _toolInventory.GetRuntime(PortableToolKind.Editor);
        if (!editor.IsReady)
        {
            return new(false, editor.Detail);
        }

        try
        {
            var executablePath = _paths.Resolve(runtime.EntrypointRelativePath);
            var editorPath = _paths.Resolve(editor.EntrypointRelativePath);
            var workspacePath = _paths.EnsureDirectory(WorkspaceRelativePath);
            var configurationPath = _paths.EnsureDirectory(ConfigurationRelativePath);
            PrepareConfiguration(configurationPath, language);

            var startInfo = new ProcessStartInfo(executablePath)
            {
                WorkingDirectory = Path.GetDirectoryName(executablePath)
                    ?? throw new InvalidOperationException("The portable file manager directory is invalid."),
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add($"--config-dir={configurationPath}");
            startInfo.ArgumentList.Add("--no-splash");
            startInfo.ArgumentList.Add("-L");
            startInfo.ArgumentList.Add(workspacePath);
            startInfo.ArgumentList.Add("-R");
            startInfo.ArgumentList.Add(workspacePath);
            startInfo.Environment["TEMP"] = _paths.EnsureDirectory("temp");
            startInfo.Environment["TMP"] = _paths.EnsureDirectory("temp");
            startInfo.Environment["PORTABLE_DEVELOPER_EDITOR"] = editorPath;

            using var process = _processStarter(startInfo);
            if (process is null)
            {
                return new(false, "The portable file manager process did not start.");
            }

            await LogSafelyAsync(
                ApplicationLogLevel.Information,
                "file-manager.started",
                $"Portable file manager started with PID {process.Id}.",
                cancellationToken);
            return new(true, "Portable file manager started.");
        }
        catch (Exception exception) when (exception is Win32Exception or IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException or System.Xml.XmlException)
        {
            await LogSafelyAsync(
                ApplicationLogLevel.Error,
                "file-manager.start.failed",
                exception.Message,
                cancellationToken);
            return new(false, exception.Message);
        }
    }

    private static void PrepareConfiguration(
        string configurationPath,
        ApplicationLanguage language)
    {
        var configurationFile = Path.Combine(configurationPath, "doublecmd.xml");
        XDocument document;
        if (File.Exists(configurationFile))
        {
            document = XDocument.Load(configurationFile, LoadOptions.PreserveWhitespace);
        }
        else
        {
            document = new XDocument(new XElement("doublecmd",
                new XAttribute("DCVersion", "1.2.8 gamma"),
                new XAttribute("ConfigVersion", "16")));
        }

        var root = document.Root
            ?? throw new InvalidDataException("The Double Commander configuration has no root element.");
        if (!string.Equals(root.Name.LocalName, "doublecmd", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The Double Commander configuration root is invalid.");
        }

        var tools = GetOrAddElement(root, "Tools");
        var editor = GetOrAddElement(tools, "Editor");
        editor.SetAttributeValue("Enabled", "True");
        SetElementValue(editor, "Path", "%PORTABLE_DEVELOPER_EDITOR%");
        SetElementValue(editor, "Parameters", "-multiInst -nosession");
        SetElementValue(editor, "RunInTerminal", "False");
        SetElementValue(editor, "KeepTerminalOpen", "False");

        var languageElement = GetOrAddElement(root, "Language");
        SetElementValue(
            languageElement,
            "POFileName",
            language == ApplicationLanguage.Czech ? "doublecmd.cs.po" : string.Empty);

        document.Save(configurationFile);
        File.WriteAllText(Path.Combine(configurationPath, "doublecmd.inf"), "SplashForm=False");
    }

    private static XElement GetOrAddElement(XElement parent, string name)
    {
        var element = parent.Element(name);
        if (element is not null)
        {
            return element;
        }

        element = new XElement(name);
        parent.Add(element);
        return element;
    }

    private static void SetElementValue(XElement parent, string name, string value) =>
        GetOrAddElement(parent, name).Value = value;

    private async Task LogSafelyAsync(
        ApplicationLogLevel level,
        string eventName,
        string message,
        CancellationToken cancellationToken)
    {
        try
        {
            await _logger.LogAsync(level, "file-manager", eventName, message, cancellationToken);
        }
        catch
        {
            // Launching a user-facing tool must not depend on diagnostic logging.
        }
    }
}
