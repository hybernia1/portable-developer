using System.Text.Json;
using System.Text.RegularExpressions;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Infrastructure.ProjectTools;

public sealed partial class PythonProjectPackageManager : IProjectPackageManagerService
{
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
        CancellationToken cancellationToken = default)
    {
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
        return document.RootElement.EnumerateArray()
            .Select(package => new ProjectPackageInfo(
                package.GetProperty("name").GetString() ?? string.Empty,
                package.GetProperty("version").GetString() ?? string.Empty,
                IsDirectDependency: true))
            .Where(package => !string.IsNullOrWhiteSpace(package.Name))
            .OrderBy(package => package.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
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

        var packagesPath = _paths.EnsureDirectory(PackagesRelativePath);
        var specification = packageName.Trim() + versionConstraint.Trim();
        var result = await RunPythonAsync(
            "python.packages.install",
            [
                "-I", "-m", "pip", "install", "--disable-pip-version-check", "--no-input",
                "--no-color", "--no-warn-script-location", "--prefer-binary", "--upgrade",
                "--target", packagesPath, specification
            ],
            TimeSpan.FromMinutes(10),
            includeProjectPackages: false,
            cancellationToken);
        return result.IsSuccess
            ? PackageOperationResult.Success($"Python package {packageName.Trim()} was installed.")
            : PackageOperationResult.Failure(DescribeFailure(result));
    }

    public async Task<PackageOperationResult> RemovePackageAsync(
        string packageName,
        CancellationToken cancellationToken = default)
    {
        if (!PythonPackageNameRegex().IsMatch(packageName.Trim()))
        {
            return PackageOperationResult.Failure("The Python package name is invalid.");
        }

        var result = await RunPythonAsync(
            "python.packages.uninstall",
            [
                "-m", "pip", "uninstall", "--disable-pip-version-check", "--no-input",
                "--yes", packageName.Trim()
            ],
            TimeSpan.FromMinutes(5),
            includeProjectPackages: true,
            cancellationToken);
        return result.IsSuccess
            ? PackageOperationResult.Success($"Python package {packageName.Trim()} was removed.")
            : PackageOperationResult.Failure(DescribeFailure(result));
    }

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
            ["PIP_CACHE_DIR"] = _paths.EnsureDirectory(Path.Combine("cache", "pip"))
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
}

file static class StringExtensions
{
    public static bool StartsWithAny(this string value, params string[] prefixes) =>
        prefixes.Any(prefix => value.StartsWith(prefix, StringComparison.Ordinal));
}
