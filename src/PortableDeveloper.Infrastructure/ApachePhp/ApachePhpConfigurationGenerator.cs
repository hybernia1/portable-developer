using System.Text;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.ApachePhp;

namespace PortableDeveloper.Infrastructure.ApachePhp;

/// <summary>
/// Creates ephemeral Apache and PHP configuration from portable relative settings.
/// Generated files are deliberately kept under temp/ and regenerated before each launch.
/// </summary>
public sealed class ApachePhpConfigurationGenerator : IApachePhpConfigurationGenerator
{
    private readonly IPortablePathResolver _paths;

    public ApachePhpConfigurationGenerator(IPortablePathResolver paths)
    {
        _paths = paths;
    }

    public GeneratedApachePhpConfiguration Generate(ApachePhpInstanceConfiguration configuration)
    {
        Validate(configuration);

        var apacheRoot = _paths.Resolve(configuration.ApacheModuleRelativePath);
        var phpRoot = _paths.Resolve(configuration.PhpModuleRelativePath);
        var documentRoot = _paths.EnsureDirectory(configuration.DocumentRootRelativePath);
        var instanceLogs = _paths.EnsureDirectory(Path.Combine("instances", configuration.InstanceId, "logs"));
        var phpSessions = _paths.EnsureDirectory(Path.Combine("instances", configuration.InstanceId, "data", "php-sessions"));
        var temporaryDirectory = _paths.EnsureDirectory("temp");
        var generatedRelativeDirectory = Path.Combine("temp", "generated", configuration.InstanceId, "apache-php");
        var generatedDirectory = _paths.EnsureDirectory(generatedRelativeDirectory);

        var apacheConfigRelativePath = Path.Combine(generatedRelativeDirectory, "httpd.conf");
        var phpIniRelativePath = Path.Combine(generatedRelativeDirectory, "php.ini");
        var apacheConfigPath = _paths.Resolve(apacheConfigRelativePath);
        var phpIniPath = _paths.Resolve(phpIniRelativePath);

        File.WriteAllText(
            phpIniPath,
            BuildPhpIni(phpRoot, instanceLogs, temporaryDirectory, phpSessions),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        File.WriteAllText(
            apacheConfigPath,
            BuildApacheConfiguration(
                apacheRoot,
                documentRoot,
                instanceLogs,
                generatedDirectory,
                configuration.ApachePort,
                configuration.PhpFastCgiPort),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        return new GeneratedApachePhpConfiguration(apacheConfigRelativePath, phpIniRelativePath);
    }

    private static string BuildPhpIni(
        string phpRoot,
        string instanceLogs,
        string temporaryDirectory,
        string phpSessions) =>
        $$"""
        [PHP]
        extension_dir = "{{ToApachePath(Path.Combine(phpRoot, "ext"))}}"
        error_log = "{{ToApachePath(Path.Combine(instanceLogs, "php-error.log"))}}"
        upload_tmp_dir = "{{ToApachePath(temporaryDirectory)}}"
        sys_temp_dir = "{{ToApachePath(temporaryDirectory)}}"
        session.save_path = "{{ToApachePath(phpSessions)}}"
        cgi.force_redirect = 0
        expose_php = Off
        """;

    private static string BuildApacheConfiguration(
        string apacheRoot,
        string documentRoot,
        string instanceLogs,
        string generatedDirectory,
        int apachePort,
        int phpFastCgiPort) =>
        $$"""
        ServerRoot "{{ToApachePath(apacheRoot)}}"
        DefaultRuntimeDir "{{ToApachePath(generatedDirectory)}}"
        PidFile "{{ToApachePath(Path.Combine(generatedDirectory, "httpd.pid"))}}"
        Listen 127.0.0.1:{{apachePort}}
        ServerName 127.0.0.1:{{apachePort}}

        LoadModule authn_core_module modules/mod_authn_core.so
        LoadModule authz_core_module modules/mod_authz_core.so
        LoadModule authz_host_module modules/mod_authz_host.so
        LoadModule dir_module modules/mod_dir.so
        LoadModule mime_module modules/mod_mime.so
        LoadModule log_config_module modules/mod_log_config.so
        LoadModule proxy_module modules/mod_proxy.so
        LoadModule proxy_fcgi_module modules/mod_proxy_fcgi.so

        ErrorLog "{{ToApachePath(Path.Combine(instanceLogs, "apache-error.log"))}}"
        LogFormat "%h %l %u %t \"%r\" %>s %b" common
        CustomLog "{{ToApachePath(Path.Combine(instanceLogs, "apache-access.log"))}}" common
        LogLevel warn

        DocumentRoot "{{ToApachePath(documentRoot)}}"
        <Directory "{{ToApachePath(documentRoot)}}">
            AllowOverride All
            Options FollowSymLinks
            Require all granted
        </Directory>
        DirectoryIndex index.php index.html
        AddType application/x-httpd-php .php
        ProxyFCGIBackendType GENERIC
        <FilesMatch "\.php$">
            SetHandler "proxy:fcgi://127.0.0.1:{{phpFastCgiPort}}"
        </FilesMatch>
        """;

    private static string ToApachePath(string path) => path.Replace('\\', '/');

    private static void Validate(ApachePhpInstanceConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        if (string.IsNullOrWhiteSpace(configuration.InstanceId) ||
            configuration.InstanceId.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
        {
            throw new ArgumentException("Instance ID may only contain ASCII letters, digits, hyphens, and underscores.", nameof(configuration));
        }

        ValidatePort(configuration.ApachePort, nameof(configuration.ApachePort));
        ValidatePort(configuration.PhpFastCgiPort, nameof(configuration.PhpFastCgiPort));
        if (configuration.ApachePort == configuration.PhpFastCgiPort)
        {
            throw new ArgumentException("Apache and PHP FastCGI ports must be different.", nameof(configuration));
        }
    }

    private static void ValidatePort(int port, string parameterName)
    {
        if (port is < 1024 or > 65535)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Only non-privileged TCP ports from 1024 to 65535 are supported.");
        }
    }
}
