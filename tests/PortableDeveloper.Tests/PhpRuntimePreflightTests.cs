using System.Security.Cryptography;
using System.Text.Json;
using PortableDeveloper.Application.NativeRuntime;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Php;

namespace PortableDeveloper.Tests;

public sealed class PhpRuntimePreflightTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void Check_reports_missing_app_local_runtime_files()
    {
        CreateFile("modules/php/8.5.9/php-cgi.exe");
        var preflight = new PhpRuntimePreflight(new PortablePathResolver(_testRoot));

        var result = preflight.Check("modules/php/8.5.9");

        Assert.False(result.IsReady);
        Assert.Equal(["vcruntime140.dll", "vcruntime140_1.dll"], result.MissingFiles);
    }

    [Fact]
    public void Check_accepts_complete_app_local_runtime()
    {
        CreateFile("modules/php/8.5.9/php-cgi.exe");
        CreateFile("modules/php/8.5.9/vcruntime140.dll");
        CreateFile("modules/php/8.5.9/vcruntime140_1.dll");
        CreateMetadata("modules/php/8.5.9", "vcruntime140.dll", "vcruntime140_1.dll");
        var preflight = new PhpRuntimePreflight(new PortablePathResolver(_testRoot));

        var result = preflight.Check("modules/php/8.5.9");

        Assert.True(result.IsReady);
        Assert.Empty(result.MissingFiles);
    }

    [Fact]
    public void Check_rejects_runtime_changed_after_import()
    {
        CreateFile("modules/php/8.5.9/php-cgi.exe");
        CreateFile("modules/php/8.5.9/vcruntime140.dll");
        CreateFile("modules/php/8.5.9/vcruntime140_1.dll");
        CreateMetadata("modules/php/8.5.9", "vcruntime140.dll", "vcruntime140_1.dll");
        File.AppendAllText(Path.Combine(_testRoot, "modules", "php", "8.5.9", "vcruntime140.dll"), "tampered");
        var preflight = new PhpRuntimePreflight(new PortablePathResolver(_testRoot));

        var result = preflight.Check("modules/php/8.5.9");

        Assert.False(result.IsReady);
        Assert.Contains(result.MissingFiles, issue => issue.Contains("SHA-256 mismatch", StringComparison.Ordinal));
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

    private void CreateMetadata(string moduleRootRelativePath, params string[] fileNames)
    {
        var moduleRoot = Path.Combine(_testRoot, moduleRootRelativePath);
        var metadata = fileNames.Select(fileName => new NativeRuntimeFileMetadata(
            fileName,
            "14.50.1.0",
            Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(moduleRoot, fileName)))).ToLowerInvariant(),
            "Microsoft Corporation",
            DateTimeOffset.UtcNow));
        File.WriteAllText(
            Path.Combine(moduleRoot, ".portable-developer-runtime.json"),
            JsonSerializer.Serialize(metadata, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }
}
