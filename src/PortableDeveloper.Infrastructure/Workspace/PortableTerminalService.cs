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
    private const string NpmCliRelativePath = "node_modules/npm/bin/npm-cli.js";
    private const int MaximumCommandLength = 4096;
    private const int MaximumArgumentCount = 128;
    private const int MaximumDiscoveryEntries = 1000;
    private const int MaximumGrepMatches = 500;
    private const long MaximumGrepFileBytes = 1024 * 1024;
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
    private static readonly IReadOnlyList<PortableTerminalCommandInfo> CommandRegistry =
    [
        new("help", [], "help [command]", "Show the allowed commands or details for one command.", "Terminal"),
        new("clear", ["cls"], "clear", "Clear terminal output.", "Terminal"),
        new("pwd", [], "pwd", "Show the current project directory.", "Filesystem"),
        new("ls", ["dir"], "ls [relative-directory]", "List files inside the active project.", "Filesystem"),
        new("find", [], "find [relative-directory]", "List project entries recursively, up to 1,000 entries.", "Filesystem"),
        new("grep", [], "grep <text> [relative-directory]", "Find UTF-8 text in project files, up to 500 matching lines.", "Filesystem"),
        new("tree", [], "tree [relative-directory]", "Show a project directory tree, up to 1,000 entries.", "Filesystem"),
        new("cd", [], "cd <relative-directory>", "Change directory inside the active project.", "Filesystem"),
        new("mkdir", [], "mkdir <relative-directory>", "Create a directory inside the active project.", "Filesystem"),
        new("cat", ["type"], "cat <relative-file>", "Show a UTF-8 text file up to 1 MiB.", "Filesystem"),
        new("touch", [], "touch <relative-file>", "Create an empty file or update its modified time.", "Filesystem"),
        new("write", [], "write <relative-file> [text]", "Create a UTF-8 text file without replacing an existing file.", "Filesystem"),
        new("cp", ["copy"], "cp <source-file> <destination-file>", "Copy one file inside the active project.", "Filesystem"),
        new("mv", ["move", "ren"], "mv <source> <destination>", "Move or rename one file or directory.", "Filesystem"),
        new("rm", ["del"], "rm <relative-file>", "Delete one file; recursive deletion is not available.", "Filesystem"),
        new("rmdir", [], "rmdir <relative-directory>", "Delete one empty directory.", "Filesystem"),
        new("echo", [], "echo [text]", "Write text to the terminal without shell expansion.", "Terminal"),
        new("php", [], "php [arguments]", "Run the bundled PHP CLI.", "Tools"),
        new("composer", [], "composer [arguments]", "Run the bundled Composer.", "Tools"),
        new("node", [], "node [arguments]", "Run the bundled Node.js runtime.", "Tools"),
        new("npm", [], "npm run <script> [arguments]", "Run a named npm project script, such as npm run dev.", "Tools"),
        new("python", ["python3"], "python [arguments]", "Run the bundled Python.", "Tools"),
        new("service", [], "service <status|start|stop|restart> [web|mariadb|selenium|all]", "Control Portable Developer services.", "Services")
    ];
    private readonly IModuleInstallationVerifier _moduleVerifier;
    private readonly IPortableToolRuntimeInventory _toolInventory;
    private readonly IPortableCommandRunner _runner;
    private readonly IPortableInteractiveCommandRunner? _interactiveRunner;
    private readonly IPortablePathResolver _paths;
    private readonly IWebProjectCatalog _projects;

    public PortableTerminalService(
        IModuleInstallationVerifier moduleVerifier,
        IPortableToolRuntimeInventory toolInventory,
        IPortableCommandRunner runner,
        IPortablePathResolver paths)
        : this(moduleVerifier, toolInventory, runner, paths, new JsonWebProjectCatalog(paths), null)
    {
    }

    public PortableTerminalService(
        IModuleInstallationVerifier moduleVerifier,
        IPortableToolRuntimeInventory toolInventory,
        IPortableCommandRunner runner,
        IPortablePathResolver paths,
        IWebProjectCatalog projects,
        IPortableInteractiveCommandRunner? interactiveRunner = null)
    {
        _moduleVerifier = moduleVerifier;
        _toolInventory = toolInventory;
        _runner = runner;
        _interactiveRunner = interactiveRunner;
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
            "find" => FindEntries(workingDirectory, arguments),
            "grep" => SearchText(workingDirectory, arguments),
            "tree" => ShowTree(workingDirectory, arguments),
            "cd" => ChangeDirectory(workingDirectory, arguments),
            "mkdir" => CreateDirectory(workingDirectory, arguments),
            "cat" or "type" => ReadFile(workingDirectory, arguments),
            "touch" => TouchFile(workingDirectory, arguments),
            "write" => WriteFile(workingDirectory, arguments),
            "cp" or "copy" => CopyFile(workingDirectory, arguments),
            "mv" or "move" or "ren" => MoveEntry(workingDirectory, arguments),
            "rm" or "del" => RemoveFile(workingDirectory, arguments),
            "rmdir" => RemoveDirectory(workingDirectory, arguments),
            "echo" => Result(workingDirectory, string.Join(' ', arguments)),
            "service" => ParseServiceCommand(workingDirectory, arguments),
            "php" => await RunPhpAsync(workingDirectory, arguments, cancellationToken),
            "composer" => await RunComposerAsync(workingDirectory, arguments, cancellationToken),
            "node" => await RunNodeAsync(workingDirectory, arguments, cancellationToken),
            "npm" => await RunNpmAsync(workingDirectory, arguments, cancellationToken),
            "python" or "python3" => await RunPythonAsync(workingDirectory, arguments, cancellationToken),
            _ => Error(workingDirectory, $"Unknown command '{tokens[0]}'. Type 'help' for the allowed commands.")
        };
    }

    public async Task<PortableTerminalSessionStartResult> TryStartSessionAsync(
        string commandLine,
        string workingDirectory,
        IProgress<PortableProcessOutput> output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(output);
        if (string.IsNullOrWhiteSpace(commandLine) || commandLine.Length > MaximumCommandLength)
        {
            return new(false);
        }

        IReadOnlyList<string> tokens;
        try
        {
            tokens = Tokenize(commandLine);
        }
        catch (ArgumentException exception)
        {
            return new(true, Error: exception.Message);
        }

        if (tokens.Count == 0 || tokens.Count > MaximumArgumentCount)
        {
            return new(false);
        }

        var command = tokens[0].ToLowerInvariant();
        if (command is not ("php" or "composer" or "node" or "npm" or "python" or "python3"))
        {
            return new(false);
        }

        if (_interactiveRunner is null)
        {
            return new(true, Error: "Interactive portable processes are not available.");
        }

        var arguments = tokens.Skip(1).ToArray();
        if (command is "python" or "python3")
        {
            var pythonArgumentError = ValidatePythonArguments(arguments);
            if (pythonArgumentError is not null)
            {
                return new(true, Error: pythonArgumentError);
            }
        }

        if (command == "npm")
        {
            var npmArgumentError = ValidateNpmArguments(arguments);
            if (npmArgumentError is not null)
            {
                return new(true, Error: npmArgumentError);
            }
        }

        var definition = CreateInteractiveDefinition(command, workingDirectory, arguments, out var error);
        if (definition is null)
        {
            return new(true, Error: error);
        }

        try
        {
            var session = await _interactiveRunner.StartAsync(definition, output, cancellationToken);
            return new(true, session);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return new(true, Error: exception.Message);
        }
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

    private PortableTerminalResult FindEntries(string workingDirectory, IReadOnlyList<string> arguments)
    {
        if (arguments.Count > 1)
        {
            return Error(workingDirectory, "Usage: find [relative-directory]");
        }

        try
        {
            var requested = arguments.Count == 0
                ? workingDirectory
                : CombineRelative(workingDirectory, arguments[0]);
            var directory = ResolveWorkspaceDirectory(requested);
            var entries = EnumerateWorkspaceEntries(directory)
                .Take(MaximumDiscoveryEntries + 1)
                .ToArray();
            var truncated = entries.Length > MaximumDiscoveryEntries;
            var displayed = truncated ? entries[..MaximumDiscoveryEntries] : entries;
            var output = displayed.Length == 0
                ? "(empty)"
                : string.Join(Environment.NewLine, displayed.Select(entry =>
                    $"{(entry.IsDirectory ? "[DIR] " : "      ")}{DisplayPath(ToWorkspaceRelativePath(entry.Path))}{(entry.IsBlockedLink ? " [blocked link]" : string.Empty)}"));
            if (truncated)
            {
                output += $"{Environment.NewLine}(output limited to {MaximumDiscoveryEntries} entries)";
            }

            return Result(workingDirectory, output);
        }
        catch (Exception exception) when (IsWorkspaceException(exception))
        {
            return Error(workingDirectory, exception.Message);
        }
    }

    private PortableTerminalResult SearchText(string workingDirectory, IReadOnlyList<string> arguments)
    {
        if (arguments.Count is < 1 or > 2 || string.IsNullOrWhiteSpace(arguments[0]))
        {
            return Error(workingDirectory, "Usage: grep <text> [relative-directory]");
        }

        try
        {
            var requested = arguments.Count == 1
                ? workingDirectory
                : CombineRelative(workingDirectory, arguments[1]);
            var directory = ResolveWorkspaceDirectory(requested);
            var matches = new List<string>();
            foreach (var entry in EnumerateWorkspaceEntries(directory))
            {
                if (matches.Count >= MaximumGrepMatches)
                {
                    break;
                }

                if (entry.IsDirectory || entry.IsBlockedLink)
                {
                    continue;
                }

                var file = new FileInfo(entry.Path);
                if (file.Length > MaximumGrepFileBytes)
                {
                    continue;
                }

                try
                {
                    var text = File.ReadAllText(entry.Path, StrictUtf8);
                    if (text.IndexOf('\0') >= 0)
                    {
                        continue;
                    }

                    var lines = text.Split(["\r\n", "\n", "\r"], StringSplitOptions.None);
                    for (var lineNumber = 0; lineNumber < lines.Length && matches.Count < MaximumGrepMatches; lineNumber++)
                    {
                        if (!lines[lineNumber].Contains(arguments[0], StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var line = lines[lineNumber];
                        if (line.Length > 500)
                        {
                            line = line[..500] + "…";
                        }

                        matches.Add($"{DisplayPath(ToWorkspaceRelativePath(entry.Path))}:{lineNumber + 1}: {line}");
                    }
                }
                catch (DecoderFallbackException)
                {
                    // Non-UTF-8 files are outside this command's intentionally narrow text scope.
                }
            }

            var output = matches.Count == 0 ? "(no matches)" : string.Join(Environment.NewLine, matches);
            if (matches.Count == MaximumGrepMatches)
            {
                output += $"{Environment.NewLine}(output limited to {MaximumGrepMatches} matching lines)";
            }

            return Result(workingDirectory, output);
        }
        catch (Exception exception) when (IsWorkspaceException(exception))
        {
            return Error(workingDirectory, exception.Message);
        }
    }

    private PortableTerminalResult ShowTree(string workingDirectory, IReadOnlyList<string> arguments)
    {
        if (arguments.Count > 1)
        {
            return Error(workingDirectory, "Usage: tree [relative-directory]");
        }

        try
        {
            var requested = arguments.Count == 0
                ? workingDirectory
                : CombineRelative(workingDirectory, arguments[0]);
            var directory = ResolveWorkspaceDirectory(requested);
            var entries = EnumerateWorkspaceEntries(directory)
                .Take(MaximumDiscoveryEntries + 1)
                .ToArray();
            var truncated = entries.Length > MaximumDiscoveryEntries;
            var displayed = truncated ? entries[..MaximumDiscoveryEntries] : entries;
            var lines = new List<string> { DisplayPath(requested) };
            lines.AddRange(displayed.Select(entry =>
                $"{new string(' ', entry.Depth * 2)}{(entry.IsDirectory ? "[DIR] " : string.Empty)}{Path.GetFileName(entry.Path)}{(entry.IsBlockedLink ? " [blocked link]" : string.Empty)}"));
            if (truncated)
            {
                lines.Add($"(output limited to {MaximumDiscoveryEntries} entries)");
            }

            return Result(workingDirectory, string.Join(Environment.NewLine, lines));
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

    private PortableTerminalResult ReadFile(
        string workingDirectory,
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 1)
        {
            return Error(workingDirectory, "Usage: cat <relative-file>");
        }

        try
        {
            var path = ResolveWorkspaceFile(CombineRelative(workingDirectory, arguments[0]));
            if (new FileInfo(path).Length > 1024 * 1024)
            {
                return Error(workingDirectory, "The file is larger than the 1 MiB terminal display limit.");
            }

            return Result(workingDirectory, File.ReadAllText(path, Encoding.UTF8));
        }
        catch (Exception exception) when (IsWorkspaceException(exception))
        {
            return Error(workingDirectory, exception.Message);
        }
    }

    private PortableTerminalResult TouchFile(
        string workingDirectory,
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 1 || string.IsNullOrWhiteSpace(arguments[0]))
        {
            return Error(workingDirectory, "Usage: touch <relative-file>");
        }

        try
        {
            var requested = CombineRelative(workingDirectory, arguments[0]);
            var path = ResolveWorkspacePath(requested);
            EnsureExistingParent(path);
            if (Directory.Exists(path))
            {
                return Error(workingDirectory, "A directory already exists at the requested file path.");
            }

            if (File.Exists(path))
            {
                File.SetLastWriteTimeUtc(path, DateTime.UtcNow);
            }
            else
            {
                using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            }

            return Result(workingDirectory, $"Touched {DisplayPath(requested)}");
        }
        catch (Exception exception) when (IsWorkspaceException(exception))
        {
            return Error(workingDirectory, exception.Message);
        }
    }

    private PortableTerminalResult WriteFile(
        string workingDirectory,
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count == 0 || string.IsNullOrWhiteSpace(arguments[0]))
        {
            return Error(workingDirectory, "Usage: write <relative-file> [text]");
        }

        try
        {
            var requested = CombineRelative(workingDirectory, arguments[0]);
            var path = ResolveWorkspacePath(requested);
            EnsureExistingParent(path);
            if (Directory.Exists(path))
            {
                return Error(workingDirectory, "A directory already exists at the requested file path.");
            }

            if (File.Exists(path))
            {
                return Error(workingDirectory, "The destination already exists.");
            }

            using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            writer.Write(string.Join(' ', arguments.Skip(1)));
            return Result(workingDirectory, $"Wrote {DisplayPath(requested)}");
        }
        catch (Exception exception) when (IsWorkspaceException(exception))
        {
            return Error(workingDirectory, exception.Message);
        }
    }

    private PortableTerminalResult CopyFile(
        string workingDirectory,
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 2)
        {
            return Error(workingDirectory, "Usage: cp <source-file> <destination-file>");
        }

        try
        {
            var source = ResolveWorkspaceFile(CombineRelative(workingDirectory, arguments[0]));
            var destinationRelative = CombineRelative(workingDirectory, arguments[1]);
            var destination = ResolveWorkspacePath(destinationRelative);
            EnsureNewDestination(destination);
            File.Copy(source, destination, overwrite: false);
            return Result(workingDirectory, $"Copied to {DisplayPath(destinationRelative)}");
        }
        catch (Exception exception) when (IsWorkspaceException(exception))
        {
            return Error(workingDirectory, exception.Message);
        }
    }

    private PortableTerminalResult MoveEntry(
        string workingDirectory,
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 2)
        {
            return Error(workingDirectory, "Usage: mv <source> <destination>");
        }

        try
        {
            var sourceRelative = CombineRelative(workingDirectory, arguments[0]);
            var destinationRelative = CombineRelative(workingDirectory, arguments[1]);
            var source = ResolveWorkspacePath(sourceRelative);
            var destination = ResolveWorkspacePath(destinationRelative);
            EnsureNewDestination(destination);
            if (File.Exists(source))
            {
                RefuseReparsePath(source);
                File.Move(source, destination);
            }
            else if (Directory.Exists(source))
            {
                if (string.IsNullOrEmpty(sourceRelative))
                {
                    return Error(workingDirectory, "The project root cannot be moved.");
                }

                RefuseReparsePath(source);
                Directory.Move(source, destination);
            }
            else
            {
                return Error(workingDirectory, "The source file or directory does not exist.");
            }

            return Result(workingDirectory, $"Moved to {DisplayPath(destinationRelative)}");
        }
        catch (Exception exception) when (IsWorkspaceException(exception))
        {
            return Error(workingDirectory, exception.Message);
        }
    }

    private PortableTerminalResult RemoveFile(
        string workingDirectory,
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 1)
        {
            return Error(workingDirectory, "Usage: rm <relative-file>");
        }

        try
        {
            var requested = CombineRelative(workingDirectory, arguments[0]);
            var path = ResolveWorkspaceFile(requested);
            File.Delete(path);
            return Result(workingDirectory, $"Deleted {DisplayPath(requested)}");
        }
        catch (Exception exception) when (IsWorkspaceException(exception))
        {
            return Error(workingDirectory, exception.Message);
        }
    }

    private PortableTerminalResult RemoveDirectory(
        string workingDirectory,
        IReadOnlyList<string> arguments)
    {
        if (arguments.Count != 1)
        {
            return Error(workingDirectory, "Usage: rmdir <relative-directory>");
        }

        try
        {
            var requested = CombineRelative(workingDirectory, arguments[0]);
            if (string.IsNullOrEmpty(requested))
            {
                return Error(workingDirectory, "The project root cannot be deleted.");
            }

            var path = ResolveWorkspaceDirectory(requested);
            Directory.Delete(path, recursive: false);
            return Result(workingDirectory, $"Deleted {DisplayPath(requested)}");
        }
        catch (Exception exception) when (IsWorkspaceException(exception))
        {
            return Error(workingDirectory, exception.Message);
        }
    }

    private string ResolveWorkspaceFile(string relativePath)
    {
        var path = ResolveWorkspacePath(relativePath);
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("The requested workspace file does not exist.");
        }

        RefuseReparsePath(path);
        return path;
    }

    private void EnsureNewDestination(string destination)
    {
        EnsureExistingParent(destination);
        if (File.Exists(destination) || Directory.Exists(destination))
        {
            throw new IOException("The destination already exists.");
        }
    }

    private void EnsureExistingParent(string path)
    {
        var parent = Path.GetDirectoryName(path)
            ?? throw new ArgumentException("The destination has no parent directory.");
        if (!Directory.Exists(parent))
        {
            throw new DirectoryNotFoundException("The destination directory does not exist.");
        }

        RefuseReparsePath(parent);
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
        var pythonArgumentError = ValidatePythonArguments(arguments);
        if (pythonArgumentError is not null)
        {
            return Error(workingDirectory, pythonArgumentError);
        }

        var python = _toolInventory.GetRuntime(PortableToolKind.Python);
        if (!python.IsReady)
        {
            return Error(workingDirectory, python.Detail);
        }

        var environment = CreateCleanEnvironment();
        environment["PYTHONNOUSERSITE"] = "1";
        environment["PYTHONUTF8"] = "1";
        environment["PYTHONIOENCODING"] = "utf-8";
        environment["PYTHONUNBUFFERED"] = "1";
        environment["PYTHONPATH"] = _paths.EnsureDirectory(Path.Combine("instances", "default", "python", "packages"));
        environment["PIP_CONFIG_FILE"] = "NUL";
        environment["PIP_NO_CACHE_DIR"] = "1";
        return await RunToolAsync(
            "terminal.python",
            python.EntrypointRelativePath,
            workingDirectory,
            arguments,
            cancellationToken,
            environment);
    }

    private async Task<PortableTerminalResult> RunNodeAsync(
        string workingDirectory,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var node = _toolInventory.GetRuntime(PortableToolKind.Node);
        if (!node.IsReady)
        {
            return Error(workingDirectory, node.Detail);
        }

        return await RunToolAsync(
            "terminal.node",
            node.EntrypointRelativePath,
            workingDirectory,
            arguments,
            cancellationToken);
    }

    private async Task<PortableTerminalResult> RunNpmAsync(
        string workingDirectory,
        IReadOnlyList<string> arguments,
        CancellationToken cancellationToken)
    {
        var argumentError = ValidateNpmArguments(arguments);
        if (argumentError is not null)
        {
            return Error(workingDirectory, argumentError);
        }

        var definition = CreateNpmDefinition("terminal.npm", workingDirectory, arguments, TimeSpan.FromMinutes(10), out var error);
        if (definition is null)
        {
            return Error(workingDirectory, error);
        }

        var result = await _runner.RunAsync(definition, cancellationToken);
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

    private PortableCommandDefinition? CreateInteractiveDefinition(
        string command,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        out string error)
    {
        error = string.Empty;
        var environment = CreateCleanEnvironment();
        string executable;
        IReadOnlyList<string> commandArguments = arguments;
        string id;

        switch (command)
        {
            case "php":
                {
                    var php = _moduleVerifier.Verify(ModuleKind.Php, "PHP");
                    if (!php.IsVerified)
                    {
                        error = php.Detail;
                        return null;
                    }

                    id = "terminal.php.interactive";
                    executable = Path.Combine(php.Installation!.ModuleRootRelativePath, "php.exe");
                    break;
                }
            case "composer":
                {
                    var php = _moduleVerifier.Verify(ModuleKind.Php, "PHP");
                    var composer = _toolInventory.GetRuntime(PortableToolKind.Composer);
                    if (!php.IsVerified)
                    {
                        error = php.Detail;
                        return null;
                    }

                    if (!composer.IsReady)
                    {
                        error = composer.Detail;
                        return null;
                    }

                    id = "terminal.composer.interactive";
                    executable = Path.Combine(php.Installation!.ModuleRootRelativePath, "php.exe");
                    commandArguments = new[] { _paths.Resolve(composer.EntrypointRelativePath) }
                        .Concat(arguments)
                        .ToArray();
                    environment["COMPOSER_HOME"] = _paths.EnsureDirectory(Path.Combine("state", "composer"));
                    environment["COMPOSER_CACHE_DIR"] = _paths.EnsureDirectory(Path.Combine("cache", "composer"));
                    environment["COMPOSER_NO_INTERACTION"] = "1";
                    environment["COMPOSER_ALLOW_SUPERUSER"] = "0";
                    break;
                }
            case "node":
                {
                    var node = _toolInventory.GetRuntime(PortableToolKind.Node);
                    if (!node.IsReady)
                    {
                        error = node.Detail;
                        return null;
                    }

                    id = "terminal.node.interactive";
                    executable = node.EntrypointRelativePath;
                    break;
                }
            case "npm":
                {
                    var npmDefinition = CreateNpmDefinition(
                        "terminal.npm.interactive",
                        workingDirectory,
                        arguments,
                        TimeSpan.FromHours(4),
                        out error);
                    return npmDefinition;
                }
            default:
                {
                    var python = _toolInventory.GetRuntime(PortableToolKind.Python);
                    if (!python.IsReady)
                    {
                        error = python.Detail;
                        return null;
                    }

                    id = "terminal.python.interactive";
                    executable = python.EntrypointRelativePath;
                    environment["PYTHONNOUSERSITE"] = "1";
                    environment["PYTHONUTF8"] = "1";
                    environment["PYTHONIOENCODING"] = "utf-8";
                    environment["PYTHONUNBUFFERED"] = "1";
                    environment["PYTHONPATH"] = _paths.EnsureDirectory(Path.Combine("instances", "default", "python", "packages"));
                    environment["PIP_CONFIG_FILE"] = "NUL";
                    environment["PIP_NO_CACHE_DIR"] = "1";
                    break;
                }
        }

        return new PortableCommandDefinition(
            id,
            executable,
            ToApplicationRelativeWorkingDirectory(workingDirectory),
            commandArguments,
            environment,
            TimeSpan.FromHours(4));
    }

    private PortableCommandDefinition? CreateNpmDefinition(
        string id,
        string workingDirectory,
        IReadOnlyList<string> arguments,
        TimeSpan timeout,
        out string error)
    {
        error = string.Empty;
        var node = _toolInventory.GetRuntime(PortableToolKind.Node);
        if (!node.IsReady)
        {
            error = node.Detail;
            return null;
        }

        var nodeExecutable = _paths.Resolve(node.EntrypointRelativePath);
        var npmCli = Path.Combine(Path.GetDirectoryName(nodeExecutable)!, NpmCliRelativePath);
        if (!File.Exists(npmCli) || IsReparsePoint(npmCli))
        {
            error = "The portable Node.js npm CLI is missing or unsafe.";
            return null;
        }

        var environment = CreateCleanEnvironment();
        environment["NPM_CONFIG_CACHE"] = _paths.EnsureDirectory(Path.Combine("cache", "npm"));
        environment["NPM_CONFIG_USERCONFIG"] = Path.Combine(_paths.EnsureDirectory(Path.Combine("state", "npm")), "npmrc");
        environment["NPM_CONFIG_AUDIT"] = "false";
        environment["NPM_CONFIG_FUND"] = "false";
        environment["NPM_CONFIG_UPDATE_NOTIFIER"] = "false";
        environment["NPM_CONFIG_IGNORE_SCRIPTS"] = "true";
        environment["NPM_CONFIG_YES"] = "true";
        return new PortableCommandDefinition(
            id,
            node.EntrypointRelativePath,
            ToApplicationRelativeWorkingDirectory(workingDirectory),
            [npmCli, .. arguments],
            environment,
            timeout);
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

        var node = _toolInventory.GetRuntime(PortableToolKind.Node);
        if (node.IsReady)
        {
            directories.Add(Path.GetDirectoryName(_paths.Resolve(node.EntrypointRelativePath))!);
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

    private IEnumerable<WorkspaceEntry> EnumerateWorkspaceEntries(string directory, int depth = 1)
    {
        var entries = new DirectoryInfo(directory)
            .EnumerateFileSystemInfos()
            .OrderByDescending(entry => entry is DirectoryInfo)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        foreach (var entry in entries)
        {
            var isDirectory = entry is DirectoryInfo;
            var isBlockedLink = IsReparsePoint(entry.FullName);
            yield return new WorkspaceEntry(entry.FullName, isDirectory, isBlockedLink, depth);
            if (isDirectory && !isBlockedLink)
            {
                foreach (var child in EnumerateWorkspaceEntries(entry.FullName, depth + 1))
                {
                    yield return child;
                }
            }
        }
    }

    private string ToWorkspaceRelativePath(string path) =>
        NormalizeWorkspaceRelative(Path.GetRelativePath(WorkspaceRoot, path));

    private static string? ValidatePythonArguments(IReadOnlyList<string> arguments)
    {
        for (var index = 0; index < arguments.Count - 1; index++)
        {
            if (!string.Equals(arguments[index], "-m", StringComparison.Ordinal))
            {
                continue;
            }

            var module = arguments[index + 1];
            if (string.Equals(module, "ensurepip", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(module, "pip", StringComparison.OrdinalIgnoreCase) ||
                module.StartsWith("pip.", StringComparison.OrdinalIgnoreCase))
            {
                return "Direct pip changes are not available in the portable terminal. Use the Python Packages page so packages remain in the portable project store.";
            }
        }

        return null;
    }

    private static string? ValidateNpmArguments(IReadOnlyList<string> arguments)
    {
        if (arguments.Count < 2 ||
            (arguments[0] is not "run" and not "run-script") ||
            !IsValidNpmScriptName(arguments[1]))
        {
            return "Use npm run <script> [arguments], for example npm run dev. Install and remove packages on the Node.js Packages page.";
        }

        return null;
    }

    private static bool IsValidNpmScriptName(string value) =>
        value.Length is > 0 and <= 128 &&
        char.IsLetterOrDigit(value[0]) &&
        value.All(character => char.IsLetterOrDigit(character) || character is '.' or '_' or '-' or ':');

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

    private sealed record WorkspaceEntry(string Path, bool IsDirectory, bool IsBlockedLink, int Depth);

}
