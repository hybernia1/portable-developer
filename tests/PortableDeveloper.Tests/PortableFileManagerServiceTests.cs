using System.Diagnostics;
using System.Xml.Linq;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Application.Settings;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.ProjectTools;

namespace PortableDeveloper.Tests;

public sealed class PortableFileManagerServiceTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task OpenAsync_uses_portable_configuration_workspace_and_bundled_editor()
    {
        var managerEntrypoint = Path.Combine("modules", "filemanager", "1.2.8", "doublecmd.exe");
        var editorEntrypoint = Path.Combine("modules", "editor", "8.9.2", "notepad++.exe");
        Directory.CreateDirectory(Path.Combine(_testRoot, "modules", "filemanager", "1.2.8"));
        Directory.CreateDirectory(Path.Combine(_testRoot, "modules", "editor", "8.9.2"));
        File.WriteAllText(Path.Combine(_testRoot, managerEntrypoint), "manager");
        File.WriteAllText(Path.Combine(_testRoot, editorEntrypoint), "editor");
        ProcessStartInfo? captured = null;
        var service = new PortableFileManagerService(
            new ReadyTools(managerEntrypoint, editorEntrypoint),
            new PortablePathResolver(_testRoot),
            new SilentLogger(),
            startInfo =>
            {
                captured = startInfo;
                return Process.GetCurrentProcess();
            });

        var result = await service.OpenAsync(ApplicationLanguage.Czech);

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.False(captured.UseShellExecute);
        var workspace = Path.Combine(_testRoot, "instances", "default", "www");
        var configDirectory = Path.Combine(_testRoot, "state", "doublecmd");
        Assert.Equal(
            [$"--config-dir={configDirectory}", "--no-splash", "-L", workspace, "-R", workspace],
            captured.ArgumentList);
        Assert.Equal(Path.Combine(_testRoot, "temp"), captured.Environment["TEMP"]);
        Assert.Equal(Path.Combine(_testRoot, editorEntrypoint), captured.Environment["PORTABLE_DEVELOPER_EDITOR"]);

        var config = XDocument.Load(Path.Combine(configDirectory, "doublecmd.xml"));
        var editor = config.Root?.Element("Tools")?.Element("Editor");
        Assert.Equal("True", editor?.Attribute("Enabled")?.Value);
        Assert.Equal("%PORTABLE_DEVELOPER_EDITOR%", editor?.Element("Path")?.Value);
        Assert.Equal("-multiInst -nosession", editor?.Element("Parameters")?.Value);
        Assert.Equal("doublecmd.cs.po", config.Root?.Element("Language")?.Element("POFileName")?.Value);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private sealed class ReadyTools(string managerEntrypoint, string editorEntrypoint) : IPortableToolRuntimeInventory
    {
        public PortableToolRuntimeInfo GetRuntime(PortableToolKind kind) => kind switch
        {
            PortableToolKind.FileManager => new(kind, true, "1.2.8", managerEntrypoint, "Verified portable file manager."),
            PortableToolKind.Editor => new(kind, true, "8.9.2", editorEntrypoint, "Verified portable editor."),
            _ => new(kind, false, string.Empty, string.Empty, "Not available.")
        };
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
