using System.Text;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Application.ProjectTools;
using PortableDeveloper.Application.Workspace;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Infrastructure.Workspace;

public sealed class PortableTerminalService : IPortableTerminalService
{
    private const int MaximumCommandLength = 4096;
    private const int MaximumArgumentCount = 128;
    private readonly IModuleInstallationVerifier _moduleVerifier;
    private readonly IPortableToolRuntimeInventory _toolInventory;
    private readonly IPortableCommandRunner _runner;
    private readonly IPortablePathResolver _paths;
    private readonly WorkspaceFileManager _workspace;

    public PortableTerminalService(
        IModuleInstallationVerifier moduleVerifier,
        IPortableToolRuntimeInventory toolInventory,
        IPortableCommandRunner runner,
        IPortablePathResolver paths)
    {
        _moduleVerifier = moduleVerifier;
        _toolInventory = toolInventory;
        _runner = runner;
        _paths = paths;
        _workspace = new WorkspaceFileManager(paths);
    }

    public string InitialWorkingDirectory => string.Empty;

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
            "help" => Result(workingDirectory, HelpText),
            "clear" or "cls" => new(workingDirectory, string.Empty, ClearScreen: true),
            "pwd" => Result(workingDirectory, DisplayPath(workingDirectory)),
            "ls" or "dir" => ListDirectory(workingDirectory, arguments),
            "cd" => ChangeDirectory(workingDirectory, arguments),
            "service" => ParseServiceCommand(workingDirectory, arguments),
            "php" => await RunPhpAsync(workingDirectory, arguments, cancellationToken),
            "composer" => await RunComposerAsync(workingDirectory, arguments, cancellationToken),
            "python" or "python3" => await RunPythonAsync(workingDirectory, arguments, cancellationToken),
            _ => Error(workingDirectory, $"Unknown command '{tokens[0]}'. Type 'help' for the allowed commands.")
        };
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
            var entries = _workspace.List(requested);
            var output = entries.Count == 0
                ? "(empty)"
                : string.Join(Environment.NewLine, entries.Select(entry =>
                    $"{(entry.IsDirectory ? "[DIR] " : "      ")}{entry.Name}{(entry.IsSafe ? string.Empty : " [blocked link]")}"));
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
            _workspace.List(requested);
            return Result(NormalizeWorkspaceRelative(requested), string.Empty);
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
        Path.Combine(_workspace.RootRelativePath, workspaceRelativePath.Replace('/', Path.DirectorySeparatorChar));

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

    private const string HelpText = """
        Portable terminal commands:
          help                              show this help
          clear                             clear terminal output
          pwd                               show current project directory
          ls [directory]                    list files inside www
          cd <directory>                    change directory inside www
          php [arguments]                   run bundled PHP CLI
          composer [arguments]              run bundled Composer
          python [arguments]                run bundled Python
          service status [name]             show service status
          service start|stop|restart <name> control web, mariadb, selenium, or all

        This terminal does not invoke cmd.exe or PowerShell. Pipes, redirects,
        shell chaining, absolute paths, and navigation outside www are blocked.
        """;
}
