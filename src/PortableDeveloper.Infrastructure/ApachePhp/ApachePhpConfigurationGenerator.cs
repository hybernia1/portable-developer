using System.Text;
using System.Security.Cryptography;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.ApachePhp;
using PortableDeveloper.Application.Php;
using PortableDeveloper.Application.Projects;

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
        var webProjects = ResolveWebProjects(configuration, documentRoot);
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
        var phpSettings = PhpSettingsValidator.Normalize(configuration.PhpSettings ?? PhpSettings.Default);
        var customPhpIni = ReadCustomPhpIni(configuration.InstanceId);

        File.WriteAllText(
            phpIniPath,
            BuildPhpIni(phpRoot, instanceLogs, temporaryDirectory, phpSessions, phpSettings, customPhpIni),
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
                configuration.PhpFastCgiPort,
                webProjects),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        return new GeneratedApachePhpConfiguration(apacheConfigRelativePath, phpIniRelativePath);
    }

    private static string BuildPhpIni(
        string phpRoot,
        string instanceLogs,
        string temporaryDirectory,
        string phpSessions,
        PhpSettings settings,
        string customPhpIni)
    {
        var extensionLines = new List<string>();
        foreach (var extension in settings.EnabledExtensions)
        {
            var extensionFile = Path.Combine(phpRoot, "ext", $"php_{extension}.dll");
            if (!File.Exists(extensionFile))
            {
                throw new InvalidOperationException($"Enabled PHP extension is missing: php_{extension}.dll.");
            }

            extensionLines.Add($"extension={extension}");
        }

        var generated = $$"""
        [PHP]
        extension_dir = "{{ToApachePath(Path.Combine(phpRoot, "ext"))}}"
        error_log = "{{ToApachePath(Path.Combine(instanceLogs, "php-error.log"))}}"
        upload_tmp_dir = "{{ToApachePath(temporaryDirectory)}}"
        sys_temp_dir = "{{ToApachePath(temporaryDirectory)}}"
        session.save_path = "{{ToApachePath(phpSessions)}}"
        log_errors = On
        display_errors = {{(settings.DisplayErrors ? "On" : "Off")}}
        display_startup_errors = {{(settings.DisplayErrors ? "On" : "Off")}}
        error_reporting = E_ALL
        memory_limit = {{settings.MemoryLimitMb}}M
        upload_max_filesize = {{settings.UploadMaxFileSizeMb}}M
        post_max_size = {{settings.PostMaxSizeMb}}M
        max_execution_time = {{settings.MaxExecutionTimeSeconds}}
        max_input_vars = {{settings.MaxInputVariables}}
        {{string.Join(Environment.NewLine, extensionLines)}}
        cgi.force_redirect = 0
        expose_php = Off
        """;

        if (string.IsNullOrWhiteSpace(customPhpIni))
        {
            return generated;
        }

        return $"{generated}{Environment.NewLine}{Environment.NewLine}" +
            $"; --- Portable Developer custom php.ini overrides ---{Environment.NewLine}" +
            customPhpIni.TrimEnd();
    }

    private string ReadCustomPhpIni(string instanceId)
    {
        var path = _paths.Resolve(PhpCustomIni.GetRelativePath(instanceId));
        if (!File.Exists(path))
        {
            return string.Empty;
        }

        var file = new FileInfo(path);
        if ((file.Attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
        {
            throw new InvalidDataException("The custom php.ini must not be a reparse point.");
        }

        if (file.Length > PhpCustomIni.MaximumSizeBytes)
        {
            throw new InvalidDataException($"The custom php.ini may not exceed {PhpCustomIni.MaximumSizeBytes} bytes.");
        }

        var content = File.ReadAllText(path);
        if (content.Contains('\0', StringComparison.Ordinal))
        {
            throw new InvalidDataException("The custom php.ini contains invalid null characters.");
        }

        return content;
    }

    private static string BuildApacheConfiguration(
        string apacheRoot,
        string documentRoot,
        string instanceLogs,
        string generatedDirectory,
        string? phpMyAdminRoot,
        int apachePort,
        int phpFastCgiPort,
        IReadOnlyList<ResolvedWebProject> webProjects) =>
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
        LoadModule rewrite_module modules/mod_rewrite.so

        ErrorLog "{{ToApachePath(Path.Combine(instanceLogs, "apache-error.log"))}}"
        LogFormat "%h %l %u %t \"%r\" %>s %b" common
        CustomLog "{{ToApachePath(Path.Combine(instanceLogs, "apache-access.log"))}}" common
        LogLevel warn

        DocumentRoot "{{ToApachePath(documentRoot)}}"
        <Directory "{{ToApachePath(documentRoot)}}">
            AllowOverride {{(webProjects[0].AllowHtaccess ? "All" : "None")}}
            Options None
            Require local
        </Directory>
        <Directory "{{ToApachePath(webProjects[0].DownloadDirectory)}}">
            AllowOverride None
            Options None
            Require all denied
        </Directory>
        AccessFileName .htaccess
        DirectoryIndex index.php index.html
        AddType application/x-httpd-php .php
        ProxyFCGIBackendType GENERIC
        ProxyFCGISetEnvIf "reqenv('SCRIPT_FILENAME') =~ m#^/(.:/.*)$#" SCRIPT_FILENAME "$1"
        <FilesMatch "\.php$">
            SetHandler "proxy:fcgi://127.0.0.1:{{phpFastCgiPort}}/"
        </FilesMatch>
        {{BuildPhpMyAdminAlias(phpMyAdminRoot)}}
        {{BuildVirtualHosts(webProjects, apachePort)}}
        """;

    private IReadOnlyList<ResolvedWebProject> ResolveWebProjects(
        ApachePhpInstanceConfiguration configuration,
        string fallbackDocumentRoot)
    {
        var projects = configuration.WebProjects?.Where(project => project.IsEnabled).ToArray();
        if (projects is null || projects.Length == 0)
        {
            return [new ResolvedWebProject(
                "localhost",
                fallbackDocumentRoot,
                _paths.EnsureDirectory(Path.Combine(configuration.DocumentRootRelativePath, "seldownloads")),
                true)];
        }

        var result = new List<ResolvedWebProject>(projects.Length);
        foreach (var project in projects)
        {
            ValidateWebProject(project);
            result.Add(new ResolvedWebProject(
                project.HostName,
                _paths.EnsureDirectory(project.DocumentRootRelativePath),
                _paths.EnsureDirectory(Path.Combine(project.ProjectRootRelativePath, "seldownloads")),
                project.AllowHtaccess));
        }

        if (!result.Any(project => project.HostName == "localhost"))
        {
            result.Insert(0, new ResolvedWebProject(
                "localhost",
                fallbackDocumentRoot,
                _paths.EnsureDirectory(Path.Combine(configuration.DocumentRootRelativePath, "seldownloads")),
                true));
        }

        return result;
    }

    private static string BuildVirtualHosts(IReadOnlyList<ResolvedWebProject> projects, int apachePort) =>
        string.Join(
            Environment.NewLine + Environment.NewLine,
            projects.Select(project => $$"""
            <VirtualHost 127.0.0.1:{{apachePort}}>
                ServerName {{project.HostName}}
                DocumentRoot "{{ToApachePath(project.DocumentRoot)}}"
                <Directory "{{ToApachePath(project.DocumentRoot)}}">
                    AllowOverride {{(project.AllowHtaccess ? "All" : "None")}}
                    Options None
                    Require local
                </Directory>
                <Directory "{{ToApachePath(project.DownloadDirectory)}}">
                    AllowOverride None
                    Options None
                    Require all denied
                </Directory>
            </VirtualHost>
            """));

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

    private static void ValidateWebProject(WebProject project)
    {
        if (string.IsNullOrWhiteSpace(project.Id) ||
            project.Id.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-') ||
            string.IsNullOrWhiteSpace(project.ProjectRootRelativePath) ||
            string.IsNullOrWhiteSpace(project.WebRootRelativePath))
        {
            throw new ArgumentException("The web project configuration is invalid.", nameof(project));
        }
    }

    private static void ValidatePort(int port, string parameterName)
    {
        if (port is < 1024 or > 65535)
        {
            throw new ArgumentOutOfRangeException(parameterName, "Only non-privileged TCP ports from 1024 to 65535 are supported.");
        }
    }

    private sealed record ResolvedWebProject(
        string HostName,
        string DocumentRoot,
        string DownloadDirectory,
        bool AllowHtaccess);
}
