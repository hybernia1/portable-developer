using System.Text;
using System.Security.Cryptography;
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
        var phpMyAdminRoot = _paths.Resolve(Path.Combine("tools", "phpmyadmin", "5.2.3"));
        var phpMyAdminAvailable = File.Exists(Path.Combine(phpMyAdminRoot, "index.php"));

        var apacheConfigRelativePath = Path.Combine(generatedRelativeDirectory, "httpd.conf");
        var phpIniRelativePath = Path.Combine(generatedRelativeDirectory, "php.ini");
        var apacheConfigPath = _paths.Resolve(apacheConfigRelativePath);
        var phpIniPath = _paths.Resolve(phpIniRelativePath);

        File.WriteAllText(
            phpIniPath,
            BuildPhpIni(phpRoot, instanceLogs, temporaryDirectory, phpSessions),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        if (phpMyAdminAvailable)
        {
            GeneratePhpMyAdminConfiguration(configuration.InstanceId, configuration.ApachePort, configuration.MariaDbPort);
        }
        File.WriteAllText(
            apacheConfigPath,
            BuildApacheConfiguration(
                apacheRoot,
                documentRoot,
                instanceLogs,
                generatedDirectory,
                phpMyAdminAvailable ? phpMyAdminRoot : null,
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
        extension=mysqli
        extension=mbstring
        extension=openssl
        extension=zip
        upload_max_filesize=128M
        post_max_size=128M
        max_execution_time=300
        cgi.force_redirect = 0
        expose_php = Off
        """;

    private static string BuildApacheConfiguration(
        string apacheRoot,
        string documentRoot,
        string instanceLogs,
        string generatedDirectory,
        string? phpMyAdminRoot,
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
        LoadModule alias_module modules/mod_alias.so
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
        ProxyFCGISetEnvIf "reqenv('SCRIPT_FILENAME') =~ m#^/(.:/.*)$#" SCRIPT_FILENAME "$1"
        <FilesMatch "\.php$">
            SetHandler "proxy:fcgi://127.0.0.1:{{phpFastCgiPort}}/"
        </FilesMatch>
        {{BuildPhpMyAdminAlias(phpMyAdminRoot)}}
        """;

    private void GeneratePhpMyAdminConfiguration(string instanceId, int apachePort, int mariaDbPort)
    {
        var secretRelativePath = Path.Combine("instances", instanceId, "state", "phpmyadmin-secret.txt");
        var secretPath = _paths.Resolve(secretRelativePath);
        if (!File.Exists(secretPath))
        {
            _paths.EnsureDirectory(Path.GetDirectoryName(secretRelativePath)!);
            File.WriteAllText(
                secretPath,
                Convert.ToBase64String(RandomNumberGenerator.GetBytes(24)),
                new UTF8Encoding(false));
        }

        var secret = File.ReadAllText(secretPath).Trim();
        if (secret.Length != 32)
        {
            throw new InvalidDataException("The portable phpMyAdmin cookie secret is invalid.");
        }

        var tempDirectory = _paths.EnsureDirectory(Path.Combine("instances", instanceId, "data", "phpmyadmin-temp"));
        var configRelativeDirectory = Path.Combine("temp", "generated", instanceId, "phpmyadmin");
        _paths.EnsureDirectory(configRelativeDirectory);
        var configPath = _paths.Resolve(Path.Combine(configRelativeDirectory, "config.inc.php"));
        File.WriteAllText(
            configPath,
            $$"""
            <?php
            declare(strict_types=1);
            $cfg['blowfish_secret'] = '{{EscapePhp(secret)}}';
            $i = 0;
            $i++;
            $cfg['Servers'][$i]['auth_type'] = 'cookie';
            $cfg['Servers'][$i]['host'] = '127.0.0.1';
            $cfg['Servers'][$i]['port'] = {{mariaDbPort}};
            $cfg['Servers'][$i]['compress'] = false;
            $cfg['Servers'][$i]['AllowNoPassword'] = true;
            $cfg['Servers'][$i]['AllowRoot'] = true;
            $cfg['Servers'][$i]['verbose'] = 'Portable Developer MariaDB';
            $cfg['TempDir'] = '{{EscapePhp(ToApachePath(tempDirectory))}}';
            $cfg['UploadDir'] = '';
            $cfg['SaveDir'] = '';
            $cfg['SendErrorReports'] = 'never';
            $cfg['CheckConfigurationPermissions'] = false;
            $cfg['PmaAbsoluteUri'] = 'http://127.0.0.1:{{apachePort}}/phpmyadmin/';
            """,
            new UTF8Encoding(false));
    }

    private static string BuildPhpMyAdminAlias(string? phpMyAdminRoot)
    {
        if (phpMyAdminRoot is null)
        {
            return string.Empty;
        }

        var root = ToApachePath(phpMyAdminRoot).TrimEnd('/');
        return $$"""

        Alias /phpmyadmin/ "{{root}}/"
        <Directory "{{root}}">
            AllowOverride None
            Options FollowSymLinks
            Require local
        </Directory>
        <Directory "{{root}}/libraries">
            Require all denied
        </Directory>
        <Directory "{{root}}/templates">
            Require all denied
        </Directory>
        <Directory "{{root}}/vendor">
            Require all denied
        </Directory>
        """;
    }

    private static string EscapePhp(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("'", "\\'", StringComparison.Ordinal);

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
        ValidatePort(configuration.MariaDbPort, nameof(configuration.MariaDbPort));
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
