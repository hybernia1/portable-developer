using PortableDeveloper.Application.ApachePhp;
using PortableDeveloper.Application.Php;
using PortableDeveloper.Infrastructure.ApachePhp;
using PortableDeveloper.Infrastructure.Paths;

namespace PortableDeveloper.Tests;

public sealed class ApachePhpConfigurationGeneratorTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void Generate_writes_transient_configuration_inside_portable_root()
    {
        Directory.CreateDirectory(_testRoot);
        CreateBundledExtensions();
        var paths = new PortablePathResolver(_testRoot);
        var generator = new ApachePhpConfigurationGenerator(paths);

        var generated = generator.Generate(CreateConfiguration());

        var apacheConfigPath = paths.Resolve(generated.ApacheConfigRelativePath);
        var phpIniPath = paths.Resolve(generated.PhpIniRelativePath);
        var apacheConfig = File.ReadAllText(apacheConfigPath);
        var phpIni = File.ReadAllText(phpIniPath);

        Assert.StartsWith(Path.Combine("temp", "generated", "default"), generated.ApacheConfigRelativePath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Listen 127.0.0.1:8080", apacheConfig);
        Assert.Contains("SetHandler \"proxy:fcgi://127.0.0.1:9000/\"", apacheConfig);
        Assert.Contains("ProxyFCGISetEnvIf \"reqenv('SCRIPT_FILENAME') =~ m#^/(.:/.*)$#\" SCRIPT_FILENAME \"$1\"", apacheConfig);
        Assert.DoesNotContain("mod_mpm_winnt.so", apacheConfig, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LogFormat \"%h %l %u %t \\\"%r\\\" %>s %b\" common", apacheConfig);
        Assert.Contains("cgi.force_redirect = 0", phpIni);
        Assert.Contains("extension=mysqli", phpIni);
        Assert.Contains("extension=mbstring", phpIni);
        Assert.Contains(_testRoot.Replace('\\', '/'), apacheConfig);
        Assert.Contains(_testRoot.Replace('\\', '/'), phpIni);
    }

    [Fact]
    public void Generate_configures_bundled_phpmyadmin_for_cookie_login_and_local_mariadb()
    {
        Directory.CreateDirectory(Path.Combine(_testRoot, "tools", "phpmyadmin", "5.2.3"));
        File.WriteAllText(Path.Combine(_testRoot, "tools", "phpmyadmin", "5.2.3", "index.php"), "<?php");
        CreateBundledExtensions();
        var paths = new PortablePathResolver(_testRoot);
        var generator = new ApachePhpConfigurationGenerator(paths);

        var generated = generator.Generate(CreateConfiguration());

        var apacheConfig = File.ReadAllText(paths.Resolve(generated.ApacheConfigRelativePath));
        var phpMyAdminConfig = File.ReadAllText(paths.Resolve("temp/generated/default/phpmyadmin/config.inc.php"));
        Assert.Contains("Alias /phpmyadmin/", apacheConfig, StringComparison.Ordinal);
        Assert.Contains("Require local", apacheConfig, StringComparison.Ordinal);
        Assert.Contains("auth_type'] = 'cookie'", phpMyAdminConfig, StringComparison.Ordinal);
        Assert.Contains("host'] = '127.0.0.1'", phpMyAdminConfig, StringComparison.Ordinal);
        Assert.Contains("port'] = 3307", phpMyAdminConfig, StringComparison.Ordinal);
        Assert.Contains("AllowNoPassword'] = true", phpMyAdminConfig, StringComparison.Ordinal);
        Assert.DoesNotContain("['password'] =", phpMyAdminConfig, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(32, File.ReadAllText(paths.Resolve("instances/default/state/phpmyadmin-secret.txt")).Length);
    }

    [Fact]
    public void Generate_applies_validated_php_settings_and_extension_allowlist()
    {
        Directory.CreateDirectory(_testRoot);
        CreateBundledExtensions();
        var paths = new PortablePathResolver(_testRoot);
        var generator = new ApachePhpConfigurationGenerator(paths);
        var settings = new PhpSettings
        {
            MemoryLimitMb = 512,
            UploadMaxFileSizeMb = 64,
            PostMaxSizeMb = 96,
            MaxExecutionTimeSeconds = 45,
            MaxInputVariables = 5000,
            DisplayErrors = false,
            EnabledExtensions = ["mbstring", "mysqli", "openssl", "sockets", "zip"]
        };

        var generated = generator.Generate(CreateConfiguration() with { PhpSettings = settings });
        var phpIni = File.ReadAllText(paths.Resolve(generated.PhpIniRelativePath));

        Assert.Contains("memory_limit = 512M", phpIni);
        Assert.Contains("upload_max_filesize = 64M", phpIni);
        Assert.Contains("post_max_size = 96M", phpIni);
        Assert.Contains("max_execution_time = 45", phpIni);
        Assert.Contains("max_input_vars = 5000", phpIni);
        Assert.Contains("display_errors = Off", phpIni);
        Assert.Contains("extension=sockets", phpIni);
        Assert.DoesNotContain("extension=curl", phpIni);
    }

    [Fact]
    public void Generate_rejects_enabled_extension_missing_from_php_module()
    {
        Directory.CreateDirectory(_testRoot);
        var paths = new PortablePathResolver(_testRoot);
        var generator = new ApachePhpConfigurationGenerator(paths);

        var exception = Assert.Throws<InvalidOperationException>(() => generator.Generate(CreateConfiguration()));

        Assert.Contains("Enabled PHP extension is missing", exception.Message);
    }

    [Fact]
    public void Generate_rejects_document_root_outside_portable_root()
    {
        Directory.CreateDirectory(_testRoot);
        var generator = new ApachePhpConfigurationGenerator(new PortablePathResolver(_testRoot));
        var configuration = CreateConfiguration() with { DocumentRootRelativePath = "../outside" };

        Assert.Throws<ArgumentException>(() => generator.Generate(configuration));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private static ApachePhpInstanceConfiguration CreateConfiguration() => new(
        InstanceId: "default",
        ApacheModuleRelativePath: "modules/apache/2.4.70",
        PhpModuleRelativePath: "modules/php/8.4.16",
        DocumentRootRelativePath: "instances/default/www");

    private void CreateBundledExtensions()
    {
        var extensionDirectory = Path.Combine(_testRoot, "modules", "php", "8.4.16", "ext");
        Directory.CreateDirectory(extensionDirectory);
        foreach (var extension in PhpExtensionCatalog.All)
        {
            File.WriteAllText(Path.Combine(extensionDirectory, $"php_{extension.Name}.dll"), extension.Name);
        }
    }
}
