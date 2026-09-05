using System.Diagnostics;
using System.IO.Compression;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Application.Packages;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Packages;
using PortableDeveloper.Domain.Processes;
using PortableDeveloper.Infrastructure.Security;

namespace PortableDeveloper.Infrastructure.Packages;

public sealed class RuntimePackageManager : IRuntimePackageManager, IDisposable
{
    // Verified archives are only download/install staging by default. Installed modules are
    // independently verified, so retaining a second copy wastes portable disk space.
    private const long DefaultMaximumCacheBytes = 0;
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();
    private static readonly IReadOnlyDictionary<RuntimePackageKind, string[]> PackageComponents =
        new Dictionary<RuntimePackageKind, string[]>
        {
            [RuntimePackageKind.Apache] = ["apache"],
            [RuntimePackageKind.Php] = ["php"],
            [RuntimePackageKind.Database] = ["mariadb"],
            [RuntimePackageKind.Selenium] = ["selenium", "openjdk"],
            [RuntimePackageKind.Composer] = ["php", "composer"],
            [RuntimePackageKind.Node] = ["node"],
            [RuntimePackageKind.Python] = ["python"],
            [RuntimePackageKind.Editor] = ["notepadpp"],
            [RuntimePackageKind.PhpMyAdmin] = ["apache", "php", "mariadb", "phpmyadmin"],
            [RuntimePackageKind.SeleniumChromeEnvironment] = ["chrome-for-testing", "chromedriver"],
            [RuntimePackageKind.SeleniumFirefoxEnvironment] = ["firefox", "geckodriver"]
        };

    private readonly IDependencyLockCatalog _dependencyCatalog;
    private readonly IModulePackageCatalog _moduleCatalog;
    private readonly IModuleInstallationVerifier _moduleVerifier;
    private readonly IPortableToolRuntimeInventory _toolInventory;
    private readonly IPortableCommandRunner _commandRunner;
    private readonly IPortablePathResolver _paths;
    private readonly IApplicationLogger _logger;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;
    private readonly long _maximumCacheBytes;
    private readonly SemaphoreSlim _installLock = new(1, 1);
    private readonly FileSha256VerificationCache _installedFileHashes = new();

    public RuntimePackageManager(
        IDependencyLockCatalog dependencyCatalog,
        IModulePackageCatalog moduleCatalog,
        IModuleInstallationVerifier moduleVerifier,
        IPortableToolRuntimeInventory toolInventory,
        IPortableCommandRunner commandRunner,
        IPortablePathResolver paths,
        IApplicationLogger logger,
        HttpClient? httpClient = null,
        long maximumCacheBytes = DefaultMaximumCacheBytes)
    {
        _dependencyCatalog = dependencyCatalog;
        _moduleCatalog = moduleCatalog;
        _moduleVerifier = moduleVerifier;
        _toolInventory = toolInventory;
        _commandRunner = commandRunner;
        _paths = paths;
        _logger = logger;
        _maximumCacheBytes = maximumCacheBytes >= 0
            ? maximumCacheBytes
            : throw new ArgumentOutOfRangeException(nameof(maximumCacheBytes));
        _ownsHttpClient = httpClient is null;
        _httpClient = httpClient ?? CreateHttpClient();
    }

    public IReadOnlyList<RuntimePackageInfo> GetPackages()
    {
        var dependencies = LoadDependencies();
        var installedComponents = PackageComponents.Values
            .SelectMany(componentIds => componentIds)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                componentId => componentId,
                componentId => IsComponentInstalled(dependencies[componentId]),
                StringComparer.OrdinalIgnoreCase);
        return Enum.GetValues<RuntimePackageKind>()
            .Select(kind => CreatePackageInfo(kind, dependencies, installedComponents))
            .ToArray();
    }

    public async Task<RuntimePackageInstallResult> InstallAsync(
        RuntimePackageKind package,
        IProgress<RuntimePackageInstallProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        await _installLock.WaitAsync(cancellationToken);
        try
        {
            var dependencies = LoadDependencies();
            var packageInfo = CreatePackageInfo(package, dependencies);
            if (packageInfo.IsInstalled)
            {
                await LogCachePruneAsync(TryPrunePackageCache());
                return new(true, "The package is already installed and verified.");
            }

            progress?.Report(new(package, RuntimePackageInstallStage.Preparing, string.Empty, 0));
            var componentIds = PackageComponents[package];
            var components = componentIds.Select(id => dependencies[id]).ToArray();
            if (components.Any(component => component.Id is "apache" or "php"))
            {
                ValidateNativeRuntimeSource(dependencies["vcredist"]);
            }

            await LogAsync(ApplicationLogLevel.Information, "package.install.started", $"package={package}");
            var archives = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (var index = 0; index < components.Length; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var component = components[index];
                if (IsComponentInstalled(component))
                {
                    continue;
                }

                archives[component.Id] = await DownloadComponentAsync(
                    package,
                    component,
                    index,
                    components.Length,
                    progress,
                    cancellationToken);
            }

            var stagingRelativePath = Path.Combine("temp", "package-installs", Guid.NewGuid().ToString("N"));
            var stagingRoot = EnsureSafeDirectory(stagingRelativePath);
            var prepared = new List<PreparedComponent>();
            var committedTargets = new List<string>();
            var replacedTargets = new List<ReplacedComponent>();
            DriverManifestBackup? driverManifestBackup = null;
            var stagingCleanupAllowed = true;
            try
            {
                for (var index = 0; index < components.Length; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var component = components[index];
                    if (!archives.TryGetValue(component.Id, out var archivePath))
                    {
                        continue;
                    }

                    progress?.Report(new(
                        package,
                        RuntimePackageInstallStage.Extracting,
                        component.DisplayName,
                        OverallPercentage(index, components.Length, 75)));
                    prepared.Add(PrepareComponent(component, archivePath, stagingRoot, dependencies));
                }

                progress?.Report(new(package, RuntimePackageInstallStage.Installing, string.Empty, 90));
                foreach (var item in prepared)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var target = _paths.Resolve(item.TargetRelativePath);
                    if (Directory.Exists(target) || File.Exists(target))
                    {
                        EnsureReplaceableInstallTarget(target);
                        var backup = Path.Combine(stagingRoot, "replaced", item.Component.Id);
                        Directory.CreateDirectory(Path.GetDirectoryName(backup)!);
                        Directory.Move(target, backup);
                        replacedTargets.Add(new(target, backup));
                    }

                    EnsureSafeDirectory(Path.GetRelativePath(_paths.RootPath, Path.GetDirectoryName(target)!));
                    try
                    {
                        Directory.Move(item.StagingPath, target);
                    }
                    catch
                    {
                        var replaced = replacedTargets.LastOrDefault(entry =>
                            entry.Target.Equals(target, StringComparison.OrdinalIgnoreCase));
                        if (replaced is not null && !Directory.Exists(target) && Directory.Exists(replaced.Backup))
                        {
                            Directory.Move(replaced.Backup, target);
                            replacedTargets.Remove(replaced);
                        }

                        throw;
                    }
                    committedTargets.Add(target);
                }

                if (prepared.Any(item => IsDriverComponent(item.Component.Id)))
                {
                    driverManifestBackup = BackupDriverManifest();
                }

                await FinishInstalledComponentsAsync(prepared, dependencies, cancellationToken);
                var refreshed = CreatePackageInfo(package, dependencies);
                if (!refreshed.IsInstalled)
                {
                    throw new InvalidDataException($"Installed package did not pass verification: {refreshed.Detail}");
                }

                await RemoveSupersededBrowserComponentsAsync(prepared);

                progress?.Report(new(package, RuntimePackageInstallStage.Completed, string.Empty, 100));
                await LogAsync(ApplicationLogLevel.Information, "package.install.completed", $"package={package}; components={string.Join(',', componentIds)}");
                if (replacedTargets.Count > 0)
                {
                    await LogAsync(
                        ApplicationLogLevel.Information,
                        "package.install.repaired",
                        $"package={package}; components={string.Join(',', replacedTargets.Select(item => Path.GetFileName(item.Target)))}");
                }
                await LogCachePruneAsync(TryPrunePackageCache());
                return new(true, "The package was downloaded, verified, and installed.");
            }
            catch
            {
                try
                {
                    foreach (var target in committedTargets.AsEnumerable().Reverse())
                    {
                        DeleteKnownInstallTarget(target);
                    }

                    foreach (var replaced in replacedTargets.AsEnumerable().Reverse())
                    {
                        if (!Directory.Exists(replaced.Target) && Directory.Exists(replaced.Backup))
                        {
                            Directory.Move(replaced.Backup, replaced.Target);
                        }
                    }

                    if (driverManifestBackup is not null)
                    {
                        RestoreDriverManifest(driverManifestBackup);
                    }
                }
                catch
                {
                    // Keep the staging tree, including original component backups, for manual recovery.
                    stagingCleanupAllowed = false;
                    throw;
                }

                throw;
            }
            finally
            {
                if (stagingCleanupAllowed)
                {
                    DeleteStagingDirectory(stagingRoot);
                }
            }
        }
        catch (OperationCanceledException)
        {
            await LogAsync(ApplicationLogLevel.Warning, "package.install.cancelled", $"package={package}");
            return new(false, "The package installation was cancelled.");
        }
        catch (Exception exception) when (exception is HttpRequestException
                                           or IOException
                                           or InvalidDataException
                                           or InvalidOperationException
                                           or ArgumentException
                                           or UnauthorizedAccessException
                                           or System.ComponentModel.Win32Exception
                                           or CryptographicException
                                           or JsonException)
        {
            await LogAsync(ApplicationLogLevel.Error, "package.install.failed", $"package={package}; error={exception.Message}");
            return new(false, exception.Message);
        }
        finally
        {
            _installLock.Release();
        }
    }

    public void Dispose()
    {
        _installLock.Dispose();
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private RuntimePackageInfo CreatePackageInfo(
        RuntimePackageKind kind,
        IReadOnlyDictionary<string, DependencyLockComponent> dependencies,
        IReadOnlyDictionary<string, bool>? installedComponents = null)
    {
        var components = PackageComponents[kind].Select(id => dependencies[id]).ToArray();
        var missing = components.Where(component => installedComponents is null
            ? !IsComponentInstalled(component)
            : !installedComponents[component.Id]).ToArray();
        var version = kind switch
        {
            RuntimePackageKind.Apache => dependencies["apache"].Version,
            RuntimePackageKind.Php => dependencies["php"].Version,
            RuntimePackageKind.Selenium => dependencies["selenium"].Version,
            RuntimePackageKind.PhpMyAdmin => dependencies["phpmyadmin"].Version,
            _ => components[^1].Version
        };
        return new(
            kind,
            version,
            missing.Length == 0,
            missing.Length == 0
                ? "Installed and verified."
                : $"Missing or failed verification: {string.Join(", ", missing.Select(component => component.DisplayName))}.",
            components.Select(component => component.Id).ToArray());
    }

    private bool IsComponentInstalled(DependencyLockComponent component) => component.Id switch
    {
        "apache" => _moduleVerifier.Verify(ModuleKind.Apache, "Apache").IsVerified,
        "php" => _moduleVerifier.Verify(ModuleKind.Php, "PHP").IsVerified,
        "mariadb" => _moduleVerifier.Verify(ModuleKind.MariaDb, "MariaDB").IsVerified,
        "selenium" => _moduleVerifier.Verify(ModuleKind.Selenium, "Selenium").IsVerified,
        "composer" => _toolInventory.GetRuntime(PortableToolKind.Composer).IsReady,
        "node" => _toolInventory.GetRuntime(PortableToolKind.Node).IsReady,
        "python" => _toolInventory.GetRuntime(PortableToolKind.Python).IsReady,
        "notepadpp" => _toolInventory.GetRuntime(PortableToolKind.Editor).IsReady,
        "openjdk" => VerifyNormalizedEntrypoint(component, Path.Combine("modules", "jre", component.Version)),
        "chrome-for-testing" or "firefox" => VerifyNormalizedEntrypoint(component, GetBrowserTargetPath(component)),
        "geckodriver" => VerifyNormalizedEntrypoint(component, GetDriverTargetPath(component)),
        "chromedriver" => VerifyNormalizedEntrypoint(component, GetDriverTargetPath(component)),
        "phpmyadmin" => VerifyPhpMyAdmin(component),
        "vcredist" => HasNativeRuntimeSource(component),
        _ => false
    };

    private bool VerifyNormalizedEntrypoint(DependencyLockComponent component, string rootRelativePath)
    {
        if (string.IsNullOrWhiteSpace(component.NormalizedEntrypointRelativePath)
            || string.IsNullOrWhiteSpace(component.NormalizedEntrypointSha256))
        {
            return false;
        }

        var path = _paths.Resolve(Path.Combine(rootRelativePath, component.NormalizedEntrypointRelativePath));
        return File.Exists(path)
               && !IsReparsePoint(path)
               && _installedFileHashes.Matches(path, component.NormalizedEntrypointSha256);
    }

    private bool VerifyPhpMyAdmin(DependencyLockComponent component)
    {
        var root = _paths.Resolve(Path.Combine("tools", "phpmyadmin", component.Version));
        if (!Directory.Exists(root) || component.ValidationFiles is null)
        {
            return false;
        }

        return component.ValidationFiles.All(item =>
        {
            var path = Path.Combine(root, item.Key);
            return File.Exists(path)
                   && !IsReparsePoint(path)
                   && _installedFileHashes.Matches(path, item.Value);
        });
    }

    private IReadOnlyDictionary<string, DependencyLockComponent> LoadDependencies()
    {
        var components = _dependencyCatalog.Load().Components.ToDictionary(component => component.Id, StringComparer.OrdinalIgnoreCase);
        var required = PackageComponents.Values.SelectMany(ids => ids).Append("vcredist").Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var id in required)
        {
            if (!components.ContainsKey(id))
            {
                throw new InvalidDataException($"The bundled dependency lock is missing component '{id}'.");
            }
        }

        foreach (var id in new[] { "openjdk", "chrome-for-testing", "firefox", "geckodriver", "chromedriver", "composer", "node", "python", "notepadpp" })
        {
            var component = components[id];
            if (string.IsNullOrWhiteSpace(component.NormalizedEntrypointRelativePath)
                || string.IsNullOrWhiteSpace(component.NormalizedEntrypointSha256))
            {
                throw new InvalidDataException($"The bundled dependency lock is missing entrypoint verification for '{id}'.");
            }
        }

        if (components["phpmyadmin"].ValidationFiles is not { Count: > 0 }
            || components["vcredist"].RuntimeFiles is not { Count: > 0 })
        {
            throw new InvalidDataException("The bundled dependency lock is missing package file verification metadata.");
        }

        return components;
    }

    private async Task<string> DownloadComponentAsync(
        RuntimePackageKind package,
        DependencyLockComponent component,
        int componentIndex,
        int componentCount,
        IProgress<RuntimePackageInstallProgress>? progress,
        CancellationToken cancellationToken)
    {
        var cacheRelativePath = Path.Combine("downloads", "packages", component.Id, component.Version, component.FileName);
        var cachePath = _paths.Resolve(cacheRelativePath);
        EnsureSafeDirectory(Path.GetDirectoryName(cacheRelativePath)!);
        if (File.Exists(cachePath))
        {
            progress?.Report(new(package, RuntimePackageInstallStage.Verifying, component.DisplayName, OverallPercentage(componentIndex, componentCount, 70)));
            if (string.Equals(ComputeSha256(cachePath), component.ArchiveSha256, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    File.SetLastWriteTimeUtc(cachePath, DateTime.UtcNow);
                }
                catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                {
                    // A verified cache entry remains usable even if its LRU timestamp cannot be refreshed.
                }

                return cachePath;
            }

            File.Delete(cachePath);
        }

        Exception? lastError = null;
        foreach (var sourceText in component.Sources)
        {
            var source = new Uri(sourceText, UriKind.Absolute);
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                var partialPath = $"{cachePath}.{Guid.NewGuid():N}.part";
                try
                {
                    using var response = await _httpClient.GetAsync(source, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                    response.EnsureSuccessStatusCode();
                    var finalUri = response.RequestMessage?.RequestUri;
                    if (finalUri is null || !DependencyLockCatalogValidator.IsAllowedDownloadUri(finalUri))
                    {
                        throw new InvalidDataException($"The download redirected to an untrusted source: {finalUri}");
                    }

                    var length = response.Content.Headers.ContentLength;
                    await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
                    await using (var output = new FileStream(partialPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
                    {
                        var buffer = new byte[81920];
                        long received = 0;
                        while (true)
                        {
                            var read = await input.ReadAsync(buffer, cancellationToken);
                            if (read == 0)
                            {
                                break;
                            }

                            await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                            received += read;
                            var filePercentage = length is > 0 ? (int)Math.Clamp(received * 100 / length.Value, 0, 100) : 0;
                            progress?.Report(new(
                                package,
                                RuntimePackageInstallStage.Downloading,
                                component.DisplayName,
                                OverallPercentage(componentIndex, componentCount, filePercentage * 70 / 100),
                                received,
                                length,
                                componentIndex + 1,
                                componentCount));
                        }

                        await output.FlushAsync(cancellationToken);
                    }

                    progress?.Report(new(package, RuntimePackageInstallStage.Verifying, component.DisplayName, OverallPercentage(componentIndex, componentCount, 70)));
                    var actual = ComputeSha256(partialPath);
                    if (!string.Equals(actual, component.ArchiveSha256, StringComparison.OrdinalIgnoreCase))
                    {
                        throw new InvalidDataException($"SHA-256 verification failed for {component.DisplayName} {component.Version}.");
                    }

                    File.Move(partialPath, cachePath);
                    return cachePath;
                }
                catch (Exception exception) when (exception is HttpRequestException or IOException or InvalidDataException)
                {
                    lastError = exception;
                }
                finally
                {
                    if (File.Exists(partialPath))
                    {
                        File.Delete(partialPath);
                    }
                }

                if (attempt < 3)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt - 1)), cancellationToken);
                }
            }
        }

        throw new HttpRequestException($"No trusted source supplied {component.DisplayName} {component.Version}.", lastError);
    }

    private CachePruneResult TryPrunePackageCache()
    {
        try
        {
            return PrunePackageCache();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return default;
        }
    }

    private CachePruneResult PrunePackageCache()
    {
        var cacheRoot = _paths.Resolve(Path.Combine("downloads", "packages"));
        if (!Directory.Exists(cacheRoot) || IsReparsePoint(cacheRoot))
        {
            return default;
        }

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint
        };
        var removedFiles = 0;
        long removedBytes = 0;
        foreach (var partialPath in Directory.EnumerateFiles(cacheRoot, "*.part", options))
        {
            TryDeleteCacheFile(partialPath, ref removedFiles, ref removedBytes);
        }

        var files = Directory.EnumerateFiles(cacheRoot, "*", options)
            .Select(path => new FileInfo(path))
            .OrderBy(file => file.LastWriteTimeUtc)
            .ToArray();
        var totalBytes = files.Sum(file => file.Exists ? file.Length : 0);
        foreach (var file in files)
        {
            if (totalBytes <= _maximumCacheBytes)
            {
                break;
            }

            var length = file.Exists ? file.Length : 0;
            if (TryDeleteCacheFile(file.FullName, ref removedFiles, ref removedBytes))
            {
                totalBytes -= length;
            }
        }

        foreach (var directory in Directory.EnumerateDirectories(cacheRoot, "*", options)
                     .OrderByDescending(path => path.Length))
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                // Cache cleanup is best-effort and must not undo a successful installation.
            }
        }

        return new CachePruneResult(removedFiles, removedBytes);
    }

    private static bool TryDeleteCacheFile(string path, ref int removedFiles, ref long removedBytes)
    {
        try
        {
            var length = new FileInfo(path).Length;
            File.Delete(path);
            removedFiles++;
            removedBytes += length;
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private Task LogCachePruneAsync(CachePruneResult result) =>
        result.RemovedFiles == 0
            ? Task.CompletedTask
            : LogAsync(
                ApplicationLogLevel.Information,
                "package.cache.pruned",
                $"files={result.RemovedFiles}; bytes={result.RemovedBytes}");

    private PreparedComponent PrepareComponent(
        DependencyLockComponent component,
        string archivePath,
        string stagingRoot,
        IReadOnlyDictionary<string, DependencyLockComponent> dependencies)
    {
        var componentStaging = Path.Combine(stagingRoot, component.Id);
        Directory.CreateDirectory(componentStaging);
        var extracted = Path.Combine(stagingRoot, $"{component.Id}-source");

        string targetRelativePath;
        switch (component.Id)
        {
            case "apache":
                ExtractZipSafely(archivePath, extracted);
                CopyDirectory(ResolveArchiveRoot(extracted, component), componentStaging);
                RemoveFiles(componentStaging, ["logs/access.log", "logs/error.log", "logs/access_log", "logs/error_log", "logs/httpd.pid"]);
                AddNativeRuntime(componentStaging, Path.Combine(componentStaging, "bin"), dependencies["vcredist"], ["vcruntime140.dll"]);
                WriteModuleMetadata(componentStaging, ModuleKind.Apache);
                targetRelativePath = Path.Combine("modules", "apache", component.Version);
                break;
            case "php":
                ExtractZipSafely(archivePath, extracted);
                CopyDirectory(ResolveArchiveRoot(extracted, component), componentStaging);
                foreach (var file in Directory.EnumerateFiles(componentStaging, "php.ini*", SearchOption.TopDirectoryOnly))
                {
                    File.Delete(file);
                }

                AddNativeRuntime(componentStaging, componentStaging, dependencies["vcredist"], ["vcruntime140.dll", "vcruntime140_1.dll"]);
                WriteModuleMetadata(componentStaging, ModuleKind.Php);
                targetRelativePath = Path.Combine("modules", "php", component.Version);
                break;
            case "mariadb":
                ExtractZipSafely(archivePath, extracted);
                CopyDirectory(ResolveArchiveRoot(extracted, component), componentStaging);
                WriteModuleMetadata(componentStaging, ModuleKind.MariaDb);
                targetRelativePath = Path.Combine("modules", "mariadb", component.Version);
                break;
            case "selenium":
                File.Copy(archivePath, Path.Combine(componentStaging, "selenium-server.jar"));
                WriteModuleMetadata(componentStaging, ModuleKind.Selenium);
                targetRelativePath = Path.Combine("modules", "selenium", component.Version);
                break;
            case "openjdk":
                ExtractZipSafely(archivePath, extracted);
                CopyJavaRuntime(ResolveArchiveRoot(extracted, component), componentStaging);
                VerifyNormalizedFile(component, componentStaging);
                targetRelativePath = Path.Combine("modules", "jre", component.Version);
                break;
            case "chrome-for-testing":
                ExtractZipSafely(archivePath, extracted);
                CopyDirectory(ResolveArchiveRoot(extracted, component), componentStaging);
                VerifyNormalizedFile(component, componentStaging);
                targetRelativePath = GetBrowserTargetPath(component);
                break;
            case "firefox":
                ExtractFirefoxInstaller(component, archivePath, extracted);
                CopyDirectory(ResolveArchiveRoot(extracted, component), componentStaging);
                ConfigureManagedFirefox(componentStaging);
                VerifyNormalizedFile(component, componentStaging);
                targetRelativePath = GetBrowserTargetPath(component);
                break;
            case "geckodriver":
            case "chromedriver":
                ExtractZipSafely(archivePath, extracted);
                CopyDirectory(ResolveArchiveRoot(extracted, component), componentStaging);
                VerifyNormalizedFile(component, componentStaging);
                targetRelativePath = GetDriverTargetPath(component);
                break;
            case "composer":
                File.Copy(archivePath, Path.Combine(componentStaging, "composer.phar"));
                WriteToolMetadata(component, componentStaging, "composer");
                targetRelativePath = Path.Combine("modules", "composer", component.Version);
                break;
            case "node":
                ExtractZipSafely(archivePath, extracted);
                CopyDirectory(ResolveArchiveRoot(extracted, component), componentStaging);
                VerifyNormalizedFile(component, componentStaging);
                WriteToolMetadata(component, componentStaging, "node");
                targetRelativePath = Path.Combine("modules", "node", component.Version);
                break;
            case "python":
                ExtractZipSafely(archivePath, extracted);
                CopyPythonRuntime(ResolveArchiveRoot(extracted, component), componentStaging);
                WriteToolMetadata(component, componentStaging, "python");
                targetRelativePath = Path.Combine("modules", "python", component.Version);
                break;
            case "notepadpp":
                ExtractZipSafely(archivePath, extracted);
                CopyPortableEditor(ResolveArchiveRoot(extracted, component), componentStaging);
                WriteToolMetadata(component, componentStaging, "editor");
                targetRelativePath = Path.Combine("modules", "editor", component.Version);
                break;
            case "phpmyadmin":
                ExtractZipSafely(archivePath, extracted);
                CopyPhpMyAdmin(ResolveArchiveRoot(extracted, component), componentStaging);
                VerifyValidationFiles(component, componentStaging);
                targetRelativePath = Path.Combine("tools", "phpmyadmin", component.Version);
                break;
            default:
                throw new InvalidDataException($"Runtime installation is not implemented for component '{component.Id}'.");
        }

        return new(component, componentStaging, targetRelativePath);
    }

    private async Task FinishInstalledComponentsAsync(
        IReadOnlyList<PreparedComponent> components,
        IReadOnlyDictionary<string, DependencyLockComponent> dependencies,
        CancellationToken cancellationToken)
    {
        if (components.Any(component => IsDriverComponent(component.Component.Id)))
        {
            var manifestPath = _paths.Resolve(Path.Combine("drivers", "bundled", "drivers.json"));
            EnsureSafeDirectory(Path.Combine("drivers", "bundled"));
            var installedDrivers = dependencies.Values
                .Where(component => IsDriverComponent(component.Id) && IsComponentInstalled(component))
                .Select(component => new
                {
                    browserName = GetDriverBrowserName(component.Id),
                    version = component.Version,
                    relativePath = Path.Combine(
                            GetDriverTargetPath(component),
                            component.NormalizedEntrypointRelativePath!)
                        .Replace(Path.DirectorySeparatorChar, '/'),
                    sha256 = component.NormalizedEntrypointSha256
                })
                .OrderBy(driver => driver.browserName, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var manifest = new
            {
                schemaVersion = 1,
                drivers = installedDrivers
            };
            WriteJsonAtomically(manifestPath, manifest);
        }

        var python = components.FirstOrDefault(component => component.Component.Id == "python");
        if (python is not null)
        {
            var result = await _commandRunner.RunAsync(
                new PortableCommandDefinition(
                    "python-ensurepip",
                    Path.Combine(python.TargetRelativePath, "python.exe"),
                    python.TargetRelativePath,
                    ["-I", "-m", "ensurepip", "--upgrade", "--default-pip"],
                    new Dictionary<string, string> { ["PYTHONNOUSERSITE"] = "1" },
                    TimeSpan.FromMinutes(2)),
                cancellationToken);
            if (!result.IsSuccess)
            {
                throw new InvalidDataException($"Python pip bootstrap failed: {result.StandardError}");
            }
        }
    }

    private async Task RemoveSupersededBrowserComponentsAsync(IReadOnlyList<PreparedComponent> components)
    {
        foreach (var item in components.Where(item =>
                     item.Component.Id is "firefox" or "chrome-for-testing" or "geckodriver" or "chromedriver"))
        {
            var current = _paths.Resolve(item.TargetRelativePath);
            var parent = Path.GetDirectoryName(current)!;
            if (!Directory.Exists(parent) || IsReparsePoint(parent))
            {
                continue;
            }

            foreach (var candidate in Directory.EnumerateDirectories(parent, "*", SearchOption.TopDirectoryOnly))
            {
                if (candidate.Equals(current, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                try
                {
                    EnsureReplaceableInstallTarget(candidate);
                    DeleteKnownInstallTarget(candidate);
                    await LogAsync(
                        ApplicationLogLevel.Information,
                        "package.superseded-component.removed",
                        $"component={item.Component.Id}; version={Path.GetFileName(candidate)}");
                }
                catch (Exception exception) when (exception is IOException
                                                   or UnauthorizedAccessException
                                                   or InvalidDataException
                                                   or InvalidOperationException)
                {
                    await LogAsync(
                        ApplicationLogLevel.Warning,
                        "package.superseded-component.cleanup-failed",
                        $"component={item.Component.Id}; version={Path.GetFileName(candidate)}; error={exception.Message}");
                }
            }
        }
    }

    private static bool IsDriverComponent(string id) =>
        id is "geckodriver" or "chromedriver";

    private static string GetDriverBrowserName(string id) => id switch
    {
        "geckodriver" => "firefox",
        "chromedriver" => "chrome",
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown Selenium driver component.")
    };

    private static string GetDriverFolderName(string id) => id switch
    {
        "geckodriver" => "firefox",
        "chromedriver" => "chrome",
        _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown Selenium driver component.")
    };

    private static string GetDriverTargetPath(DependencyLockComponent component) =>
        Path.Combine("drivers", "bundled", GetDriverFolderName(component.Id), component.Version);

    private static string GetBrowserTargetPath(DependencyLockComponent component) =>
        Path.Combine(
            "modules",
            "browsers",
            component.Id == "chrome-for-testing" ? "chrome-for-testing" : component.Id,
            component.Version);

    private static void ExtractFirefoxInstaller(
        DependencyLockComponent component,
        string installerPath,
        string destination)
    {
        VerifyExecutableSigner(component, installerPath);
        if (Directory.Exists(destination) || File.Exists(destination))
        {
            throw new IOException("The Firefox extraction target already exists.");
        }

        var startInfo = new ProcessStartInfo(installerPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(installerPath)!
        };
        startInfo.ArgumentList.Add($"/ExtractDir={destination}");
        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("The verified Firefox extractor could not be started.");
        if (!process.WaitForExit((int)TimeSpan.FromMinutes(2).TotalMilliseconds))
        {
            process.Kill(entireProcessTree: true);
            process.WaitForExit();
            throw new InvalidDataException("Firefox extraction timed out.");
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidDataException($"Firefox extraction failed with exit code {process.ExitCode}.");
        }
    }

    private static void VerifyExecutableSigner(DependencyLockComponent component, string path)
    {
        if (string.IsNullOrWhiteSpace(component.SignerSubjectContains))
        {
            throw new InvalidDataException($"The dependency lock does not pin the signer for {component.DisplayName}.");
        }

#pragma warning disable SYSLIB0057 // Signed PE extraction has no X509CertificateLoader equivalent.
        using var certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(path));
#pragma warning restore SYSLIB0057
        if (!certificate.Subject.Contains(component.SignerSubjectContains, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"The downloaded {component.DisplayName} installer has an unexpected signer.");
        }
    }

    private static void ConfigureManagedFirefox(string root)
    {
        var distribution = Path.Combine(root, "distribution");
        Directory.CreateDirectory(distribution);
        File.WriteAllText(
            Path.Combine(distribution, "policies.json"),
            CreateManagedFirefoxPoliciesJson(),
            new UTF8Encoding(false));
    }

    internal static string CreateManagedFirefoxPoliciesJson()
    {
        var policies = new Dictionary<string, object>
        {
            ["policies"] = new Dictionary<string, bool>
            {
                ["DisableAppUpdate"] = true,
                ["DisableDefaultBrowserAgent"] = true,
                ["DisableFirefoxStudies"] = true,
                ["DisableTelemetry"] = true,
                ["DontCheckDefaultBrowser"] = true
            }
        };
        return JsonSerializer.Serialize(policies, JsonOptions);
    }

    private DriverManifestBackup BackupDriverManifest()
    {
        var path = _paths.Resolve(Path.Combine("drivers", "bundled", "drivers.json"));
        return new DriverManifestBackup(path, File.Exists(path) ? File.ReadAllBytes(path) : null);
    }

    private static void RestoreDriverManifest(DriverManifestBackup backup)
    {
        if (backup.Content is null)
        {
            if (File.Exists(backup.Path))
            {
                File.Delete(backup.Path);
            }

            return;
        }

        File.WriteAllBytes(backup.Path, backup.Content);
    }

    private void WriteModuleMetadata(string targetRoot, ModuleKind kind)
    {
        var package = _moduleCatalog.Load().Packages.Single(item => item.Kind == kind);
        var entrypoint = Path.Combine(targetRoot, package.EntrypointRelativePath);
        if (!File.Exists(entrypoint)
            || !string.Equals(ComputeSha256(entrypoint), package.EntrypointSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"The extracted {kind} entrypoint failed SHA-256 verification.");
        }

        var metadata = new InstalledModuleMetadata(
            package.Kind,
            package.Version,
            package.SourceUrl,
            package.EntrypointSha256,
            package.EntrypointRelativePath);
        File.WriteAllText(Path.Combine(targetRoot, ".portable-developer-module.json"), JsonSerializer.Serialize(metadata, JsonOptions));
    }

    private static void WriteToolMetadata(DependencyLockComponent component, string targetRoot, string kind)
    {
        VerifyNormalizedFile(component, targetRoot);
        var metadata = new
        {
            schemaVersion = 1,
            kind,
            version = component.Version,
            entrypointRelativePath = component.NormalizedEntrypointRelativePath,
            entrypointSha256 = component.NormalizedEntrypointSha256
        };
        File.WriteAllText(Path.Combine(targetRoot, ".portable-developer-tool.json"), JsonSerializer.Serialize(metadata, JsonOptions));
    }

    private void AddNativeRuntime(
        string moduleRoot,
        string destination,
        DependencyLockComponent runtime,
        IReadOnlyCollection<string> metadataFiles)
    {
        if (runtime.RuntimeFiles is null)
        {
            throw new InvalidDataException("The VC++ runtime file catalog is missing.");
        }

        Directory.CreateDirectory(destination);
        var metadata = new List<object>();
        foreach (var (fileName, expectedHash) in runtime.RuntimeFiles)
        {
            var source = FindNativeRuntimeFile(runtime, fileName, expectedHash)
                ?? throw new FileNotFoundException($"Portable VC++ support file is missing from the base package: {fileName}");
            var target = Path.Combine(destination, fileName);
            File.Copy(source, target, overwrite: true);
            if (metadataFiles.Contains(fileName))
            {
                metadata.Add(new
                {
                    fileName,
                    fileVersion = FileVersionInfo.GetVersionInfo(source).FileVersion ?? runtime.Version,
                    sha256 = expectedHash,
                    signer = "Microsoft Corporation",
                    importedAtUtc = DateTimeOffset.UtcNow
                });
            }
        }

        File.WriteAllText(
            Path.Combine(moduleRoot, ".portable-developer-runtime.json"),
            JsonSerializer.Serialize(metadata, JsonOptions));
    }

    private void ValidateNativeRuntimeSource(DependencyLockComponent runtime)
    {
        if (!HasNativeRuntimeSource(runtime))
        {
            throw new FileNotFoundException("Portable VC++ support files are missing from this application package. Download the complete current Portable Developer release again.");
        }
    }

    private bool HasNativeRuntimeSource(DependencyLockComponent runtime) =>
        runtime.RuntimeFiles is { Count: > 0 }
        && runtime.RuntimeFiles.All(item => FindNativeRuntimeFile(runtime, item.Key, item.Value) is not null);

    private string? FindNativeRuntimeFile(DependencyLockComponent runtime, string fileName, string expectedHash)
    {
        var candidates = new List<string>
        {
            _paths.Resolve(Path.Combine("runtime", "vcredist", runtime.Version, fileName))
        };
        var phpRoot = _paths.EnsureDirectory(Path.Combine("modules", "php"));
        candidates.AddRange(Directory.EnumerateDirectories(phpRoot).Select(path => Path.Combine(path, fileName)));
        var apacheRoot = _paths.EnsureDirectory(Path.Combine("modules", "apache"));
        candidates.AddRange(Directory.EnumerateDirectories(apacheRoot).Select(path => Path.Combine(path, "bin", fileName)));
        return candidates.FirstOrDefault(path =>
            File.Exists(path)
            && !IsReparsePoint(path)
            && string.Equals(ComputeSha256(path), expectedHash, StringComparison.OrdinalIgnoreCase));
    }

    private static void ExtractZipSafely(string archivePath, string destination)
    {
        Directory.CreateDirectory(destination);
        var destinationPrefix = Path.GetFullPath(destination).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        using var archive = ZipFile.OpenRead(archivePath);
        foreach (var entry in archive.Entries)
        {
            var unixMode = (entry.ExternalAttributes >> 16) & 0xF000;
            if (unixMode == 0xA000 || (entry.ExternalAttributes & (int)FileAttributes.ReparsePoint) != 0)
            {
                throw new InvalidDataException($"Archive contains a symbolic link: {entry.FullName}");
            }

            var target = Path.GetFullPath(Path.Combine(destination, entry.FullName.Replace('/', Path.DirectorySeparatorChar)));
            if (!target.StartsWith(destinationPrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"Archive entry escapes the staging directory: {entry.FullName}");
            }

            if (string.IsNullOrEmpty(entry.Name))
            {
                Directory.CreateDirectory(target);
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target, overwrite: false);
        }
    }

    private static string ResolveArchiveRoot(string extractionRoot, DependencyLockComponent component)
    {
        var archiveRoot = string.IsNullOrWhiteSpace(component.ArchiveRoot) || component.ArchiveRoot == "."
            ? extractionRoot
            : Path.GetFullPath(Path.Combine(extractionRoot, component.ArchiveRoot));
        var prefix = Path.GetFullPath(extractionRoot).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if ((!archiveRoot.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
             && !string.Equals(archiveRoot, Path.GetFullPath(extractionRoot), StringComparison.OrdinalIgnoreCase))
            || !Directory.Exists(archiveRoot))
        {
            throw new InvalidDataException($"Archive root is missing or unsafe for {component.DisplayName}.");
        }

        return archiveRoot;
    }

    private static void CopyDirectory(string source, string destination, Func<string, bool>? include = null)
    {
        Directory.CreateDirectory(destination);
        var sourcePrefix = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        foreach (var directory in Directory.EnumerateDirectories(source, "*", SearchOption.AllDirectories))
        {
            if (IsReparsePoint(directory))
            {
                throw new InvalidDataException("Extracted dependency contains a reparse point.");
            }

            var relative = Path.GetRelativePath(source, directory);
            if (include is null || include(relative))
            {
                Directory.CreateDirectory(Path.Combine(destination, relative));
            }
        }

        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            if (IsReparsePoint(file))
            {
                throw new InvalidDataException("Extracted dependency contains a reparse point.");
            }

            var full = Path.GetFullPath(file);
            if (!full.StartsWith(sourcePrefix, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Extracted dependency escaped its source directory.");
            }

            var relative = Path.GetRelativePath(source, file);
            if (include is null || include(relative))
            {
                var target = Path.Combine(destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(file, target);
            }
        }
    }

    private static void CopyJavaRuntime(string source, string destination) =>
        CopyDirectory(source, destination, relative =>
        {
            var first = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
            return first is not "include" and not "jmods" and not "demo" and not "man"
                   && !relative.Equals(Path.Combine("lib", "src.zip"), StringComparison.OrdinalIgnoreCase);
        });

    private static void CopyPythonRuntime(string source, string destination)
    {
        CopyDirectory(source, destination, relative =>
            !relative.StartsWith($"Scripts{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
            && !relative.Equals("Scripts", StringComparison.OrdinalIgnoreCase)
            && !relative.StartsWith(Path.Combine("Lib", "site-packages") + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
            && !relative.Equals(Path.Combine("Lib", "site-packages"), StringComparison.OrdinalIgnoreCase));
        Directory.CreateDirectory(Path.Combine(destination, "Scripts"));
        Directory.CreateDirectory(Path.Combine(destination, "Lib", "site-packages"));
    }

    private static void CopyPortableEditor(string source, string destination)
    {
        var rootFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "notepad++.exe", "doLocalConf.xml", "langs.model.xml", "stylers.model.xml", "contextMenu.xml", "readme.txt", "change.log"
        };
        CopyDirectory(source, destination, relative =>
        {
            var normalized = relative.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            var first = normalized.Split(Path.DirectorySeparatorChar)[0];
            return rootFiles.Contains(normalized)
                   || first is "autoCompletion" or "functionList" or "userDefineLangs"
                   || normalized.Equals(Path.Combine("localization", "czech.xml"), StringComparison.OrdinalIgnoreCase);
        });
        File.WriteAllText(Path.Combine(destination, "doLocalConf.xml"), string.Empty);
    }

    private static void CopyPhpMyAdmin(string source, string destination)
    {
        CopyDirectory(source, destination, relative =>
        {
            var first = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
            return first is not "setup" and not "tmp" && !relative.Equals("config.inc.php", StringComparison.OrdinalIgnoreCase);
        });
        const string bridge = "<?php\ndeclare(strict_types=1);\n$portableRoot = dirname(__DIR__, 3);\nrequire $portableRoot . '/temp/generated/default/phpmyadmin/config.inc.php';\n";
        File.WriteAllText(Path.Combine(destination, "config.inc.php"), bridge, new UTF8Encoding(false));
    }

    private static void VerifyNormalizedFile(DependencyLockComponent component, string root)
    {
        if (string.IsNullOrWhiteSpace(component.NormalizedEntrypointRelativePath)
            || string.IsNullOrWhiteSpace(component.NormalizedEntrypointSha256))
        {
            throw new InvalidDataException($"Normalized entrypoint metadata is missing for {component.DisplayName}.");
        }

        var path = Path.Combine(root, component.NormalizedEntrypointRelativePath);
        if (!File.Exists(path)
            || !string.Equals(ComputeSha256(path), component.NormalizedEntrypointSha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException($"The extracted {component.DisplayName} entrypoint failed SHA-256 verification.");
        }
    }

    private static void VerifyValidationFiles(DependencyLockComponent component, string root)
    {
        if (component.ValidationFiles is null || component.ValidationFiles.Count == 0)
        {
            throw new InvalidDataException($"Validation file metadata is missing for {component.DisplayName}.");
        }

        foreach (var (relativePath, expectedHash) in component.ValidationFiles)
        {
            var path = Path.Combine(root, relativePath);
            if (!File.Exists(path) || !string.Equals(ComputeSha256(path), expectedHash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"The extracted {component.DisplayName} file failed SHA-256 verification: {relativePath}");
            }
        }
    }

    private static void RemoveFiles(string root, IEnumerable<string> relativePaths)
    {
        foreach (var relativePath in relativePaths)
        {
            var path = Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    private void DeleteStagingDirectory(string stagingRoot)
    {
        if (!Directory.Exists(stagingRoot))
        {
            return;
        }

        var allowedRoot = _paths.Resolve(Path.Combine("temp", "package-installs"));
        EnsureChildPath(stagingRoot, allowedRoot);
        Directory.Delete(stagingRoot, recursive: true);
    }

    private string EnsureSafeDirectory(string relativePath)
    {
        ModulePackageManifestValidator.EnsureSafeRelativePath(relativePath, nameof(relativePath), allowEmpty: false);
        var current = _paths.RootPath;
        foreach (var segment in relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);
            if (File.Exists(current))
            {
                throw new InvalidDataException($"A file blocks the package directory: {relativePath}");
            }

            if (Directory.Exists(current))
            {
                if (IsReparsePoint(current))
                {
                    throw new InvalidDataException($"A package directory is a reparse point: {relativePath}");
                }

                continue;
            }

            Directory.CreateDirectory(current);
        }

        return current;
    }

    private void DeleteKnownInstallTarget(string target)
    {
        var allowedRoots = new[]
        {
            _paths.Resolve("modules"),
            _paths.Resolve(Path.Combine("drivers", "bundled")),
            _paths.Resolve(Path.Combine("tools", "phpmyadmin"))
        };
        if (!allowedRoots.Any(root => IsChildPath(target, root)))
        {
            throw new InvalidOperationException($"Refusing to roll back an unexpected package target: {target}");
        }

        if (Directory.Exists(target))
        {
            Directory.Delete(target, recursive: true);
        }
    }

    private void EnsureReplaceableInstallTarget(string target)
    {
        var allowedRoots = new[]
        {
            _paths.Resolve("modules"),
            _paths.Resolve(Path.Combine("drivers", "bundled")),
            _paths.Resolve(Path.Combine("tools", "phpmyadmin"))
        };
        if (!allowedRoots.Any(root => IsChildPath(target, root)))
        {
            throw new InvalidOperationException($"Refusing to replace an unexpected package target: {target}");
        }

        if (File.Exists(target))
        {
            throw new InvalidDataException("A file blocks the expected package directory.");
        }

        var pending = new Stack<string>();
        pending.Push(target);
        while (pending.Count > 0)
        {
            var current = pending.Pop();
            if (IsReparsePoint(current))
            {
                throw new InvalidDataException("The unverified package contains a reparse point and cannot be repaired automatically.");
            }

            foreach (var directory in Directory.EnumerateDirectories(current, "*", SearchOption.TopDirectoryOnly))
            {
                pending.Push(directory);
            }

            foreach (var file in Directory.EnumerateFiles(current, "*", SearchOption.TopDirectoryOnly))
            {
                if (IsReparsePoint(file))
                {
                    throw new InvalidDataException("The unverified package contains a reparse point and cannot be repaired automatically.");
                }
            }
        }
    }

    private static void EnsureChildPath(string path, string root)
    {
        if (!IsChildPath(path, root))
        {
            throw new InvalidOperationException($"Path is outside the expected root: {path}");
        }
    }

    private static bool IsChildPath(string path, string root)
    {
        var prefix = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }

    private static int OverallPercentage(int componentIndex, int componentCount, int componentPercentage) =>
        Math.Clamp((componentIndex * 80 + componentPercentage) / Math.Max(componentCount, 1), 0, 89);

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;

    private static HttpClient CreateHttpClient()
    {
        var handler = new HttpClientHandler
        {
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 10,
            AutomaticDecompression = DecompressionMethods.All
        };
        var client = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromMinutes(15)
        };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("PortableDeveloper", "1.27.0"));
        return client;
    }

    private static void WriteJsonAtomically<T>(string path, T value)
    {
        var temporary = $"{path}.{Guid.NewGuid():N}.tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(value, JsonOptions));
        File.Move(temporary, path, overwrite: true);
    }

    private async Task LogAsync(ApplicationLogLevel level, string eventName, string message)
    {
        try
        {
            await _logger.LogAsync(level, "packages", eventName, message);
        }
        catch
        {
            // Installation and rollback cannot depend on diagnostic logging.
        }
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true };
        options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        return options;
    }

    private sealed record PreparedComponent(
        DependencyLockComponent Component,
        string StagingPath,
        string TargetRelativePath);

    private sealed record ReplacedComponent(string Target, string Backup);

    private readonly record struct CachePruneResult(int RemovedFiles, long RemovedBytes);

    private sealed record DriverManifestBackup(string Path, byte[]? Content);
}
