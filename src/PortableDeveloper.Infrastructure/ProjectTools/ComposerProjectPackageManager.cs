using System.Text.Json;
using System.Text.RegularExpressions;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Application.Projects;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Infrastructure.ProjectTools;

public sealed partial class ComposerProjectPackageManager : IProjectPackageManagerService
{
    private static readonly string[] RequiredExtensions =
        ["php_curl.dll", "php_fileinfo.dll", "php_mbstring.dll", "php_openssl.dll", "php_zip.dll"];

    private readonly IPortableToolRuntimeInventory _toolInventory;
    private readonly IModuleInstallationVerifier _moduleVerifier;
    private readonly IPortableCommandRunner _runner;
    private readonly IPortablePathResolver _paths;
    private readonly IProjectContext _projectContext;

    public ComposerProjectPackageManager(
        IPortableToolRuntimeInventory toolInventory,
        IModuleInstallationVerifier moduleVerifier,
        IPortableCommandRunner runner,
        IPortablePathResolver paths,
        IProjectContext projectContext)
    {
        _toolInventory = toolInventory;
        _moduleVerifier = moduleVerifier;
        _runner = runner;
        _paths = paths;
        _projectContext = projectContext;
    }

    public PortableToolKind Kind => PortableToolKind.Composer;

    public string ProjectRelativePath => _projectContext.ActiveProject.RootRelativePath;

    public PortableToolRuntimeInfo GetRuntime()
    {
        var composer = _toolInventory.GetRuntime(Kind);
        if (!composer.IsReady)
        {
            return composer;
        }

        var php = _moduleVerifier.Verify(ModuleKind.Php, "PHP");
        if (!php.IsVerified)
        {
            return composer with { IsReady = false, Detail = php.Detail };
        }

        var phpRoot = php.Installation!.ModuleRootRelativePath;
        var requiredRelativePaths = new[] { Path.Combine(phpRoot, "php.exe") }
            .Concat(RequiredExtensions.Select(file => Path.Combine(phpRoot, "ext", file)));
        var missing = requiredRelativePaths.FirstOrDefault(path => !File.Exists(_paths.Resolve(path)));
        return missing is null
            ? composer
            : composer with { IsReady = false, Detail = $"Composer requires the missing PHP file: {missing}." };
    }

    public async Task<IReadOnlyList<ProjectPackageInfo>> ListPackagesAsync(
        CancellationToken cancellationToken = default,
        IProgress<ProjectPackageOperationProgress>? progress = null)
    {
        Report(progress, ProjectPackageOperationKind.Refresh, ProjectPackageOperationPhase.RefreshingInventory);
        var project = _paths.EnsureDirectory(ProjectRelativePath);
        if (!File.Exists(Path.Combine(project, "composer.json")))
        {
            ReportCompleted(progress, ProjectPackageOperationKind.Refresh);
            return [];
        }

        var result = await RunComposerAsync(
            "composer.list",
            ["show", "--format=json", "--no-ansi", "--no-interaction", "--no-plugins", "--no-scripts"],
            TimeSpan.FromMinutes(2),
            cancellationToken);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(DescribeFailure(result));
        }

        var packages = ParsePackageList(result.StandardOutput, ReadDirectDependencies(project));
        ReportCompleted(progress, ProjectPackageOperationKind.Refresh);
        return packages;
    }

    public async Task<PackageOperationResult> InstallPackageAsync(
        string packageName,
        string versionConstraint,
        CancellationToken cancellationToken = default,
        IProgress<ProjectPackageOperationProgress>? progress = null)
    {
        Report(progress, ProjectPackageOperationKind.Install, ProjectPackageOperationPhase.Preparing, packageName);
        var validation = ValidatePackage(packageName, versionConstraint);
        if (validation is not null)
        {
            return PackageOperationResult.Failure(validation);
        }

        var normalizedName = packageName.Trim();
        var project = _paths.EnsureDirectory(ProjectRelativePath);
        var directBefore = ReadDirectDependencies(project).Contains(normalizedName);
        var installedBefore = ReadLockedPackageNames(project).Contains(normalizedName);
        var specification = string.IsNullOrWhiteSpace(versionConstraint)
            ? normalizedName
            : $"{normalizedName}:{versionConstraint.Trim()}";
        Report(progress, ProjectPackageOperationKind.Install, ProjectPackageOperationPhase.RunningPackageManager, normalizedName);
        var result = await RunComposerAsync(
            "composer.require",
            [
                "require", specification, "--no-ansi", "--no-interaction", "--no-progress",
                "--no-plugins", "--no-scripts"
            ],
            TimeSpan.FromMinutes(10),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return PackageOperationResult.Failure(DescribeFailure(result));
        }

        ReportCompleted(progress, ProjectPackageOperationKind.Install, normalizedName);
        return installedBefore && !directBefore
            ? PackageOperationResult.Success(
                $"Composer package {normalizedName} was already installed and is now a direct project dependency.",
                PackageOperationOutcome.PromotedToDirect)
            : directBefore
                ? PackageOperationResult.Success(
                    $"Composer package {normalizedName} is already a direct project dependency.",
                    PackageOperationOutcome.AlreadyDirect)
                : PackageOperationResult.Success(
                    $"Composer package {normalizedName} was installed.",
                    PackageOperationOutcome.Installed);
    }

    public async Task<PackageOperationResult> RemovePackageAsync(
        string packageName,
        CancellationToken cancellationToken = default,
        IProgress<ProjectPackageOperationProgress>? progress = null)
    {
        Report(progress, ProjectPackageOperationKind.Remove, ProjectPackageOperationPhase.Preparing, packageName);
        if (!ComposerPackageNameRegex().IsMatch(packageName.Trim()))
        {
            return PackageOperationResult.Failure("The Composer package name is invalid.");
        }

        Report(progress, ProjectPackageOperationKind.Remove, ProjectPackageOperationPhase.RunningPackageManager, packageName.Trim());
        var result = await RunComposerAsync(
            "composer.remove",
            [
                "remove", packageName.Trim(), "--no-ansi", "--no-interaction", "--no-progress",
                "--no-plugins", "--no-scripts"
            ],
            TimeSpan.FromMinutes(10),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return PackageOperationResult.Failure(DescribeFailure(result));
        }

        ReportCompleted(progress, ProjectPackageOperationKind.Remove, packageName.Trim());
        return PackageOperationResult.Success(
            $"Composer package {packageName.Trim()} was removed.",
            PackageOperationOutcome.Removed);
    }

    private static void Report(
        IProgress<ProjectPackageOperationProgress>? progress,
        ProjectPackageOperationKind operation,
        ProjectPackageOperationPhase phase,
        string packageName = "") =>
        progress?.Report(new(operation, phase, packageName));

    private static void ReportCompleted(
        IProgress<ProjectPackageOperationProgress>? progress,
        ProjectPackageOperationKind operation,
        string packageName = "") =>
        progress?.Report(new(operation, ProjectPackageOperationPhase.Completed, packageName, false, 100));

    private async Task<PortableCommandResult> RunComposerAsync(
        string id,
        IReadOnlyList<string> composerArguments,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var runtime = GetRuntime();
        if (!runtime.IsReady)
        {
            return new(null, string.Empty, runtime.Detail);
        }

        var php = _moduleVerifier.Verify(ModuleKind.Php, "PHP").Installation!;
        var phpRoot = php.ModuleRootRelativePath;
        var extensionDirectory = _paths.Resolve(Path.Combine(phpRoot, "ext"));
        var arguments = new List<string>
        {
            "-n",
            "-d", $"extension_dir={extensionDirectory}"
        };
        foreach (var extension in RequiredExtensions)
        {
            arguments.Add("-d");
            arguments.Add($"extension={extension}");
        }

        arguments.Add(_paths.Resolve(runtime.EntrypointRelativePath));
        arguments.AddRange(composerArguments);
        var composerHome = _paths.EnsureDirectory(Path.Combine("state", "composer"));
        var composerCache = _paths.EnsureDirectory(Path.Combine("cache", "composer"));
        return await _runner.RunAsync(
            new PortableCommandDefinition(
                id,
                Path.Combine(phpRoot, "php.exe"),
                ProjectRelativePath,
                arguments,
                new Dictionary<string, string>
                {
                    ["COMPOSER_HOME"] = composerHome,
                    ["COMPOSER_CACHE_DIR"] = composerCache,
                    ["COMPOSER_NO_INTERACTION"] = "1",
                    ["COMPOSER_PROCESS_TIMEOUT"] = "600",
                    ["COMPOSER_ALLOW_SUPERUSER"] = "0"
                },
                timeout),
            cancellationToken);
    }

    private static IReadOnlyList<ProjectPackageInfo> ParsePackageList(
        string json,
        IReadOnlySet<string> directDependencies)
    {
        using var document = JsonDocument.Parse(json);
        JsonElement installed;
        if (document.RootElement.ValueKind == JsonValueKind.Array)
        {
            installed = document.RootElement;
        }
        else if (document.RootElement.ValueKind == JsonValueKind.Object &&
                 document.RootElement.TryGetProperty("installed", out var installedProperty) &&
                 installedProperty.ValueKind == JsonValueKind.Array)
        {
            installed = installedProperty;
        }
        else
        {
            return [];
        }

        return installed.EnumerateArray()
            .Select(package => new ProjectPackageInfo(
                GetString(package, "name"),
                GetString(package, "version"),
                GetString(package, "description"),
                directDependencies.Contains(GetString(package, "name"))))
            .Where(package => !string.IsNullOrWhiteSpace(package.Name))
            .OrderByDescending(package => package.IsDirectDependency)
            .ThenBy(package => package.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IReadOnlySet<string> ReadDirectDependencies(string projectPath)
    {
        var path = Path.Combine(projectPath, "composer.json");
        if (!File.Exists(path))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                return result;
            }

            AddPropertyNames(document.RootElement, "require", result);
            AddPropertyNames(document.RootElement, "require-dev", result);
            result.Remove("php");
            return result;
        }
        catch (JsonException)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static IReadOnlySet<string> ReadLockedPackageNames(string projectPath)
    {
        var path = Path.Combine(projectPath, "composer.lock");
        if (!File.Exists(path))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
            {
                AddPackageNames(document.RootElement, "packages", result);
                AddPackageNames(document.RootElement, "packages-dev", result);
            }

            return result;
        }
        catch (JsonException)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static void AddPackageNames(JsonElement root, string propertyName, HashSet<string> destination)
    {
        if (!root.TryGetProperty(propertyName, out var packages) || packages.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var package in packages.EnumerateArray())
        {
            var name = GetString(package, "name");
            if (!string.IsNullOrWhiteSpace(name))
            {
                destination.Add(name);
            }
        }
    }

    private static void AddPropertyNames(JsonElement root, string propertyName, HashSet<string> destination)
    {
        if (root.TryGetProperty(propertyName, out var dependencies) && dependencies.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in dependencies.EnumerateObject())
            {
                destination.Add(property.Name);
            }
        }
    }

    private static string? ValidatePackage(string packageName, string versionConstraint)
    {
        if (!ComposerPackageNameRegex().IsMatch(packageName.Trim()))
        {
            return "Use a Composer package name in vendor/package format.";
        }

        return string.IsNullOrWhiteSpace(versionConstraint) || ComposerConstraintRegex().IsMatch(versionConstraint.Trim())
            ? null
            : "The Composer version constraint is invalid.";
    }

    private static string DescribeFailure(PortableCommandResult result)
    {
        if (result.TimedOut)
        {
            return "Composer exceeded its ten-minute time limit.";
        }

        var detail = string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError;
        detail = detail.Trim();
        var missing = MissingPackageRegex().Match(detail);
        if (missing.Success)
        {
            var suggestion = PackageSuggestionRegex().Match(detail);
            return suggestion.Success
                ? $"Composer could not find package {missing.Groups[1].Value}. Did you mean {suggestion.Groups[1].Value}?"
                : $"Composer could not find package {missing.Groups[1].Value}. Check the package name and stability constraint.";
        }

        return detail.Length == 0
            ? $"Composer failed with exit code {result.ExitCode?.ToString() ?? "unknown"}."
            : detail.Length <= 3000 ? detail : detail[^3000..];
    }

    private static string GetString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;

    [GeneratedRegex("^[a-z0-9](?:[a-z0-9._-]*[a-z0-9])?/[a-z0-9](?:[a-z0-9._-]*[a-z0-9])?$", RegexOptions.IgnoreCase)]
    private static partial Regex ComposerPackageNameRegex();

    [GeneratedRegex("^[a-z0-9.*^~<>=|!@+,_ -]{1,64}$", RegexOptions.IgnoreCase)]
    private static partial Regex ComposerConstraintRegex();

    [GeneratedRegex(@"Could not find package\s+([^\s.]+)", RegexOptions.IgnoreCase)]
    private static partial Regex MissingPackageRegex();

    [GeneratedRegex(@"Did you mean this\?\s*\r?\n\s*([^\s]+)", RegexOptions.IgnoreCase)]
    private static partial Regex PackageSuggestionRegex();
}
