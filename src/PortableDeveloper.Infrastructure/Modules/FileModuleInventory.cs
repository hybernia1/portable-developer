using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Domain.Modules;

namespace PortableDeveloper.Infrastructure.Modules;

/// <summary>
/// Detects manually placed modules using the normalized modules/&lt;kind&gt;/&lt;version&gt; layout.
/// It only reports files; hash verification is deliberately handled by the package catalog later.
/// </summary>
public sealed class FileModuleInventory : IModuleInventory
{
    private readonly IPortablePathResolver _paths;

    public FileModuleInventory(IPortablePathResolver paths)
    {
        _paths = paths;
    }

    public IReadOnlyList<ModuleInstallation> GetInstalled(ModuleKind kind)
    {
        var specification = GetSpecification(kind);
        var kindRelativePath = Path.Combine("modules", specification.DirectoryName);
        var kindPath = _paths.EnsureDirectory(kindRelativePath);
        var installations = new List<ModuleInstallation>();

        foreach (var versionPath in Directory.EnumerateDirectories(kindPath))
        {
            if (IsReparsePoint(versionPath))
            {
                continue;
            }

            var version = Path.GetFileName(versionPath);
            if (string.IsNullOrWhiteSpace(version))
            {
                continue;
            }

            var entrypointPath = Path.Combine(versionPath, specification.EntrypointRelativePath);
            if (!File.Exists(entrypointPath) || IsReparsePoint(entrypointPath))
            {
                continue;
            }

            var moduleRootRelativePath = Path.Combine(kindRelativePath, version);
            var entrypointRelativePath = Path.Combine(moduleRootRelativePath, specification.EntrypointRelativePath);
            _paths.Resolve(entrypointRelativePath);
            installations.Add(new ModuleInstallation(kind, version, moduleRootRelativePath, entrypointRelativePath));
        }

        return installations
            .OrderByDescending(installation => ParseVersion(installation.Version))
            .ThenByDescending(installation => installation.Version, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ModuleSpecification GetSpecification(ModuleKind kind) => kind switch
    {
        ModuleKind.Apache => new ModuleSpecification("apache", Path.Combine("bin", "httpd.exe")),
        ModuleKind.Php => new ModuleSpecification("php", "php-cgi.exe"),
        ModuleKind.MariaDb => new ModuleSpecification("mariadb", Path.Combine("bin", "mariadbd.exe")),
        ModuleKind.Selenium => new ModuleSpecification("selenium", "selenium-server.jar"),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported module kind.")
    };

    private static Version ParseVersion(string version) =>
        Version.TryParse(version, out var parsedVersion) ? parsedVersion : new Version(0, 0);

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;

    private sealed record ModuleSpecification(string DirectoryName, string EntrypointRelativePath);
}
