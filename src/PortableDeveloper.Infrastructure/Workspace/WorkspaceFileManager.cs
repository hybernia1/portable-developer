using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Workspace;

namespace PortableDeveloper.Infrastructure.Workspace;

public sealed class WorkspaceFileManager : IWorkspaceFileManager
{
    private readonly IPortablePathResolver _paths;
    private readonly string _rootPath;
    private readonly string _rootPrefix;

    public WorkspaceFileManager(IPortablePathResolver paths)
    {
        _paths = paths;
        _rootPath = paths.EnsureDirectory(RootRelativePath);
        _rootPrefix = _rootPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        RefuseReparsePoint(_rootPath);
    }

    public string RootRelativePath => Path.Combine("instances", "default", "www");

    public IReadOnlyList<WorkspaceEntry> List(string relativeDirectory)
    {
        var directory = ResolveInsideWorkspace(relativeDirectory, allowRoot: true);
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException("The requested workspace directory does not exist.");
        }

        RefuseReparsePath(directory);
        return new DirectoryInfo(directory)
            .EnumerateFileSystemInfos()
            .OrderByDescending(entry => entry is DirectoryInfo)
            .ThenBy(entry => entry.Name, StringComparer.OrdinalIgnoreCase)
            .Select(entry =>
            {
                var isSafe = !IsReparsePoint(entry.FullName);
                return new WorkspaceEntry(
                    entry.Name,
                    NormalizeRelative(Path.GetRelativePath(_rootPath, entry.FullName)),
                    entry is DirectoryInfo,
                    isSafe && entry is FileInfo file ? file.Length : null,
                    entry.LastWriteTime,
                    isSafe);
            })
            .ToArray();
    }

    public void CreateFile(string relativeDirectory, string name)
    {
        var target = ResolveChild(relativeDirectory, name);
        if (File.Exists(target) || Directory.Exists(target))
        {
            throw new IOException("An item with this name already exists.");
        }

        using var stream = new FileStream(target, FileMode.CreateNew, FileAccess.Write, FileShare.Read);
    }

    public void CreateDirectory(string relativeDirectory, string name)
    {
        var target = ResolveChild(relativeDirectory, name);
        if (File.Exists(target) || Directory.Exists(target))
        {
            throw new IOException("An item with this name already exists.");
        }

        Directory.CreateDirectory(target);
    }

    public void Rename(string relativePath, string newName)
    {
        ValidateLeafName(newName);
        var source = ResolveInsideWorkspace(relativePath, allowRoot: false);
        RefuseReparsePath(source);
        var parent = Path.GetDirectoryName(source)
            ?? throw new IOException("The workspace item has no parent directory.");
        var destination = ResolveInsideWorkspace(
            NormalizeRelative(Path.GetRelativePath(_rootPath, Path.Combine(parent, newName.Trim()))),
            allowRoot: false);
        if (File.Exists(destination) || Directory.Exists(destination))
        {
            throw new IOException("An item with this name already exists.");
        }

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
            throw new FileNotFoundException("The workspace item does not exist.");
        }
    }

    public void Delete(string relativePath)
    {
        var target = ResolveInsideWorkspace(relativePath, allowRoot: false);
        RefuseReparsePath(target);
        if (Directory.Exists(target))
        {
            RefuseReparseDescendants(target);
            Directory.Delete(target, recursive: true);
        }
        else if (File.Exists(target))
        {
            File.Delete(target);
        }
        else
        {
            throw new FileNotFoundException("The workspace item does not exist.");
        }
    }

    private string ResolveChild(string relativeDirectory, string name)
    {
        ValidateLeafName(name);
        var directory = ResolveInsideWorkspace(relativeDirectory, allowRoot: true);
        RefuseReparsePath(directory);
        if (!Directory.Exists(directory))
        {
            throw new DirectoryNotFoundException("The requested workspace directory does not exist.");
        }

        return ResolveInsideWorkspace(
            NormalizeRelative(Path.GetRelativePath(_rootPath, Path.Combine(directory, name.Trim()))),
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

        var resolved = Path.GetFullPath(Path.Combine(_rootPath, relativePath));
        if (!string.Equals(resolved, _rootPath, StringComparison.OrdinalIgnoreCase) &&
            !resolved.StartsWith(_rootPrefix, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The path leaves the project workspace.", nameof(relativePath));
        }

        if (!allowRoot && string.Equals(resolved, _rootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("The workspace root is protected.");
        }

        return resolved;
    }

    private void RefuseReparsePath(string path)
    {
        var current = _rootPath;
        RefuseReparsePoint(current);
        var relative = Path.GetRelativePath(_rootPath, path);
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

    private static void RefuseReparseDescendants(string directory)
    {
        var pending = new Stack<DirectoryInfo>();
        pending.Push(new DirectoryInfo(directory));
        while (pending.Count > 0)
        {
            foreach (var entry in pending.Pop().EnumerateFileSystemInfos())
            {
                RefuseReparsePoint(entry.FullName);
                if (entry is DirectoryInfo child)
                {
                    pending.Push(child);
                }
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
}
