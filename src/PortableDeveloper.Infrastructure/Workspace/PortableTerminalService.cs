using System.Text;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Application.Workspace;
using PortableDeveloper.Application.Projects;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;
using PortableDeveloper.Infrastructure.Projects;

namespace PortableDeveloper.Infrastructure.Workspace;

public sealed class PortableTerminalService : IPortableTerminalService
{
    private const int MaximumCommandLength = 4096;
    private const int MaximumArgumentCount = 128;
    private static readonly IReadOnlyList<PortableTerminalCommandInfo> CommandRegistry =
    [
        new("help", [], "help [command]", "Show the allowed commands or details for one command.", "Terminal"),
        new("clear", ["cls"], "clear", "Clear terminal output.", "Terminal"),
        new("pwd", [], "pwd", "Show the current project directory.", "Filesystem"),
        new("ls", ["dir"], "ls [relative-directory]", "List files inside the active project.", "Filesystem"),
        new("cd", [], "cd <relative-directory>", "Change directory inside the active project.", "Filesystem"),
        new("mkdir", [], "mkdir <relative-directory>", "Create a directory inside the active project.", "Filesystem"),
        new("php", [], "php [arguments]", "Run the bundled PHP CLI.", "Tools"),
        new("composer", [], "composer [arguments]", "Run the bundled Composer.", "Tools"),
        new("python", ["python3"], "python [arguments]", "Run the bundled Python.", "Tools"),
        new("service", [], "service <status|start|stop|restart> [web|mariadb|selenium|all]", "Control Portable Developer services.", "Services")
    ];
    private readonly IModuleInstallationVerifier _moduleVerifier;
    private readonly IPortableToolRuntimeInventory _toolInventory;
    private readonly IPortableCommandRunner _runner;
    private readonly IPortablePathResolver _paths;
    private readonly IWebProjectCatalog _projects;

    public PortableTerminalService(
        IModuleInstallationVerifier moduleVerifier,
        IPortableToolRuntimeInventory toolInventory,
        IPortableCommandRunner runner,
        IPortablePathResolver paths)
        : this(moduleVerifier, toolInventory, runner, paths, new JsonWebProjectCatalog(paths))
    {
    }

    public PortableTerminalService(
        IModuleInstallationVerifier moduleVerifier,
        IPortableToolRuntimeInventory toolInventory,
        IPortableCommandRunner runner,
        IPortablePathResolver paths,
        IWebProjectCatalog projects)
    {
        _moduleVerifier = moduleVerifier;
        _toolInventory = toolInventory;
        _runner = runner;
        _paths = paths;
        _projects = projects;
        RefuseReparsePoint(WorkspaceRoot);
    }

    public string InitialWorkingDirectory => string.Empty;

    public IReadOnlyList<PortableTerminalCommandInfo> Commands => CommandRegistry;

    private string WorkspaceRootRelativePath => _projects.ActiveProject.ProjectRootRelativePath;

    private string WorkspaceRoot => _paths.EnsureDirectory(WorkspaceRootRelativePath);

    public async Task<PortableTerminalResult> ExecuteAsync(
        string commandLine,
        string workingDirectory,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return Result(workingDirectory, string.Empty);
        }

        if (commandLine.Length > MaximumCommandLength)
        {
            return Error(workingDirectory, "The command is too long.");
        }

        IReadOnlyList<string> tokens;
        try
        {
            tokens = Tokenize(commandLine);
        }
        catch (ArgumentException exception)
        {
            return Error(workingDirectory, exception.Message);
        }

        if (tokens.Count == 0)
        {
            return Result(workingDirectory, string.Empty);
        }

        if (tokens.Count > MaximumArgumentCount)
        {
            return Error(workingDirectory, "The command has too many arguments.");
        }

        var command = tokens[0].ToLowerInvariant();
        var arguments = tokens.Skip(1).ToArray();
        return command switch
        {
            "help" => ShowHelp(workingDirectory, arguments),
            "clear" or "cls" => new(workingDirectory, string.Empty, ClearScreen: true),
            "pwd" => Result(workingDirectory, DisplayPath(workingDirectory)),
            "ls" or "dir" => ListDirectory(workingDirectory, arguments),
            "cd" => ChangeDirectory(workingDirectory, arguments),
            "mkdir" => CreateDirectory(workingDirectory, arguments),
            "service" => ParseServiceCommand(workingDirectory, arguments),
            "php" => await RunPhpAsync(workingDirectory, arguments, cancellationToken),
            "composer" => await RunComposerAsync(workingDirectory, arguments, cancellationToken),
            "python" or "python3" => await RunPythonAsync(workingDirectory, arguments, cancellationToken),
            _ => Error(workingDirectory, $"Unknown command '{tokens[0]}'. Type 'help' for the allowed commands.")
        };
    }

    private static PortableTerminalResult ShowHelp(
        string workingDirectory,
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count > 1)
        {
            return Error(workingDirectory, "Usage: help [command]");
        }

        if (arguments.Count == 1)
        {
            var requested = arguments[0];
            var definition = CommandRegistry.FirstOrDefault(command =>
                string.Equals(command.Name, requested, StringComparison.OrdinalIgnoreCase) ||
                command.Aliases.Contains(requested, StringComparer.OrdinalIgnoreCase));
            if (definition is null)
            {
                return Error(workingDirectory, $"Unknown command '{requested}'. Type 'help' for the allowed commands.");
            }

            var aliases = definition.Aliases.Count == 0
                ? string.Empty
                : $"{Environment.NewLine}Aliases: {string.Join(", ", definition.Aliases)}";
            return Result(
                workingDirectory,
                $"Usage: {definition.Usage}{Environment.NewLine}{definition.Description}{aliases}");
        }

        var width = CommandRegistry.Max(command => command.Usage.Length) + 2;
        var commands = string.Join(
            Environment.NewLine,
            CommandRegistry.Select(command => $"  {command.Usage.PadRight(width)}{command.Description}"));
        return Result(
            workingDirectory,
            $"Portable terminal commands:{Environment.NewLine}{commands}{Environment.NewLine}{Environment.NewLine}" +
            "This terminal does not invoke cmd.exe or PowerShell. Pipes, redirects, shell chaining, " +
            "absolute paths, and navigation outside the active project are blocked.");
    }

    private PortableTerminalResult ListDirectory(string workingDirectory, IReadOnlyList<string> arguments)
    {
        if (arguments.Count > 1)
        {
            return Error(workingDirectory, "Usage: ls [relative-directory]");
        }

        try
        {
            var requested = arguments.Count == 0
                ? workingDirectory
                : CombineRelative(workingDirectory, arguments[0]);
            var directory = ResolveWorkspaceDirectory(requested);
            var entries = new DirectoryInfo(directory)
                .EnumerateFileSystemInfos()
                .OrderByDescending(entry => entry is DirectoryInfo)
                .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var output = entries.Length == 0
                ? "(empty)"
                : string.Join(Environment.NewLine, entries.Select(entry =>
                    $"{(entry is DirectoryInfo ? "[DIR] " : "      ")}{entry.Name}{(IsReparsePoint(entry.FullName) ? " [blocked link]" : string.Empty)}"));
            return Result(workingDirectory, output);
        }
        catch (Exception exception) when (IsWorkspaceException(exception))
        {
            return Error(workingDirectory, exception.Message);
        }
    }

    private PortableTerminalResult ChangeDirectory(string workingDirectory, IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 1)
        {
            return Error(workingDirectory, "Usage: cd <relative-directory>");
        }

        try
        {
            var requested = arguments[0] is "/" or "\\"
                ? string.Empty
                : CombineRelative(workingDirectory, arguments[0]);
            ResolveWorkspaceDirectory(requested);
            return Result(NormalizeWorkspaceRelative(requested), string.Empty);
        }
        catch (Exception exception) when (IsWorkspaceException(exception))
        {
            return Error(workingDirectory, exception.Message);
        }
    }

    private PortableTerminalResult CreateDirectory(
        string workingDirectory,
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 1 || string.IsNullOrWhiteSpace(arguments[0]))
        {
            return Error(workingDirectory, "Usage: mkdir <relative-directory>");
        }

        try
        {
            var requested = CombineRelative(workingDirectory, arguments[0]);
            if (string.IsNullOrWhiteSpace(requested))
            {
                return Error(workingDirectory, "The project root already exists.");
            }

            var target = ResolveWorkspacePath(requested);
            if (Directory.Exists(target))
            {
                return Error(workingDirectory, "The requested directory already exists.");
            }

            if (File.Exists(target))
            {
                return Error(workingDirectory, "A file already exists at the requested directory path.");
            }

            Directory.CreateDirectory(target);
            RefuseReparsePath(target);
            return Result(workingDirectory, $"Created directory {DisplayPath(requested)}");
        }
        catch (Exception exception) when (IsWorkspaceException(exception))
        {
            return Error(workingDirectory, exception.Message);
        }
    }

    private static PortableTerminalResult ParseServiceCommand(
        string workingDirectory,
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count is < 1 or > 2 ||
            !Enum.TryParse<PortableTerminalServiceOperation>(arguments[0], ignoreCase: true, out var operation) ||
            !Enum.IsDefined(operation))
        {
            return Error(workingDirectory, "Usage: service <status|start|stop|restart> [web|mariadb|selenium|all]");
        }

        var serviceName = arguments.Count == 2 ? arguments[1] : "all";
        if (!Enum.TryParse<PortableServiceTarget>(serviceName, ignoreCase: true, out var service) ||
            !Enum.IsDefined(service))
        {
            return Error(workingDirectory, "Unknown service. Use web, mariadb, selenium, or all.");
        }

        return new(
            workingDirectory,
            string.Empty,
            ServiceRequest: new PortableTerminalServiceRequest(operation, service));
    }

    private async Task<PortableTerminalResult> RunPhpAsync(
        string workingDirectory,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var php = _moduleVerifier.Verify(ModuleKind.Php, "PHP");
        if (!php.IsVerified)
        {
            return Error(workingDirectory, php.Detail);
        }

        var executable = Path.Combine(php.Installation!.ModuleRootRelativePath, "php.exe");
        return await RunToolAsync("terminal.php", executable, workingDirectory, arguments, cancellationToken);
    }

    private async Task<PortableTerminalResult> RunComposerAsync(
        string workingDirectory,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var php = _moduleVerifier.Verify(ModuleKind.Php, "PHP");
        var composer = _toolInventory.GetRuntime(PortableToolKind.Composer);
        if (!php.IsVerified)
        {
            return Error(workingDirectory, php.Detail);
        }

        if (!composer.IsReady)
        {
            return Error(workingDirectory, composer.Detail);
        }

        var commandArguments = new[] { _paths.Resolve(composer.EntrypointRelativePath) }
            .Concat(arguments)
            .ToArray();
        var environment = CreateCleanEnvironment();
        environment["COMPOSER_HOME"] = _paths.EnsureDirectory(Path.Combine("state", "composer"));
        environment["COMPOSER_CACHE_DIR"] = _paths.EnsureDirectory(Path.Combine("cache", "composer"));
        environment["COMPOSER_NO_INTERACTION"] = "1";
        environment["COMPOSER_ALLOW_SUPERUSER"] = "0";
        return await RunToolAsync(
            "terminal.composer",
            Path.Combine(php.Installation!.ModuleRootRelativePath, "php.exe"),
            workingDirectory,
            commandArguments,
            cancellationToken,
            environment);
    }

    private async Task<PortableTerminalResult> RunPythonAsync(
        string workingDirectory,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var python = _toolInventory.GetRuntime(PortableToolKind.Python);
        if (!python.IsReady)
        {
            return Error(workingDirectory, python.Detail);
        }

        var environment = CreateCleanEnvironment();
        environment["PYTHONNOUSERSITE"] = "1";
        environment["PYTHONUTF8"] = "1";
        environment["PYTHONPATH"] = _paths.EnsureDirectory(Path.Combine("instances", "default", "python", "packages"));
        environment["PIP_CONFIG_FILE"] = "NUL";
        environment["PIP_CACHE_DIR"] = _paths.EnsureDirectory(Path.Combine("cache", "pip"));
        return await RunToolAsync(
            "terminal.python",
            python.EntrypointRelativePath,
            workingDirectory,
            arguments,
            cancellationToken,
            environment);
    }

    private async Task<PortableTerminalResult> RunToolAsync(
        string id,
        string executableRelativePath,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken,
        Dictionary<string, string>? environment = null)
    {
        environment ??= CreateCleanEnvironment();
        var result = await _runner.RunAsync(
            new PortableCommandDefinition(
                id,
                executableRelativePath,
                ToApplicationRelativeWorkingDirectory(workingDirectory),
                arguments,
                environment,
                TimeSpan.FromMinutes(10)),
            cancellationToken);
        var output = string.Join(
            Environment.NewLine,
            new[] { result.StandardOutput.TrimEnd(), result.StandardError.TrimEnd() }
                .Where(value => !string.IsNullOrWhiteSpace(value)));
        if (string.IsNullOrWhiteSpace(output))
        {
            output = result.IsSuccess ? $"Process completed with exit code {result.ExitCode}." : "The process failed without output.";
        }

        return new(workingDirectory, output, IsError: !result.IsSuccess);
    }

    private Dictionary<string, string> CreateCleanEnvironment()
    {
        var directories = new List<string>();
        var php = _moduleVerifier.Verify(ModuleKind.Php, "PHP");
        if (php.IsVerified)
        {
            directories.Add(_paths.Resolve(php.Installation!.ModuleRootRelativePath));
        }

        var python = _toolInventory.GetRuntime(PortableToolKind.Python);
        if (python.IsReady)
        {
            directories.Add(Path.GetDirectoryName(_paths.Resolve(python.EntrypointRelativePath))!);
        }

        return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["PATH"] = string.Join(Path.PathSeparator, directories.Distinct(StringComparer.OrdinalIgnoreCase)),
            ["NO_COLOR"] = "1"
        };
    }

    private string ToApplicationRelativeWorkingDirectory(string workspaceRelativePath) =>
        Path.Combine(WorkspaceRootRelativePath, workspaceRelativePath.Replace('/', Path.DirectorySeparatorChar));

    private string ResolveWorkspaceDirectory(string relativePath)
    {
        var resolved = ResolveWorkspacePath(relativePath);

        if (!Directory.Exists(resolved))
        {
            throw new DirectoryNotFoundException("The requested workspace directory does not exist.");
        }

        RefuseReparsePath(resolved);
        return resolved;
    }

    private string ResolveWorkspacePath(string relativePath)
    {
        relativePath = string.IsNullOrWhiteSpace(relativePath) || relativePath == "."
            ? string.Empty
            : relativePath.Trim();
        if (Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException("Absolute paths are not allowed.", nameof(relativePath));
        }

        var workspaceRoot = WorkspaceRoot;
        var workspacePrefix = workspaceRoot.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var resolved = Path.GetFullPath(Path.Combine(workspaceRoot, relativePath));
        if (!string.Equals(resolved, workspaceRoot, StringComparison.OrdinalIgnoreCase) &&
            !resolved.StartsWith(workspacePrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The path leaves the project workspace.", nameof(relativePath));
        }

        RefuseReparsePath(resolved);
        return resolved;
    }

    private void RefuseReparsePath(string path)
    {
        var workspaceRoot = WorkspaceRoot;
        var current = workspaceRoot;
        RefuseReparsePoint(current);
        var relative = Path.GetRelativePath(workspaceRoot, path);
        if (relative == ".")
        {
            return;
        }

        foreach (var segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);
            if (File.Exists(current) || Directory.Exists(current))
            {
                RefuseReparsePoint(current);
            }
        }
    }

    private static void RefuseReparsePoint(string path)
    {
        if (IsReparsePoint(path))
        {
            throw new IOException("Links and reparse points are not allowed in the managed workspace.");
        }
    }

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;

    private static string CombineRelative(string current, string requested)
    {
        if (Path.IsPathRooted(requested))
        {
            throw new ArgumentException("Absolute paths are not allowed.");
        }

        return NormalizeWorkspaceRelative(Path.Combine(
            current.Replace('/', Path.DirectorySeparatorChar),
            requested.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static string NormalizeWorkspaceRelative(string value)
    {
        var parts = new List<string>();
        foreach (var segment in value.Split(
                     [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar, '/'],
                     StringSplitOptions.RemoveEmptyEntries))
        {
            if (segment == ".")
            {
                continue;
            }

            if (segment == "..")
            {
                if (parts.Count == 0)
                {
                    throw new ArgumentException("The path leaves the project workspace.");
                }

                parts.RemoveAt(parts.Count - 1);
            }
            else
            {
                parts.Add(segment);
            }
        }

        return string.Join('/', parts);
    }

    private static IReadOnlyList<string> Tokenize(string commandLine)
    {
        var tokens = new List<string>();
        var current = new StringBuilder();
        char? quote = null;
        for (var index = 0; index < commandLine.Length; index++)
        {
            var character = commandLine[index];
            if (quote is null && character is '&' or '|' or '<' or '>' or '`')
            {
                throw new ArgumentException("Pipes, redirection and shell chaining are not available in the portable terminal.");
            }

            if (character is '\'' or '"')
            {
                if (quote is null)
                {
                    quote = character;
                    continue;
                }

                if (quote == character)
                {
                    quote = null;
                    continue;
                }
            }

            if (char.IsWhiteSpace(character) && quote is null)
            {
                if (current.Length > 0)
                {
                    tokens.Add(current.ToString());
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
            throw new ArgumentException("The command contains an unterminated quote.");
        }

        if (current.Length > 0)
        {
            tokens.Add(current.ToString());
        }

        return tokens;
    }

    private static string DisplayPath(string workingDirectory) =>
        string.IsNullOrEmpty(workingDirectory) ? "www:/" : $"www:/{workingDirectory}";

    private static PortableTerminalResult Result(string workingDirectory, string output) =>
        new(workingDirectory, output);

    private static PortableTerminalResult Error(string workingDirectory, string output) =>
        new(workingDirectory, output, IsError: true);

    private static bool IsWorkspaceException(Exception exception) =>
        exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException;

}
