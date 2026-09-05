using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Workspace;
using PortableDeveloper.Application.Projects;

namespace PortableDeveloper.Infrastructure.Workspace;

public sealed class WorkspaceFileManager : IWorkspaceFileManager
{
    private const int MaximumPageSize = 100;
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
