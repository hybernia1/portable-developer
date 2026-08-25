using System.Text.Json;
using System.Text.RegularExpressions;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Application.Projects;
using PortableDeveloper.Domain.Processes;
using PortableDeveloper.Infrastructure.Projects;

namespace PortableDeveloper.Infrastructure.ProjectTools;

/// <summary>Runs npm through the verified portable Node.js runtime for the active project only.</summary>
public sealed partial class NpmProjectPackageManager : IProjectPackageManagerService
{
    private const string NpmCliRelativePath = "node_modules/npm/bin/npm-cli.js";
    private readonly IPortableToolRuntimeInventory _toolInventory;
    private readonly IPortableCommandRunner _runner;
    private readonly IPortablePathResolver _paths;
    private readonly IWebProjectCatalog _projects;

    public NpmProjectPackageManager(
        IPortableToolRuntimeInventory toolInventory,
        IPortableCommandRunner runner,
        IPortablePathResolver paths)
        : this(toolInventory, runner, paths, new JsonWebProjectCatalog(paths))
    {
    }

    public NpmProjectPackageManager(
        IPortableToolRuntimeInventory toolInventory,
        IPortableCommandRunner runner,
        IPortablePathResolver paths,
        IWebProjectCatalog projects)
    {
        _toolInventory = toolInventory;
        _runner = runner;
        _paths = paths;
        _projects = projects;
    }

    public PortableToolKind Kind => PortableToolKind.Node;

    public string ProjectRelativePath => _projects.ActiveProject.ProjectRootRelativePath;

    public PortableToolRuntimeInfo GetRuntime()
    {
        var node = _toolInventory.GetRuntime(Kind);
        if (!node.IsReady)
        {
            return node;
        }

        var npmCli = Path.Combine(Path.GetDirectoryName(_paths.Resolve(node.EntrypointRelativePath))!, NpmCliRelativePath);
        return File.Exists(npmCli) && !IsReparsePoint(npmCli)
            ? node
            : node with { IsReady = false, Detail = "The portable Node.js npm CLI is missing or unsafe." };
    }

    public async Task<IReadOnlyList<ProjectPackageInfo>> ListPackagesAsync(
        CancellationToken cancellationToken = default,
        IProgress<ProjectPackageOperationProgress>? progress = null)
    {
        Report(progress, ProjectPackageOperationKind.Refresh, ProjectPackageOperationPhase.RefreshingInventory);
        var project = _paths.EnsureDirectory(ProjectRelativePath);
        if (!File.Exists(Path.Combine(project, "package.json")))
        {
            ReportCompleted(progress, ProjectPackageOperationKind.Refresh);
            return [];
        }

        var result = await RunNpmAsync(
            "npm.list",
            ["ls", "--json", "--all", "--omit=dev", "--ignore-scripts"],
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
        var installedBefore = ReadInstalledPackageNames(project).Contains(normalizedName);
        var specification = string.IsNullOrWhiteSpace(versionConstraint)
            ? normalizedName
            : $"{normalizedName}@{versionConstraint.Trim()}";
        Report(progress, ProjectPackageOperationKind.Install, ProjectPackageOperationPhase.RunningPackageManager, normalizedName);
        var result = await RunNpmAsync(
            "npm.install",
            ["install", specification, "--save", "--no-audit", "--fund=false", "--ignore-scripts"],
            TimeSpan.FromMinutes(10),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return PackageOperationResult.Failure(DescribeFailure(result));
        }

        ReportCompleted(progress, ProjectPackageOperationKind.Install, normalizedName);
        return installedBefore && !directBefore
            ? PackageOperationResult.Success(
                $"npm package {normalizedName} was already installed and is now a direct project dependency.",
                PackageOperationOutcome.PromotedToDirect)
            : directBefore
                ? PackageOperationResult.Success(
                    $"npm package {normalizedName} is already a direct project dependency.",
                    PackageOperationOutcome.AlreadyDirect)
                : PackageOperationResult.Success(
                    $"npm package {normalizedName} was installed.",
                    PackageOperationOutcome.Installed);
    }

    public async Task<PackageOperationResult> RemovePackageAsync(
        string packageName,
        CancellationToken cancellationToken = default,
        IProgress<ProjectPackageOperationProgress>? progress = null)
    {
        Report(progress, ProjectPackageOperationKind.Remove, ProjectPackageOperationPhase.Preparing, packageName);
        if (!NpmPackageNameRegex().IsMatch(packageName.Trim()))
        {
            return PackageOperationResult.Failure("The npm package name is invalid.");
        }

        var normalizedName = packageName.Trim();
        Report(progress, ProjectPackageOperationKind.Remove, ProjectPackageOperationPhase.RunningPackageManager, normalizedName);
        var result = await RunNpmAsync(
            "npm.uninstall",
            ["uninstall", normalizedName, "--no-audit", "--fund=false", "--ignore-scripts"],
            TimeSpan.FromMinutes(10),
            cancellationToken);
        if (!result.IsSuccess)
        {
            return PackageOperationResult.Failure(DescribeFailure(result));
        }

        ReportCompleted(progress, ProjectPackageOperationKind.Remove, normalizedName);
        return PackageOperationResult.Success(
            $"npm package {normalizedName} was removed.",
            PackageOperationOutcome.Removed);
    }

    private async Task<PortableCommandResult> RunNpmAsync(
        string id,
        IReadOnlyList<string> npmArguments,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var runtime = GetRuntime();
        if (!runtime.IsReady)
        {
            return new(null, string.Empty, runtime.Detail);
        }

        var nodeExecutable = _paths.Resolve(runtime.EntrypointRelativePath);
        var npmCli = Path.Combine(Path.GetDirectoryName(nodeExecutable)!, NpmCliRelativePath);
        var npmCache = _paths.EnsureDirectory(Path.Combine("cache", "npm"));
        var npmHome = _paths.EnsureDirectory(Path.Combine("state", "npm"));
        return await _runner.RunAsync(
            new PortableCommandDefinition(
                id,
                runtime.EntrypointRelativePath,
                ProjectRelativePath,
                [npmCli, .. npmArguments],
                new Dictionary<string, string>
                {
                    ["NPM_CONFIG_CACHE"] = npmCache,
                    ["NPM_CONFIG_USERCONFIG"] = Path.Combine(npmHome, "npmrc"),
                    ["NPM_CONFIG_AUDIT"] = "false",
                    ["NPM_CONFIG_FUND"] = "false",
                    ["NPM_CONFIG_UPDATE_NOTIFIER"] = "false",
                    ["NPM_CONFIG_IGNORE_SCRIPTS"] = "true",
                    ["NPM_CONFIG_YES"] = "true"
                },
                timeout),
            cancellationToken);
    }

    private static IReadOnlyList<ProjectPackageInfo> ParsePackageList(string json, IReadOnlySet<string> directDependencies)
    {
        using var document = JsonDocument.Parse(json);
        if (document.RootElement.ValueKind != JsonValueKind.Object ||
            !document.RootElement.TryGetProperty("dependencies", out var dependencies) ||
            dependencies.ValueKind != JsonValueKind.Object)
        {
            return [];
        }

        var packages = new Dictionary<string, ProjectPackageInfo>(StringComparer.OrdinalIgnoreCase);
        AddPackages(dependencies, directDependencies, packages);
        return packages.Values
            .OrderByDescending(package => package.IsDirectDependency)
            .ThenBy(package => package.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static void AddPackages(
        JsonElement dependencies,
        IReadOnlySet<string> directDependencies,
        Dictionary<string, ProjectPackageInfo> destination)
    {
        foreach (var property in dependencies.EnumerateObject())
        {
            var package = property.Value;
            var name = property.Name;
            if (!destination.ContainsKey(name))
            {
                destination[name] = new ProjectPackageInfo(
                    name,
                    GetString(package, "version"),
                    GetString(package, "description"),
                    directDependencies.Contains(name));
            }

            if (package.ValueKind == JsonValueKind.Object &&
                package.TryGetProperty("dependencies", out var nested) &&
                nested.ValueKind == JsonValueKind.Object)
            {
                AddPackages(nested, directDependencies, destination);
            }
        }
    }

    private static IReadOnlySet<string> ReadDirectDependencies(string projectPath)
    {
        var path = Path.Combine(projectPath, "package.json");
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
                AddPropertyNames(document.RootElement, "dependencies", result);
                AddPropertyNames(document.RootElement, "devDependencies", result);
            }

            return result;
        }
        catch (JsonException)
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static IReadOnlySet<string> ReadInstalledPackageNames(string projectPath)
    {
        var path = Path.Combine(projectPath, "package-lock.json");
        if (!File.Exists(path))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (document.RootElement.ValueKind == JsonValueKind.Object &&
                document.RootElement.TryGetProperty("packages", out var packages) &&
                packages.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in packages.EnumerateObject())
                {
                    const string prefix = "node_modules/";
                    if (property.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Add(property.Name[prefix.Length..]);
                    }
                }
            }

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
        if (!NpmPackageNameRegex().IsMatch(packageName.Trim()))
        {
            return "Use an npm package name such as lodash or @scope/package.";
        }

        return string.IsNullOrWhiteSpace(versionConstraint) || NpmConstraintRegex().IsMatch(versionConstraint.Trim())
            ? null
            : "The npm version constraint is invalid.";
    }

    private static string DescribeFailure(PortableCommandResult result)
    {
        if (result.TimedOut)
        {
            return "npm exceeded its ten-minute time limit.";
        }

        var detail = string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError;
        detail = detail.Trim();
        return detail.Length == 0
            ? $"npm failed with exit code {result.ExitCode?.ToString() ?? "unknown"}."
            : detail.Length <= 3000 ? detail : detail[^3000..];
    }

    private static string GetString(JsonElement element, string propertyName) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(propertyName, out var property) && property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? string.Empty
            : string.Empty;

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;

    private static void Report(IProgress<ProjectPackageOperationProgress>? progress, ProjectPackageOperationKind operation, ProjectPackageOperationPhase phase, string packageName = "") =>
        progress?.Report(new(operation, phase, packageName));

    private static void ReportCompleted(IProgress<ProjectPackageOperationProgress>? progress, ProjectPackageOperationKind operation, string packageName = "") =>
        progress?.Report(new(operation, ProjectPackageOperationPhase.Completed, packageName, false, 100));

    [GeneratedRegex("^(?:@[a-z0-9][a-z0-9._-]*/)?[a-z0-9][a-z0-9._-]*$", RegexOptions.IgnoreCase)]
    private static partial Regex NpmPackageNameRegex();

    [GeneratedRegex("^[a-z0-9.*^~<>=|!@+,_ -]{1,64}$", RegexOptions.IgnoreCase)]
    private static partial Regex NpmConstraintRegex();
}
