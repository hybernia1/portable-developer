using System.Text;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Application.Projects;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Application.Scheduling;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Infrastructure.Scheduling;

public sealed class PortableScheduledTaskExecutor : IScheduledTaskExecutor
{
    private const string NpmCliRelativePath = "node_modules/npm/bin/npm-cli.js";
    private readonly IProjectCatalog _projects;
    private readonly IModuleInstallationVerifier _moduleVerifier;
    private readonly IPortableToolRuntimeInventory _toolInventory;
    private readonly IPortableCommandRunner _runner;
    private readonly IPortablePathResolver _paths;

    public PortableScheduledTaskExecutor(
        IProjectCatalog projects,
        IModuleInstallationVerifier moduleVerifier,
        IPortableToolRuntimeInventory toolInventory,
        IPortableCommandRunner runner,
        IPortablePathResolver paths)
    {
        _projects = projects;
        _moduleVerifier = moduleVerifier;
        _toolInventory = toolInventory;
        _runner = runner;
        _paths = paths;
    }

    public async Task<ScheduledTaskExecutionResult> ExecuteAsync(
        PortableScheduledTask task,
        CancellationToken cancellationToken = default)
    {
        task = ScheduledTaskValidator.Validate(task);
        var project = _projects.GetRequired(task.ProjectId);
        var projectRoot = ResolveProjectRoot(project);
        var arguments = ParseArguments(task.Arguments);
        if (arguments.Count > 128)
        {
            throw new ArgumentException("The scheduled task has too many arguments.", nameof(task));
        }
        var environment = CreateCleanEnvironment();
        string executable;
        IReadOnlyList<string> commandArguments;

        switch (task.CommandKind)
        {
            case ScheduledTaskCommandKind.PhpScript:
                var php = _moduleVerifier.Verify(ModuleKind.Php, "PHP");
                if (!php.IsVerified)
                {
                    return Failure(php.Detail);
                }

                executable = Path.Combine(php.Installation!.ModuleRootRelativePath, "php.exe");
                commandArguments = [ResolveScriptArgument(projectRoot, task.Target), .. arguments];
                break;

            case ScheduledTaskCommandKind.PythonScript:
                var python = _toolInventory.GetRuntime(PortableToolKind.Python);
                if (!python.IsReady)
                {
                    return Failure(python.Detail);
                }

                executable = python.EntrypointRelativePath;
                commandArguments = [ResolveScriptArgument(projectRoot, task.Target), .. arguments];
                environment["PYTHONNOUSERSITE"] = "1";
                environment["PYTHONUTF8"] = "1";
                environment["PYTHONIOENCODING"] = "utf-8";
                environment["PYTHONUNBUFFERED"] = "1";
                environment["PYTHONPATH"] = _paths.EnsureDirectory(Path.Combine("instances", "default", "python", "packages"));
                environment["PIP_CONFIG_FILE"] = "NUL";
                environment["PIP_NO_CACHE_DIR"] = "1";
                break;

            case ScheduledTaskCommandKind.NodeScript:
                var node = _toolInventory.GetRuntime(PortableToolKind.Node);
                if (!node.IsReady)
                {
                    return Failure(node.Detail);
                }

                executable = node.EntrypointRelativePath;
                commandArguments = [ResolveScriptArgument(projectRoot, task.Target), .. arguments];
                break;

            case ScheduledTaskCommandKind.NpmScript:
                var npmNode = _toolInventory.GetRuntime(PortableToolKind.Node);
                if (!npmNode.IsReady)
                {
                    return Failure(npmNode.Detail);
                }

                var nodeDirectory = Path.GetDirectoryName(npmNode.EntrypointRelativePath)
                    ?? throw new InvalidDataException("The portable Node.js path is invalid.");
                var npmCliRelativePath = Path.Combine(nodeDirectory, NpmCliRelativePath);
                var npmCli = _paths.Resolve(npmCliRelativePath);
                RefuseReparsePath(npmCli, _paths.RootPath);
                if (!File.Exists(npmCli))
                {
                    return Failure("The portable npm CLI is missing.");
                }

                executable = npmNode.EntrypointRelativePath;
                commandArguments = [npmCli, "run", task.Target, "--", .. arguments];
                environment["NPM_CONFIG_CACHE"] = _paths.EnsureDirectory(Path.Combine("cache", "npm"));
                environment["NPM_CONFIG_USERCONFIG"] = Path.Combine(_paths.EnsureDirectory(Path.Combine("state", "npm")), "npmrc");
                environment["NPM_CONFIG_AUDIT"] = "false";
                environment["NPM_CONFIG_FUND"] = "false";
                environment["NPM_CONFIG_UPDATE_NOTIFIER"] = "false";
                environment["NPM_CONFIG_IGNORE_SCRIPTS"] = "true";
                environment["NPM_CONFIG_YES"] = "true";
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(task));
        }

        var result = await _runner.RunAsync(
            new PortableCommandDefinition(
                $"scheduler.{task.Id}",
                executable,
                project.RootRelativePath,
                commandArguments,
                environment,
                TimeSpan.FromMinutes(task.TimeoutMinutes)),
            cancellationToken);
        return new(result.ExitCode, result.StandardOutput, result.StandardError, result.TimedOut);
    }

    private string ResolveProjectRoot(PortableProject project)
    {
        var root = _paths.Resolve(project.RootRelativePath);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException("The scheduled task project directory does not exist.");
        }

        RefuseReparsePath(root, _paths.RootPath);
        return root;
    }

    private static string ResolveScriptArgument(string projectRoot, string relativePath)
    {
        var target = Path.GetFullPath(Path.Combine(projectRoot, relativePath));
        var prefix = projectRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!target.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The scheduled script leaves the project directory.");
        }

        RefuseReparsePath(target, projectRoot);
        if (!File.Exists(target))
        {
            throw new FileNotFoundException("The scheduled script was not found.", relativePath);
        }

        return Path.GetRelativePath(projectRoot, target);
    }

    private Dictionary<string, string> CreateCleanEnvironment()
    {
        var directories = new List<string>();
        var php = _moduleVerifier.Verify(ModuleKind.Php, "PHP");
        if (php.IsVerified)
        {
            directories.Add(_paths.Resolve(php.Installation!.ModuleRootRelativePath));
        }

        foreach (var kind in new[] { PortableToolKind.Python, PortableToolKind.Node })
        {
            var runtime = _toolInventory.GetRuntime(kind);
            if (runtime.IsReady)
            {
                directories.Add(Path.GetDirectoryName(_paths.Resolve(runtime.EntrypointRelativePath))!);
            }
        }

        return new(StringComparer.OrdinalIgnoreCase)
        {
            ["PATH"] = string.Join(Path.PathSeparator, directories.Distinct(StringComparer.OrdinalIgnoreCase)),
            ["NO_COLOR"] = "1"
        };
    }

    internal static IReadOnlyList<string> ParseArguments(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return [];
        }

        var arguments = new List<string>();
        var current = new StringBuilder();
        char? quote = null;
        for (var index = 0; index < input.Length; index++)
        {
            var character = input[index];
            if (quote is not null)
            {
                if (character == quote)
                {
                    quote = null;
                }
                else if (character == '\\' && index + 1 < input.Length && input[index + 1] == quote)
                {
                    current.Append(input[++index]);
                }
                else
                {
                    current.Append(character);
                }

                continue;
            }

            if (character is '\'' or '"')
            {
                quote = character;
            }
            else if (char.IsWhiteSpace(character))
            {
                if (current.Length > 0)
                {
                    arguments.Add(current.ToString());
                    current.Clear();
                }
            }
            else
            {
                current.Append(character);
            }
        }

        if (quote is not null)
        {
            throw new ArgumentException("The task arguments contain an unclosed quote.", nameof(input));
        }

        if (current.Length > 0)
        {
            arguments.Add(current.ToString());
        }

        return arguments;
    }

    private static ScheduledTaskExecutionResult Failure(string detail) => new(null, string.Empty, detail);

    private static void RefuseReparsePath(string path, string root)
    {
        var current = root;
        var relative = Path.GetRelativePath(root, path);
        foreach (var segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            if (segment == ".")
            {
                continue;
            }

            current = Path.Combine(current, segment);
            if ((File.Exists(current) || Directory.Exists(current)) &&
                (File.GetAttributes(current) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
            {
                throw new InvalidDataException("Scheduled task paths must not use links or reparse points.");
            }
        }
    }
}
