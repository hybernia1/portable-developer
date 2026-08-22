using System.Security.Cryptography;
using System.Text.Json;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.ProjectTools;

namespace PortableDeveloper.Tests;

public sealed class PortableToolRuntimeInventoryTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void GetRuntime_accepts_only_entrypoint_matching_portable_tool_metadata()
    {
        var module = Path.Combine(_testRoot, "modules", "python", "3.13.0");
        Directory.CreateDirectory(module);
        var executable = Path.Combine(module, "python.exe");
        File.WriteAllText(executable, "python runtime");
        File.WriteAllText(
            Path.Combine(module, ".portable-developer-tool.json"),
            JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                kind = "python",
                version = "3.13.0",
                entrypointRelativePath = "python.exe",
                entrypointSha256 = ComputeSha256(executable)
            }));

        var runtime = new PortableToolRuntimeInventory(new PortablePathResolver(_testRoot))
            .GetRuntime(PortableToolKind.Python);

        Assert.True(runtime.IsReady);
        Assert.Equal("3.13.0", runtime.Version);
        Assert.Equal(Path.Combine("modules", "python", "3.13.0", "python.exe"), runtime.EntrypointRelativePath);
    }

    [Fact]
    public void GetRuntime_rejects_modified_entrypoint()
    {
        var module = Path.Combine(_testRoot, "modules", "composer", "2.10.2");
        Directory.CreateDirectory(module);
        var executable = Path.Combine(module, "composer.phar");
        File.WriteAllText(executable, "modified");
        File.WriteAllText(
            Path.Combine(module, ".portable-developer-tool.json"),
            JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                kind = "composer",
                version = "2.10.2",
                entrypointRelativePath = "composer.phar",
                entrypointSha256 = new string('0', 64)
            }));

        var runtime = new PortableToolRuntimeInventory(new PortablePathResolver(_testRoot))
            .GetRuntime(PortableToolKind.Composer);

        Assert.False(runtime.IsReady);
        Assert.Contains("integrity", runtime.Detail, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetRuntime_accepts_verified_portable_editor()
    {
        var module = Path.Combine(_testRoot, "modules", "editor", "8.9.2");
        Directory.CreateDirectory(module);
        var executable = Path.Combine(module, "notepad++.exe");
        File.WriteAllText(executable, "portable editor");
        File.WriteAllText(
            Path.Combine(module, ".portable-developer-tool.json"),
            JsonSerializer.Serialize(new
            {
                schemaVersion = 1,
                kind = "editor",
                version = "8.9.2",
                entrypointRelativePath = "notepad++.exe",
                entrypointSha256 = ComputeSha256(executable)
            }));

        var runtime = new PortableToolRuntimeInventory(new PortablePathResolver(_testRoot))
            .GetRuntime(PortableToolKind.Editor);

        Assert.True(runtime.IsReady);
        Assert.Equal("8.9.2", runtime.Version);
        Assert.Equal(Path.Combine("modules", "editor", "8.9.2", "notepad++.exe"), runtime.EntrypointRelativePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
