using PortableDeveloper.Application.Php;
using PortableDeveloper.Application.Projects;

namespace PortableDeveloper.Application.ApachePhp;

/// <summary>
/// Portable, user-owned settings for an Apache and PHP FastCGI instance.
/// Every path is relative to the application root.
/// </summary>
public sealed record ApachePhpInstanceConfiguration(
    string InstanceId,
    string ApacheModuleRelativePath,
    string PhpModuleRelativePath,
    string DocumentRootRelativePath,
    int ApachePort = 8080,
    int PhpFastCgiPort = 9000,
    int MariaDbPort = 3307,
    PhpSettings? PhpSettings = null,
    IReadOnlyList<WebProject>? WebProjects = null);
