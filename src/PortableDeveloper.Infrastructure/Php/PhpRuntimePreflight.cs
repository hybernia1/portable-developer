using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Php;
using PortableDeveloper.Infrastructure.NativeRuntime;

namespace PortableDeveloper.Infrastructure.Php;

/// <summary>
/// Requires the native runtime next to php-cgi.exe, not as a machine-wide installation.
/// </summary>
public sealed class PhpRuntimePreflight : IPhpRuntimePreflight
{
    private static readonly string[] RequiredRuntimeFiles = ["vcruntime140.dll", "vcruntime140_1.dll"];
    private readonly IPortablePathResolver _paths;

    public PhpRuntimePreflight(IPortablePathResolver paths)
    {
        _paths = paths;
    }

    public PhpRuntimeReadiness Check(string phpModuleRootRelativePath)
    {
        var moduleRoot = _paths.Resolve(phpModuleRootRelativePath);
        if (!File.Exists(Path.Combine(moduleRoot, "php-cgi.exe")))
        {
            return new PhpRuntimeReadiness(false, ["php-cgi.exe"]);
        }

        var issues = NativeRuntimeMetadataValidator.FindIssues(moduleRoot, RequiredRuntimeFiles);
        return new PhpRuntimeReadiness(issues.Count == 0, issues);
    }
}
