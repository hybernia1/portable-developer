using System.ComponentModel;
using System.Diagnostics;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Application.Settings;
using PortableDeveloper.Application.Workspace;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Workspace;

namespace PortableDeveloper.Tests;

public sealed class PortableFileLauncherTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task Safe_file_uses_windows_association_inside_allowed_root()
    {
        ProcessStartInfo? captured = null;
        var service = CreateLauncher(
            new UnavailableEditor(),
            startInfo =>
            {
                captured = startInfo;
                return Process.GetCurrentProcess();
            });

        var result = await service.LaunchAsync(
            "instances/default/www/image.png",
            "instances/default/www",
            PortableFileLaunchIntent.Open,
            ApplicationLanguage.English,
            "image");

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.True(captured.UseShellExecute);
        Assert.Equal(Path.Combine(_testRoot, "instances", "default", "www", "image.png"), captured.FileName);
    }

    [Fact]
    public async Task Missing_association_falls_back_to_verified_portable_editor()
    {
        var editor = new CapturingEditor();
        var service = CreateLauncher(editor, _ => throw new Win32Exception(1155));

        var result = await service.LaunchAsync(
            "instances/default/config/php-custom.ini",
            "instances/default/config",
            PortableFileLaunchIntent.Edit,
            ApplicationLanguage.Czech,
            "; custom");

        Assert.True(result.IsSuccess);
        Assert.True(result.UsedPortableEditor);
        Assert.Equal("instances/default/config/php-custom.ini", editor.RelativePath);
    }

    [Fact]
    public async Task Executable_types_and_paths_outside_allowed_root_are_refused()
    {
        var service = CreateLauncher(new UnavailableEditor(), _ => throw new InvalidOperationException("Must not start."));

        var executable = await service.LaunchAsync(
            "instances/default/www/run.cmd",
            "instances/default/www",
            PortableFileLaunchIntent.Open,
            ApplicationLanguage.English,
            "echo unsafe");
        var escape = await service.LaunchAsync(
            "state/settings.json",
            "instances/default/www",
            PortableFileLaunchIntent.Open,
            ApplicationLanguage.English,
            "{}");

        Assert.False(executable.IsSuccess);
        Assert.False(escape.IsSuccess);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private PortableFileLauncher CreateLauncher(
        IPortableEditorService editor,
        Func<ProcessStartInfo, Process?> starter) =>
        new(new PortablePathResolver(_testRoot), editor, new SilentLogger(), starter);

    private sealed class CapturingEditor : IPortableEditorService
    {
        public string? RelativePath { get; private set; }

        public PortableToolRuntimeInfo GetRuntime() =>
            new(PortableToolKind.Editor, true, "test", "modules/editor/notepad++.exe", "Ready");

        public Task<PortableEditorLaunchResult> OpenAsync(
            ApplicationLanguage language,
            string? relativeFilePath = null,
            string? initialContent = null,
            CancellationToken cancellationToken = default)
        {
            RelativePath = relativeFilePath;
            return Task.FromResult(new PortableEditorLaunchResult(true, "Portable editor opened."));
        }
    }

    private sealed class UnavailableEditor : IPortableEditorService
    {
        public PortableToolRuntimeInfo GetRuntime() =>
            new(PortableToolKind.Editor, false, string.Empty, string.Empty, "Not installed");

        public Task<PortableEditorLaunchResult> OpenAsync(
            ApplicationLanguage language,
            string? relativeFilePath = null,
            string? initialContent = null,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Editor must not be used.");
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
