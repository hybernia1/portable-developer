using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.ApachePhp;
using PortableDeveloper.Infrastructure.NativeRuntime;

namespace PortableDeveloper.Infrastructure.ApachePhp;

/// <summary>
/// Requires Apache's native runtime beside httpd.exe instead of using a machine-wide installation.
/// </summary>
public sealed class ApacheRuntimePreflight : IApacheRuntimePreflight
{
    private static readonly string[] RequiredRuntimeFiles = ["bin/vcruntime140.dll"];
    private readonly IPortablePathResolver _paths;

    public ApacheRuntimePreflight(IPortablePathResolver paths)
    {
        _paths = paths;
    }

    public ApacheRuntimeReadiness Check(string apacheModuleRootRelativePath)
    {
        var moduleRoot = _paths.Resolve(apacheModuleRootRelativePath);
        if (!File.Exists(Path.Combine(moduleRoot, "bin", "httpd.exe")))
        {
            return new ApacheRuntimeReadiness(false, ["bin/httpd.exe"]);
        }

        var issues = NativeRuntimeMetadataValidator.FindIssues(moduleRoot, RequiredRuntimeFiles);
        return new ApacheRuntimeReadiness(issues.Count == 0, issues);
    }
}
