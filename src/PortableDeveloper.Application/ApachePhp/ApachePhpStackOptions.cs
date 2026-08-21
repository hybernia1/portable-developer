namespace PortableDeveloper.Application.ApachePhp;

public sealed record ApachePhpStackOptions(
    string InstanceId = "default",
    int ApachePort = 8080,
    int PhpFastCgiPort = 9000,
    string DocumentRootRelativePath = "instances/default/www");
