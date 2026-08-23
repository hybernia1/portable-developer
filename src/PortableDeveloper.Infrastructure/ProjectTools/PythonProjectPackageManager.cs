using System.Text.Json;
using System.Text;
using System.Text.RegularExpressions;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Infrastructure.ProjectTools;

public sealed partial class PythonProjectPackageManager : IProjectPackageManagerService
{
    private static readonly JsonSerializerOptions StateJsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };
    private readonly IPortableToolRuntimeInventory _toolInventory;
    private readonly IPortableCommandRunner _runner;
    private readonly IPortablePathResolver _paths;

    public PythonProjectPackageManager(
        IPortableToolRuntimeInventory toolInventory,
        IPortableCommandRunner runner,
        IPortablePathResolver paths)
    {
        _toolInventory = toolInventory;
        _runner = runner;
        _paths = paths;
    }

    public PortableToolKind Kind => PortableToolKind.Python;

    public string ProjectRelativePath => Path.Combine("instances", "default", "python");

    public string PackagesRelativePath => Path.Combine(ProjectRelativePath, "packages");

    public PortableToolRuntimeInfo GetRuntime()
    {
        var runtime = _toolInventory.GetRuntime(Kind);
        if (!runtime.IsReady)
        {
            return runtime;
        }

        var runtimeRoot = Path.GetDirectoryName(runtime.EntrypointRelativePath)!;
        var pipPath = _paths.Resolve(Path.Combine(runtimeRoot, "Lib", "site-packages", "pip"));
        return Directory.Exists(pipPath)
            ? runtime
            : runtime with { IsReady = false, Detail = "The portable Python runtime does not contain pip." };
    }

    public async Task<IReadOnlyList<ProjectPackageInfo>> ListPackagesAsync(
        CancellationToken cancellationToken = default,
        IProgress<ProjectPackageOperationProgress>? progress = null)
    {
        Report(progress, ProjectPackageOperationKind.Refresh, ProjectPackageOperationPhase.RefreshingInventory);
        var packagesPath = _paths.EnsureDirectory(PackagesRelativePath);
        var result = await RunPythonAsync(
            "python.packages.list",
            [
                "-I", "-m", "pip", "list", "--disable-pip-version-check", "--no-color",
                "--format=json", "--path", packagesPath
            ],
            TimeSpan.FromMinutes(2),
            includeProjectPackages: false,
            cancellationToken);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(DescribeFailure(result));
        }

        using var document = JsonDocument.Parse(result.StandardOutput);
        var installed = document.RootElement.EnumerateArray()
            .Select(package => new ProjectPackageInfo(
                package.GetProperty("name").GetString() ?? string.Empty,
                package.GetProperty("version").GetString() ?? string.Empty))
            .Where(package => !string.IsNullOrWhiteSpace(package.Name))
            .ToArray();
        var state = LoadOrCreateState(installed);
        var packages = installed
            .Select(package => package with
            {
                IsDirectDependency = state.DirectRequirements.ContainsKey(NormalizePackageName(package.Name))
            })
            .OrderByDescending(package => package.IsDirectDependency)
            .ThenBy(package => package.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
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

        var packagesPath = _paths.EnsureDirectory(PackagesRelativePath);
        var normalizedName = NormalizePackageName(packageName);
        var installed = ReadInstalledMetadata();
        var state = LoadOrCreateState(installed.Keys.Select(name => new ProjectPackageInfo(name, string.Empty)).ToArray());
        if (installed.ContainsKey(normalizedName))
        {
            var wasDirect = state.DirectRequirements.ContainsKey(normalizedName);
            state.DirectRequirements[normalizedName] = versionConstraint.Trim();
            state.ManagedPackages.Add(normalizedName);
            SaveState(state);
            ReportCompleted(progress, ProjectPackageOperationKind.Install, packageName.Trim());
            return wasDirect
                ? PackageOperationResult.Success(
                    $"Python package {packageName.Trim()} is already a direct project dependency.",
                    PackageOperationOutcome.AlreadyDirect)
                : PackageOperationResult.Success(
                    $"Python package {packageName.Trim()} was already installed and is now a direct project dependency.",
                    PackageOperationOutcome.PromotedToDirect);
        }

        var specification = packageName.Trim() + versionConstraint.Trim();
        Report(progress, ProjectPackageOperationKind.Install, ProjectPackageOperationPhase.RunningPackageManager, packageName.Trim());
        var result = await RunPythonAsync(
            "python.packages.install",
            [
                "-I", "-m", "pip", "install", "--disable-pip-version-check", "--no-input",
                "--no-color", "--no-cache-dir", "--no-warn-script-location", "--prefer-binary", "--upgrade",
                "--target", packagesPath, specification
            ],
            TimeSpan.FromMinutes(10),
            includeProjectPackages: false,
            cancellationToken);
        if (!result.IsSuccess)
        {
            return PackageOperationResult.Failure(DescribeFailure(result));
        }

        var installedAfter = ReadInstalledMetadata();
        state.DirectRequirements[normalizedName] = versionConstraint.Trim();
        foreach (var name in installedAfter.Keys)
        {
            state.ManagedPackages.Add(name);
        }

        state.ManagedPackages.Add(normalizedName);
        SaveState(state);
        ReportCompleted(progress, ProjectPackageOperationKind.Install, packageName.Trim());
        return PackageOperationResult.Success(
            $"Python package {packageName.Trim()} was installed.",
            PackageOperationOutcome.Installed);
    }

    public async Task<PackageOperationResult> RemovePackageAsync(
        string packageName,
        CancellationToken cancellationToken = default,
        IProgress<ProjectPackageOperationProgress>? progress = null)
    {
        Report(progress, ProjectPackageOperationKind.Remove, ProjectPackageOperationPhase.Preparing, packageName);
        if (!PythonPackageNameRegex().IsMatch(packageName.Trim()))
        {
            return PackageOperationResult.Failure("The Python package name is invalid.");
        }

        var normalizedName = NormalizePackageName(packageName);
        var installed = ReadInstalledMetadata();
        var state = LoadOrCreateState(installed.Keys.Select(name => new ProjectPackageInfo(name, string.Empty)).ToArray());
        if (!state.DirectRequirements.ContainsKey(normalizedName))
        {
            return PackageOperationResult.Failure($"Python package {packageName.Trim()} is not a direct project dependency.");
        }

        var previousConstraint = state.DirectRequirements[normalizedName];
        state.DirectRequirements.Remove(normalizedName);
        var reachable = FindReachablePackages(state.DirectRequirements.Keys, installed);
        var removable = state.ManagedPackages
            .Where(name => !reachable.Contains(name) && installed.ContainsKey(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        removable.Add(normalizedName);
        Report(progress, ProjectPackageOperationKind.Remove, ProjectPackageOperationPhase.RunningPackageManager, packageName.Trim());
        var result = await RunPythonAsync(
            "python.packages.uninstall",
            [
                "-m", "pip", "uninstall", "--disable-pip-version-check", "--no-input",
                "--yes", .. removable.OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            ],
            TimeSpan.FromMinutes(5),
            includeProjectPackages: true,
            cancellationToken);
        if (!result.IsSuccess)
        {
            state.DirectRequirements[normalizedName] = previousConstraint;
            return PackageOperationResult.Failure(DescribeFailure(result));
        }

        state.ManagedPackages.ExceptWith(removable);
        SaveState(state);
        ReportCompleted(progress, ProjectPackageOperationKind.Remove, packageName.Trim());
        return PackageOperationResult.Success(
            $"Python package {packageName.Trim()} was removed.",
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

    private async Task<PortableCommandResult> RunPythonAsync(
        string id,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        bool includeProjectPackages,
        CancellationToken cancellationToken)
    {
        var runtime = GetRuntime();
        if (!runtime.IsReady)
        {
            return new(null, string.Empty, runtime.Detail);
        }

        _paths.EnsureDirectory(ProjectRelativePath);
        var packagesPath = _paths.EnsureDirectory(PackagesRelativePath);
        var environment = new Dictionary<string, string>
        {
            ["PYTHONNOUSERSITE"] = "1",
            ["PYTHONUTF8"] = "1",
            ["PIP_CONFIG_FILE"] = "NUL",
            ["PIP_DISABLE_PIP_VERSION_CHECK"] = "1",
            ["PIP_NO_INPUT"] = "1",
            ["PIP_NO_CACHE_DIR"] = "1"
        };
        if (includeProjectPackages)
        {
            environment["PYTHONPATH"] = packagesPath;
        }

        return await _runner.RunAsync(
            new PortableCommandDefinition(
                id,
                runtime.EntrypointRelativePath,
                ProjectRelativePath,
                arguments,
                environment,
                timeout),
            cancellationToken);
    }

    private static string? ValidatePackage(string packageName, string versionConstraint)
    {
        if (!PythonPackageNameRegex().IsMatch(packageName.Trim()))
        {
            return "Use a Python package name without a URL or file path.";
        }

        if (string.IsNullOrWhiteSpace(versionConstraint))
        {
            return null;
        }

        var constraint = versionConstraint.Trim();
        return PythonConstraintRegex().IsMatch(constraint) &&
            constraint.StartsWithAny("==", "!=", ">=", "<=", "~=", ">", "<")
                ? null
                : "Use a Python version constraint such as ==4.35.0 or >=4,<5.";
    }

    private static string DescribeFailure(PortableCommandResult result)
    {
        if (result.TimedOut)
        {
            return "pip exceeded its ten-minute time limit.";
        }

        var detail = string.IsNullOrWhiteSpace(result.StandardError) ? result.StandardOutput : result.StandardError;
        detail = detail.Trim();
        return detail.Length == 0
            ? $"pip failed with exit code {result.ExitCode?.ToString() ?? "unknown"}."
            : detail.Length <= 3000 ? detail : detail[^3000..];
    }

    [GeneratedRegex("^[a-z0-9](?:[a-z0-9._-]{0,126}[a-z0-9])?$", RegexOptions.IgnoreCase)]
    private static partial Regex PythonPackageNameRegex();

    [GeneratedRegex("^[0-9a-z.*+!<>=~,_-]{1,64}$", RegexOptions.IgnoreCase)]
    private static partial Regex PythonConstraintRegex();

    [GeneratedRegex("[-_.]+")]
    private static partial Regex PackageSeparatorRegex();

    [GeneratedRegex(@"^Requires-Dist:\s*([A-Za-z0-9_.-]+)", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex RequiresDistRegex();

    private PythonRequirementsState LoadOrCreateState(IReadOnlyList<ProjectPackageInfo> installed)
    {
        var path = GetStatePath();
        if (File.Exists(path))
        {
            return LoadState();
        }

        var metadata = ReadInstalledMetadata();
        var required = metadata.Values
            .SelectMany(package => package.Requirements)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var direct = installed
            .Select(package => NormalizePackageName(package.Name))
            .Where(name => metadata.Count == 0 || !required.Contains(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToDictionary(name => name, _ => string.Empty, StringComparer.OrdinalIgnoreCase);
        var managed = installed
            .Select(package => NormalizePackageName(package.Name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var state = new PythonRequirementsState(direct, managed);
        SaveState(state);
        return state;
    }

    private PythonRequirementsState LoadState()
    {
        var path = GetStatePath();
        if (!File.Exists(path))
        {
            return new(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }

        try
        {
            var stored = JsonSerializer.Deserialize<StoredPythonRequirementsState>(File.ReadAllText(path), StateJsonOptions);
            return new(
                (stored?.DirectRequirements ?? new Dictionary<string, string>())
                    .ToDictionary(item => NormalizePackageName(item.Key), item => item.Value ?? string.Empty, StringComparer.OrdinalIgnoreCase),
                (stored?.ManagedPackages ?? [])
                    .Select(NormalizePackageName)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase));
        }
        catch (JsonException)
        {
            return new(
                new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
                new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        }
    }

    private void SaveState(PythonRequirementsState state)
    {
        var path = GetStatePath();
        var temporary = path + ".part";
        var stored = new StoredPythonRequirementsState(
            1,
            state.DirectRequirements.OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase),
            state.ManagedPackages.OrderBy(name => name, StringComparer.OrdinalIgnoreCase).ToArray());
        File.WriteAllText(temporary, JsonSerializer.Serialize(stored, StateJsonOptions), new UTF8Encoding(false));
        File.Move(temporary, path, overwrite: true);
    }

    private string GetStatePath()
    {
        var directory = _paths.EnsureDirectory(Path.Combine(ProjectRelativePath, "state"));
        return Path.Combine(directory, "direct-requirements.json");
    }

    private IReadOnlyDictionary<string, InstalledPythonPackage> ReadInstalledMetadata()
    {
        var packagesPath = _paths.EnsureDirectory(PackagesRelativePath);
        var result = new Dictionary<string, InstalledPythonPackage>(StringComparer.OrdinalIgnoreCase);
        foreach (var metadataDirectory in Directory.EnumerateDirectories(packagesPath, "*.dist-info", SearchOption.TopDirectoryOnly))
        {
            var metadataPath = Path.Combine(metadataDirectory, "METADATA");
            if (!File.Exists(metadataPath))
            {
                continue;
            }

            var content = File.ReadAllText(metadataPath);
            var nameLine = content.Split('\n').FirstOrDefault(line => line.StartsWith("Name:", StringComparison.OrdinalIgnoreCase));
            if (nameLine is null)
            {
                continue;
            }

            var name = NormalizePackageName(nameLine[5..].Trim());
            var requirements = RequiresDistRegex().Matches(content)
                .Select(match => NormalizePackageName(match.Groups[1].Value))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            result[name] = new InstalledPythonPackage(name, requirements);
        }

        return result;
    }

    private static HashSet<string> FindReachablePackages(
        IEnumerable<string> roots,
        IReadOnlyDictionary<string, InstalledPythonPackage> installed)
    {
        var reachable = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pending = new Stack<string>(roots.Select(NormalizePackageName));
        while (pending.Count > 0)
        {
            var name = pending.Pop();
            if (!reachable.Add(name) || !installed.TryGetValue(name, out var package))
            {
                continue;
            }

            foreach (var requirement in package.Requirements)
            {
                pending.Push(requirement);
            }
        }

        return reachable;
    }

    private static string NormalizePackageName(string name) =>
        PackageSeparatorRegex().Replace(name.Trim().ToLowerInvariant(), "-");

    private sealed record InstalledPythonPackage(string Name, IReadOnlySet<string> Requirements);

    private sealed record PythonRequirementsState(
        Dictionary<string, string> DirectRequirements,
        HashSet<string> ManagedPackages);

    private sealed record StoredPythonRequirementsState(
        int SchemaVersion,
        IReadOnlyDictionary<string, string> DirectRequirements,
        IReadOnlyList<string> ManagedPackages);
}

file static class StringExtensions
{
    public static bool StartsWithAny(this string value, params string[] prefixes) =>
        prefixes.Any(prefix => value.StartsWith(prefix, StringComparison.Ordinal));
}
