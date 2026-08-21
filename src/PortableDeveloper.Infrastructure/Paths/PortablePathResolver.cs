using PortableDeveloper.Application.Abstractions;

namespace PortableDeveloper.Infrastructure.Paths;

/// <summary>
/// Resolves portable application paths and refuses paths that escape its root.
/// </summary>
public sealed class PortablePathResolver : IPortablePathResolver
{
    private readonly string _rootWithSeparator;

    public PortablePathResolver(string rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath))
        {
            throw new ArgumentException("Portable application root must be provided.", nameof(rootPath));
        }

        RootPath = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        _rootWithSeparator = RootPath + Path.DirectorySeparatorChar;
    }

    public string RootPath { get; }

    public string Resolve(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("A relative path must be provided.", nameof(relativePath));
        }

        if (Path.IsPathRooted(relativePath))
        {
            throw new ArgumentException("Absolute paths are not allowed in portable configuration.", nameof(relativePath));
        }

        var resolvedPath = Path.GetFullPath(Path.Combine(RootPath, relativePath));
        if (!resolvedPath.StartsWith(_rootWithSeparator, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(resolvedPath, RootPath, StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("The path escapes the portable application root.", nameof(relativePath));
        }

        return resolvedPath;
    }

    public string EnsureDirectory(string relativePath)
    {
        var directoryPath = Resolve(relativePath);
        Directory.CreateDirectory(directoryPath);
        return directoryPath;
    }
}
