using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Workspace;
using PortableDeveloper.Application.Projects;

namespace PortableDeveloper.Infrastructure.Workspace;

public sealed class WorkspaceFileManager : IWorkspaceFileManager
{
    private const int MaximumPageSize = 100;
    private const int MaximumImportSourceCount = 1_000;
    private static readonly IReadOnlyDictionary<string, WorkspaceFileKind> KnownFileNames =
        new Dictionary<string, WorkspaceFileKind>(StringComparer.OrdinalIgnoreCase)
        {
            ["Dockerfile"] = WorkspaceFileKind.Configuration,
            ["Makefile"] = WorkspaceFileKind.Configuration,
            ["composer.json"] = WorkspaceFileKind.Json,
            ["composer.lock"] = WorkspaceFileKind.Json,
            ["package.json"] = WorkspaceFileKind.Json,
            ["package-lock.json"] = WorkspaceFileKind.Json,
            [".env"] = WorkspaceFileKind.Configuration,
            [".gitignore"] = WorkspaceFileKind.Configuration,
            [".htaccess"] = WorkspaceFileKind.Configuration
        };

    private readonly IPortablePathResolver _paths;
    private readonly IProjectContext _projectContext;

    public WorkspaceFileManager(IPortablePathResolver paths, IProjectContext projectContext)
    {
        _paths = paths;
        _projectContext = projectContext;
    }

    public string RootRelativePath => _projectContext.ActiveProject.RootRelativePath;

    private string RootPath => _paths.EnsureDirectory(RootRelativePath);

    public IReadOnlyList<WorkspaceEntry> List(string relativeDirectory)
    {
        var page = ListPage(new WorkspacePageRequest(relativeDirectory, 1, MaximumPageSize));
        if (page.TotalCount <= MaximumPageSize)
        {
            return page.Entries;
        }

        return EnumerateEntries(relativeDirectory)
            .OrderByDescending(entry => entry.IsDirectory)
            .ThenBy(entry => entry.Name, NaturalStringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public WorkspacePage ListPage(WorkspacePageRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.PageNumber < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(request), "The page number must be at least one.");
        }

        if (request.PageSize is < 1 or > MaximumPageSize)
        {
            throw new ArgumentOutOfRangeException(nameof(request), $"The page size must be between 1 and {MaximumPageSize}.");
        }

        var entries = EnumerateEntries(request.RelativeDirectory);
        var ordered = ApplySort(entries, request.SortColumn, request.SortDirection);
        var totalCount = entries.Count;
        var totalPages = totalCount == 0 ? 1 : (int)Math.Ceiling(totalCount / (double)request.PageSize);
        var pageNumber = Math.Min(request.PageNumber, totalPages);
        var page = ordered
            .Skip((pageNumber - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToArray();
        return new WorkspacePage(page, pageNumber, request.PageSize, totalCount);
    }

    public string NormalizeDirectory(string relativeDirectory)
    {
        var resolved = ResolveInsideWorkspace(relativeDirectory, allowRoot: true);
        if (!Directory.Exists(resolved))
        {
            throw new DirectoryNotFoundException("The requested project directory does not exist.");
        }

        EnsurePathHasNoLinks(resolved);
        return NormalizeRelative(Path.GetRelativePath(RootPath, resolved));
    }

    private IReadOnlyList<WorkspaceEntry> EnumerateEntries(string relativeDirectory)
    {
        var directory = ResolveInsideWorkspace(relativeDirectory, allowRoot: true);
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException("The requested project directory does not exist.");
        }

        EnsurePathHasNoLinks(directory);
        var rootPath = RootPath;
        return new DirectoryInfo(directory)
            .EnumerateFileSystemInfos()
            .Select(entry =>
            {
                var isSafe = !IsReparsePoint(entry.FullName);
                var isDirectory = entry is DirectoryInfo;
                return new WorkspaceEntry(
                    entry.Name,
                    NormalizeRelative(Path.GetRelativePath(rootPath, entry.FullName)),
                    isDirectory,
                    isSafe && entry is FileInfo file ? file.Length : null,
                    entry.LastWriteTime,
                    isSafe,
                    Classify(entry.Name, isDirectory));
            })
            .ToArray();
    }

    private static IOrderedEnumerable<WorkspaceEntry> ApplySort(
        IEnumerable<WorkspaceEntry> entries,
        WorkspaceSortColumn column,
        WorkspaceSortDirection direction)
    {
        var descending = direction == WorkspaceSortDirection.Descending;
        var foldersFirst = entries.OrderByDescending(entry => entry.IsDirectory);
        IOrderedEnumerable<WorkspaceEntry> sorted = column switch
        {
            WorkspaceSortColumn.Type => ThenOrder(foldersFirst, entry => entry.FileKind, descending),
            WorkspaceSortColumn.Size => ThenOrder(foldersFirst, entry => entry.SizeBytes ?? -1, descending),
            WorkspaceSortColumn.Modified => ThenOrder(foldersFirst, entry => entry.LastWriteTime, descending),
            _ => descending
                ? foldersFirst.ThenByDescending(entry => entry.Name, NaturalStringComparer.OrdinalIgnoreCase)
                : foldersFirst.ThenBy(entry => entry.Name, NaturalStringComparer.OrdinalIgnoreCase)
        };

        return sorted.ThenBy(entry => entry.Name, NaturalStringComparer.OrdinalIgnoreCase);
    }

    private static IOrderedEnumerable<WorkspaceEntry> ThenOrder<TKey>(
        IOrderedEnumerable<WorkspaceEntry> entries,
        Func<WorkspaceEntry, TKey> selector,
        bool descending) => descending
            ? entries.ThenByDescending(selector)
            : entries.ThenBy(selector);

    private static WorkspaceFileKind Classify(string name, bool isDirectory)
    {
        if (isDirectory)
        {
            return WorkspaceFileKind.Folder;
        }

        if (KnownFileNames.TryGetValue(name, out var known))
        {
            return known;
        }

        return Path.GetExtension(name).ToLowerInvariant() switch
        {
            ".php" or ".phtml" => WorkspaceFileKind.Php,
            ".py" or ".pyw" or ".pyi" => WorkspaceFileKind.Python,
            ".js" or ".jsx" or ".mjs" or ".cjs" or ".ts" or ".tsx" => WorkspaceFileKind.JavaScript,
            ".css" or ".scss" or ".sass" or ".less" => WorkspaceFileKind.StyleSheet,
            ".html" or ".htm" => WorkspaceFileKind.Html,
            ".xml" or ".xsl" or ".xslt" or ".svg" => WorkspaceFileKind.Xml,
            ".json" => WorkspaceFileKind.Json,
            ".yaml" or ".yml" => WorkspaceFileKind.Yaml,
            ".md" or ".markdown" => WorkspaceFileKind.Markdown,
            ".txt" or ".log" => WorkspaceFileKind.Text,
            ".doc" or ".docx" or ".docm" or ".odt" or ".rtf" or ".pdf" => WorkspaceFileKind.Document,
            ".xls" or ".xlsx" or ".xlsm" or ".xlsb" or ".ods" or ".csv" => WorkspaceFileKind.Spreadsheet,
            ".ini" or ".conf" or ".config" or ".toml" or ".properties" => WorkspaceFileKind.Configuration,
            ".png" or ".jpg" or ".jpeg" or ".gif" or ".webp" or ".bmp" or ".ico" => WorkspaceFileKind.Image,
            ".zip" or ".7z" or ".rar" or ".tar" or ".gz" or ".xz" or ".jar" or ".war" or ".ear" => WorkspaceFileKind.Archive,
            ".db" or ".sqlite" or ".sqlite3" => WorkspaceFileKind.Database,
            ".exe" or ".dll" or ".bat" or ".cmd" or ".ps1" => WorkspaceFileKind.Executable,
            _ => WorkspaceFileKind.File
        };
    }

    public void CreateFile(string relativeDirectory, string name)
    {
        var target = ResolveChild(relativeDirectory, name);
        EnsureTargetDoesNotExist(target);
        using var stream = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
    }

    public void CreateDirectory(string relativeDirectory, string name)
    {
        var target = ResolveChild(relativeDirectory, name);
        EnsureTargetDoesNotExist(target);
        Directory.CreateDirectory(target);
    }

    public void Rename(string relativePath, string newName)
    {
        ValidateLeafName(newName);
        var source = ResolveInsideWorkspace(relativePath, allowRoot: false);
        EnsurePathHasNoLinks(source);
        var parent = Path.GetDirectoryName(source)
            ?? throw new IOException("The project item has no parent directory.");
        var destination = ResolveInsideWorkspace(
            NormalizeRelative(Path.GetRelativePath(RootPath, Path.Combine(parent, newName.Trim()))),
            allowRoot: false);
        EnsureTargetDoesNotExist(destination);

        if (Directory.Exists(source))
        {
            Directory.Move(source, destination);
        }
        else if (File.Exists(source))
        {
            File.Move(source, destination);
        }
        else
        {
            throw new FileNotFoundException("The project item does not exist.");
        }
    }

    public bool Copy(
        string sourceRelativePath,
        string destinationRelativeDirectory,
        string copyNameSuffix,
        Func<WorkspaceConflict, WorkspaceConflictDecision> resolveConflict)
    {
        ValidateCopyArguments(copyNameSuffix, resolveConflict);

        var source = ResolveInsideWorkspace(sourceRelativePath, allowRoot: false);
        EnsurePathHasNoLinks(source);
        var sourceIsDirectory = Directory.Exists(source);
        if (!sourceIsDirectory && !File.Exists(source))
        {
            throw new FileNotFoundException("The project item does not exist.");
        }

        var destinationDirectory = ResolveExistingDirectory(destinationRelativeDirectory);
        var sourceName = Path.GetFileName(source);
        var destination = ResolveInsideWorkspace(
            NormalizeRelative(Path.GetRelativePath(RootPath, Path.Combine(destinationDirectory, sourceName))),
            allowRoot: false);

        if (sourceIsDirectory)
        {
            EnsureTreeHasNoLinks(source);
            EnsureDirectoryTargetIsOutsideSource(source, destination);
        }

        return CopyEntry(source, destination, copyNameSuffix, resolveConflict);
    }

    public bool Move(
        string sourceRelativePath,
        string destinationRelativeDirectory,
        string copyNameSuffix,
        Func<WorkspaceConflict, WorkspaceConflictDecision> resolveConflict)
    {
        ValidateCopyArguments(copyNameSuffix, resolveConflict);
        var source = ResolveInsideWorkspace(sourceRelativePath, allowRoot: false);
        EnsurePathHasNoLinks(source);
        var destinationDirectory = ResolveExistingDirectory(destinationRelativeDirectory);
        var destination = ResolveInsideWorkspace(
            NormalizeRelative(Path.GetRelativePath(RootPath, Path.Combine(destinationDirectory, Path.GetFileName(source)))),
            allowRoot: false);

        if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (Directory.Exists(source))
        {
            EnsureTreeHasNoLinks(source);
            EnsureDirectoryTargetIsOutsideSource(source, destination);
        }
        else if (!File.Exists(source))
        {
            throw new FileNotFoundException("The project item does not exist.");
        }

        return MoveEntry(source, destination, copyNameSuffix, resolveConflict);
    }

    public string GetExportPath(string relativePath)
    {
        var source = ResolveInsideWorkspace(relativePath, allowRoot: false);
        EnsurePathHasNoLinks(source);
        if (Directory.Exists(source))
        {
            EnsureTreeHasNoLinks(source);
            return source;
        }

        if (File.Exists(source))
        {
            return source;
        }

        throw new FileNotFoundException("The project item does not exist.");
    }

    public bool TryGetRelativePath(string absolutePath, out string relativePath)
    {
        relativePath = string.Empty;
        if (string.IsNullOrWhiteSpace(absolutePath))
        {
            return false;
        }

        var rootPath = RootPath;
        var rootPrefix = rootPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var resolved = Path.GetFullPath(absolutePath);
        if (!resolved.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase)
            || (!File.Exists(resolved) && !Directory.Exists(resolved)))
        {
            return false;
        }

        EnsurePathHasNoLinks(resolved);
        relativePath = NormalizeRelative(Path.GetRelativePath(rootPath, resolved));
        return relativePath.Length > 0;
    }

    public int Import(
        IReadOnlyList<string> sourcePaths,
        string destinationRelativeDirectory,
        string copyNameSuffix,
        Func<WorkspaceConflict, WorkspaceConflictDecision> resolveConflict)
    {
        ArgumentNullException.ThrowIfNull(sourcePaths);
        if (sourcePaths.Count is < 1 or > MaximumImportSourceCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourcePaths),
                $"Select between 1 and {MaximumImportSourceCount} files or directories.");
        }

        ValidateCopyArguments(copyNameSuffix, resolveConflict);

        var destinationDirectory = ResolveExistingDirectory(destinationRelativeDirectory);
        var importedCount = 0;
        foreach (var requestedSource in sourcePaths.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(requestedSource))
            {
                throw new ArgumentException("An imported path is empty.", nameof(sourcePaths));
            }

            var source = Path.GetFullPath(requestedSource);
            ThrowIfReparsePoint(source);
            var sourceIsDirectory = Directory.Exists(source);
            if (!sourceIsDirectory && !File.Exists(source))
            {
                throw new FileNotFoundException("An imported item does not exist.", source);
            }

            var sourceName = Path.GetFileName(source.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            ValidateLeafName(sourceName);
            var destination = ResolveInsideWorkspace(
                NormalizeRelative(Path.GetRelativePath(RootPath, Path.Combine(destinationDirectory, sourceName))),
                allowRoot: false);
            if (sourceIsDirectory)
            {
                EnsureTreeHasNoLinks(source);
                EnsureDirectoryTargetIsOutsideSource(source, destination);
            }

            if (CopyEntry(source, destination, copyNameSuffix, resolveConflict))
            {
                importedCount++;
            }
        }

        return importedCount;
    }

    public void Delete(string relativePath)
    {
        var target = ResolveInsideWorkspace(relativePath, allowRoot: false);
        EnsurePathHasNoLinks(target);
        if (Directory.Exists(target))
        {
            EnsureTreeHasNoLinks(target);
            Directory.Delete(target, recursive: true);
        }
        else if (File.Exists(target))
        {
            File.Delete(target);
        }
        else
        {
            throw new FileNotFoundException("The project item does not exist.");
        }
    }

    private string ResolveChild(string relativeDirectory, string name)
    {
        ValidateLeafName(name);
        var directory = ResolveInsideWorkspace(relativeDirectory, allowRoot: true);
        EnsurePathHasNoLinks(directory);
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException("The requested project directory does not exist.");
        }

        return ResolveInsideWorkspace(
            NormalizeRelative(Path.GetRelativePath(RootPath, Path.Combine(directory, name.Trim()))),
            allowRoot: false);
    }

    private string ResolveExistingDirectory(string relativeDirectory)
    {
        var directory = ResolveInsideWorkspace(relativeDirectory, allowRoot: true);
        EnsurePathHasNoLinks(directory);
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException("The requested project directory does not exist.");
        }

        return directory;
    }

    private string GetAvailableCopyTarget(
        string destinationDirectory,
        string sourceName,
        string copyNameSuffix,
        bool sourceIsDirectory)
    {
        var directTarget = ResolveInsideWorkspace(
            NormalizeRelative(Path.GetRelativePath(RootPath, Path.Combine(destinationDirectory, sourceName))),
            allowRoot: false);
        if (!File.Exists(directTarget) && !Directory.Exists(directTarget))
        {
            return directTarget;
        }

        var extension = sourceIsDirectory ? string.Empty : Path.GetExtension(sourceName);
        var baseName = extension.Length == 0 ? sourceName : sourceName[..^extension.Length];
        for (var copyNumber = 1; copyNumber <= 10_000; copyNumber++)
        {
            var suffix = copyNumber == 1 ? copyNameSuffix : $"{copyNameSuffix} ({copyNumber})";
            var candidateName = $"{baseName}{suffix}{extension}";
            ValidateLeafName(candidateName);
            var candidate = ResolveInsideWorkspace(
                NormalizeRelative(Path.GetRelativePath(RootPath, Path.Combine(destinationDirectory, candidateName))),
                allowRoot: false);
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new IOException("A free destination name could not be found.");
    }

    private bool CopyEntry(
        string source,
        string destination,
        string copyNameSuffix,
        Func<WorkspaceConflict, WorkspaceConflictDecision> resolveConflict)
    {
        var sourceIsDirectory = Directory.Exists(source);
        if (!sourceIsDirectory && !File.Exists(source))
        {
            throw new FileNotFoundException("The copied item does not exist.", source);
        }

        if (File.Exists(destination) || Directory.Exists(destination))
        {
            ThrowIfReparsePoint(destination);
            var decision = ResolveConflict(resolveConflict, source, destination, sourceIsDirectory);
            switch (decision.Action)
            {
                case WorkspaceConflictAction.Cancel:
                    throw new OperationCanceledException("The file operation was canceled.");
                case WorkspaceConflictAction.Skip:
                    return false;
                case WorkspaceConflictAction.Rename:
                    destination = GetAvailableCopyTarget(
                        Path.GetDirectoryName(destination) ?? RootPath,
                        Path.GetFileName(source),
                        copyNameSuffix,
                        sourceIsDirectory);
                    break;
                case WorkspaceConflictAction.Overwrite:
                    if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }

                    if (sourceIsDirectory && Directory.Exists(destination))
                    {
                        return MergeCopyDirectory(source, destination, copyNameSuffix, resolveConflict);
                    }

                    DeleteExistingTarget(destination);
                    break;
                default:
                    throw new InvalidOperationException("The conflict resolution is not supported.");
            }
        }

        if (sourceIsDirectory)
        {
            CopyDirectory(source, destination);
        }
        else
        {
            File.Copy(source, destination, overwrite: false);
        }

        return true;
    }

    private bool MoveEntry(
        string source,
        string destination,
        string copyNameSuffix,
        Func<WorkspaceConflict, WorkspaceConflictDecision> resolveConflict)
    {
        var sourceIsDirectory = Directory.Exists(source);
        if (!sourceIsDirectory && !File.Exists(source))
        {
            throw new FileNotFoundException("The moved item does not exist.", source);
        }

        if (string.Equals(source, destination, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (File.Exists(destination) || Directory.Exists(destination))
        {
            ThrowIfReparsePoint(destination);
            var decision = ResolveConflict(resolveConflict, source, destination, sourceIsDirectory);
            switch (decision.Action)
            {
                case WorkspaceConflictAction.Cancel:
                    throw new OperationCanceledException("The file operation was canceled.");
                case WorkspaceConflictAction.Skip:
                    return false;
                case WorkspaceConflictAction.Rename:
                    destination = GetAvailableCopyTarget(
                        Path.GetDirectoryName(destination) ?? RootPath,
                        Path.GetFileName(source),
                        copyNameSuffix,
                        sourceIsDirectory);
                    break;
                case WorkspaceConflictAction.Overwrite:
                    if (sourceIsDirectory && Directory.Exists(destination))
                    {
                        return MergeMoveDirectory(source, destination, copyNameSuffix, resolveConflict);
                    }

                    DeleteExistingTarget(destination);
                    break;
                default:
                    throw new InvalidOperationException("The conflict resolution is not supported.");
            }
        }

        if (sourceIsDirectory)
        {
            Directory.Move(source, destination);
        }
        else
        {
            File.Move(source, destination);
        }

        return true;
    }

    private bool MergeCopyDirectory(
        string source,
        string destination,
        string copyNameSuffix,
        Func<WorkspaceConflict, WorkspaceConflictDecision> resolveConflict)
    {
        foreach (var entry in new DirectoryInfo(source).EnumerateFileSystemInfos())
        {
            ThrowIfReparsePoint(entry.FullName);
            CopyEntry(
                entry.FullName,
                Path.Combine(destination, entry.Name),
                copyNameSuffix,
                resolveConflict);
        }

        return true;
    }

    private bool MergeMoveDirectory(
        string source,
        string destination,
        string copyNameSuffix,
        Func<WorkspaceConflict, WorkspaceConflictDecision> resolveConflict)
    {
        foreach (var entry in new DirectoryInfo(source).EnumerateFileSystemInfos().ToArray())
        {
            ThrowIfReparsePoint(entry.FullName);
            MoveEntry(
                entry.FullName,
                Path.Combine(destination, entry.Name),
                copyNameSuffix,
                resolveConflict);
        }

        if (!Directory.EnumerateFileSystemEntries(source).Any())
        {
            Directory.Delete(source);
            return true;
        }

        return false;
    }

    private WorkspaceConflictDecision ResolveConflict(
        Func<WorkspaceConflict, WorkspaceConflictDecision> resolveConflict,
        string source,
        string destination,
        bool sourceIsDirectory) =>
        resolveConflict(new WorkspaceConflict(
            Path.GetFileName(source),
            NormalizeRelative(Path.GetRelativePath(RootPath, destination)),
            sourceIsDirectory));

    private static void ValidateCopyArguments(
        string copyNameSuffix,
        Func<WorkspaceConflict, WorkspaceConflictDecision> resolveConflict)
    {
        ArgumentNullException.ThrowIfNull(resolveConflict);
        if (string.IsNullOrWhiteSpace(copyNameSuffix) || copyNameSuffix.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Enter a valid copy-name suffix.", nameof(copyNameSuffix));
        }
    }

    private static void DeleteExistingTarget(string target)
    {
        if (Directory.Exists(target))
        {
            ThrowIfReparsePoint(target);
            EnsureTreeHasNoLinks(target);
            Directory.Delete(target, recursive: true);
        }
        else if (File.Exists(target))
        {
            ThrowIfReparsePoint(target);
            File.Delete(target);
        }
    }

    private static void EnsureDirectoryTargetIsOutsideSource(string source, string destination)
    {
        var sourcePrefix = source.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (destination.StartsWith(sourcePrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException("A directory cannot be copied or moved into itself.");
        }
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        try
        {
            var pending = new Stack<(string Source, string Destination)>();
            pending.Push((source, destination));
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                foreach (var entry in new DirectoryInfo(current.Source).EnumerateFileSystemInfos())
                {
                    ThrowIfReparsePoint(entry.FullName);
                    var target = Path.Combine(current.Destination, entry.Name);
                    if (entry is DirectoryInfo)
                    {
                        Directory.CreateDirectory(target);
                        pending.Push((entry.FullName, target));
                    }
                    else
                    {
                        File.Copy(entry.FullName, target, overwrite: false);
                    }
                }
            }
        }
        catch
        {
            if (Directory.Exists(destination))
            {
                Directory.Delete(destination, recursive: true);
            }

            throw;
        }
    }

    private string ResolveInsideWorkspace(string relativePath, bool allowRoot)
    {
        relativePath = string.IsNullOrWhiteSpace(relativePath) || relativePath == "."
            ? string.Empty
            : relativePath.Trim();
        if (Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException("Absolute paths are not allowed.", nameof(relativePath));
        }

        var rootPath = RootPath;
        var rootPrefix = rootPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var resolved = Path.GetFullPath(Path.Combine(rootPath, relativePath));
        if (!string.Equals(resolved, rootPath, StringComparison.OrdinalIgnoreCase) &&
            !resolved.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The path leaves the project directory.", nameof(relativePath));
        }

        if (!allowRoot && string.Equals(resolved, rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The project root cannot be changed or deleted.");
        }

        return resolved;
    }

    private void EnsurePathHasNoLinks(string path)
    {
        var rootPath = RootPath;
        var current = rootPath;
        ThrowIfReparsePoint(current);
        var relative = Path.GetRelativePath(rootPath, path);
        if (relative == ".")
        {
            return;
        }

        foreach (var segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);
            if (File.Exists(current) || Directory.Exists(current))
            {
                ThrowIfReparsePoint(current);
            }
        }
    }

    private static void EnsureTreeHasNoLinks(string directory)
    {
        var pending = new Stack<DirectoryInfo>();
        pending.Push(new DirectoryInfo(directory));
        while (pending.Count > 0)
        {
            foreach (var entry in pending.Pop().EnumerateFileSystemInfos())
            {
                ThrowIfReparsePoint(entry.FullName);
                if (entry is DirectoryInfo child)
                {
                    pending.Push(child);
                }
            }
        }
    }

    private static void ThrowIfReparsePoint(string path)
    {
        if (IsReparsePoint(path))
        {
            throw new IOException("Links are displayed but cannot be changed by the project file manager.");
        }
    }

    private static bool IsReparsePoint(string path) =>
        (File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint;

    private static void EnsureTargetDoesNotExist(string path)
    {
        if (File.Exists(path) || Directory.Exists(path))
        {
            throw new IOException("An item with this name already exists.");
        }
    }

    private static void ValidateLeafName(string name)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name) ||
            name is "." or ".." ||
            !string.Equals(Path.GetFileName(name), name, StringComparison.Ordinal) ||
            name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            throw new ArgumentException("Enter a valid file or directory name.", nameof(name));
        }
    }

    private static string NormalizeRelative(string path) =>
        path == "." ? string.Empty : path.Replace(Path.DirectorySeparatorChar, '/');

    private sealed class NaturalStringComparer : IComparer<string>
    {
        public static NaturalStringComparer OrdinalIgnoreCase { get; } = new();

        public int Compare(string? left, string? right)
        {
            left ??= string.Empty;
            right ??= string.Empty;
            var leftIndex = 0;
            var rightIndex = 0;
            while (leftIndex < left.Length && rightIndex < right.Length)
            {
                if (char.IsDigit(left[leftIndex]) && char.IsDigit(right[rightIndex]))
                {
                    var leftStart = leftIndex;
                    var rightStart = rightIndex;
                    while (leftIndex < left.Length && char.IsDigit(left[leftIndex])) leftIndex++;
                    while (rightIndex < right.Length && char.IsDigit(right[rightIndex])) rightIndex++;
                    var leftDigits = left.AsSpan(leftStart, leftIndex - leftStart).TrimStart('0');
                    var rightDigits = right.AsSpan(rightStart, rightIndex - rightStart).TrimStart('0');
                    var lengthComparison = leftDigits.Length.CompareTo(rightDigits.Length);
                    if (lengthComparison != 0) return lengthComparison;
                    var digitComparison = leftDigits.CompareTo(rightDigits, StringComparison.Ordinal);
                    if (digitComparison != 0) return digitComparison;
                    var paddedComparison = (leftIndex - leftStart).CompareTo(rightIndex - rightStart);
                    if (paddedComparison != 0) return paddedComparison;
                    continue;
                }

                var characterComparison = char.ToUpperInvariant(left[leftIndex]).CompareTo(char.ToUpperInvariant(right[rightIndex]));
                if (characterComparison != 0) return characterComparison;
                leftIndex++;
                rightIndex++;
            }

            return left.Length.CompareTo(right.Length);
        }
    }
}
