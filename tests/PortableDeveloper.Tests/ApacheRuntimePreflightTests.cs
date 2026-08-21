using System.Security.Cryptography;
using System.Text.Json;
using PortableDeveloper.Application.NativeRuntime;
using PortableDeveloper.Infrastructure.ApachePhp;
using PortableDeveloper.Infrastructure.Paths;

namespace PortableDeveloper.Tests;

public sealed class ApacheRuntimePreflightTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void Check_reports_missing_app_local_runtime()
    {
        CreateFile("modules/apache/2.4.68/bin/httpd.exe");
        var preflight = new ApacheRuntimePreflight(new PortablePathResolver(_testRoot));

        var result = preflight.Check("modules/apache/2.4.68");

        Assert.False(result.IsReady);
        Assert.Equal(["bin/vcruntime140.dll"], result.MissingFiles);
    }

    [Fact]
    public void Check_accepts_complete_app_local_runtime()
    {
        CreateFile("modules/apache/2.4.68/bin/httpd.exe");
        CreateFile("modules/apache/2.4.68/bin/vcruntime140.dll");
        CreateMetadata("modules/apache/2.4.68", "bin/vcruntime140.dll");
        var preflight = new ApacheRuntimePreflight(new PortablePathResolver(_testRoot));

        var result = preflight.Check("modules/apache/2.4.68");

        Assert.True(result.IsReady);
        Assert.Empty(result.MissingFiles);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private void CreateFile(string relativePath)
    {
        var path = Path.Combine(_testRoot, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, "test runtime placeholder");
    }

    private void CreateMetadata(string moduleRootRelativePath, params string[] relativePaths)
    {
        var moduleRoot = Path.Combine(_testRoot, moduleRootRelativePath);
        var metadata = relativePaths.Select(relativePath => new NativeRuntimeFileMetadata(
            Path.GetFileName(relativePath),
            "14.50.1.0",
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(moduleRoot, relativePath)))).ToLowerInvariant(),
            "Microsoft Corporation",
            DateTimeOffset.UtcNow));
        File.WriteAllText(
            Path.Combine(moduleRoot, ".portable-developer-runtime.json"),
            JsonSerializer.Serialize(metadata, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}
