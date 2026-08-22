using System.Text.Json;
using System.Text.RegularExpressions;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Application.ProjectTools;
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

    public ComposerProjectPackageManager(
        IPortableToolRuntimeInventory toolInventory,
        IModuleInstallationVerifier moduleVerifier,
        IPortableCommandRunner runner,
        IPortablePathResolver paths)
    {
        _toolInventory = toolInventory;
        _moduleVerifier = moduleVerifier;
        _runner = runner;
        _paths = paths;
    }

    public PortableToolKind Kind => PortableToolKind.Composer;

    public string ProjectRelativePath => Path.Combine("instances", "default", "www");

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
        CancellationToken cancellationToken = default)
    {
        var project = _paths.EnsureDirectory(ProjectRelativePath);
        if (!File.Exists(Path.Combine(project, "composer.json")))
        {
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

        return ParsePackageList(result.StandardOutput, ReadDirectDependencies(project));
    }

    public async Task<PackageOperationResult> InstallPackageAsync(
        string packageName,
        string versionConstraint,
        CancellationToken cancellationToken = default)
    {
        var validation = ValidatePackage(packageName, versionConstraint);
        if (validation is not null)
        {
            return PackageOperationResult.Failure(validation);
        }

        var specification = string.IsNullOrWhiteSpace(versionConstraint)
            ? packageName.Trim()
            : $"{packageName.Trim()}:{versionConstraint.Trim()}";
        var result = await RunComposerAsync(
            "composer.require",
            [
                "require", specification, "--no-ansi", "--no-interaction", "--no-progress",
                "--no-plugins", "--no-scripts"
            ],
            TimeSpan.FromMinutes(10),
            cancellationToken);
        return result.IsSuccess
            ? PackageOperationResult.Success($"Composer package {packageName.Trim()} was installed.")
            : PackageOperationResult.Failure(DescribeFailure(result));
    }

    public async Task<PackageOperationResult> RemovePackageAsync(
        string packageName,
        CancellationToken cancellationToken = default)
    {
        if (!ComposerPackageNameRegex().IsMatch(packageName.Trim()))
        {
            return PackageOperationResult.Failure("The Composer package name is invalid.");
        }

        var result = await RunComposerAsync(
            "composer.remove",
            [
                "remove", packageName.Trim(), "--no-ansi", "--no-interaction", "--no-progress",
                "--no-plugins", "--no-scripts"
            ],
            TimeSpan.FromMinutes(10),
            cancellationToken);
        return result.IsSuccess
            ? PackageOperationResult.Success($"Composer package {packageName.Trim()} was removed.")
            : PackageOperationResult.Failure(DescribeFailure(result));
    }

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
        if (!document.RootElement.TryGetProperty("installed", out var installed) ||
            installed.ValueKind != JsonValueKind.Array)
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
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(Path.Combine(projectPath, "composer.json")));
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
}
