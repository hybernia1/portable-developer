using PortableDeveloper.Application.Php;

namespace PortableDeveloper.Application.ApachePhp;

public sealed record ApachePhpStackOptions(
    string InstanceId = "default",
    int ApachePort = 8080,
    int PhpFastCgiPort = 9000,
    int MariaDbPort = 3307,
    string DocumentRootRelativePath = "instances/default/www",
    PhpSettings? PhpSettings = null);
