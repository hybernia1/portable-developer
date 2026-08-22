using System.Diagnostics;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Application.Settings;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.ProjectTools;

namespace PortableDeveloper.Tests;

public sealed class PortableEditorServiceTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task OpenAsync_creates_portable_file_and_starts_verified_editor_without_shell()
    {
        var entrypoint = Path.Combine("modules", "editor", "8.9.2", "notepad++.exe");
        Directory.CreateDirectory(Path.Combine(_testRoot, "modules", "editor", "8.9.2"));
        File.WriteAllText(Path.Combine(_testRoot, entrypoint), "editor");
        ProcessStartInfo? captured = null;
        var service = new PortableEditorService(
            new ReadyEditor(entrypoint),
            new PortablePathResolver(_testRoot),
            new SilentLogger(),
            startInfo =>
            {
                captured = startInfo;
                return Process.GetCurrentProcess();
            });
        var relativeFile = Path.Combine("instances", "default", "config", "php-custom.ini");

        var result = await service.OpenAsync(ApplicationLanguage.Czech, relativeFile, "; custom settings");

        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.False(captured.UseShellExecute);
        Assert.Equal(Path.Combine(_testRoot, "modules", "editor", "8.9.2"), captured.WorkingDirectory);
        Assert.Equal(["-multiInst", "-nosession", "-Lcs", Path.Combine(_testRoot, relativeFile)], captured.ArgumentList);
        Assert.Equal(Path.Combine(_testRoot, "temp"), captured.Environment["TEMP"]);
        Assert.Equal("; custom settings", File.ReadAllText(Path.Combine(_testRoot, relativeFile)));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private sealed class ReadyEditor(string entrypoint) : IPortableToolRuntimeInventory
    {
        public PortableToolRuntimeInfo GetRuntime(PortableToolKind kind) =>
            new(kind, true, "8.9.2", entrypoint, "Verified portable editor.");
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
