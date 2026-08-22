using System.IO.Compression;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Packages;
using PortableDeveloper.Domain.Processes;
using PortableDeveloper.Infrastructure.Modules;
using PortableDeveloper.Infrastructure.Packages;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.ProjectTools;
using PortableDeveloper.Infrastructure.Selenium;

namespace PortableDeveloper.Tests;

public sealed class RuntimePackageManagerTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public async Task InstallAsync_downloads_verifies_and_atomically_registers_database_module()
    {
        Directory.CreateDirectory(_testRoot);
        var executable = Encoding.UTF8.GetBytes("portable-test-mariadbd");
        var executableHash = Sha256(executable);
        var archive = CreateMariaDbArchive(executable);
        var archiveHash = Sha256(archive);
        WriteCatalogs(archiveHash, executableHash);

        var paths = new PortablePathResolver(_testRoot);
        var moduleCatalog = new JsonModulePackageCatalog(paths);
        var inventory = new FileModuleInventory(paths);
        var verifier = new ModuleInstallationVerifier(inventory, moduleCatalog, paths);
        var handler = new TransientArchiveHandler(archive, failuresBeforeSuccess: 2);
        using var httpClient = new HttpClient(handler);
        using var manager = new RuntimePackageManager(
            new JsonDependencyLockCatalog(paths),
            moduleCatalog,
            verifier,
            new PortableToolRuntimeInventory(paths),
            new NeverCalledRunner(),
            paths,
            new SilentLogger(),
            httpClient);

        var result = await manager.InstallAsync(RuntimePackageKind.Database);

        Assert.True(result.Success, result.Detail);
        Assert.True(File.Exists(Path.Combine(_testRoot, "modules", "mariadb", "12.3.2", "bin", "mariadbd.exe")));
        Assert.True(File.Exists(Path.Combine(_testRoot, "modules", "mariadb", "12.3.2", ".portable-developer-module.json")));
        Assert.True(manager.GetPackages().Single(package => package.Kind == RuntimePackageKind.Database).IsInstalled);
        Assert.Empty(Directory.EnumerateDirectories(Path.Combine(_testRoot, "temp", "package-installs")));
        Assert.Equal(3, handler.RequestCount);
    }

    [Fact]
    public async Task InstallAsync_registers_a_catalog_driver_without_installing_it_with_selenium()
    {
        Directory.CreateDirectory(_testRoot);
        var executable = Encoding.UTF8.GetBytes("portable-test-chromedriver");
        var executableHash = Sha256(executable);
        var archive = CreateDriverArchive("chromedriver-win64", "chromedriver.exe", executable);
        var archiveHash = Sha256(archive);
        WriteCatalogs(archiveHash, executableHash, "chromedriver");

        var paths = new PortablePathResolver(_testRoot);
        var moduleCatalog = new JsonModulePackageCatalog(paths);
        var inventory = new FileModuleInventory(paths);
        using var httpClient = new HttpClient(new TransientArchiveHandler(archive, failuresBeforeSuccess: 0));
        using var manager = new RuntimePackageManager(
            new JsonDependencyLockCatalog(paths),
            moduleCatalog,
            new ModuleInstallationVerifier(inventory, moduleCatalog, paths),
            new PortableToolRuntimeInventory(paths),
            new NeverCalledRunner(),
            paths,
            new SilentLogger(),
            httpClient);

        var result = await manager.InstallAsync(RuntimePackageKind.SeleniumChromeDriver);

        Assert.True(result.Success, result.Detail);
        Assert.True(manager.GetPackages().Single(package => package.Kind == RuntimePackageKind.SeleniumChromeDriver).IsInstalled);
        Assert.False(manager.GetPackages().Single(package => package.Kind == RuntimePackageKind.Selenium).IsInstalled);
        var driver = Assert.Single(new SeleniumDriverInventory(paths).Scan());
        Assert.Equal("chrome", driver.BrowserName);
        Assert.True(driver.IsBundled);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private void WriteCatalogs(string archiveHash, string executableHash, string targetId = "mariadb")
    {
        var catalogRoot = Path.Combine(_testRoot, "catalog");
        Directory.CreateDirectory(catalogRoot);
        var componentIds = new[]
        {
            "apache", "php", "mariadb", "selenium", "geckodriver", "chromedriver", "msedgedriver", "openjdk", "composer", "python", "notepadpp", "phpmyadmin", "vcredist"
        };
        var components = componentIds.Select(id => new
        {
            id,
            displayName = id,
            version = id == "mariadb" ? "12.3.2" : "1.0.0",
            fileName = $"{id}.zip",
            archiveSha256 = archiveHash,
            archiveRoot = id == "mariadb" ? "mariadb-test" : id == "chromedriver" ? "chromedriver-win64" : ".",
            normalizedEntrypointRelativePath = id switch
            {
                "chromedriver" => "chromedriver.exe",
                "msedgedriver" => "msedgedriver.exe",
                "geckodriver" => "geckodriver.exe",
                _ => "entrypoint.exe"
            },
            normalizedEntrypointSha256 = id == targetId ? executableHash : new string('d', 64),
            validationFiles = new Dictionary<string, string> { ["validation.txt"] = new string('e', 64) },
            runtimeFiles = new Dictionary<string, string> { ["vcruntime140.dll"] = new string('f', 64) },
            sources = new[] { $"https://github.com/portable-developer-tests/{id}.zip" },
            licenseUrl = "https://example.test/license"
        });
        File.WriteAllText(
            Path.Combine(catalogRoot, "dependencies.lock.json"),
            JsonSerializer.Serialize(new { schemaVersion = 1, components }));

        var packages = new object[]
        {
            Module("apache", "1.0.0", "bin/httpd.exe", new string('a', 64)),
            Module("php", "1.0.0", "php-cgi.exe", new string('b', 64)),
            Module("mariaDb", "12.3.2", "bin/mariadbd.exe", executableHash),
            Module("selenium", "1.0.0", "selenium-server.jar", new string('c', 64))
        };
        File.WriteAllText(
            Path.Combine(catalogRoot, "modules.json"),
            JsonSerializer.Serialize(new { schemaVersion = 1, packages }));
    }

    private static object Module(string kind, string version, string entrypointRelativePath, string entrypointSha256) => new
    {
        kind,
        version,
        sourceUrl = $"https://github.com/portable-developer-tests/{kind.ToLowerInvariant()}.zip",
        entrypointSha256,
        entrypointRelativePath,
        licenseUrl = "https://example.test/license"
    };

    private static byte[] CreateMariaDbArchive(byte[] executable)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry("mariadb-test/bin/mariadbd.exe");
            using var output = entry.Open();
            output.Write(executable);
        }

        return stream.ToArray();
    }

    private static byte[] CreateDriverArchive(string root, string fileName, byte[] executable)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var entry = archive.CreateEntry($"{root}/{fileName}");
            using var output = entry.Open();
            output.Write(executable);
        }

        return stream.ToArray();
    }

    private static string Sha256(byte[] value) =>
        Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();

    private sealed class TransientArchiveHandler(byte[] archive, int failuresBeforeSuccess) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            if (RequestCount <= failuresBeforeSuccess)
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                {
                    RequestMessage = request
                });
            }

            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new ByteArrayContent(archive)
            };
            response.Content.Headers.ContentLength = archive.Length;
            return Task.FromResult(response);
        }
    }

    private sealed class NeverCalledRunner : IPortableCommandRunner
    {
        public Task<PortableCommandResult> RunAsync(PortableCommandDefinition definition, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The database package must not execute a command during installation.");
    }

    private sealed class SilentLogger : IApplicationLogger
    {
        public ValueTask LogAsync(
            ApplicationLogLevel level,
            string component,
            string eventName,
            string message,
            CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
