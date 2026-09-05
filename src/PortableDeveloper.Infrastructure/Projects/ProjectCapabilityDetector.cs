using System.Text;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Projects;

namespace PortableDeveloper.Infrastructure.Projects;

public sealed class ProjectCapabilityDetector : IProjectCapabilityDetector
{
    private const int MaximumEntries = 2_000;
    private const int MaximumDepth = 6;
    private const int MaximumContentFiles = 64;
    private const int MaximumContentBytes = 128 * 1024;
    private static readonly HashSet<string> ExcludedDirectories = new(StringComparer.OrdinalIgnoreCase)
    {
        ".git", ".idea", ".vs", ".vscode", ".venv", "node_modules", "vendor", "packages", "dist", "build"
    };

    private readonly ProjectRootPathValidator _rootValidator;

    public ProjectCapabilityDetector(IPortablePathResolver paths)
    {
        _rootValidator = new ProjectRootPathValidator(paths);
    }

    public Task<ProjectCapabilitySnapshot> DetectAsync(
        PortableProject project,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var evidence = Enum.GetValues<ProjectCapabilityKind>()
            .ToDictionary(kind => kind, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        if (project.Web is not null)
        {
            evidence[ProjectCapabilityKind.Web].Add("project web configuration");
        }

        var root = _rootValidator.ResolveManagedRoot(project);
        if (!Directory.Exists(root))
        {
            return Task.FromResult(ToSnapshot(project.Id, evidence));
        }

        var pending = new Stack<(string Path, string RelativePath, int Depth)>();
        pending.Push((root, string.Empty, 0));
        var entryCount = 0;
        var contentCount = 0;
        while (pending.Count > 0 && entryCount < MaximumEntries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = pending.Pop();
            foreach (var path in Directory.EnumerateFileSystemEntries(directory.Path))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (++entryCount > MaximumEntries)
                {
                    break;
                }

                var attributes = File.GetAttributes(path);
                if ((attributes & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
                {
                    continue;
                }

                var name = Path.GetFileName(path);
                var relative = string.IsNullOrEmpty(directory.RelativePath)
                    ? name
                    : Path.Combine(directory.RelativePath, name);
                if ((attributes & FileAttributes.Directory) != 0)
                {
                    if (directory.Depth < MaximumDepth && !ExcludedDirectories.Contains(name))
                    {
                        pending.Push((path, relative, directory.Depth + 1));
                    }

                    if (string.Equals(name, "public", StringComparison.OrdinalIgnoreCase))
                    {
                        evidence[ProjectCapabilityKind.Web].Add(relative);
                    }

                    continue;
                }

                DetectFromName(relative, evidence);
                if (contentCount < MaximumContentFiles && ShouldInspectContent(name, new FileInfo(path).Length))
                {
                    contentCount++;
                    DetectFromContent(relative, ReadSmallUtf8(path), evidence);
                }
            }
        }

        return Task.FromResult(ToSnapshot(project.Id, evidence));
    }

    private static void DetectFromName(
        string relativePath,
        IReadOnlyDictionary<ProjectCapabilityKind, HashSet<string>> evidence)
    {
        var name = Path.GetFileName(relativePath);
        var extension = Path.GetExtension(name);
        if (extension.Equals(".php", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("composer.json", StringComparison.OrdinalIgnoreCase))
        {
            evidence[ProjectCapabilityKind.Php].Add(relativePath);
            evidence[ProjectCapabilityKind.Web].Add(relativePath);
        }

        if (name.Equals("package.json", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".js", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".mjs", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".cjs", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".ts", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".tsx", StringComparison.OrdinalIgnoreCase))
        {
            evidence[ProjectCapabilityKind.NodeJs].Add(relativePath);
        }

        if (name.Equals("requirements.txt", StringComparison.OrdinalIgnoreCase) ||
            name.Equals("pyproject.toml", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".py", StringComparison.OrdinalIgnoreCase))
        {
            evidence[ProjectCapabilityKind.Python].Add(relativePath);
        }

        if (extension.Equals(".html", StringComparison.OrdinalIgnoreCase) ||
            extension.Equals(".htm", StringComparison.OrdinalIgnoreCase))
        {
            evidence[ProjectCapabilityKind.Web].Add(relativePath);
        }
    }

    private static void DetectFromContent(
        string relativePath,
        string content,
        IReadOnlyDictionary<ProjectCapabilityKind, HashSet<string>> evidence)
    {
        if (content.Contains("selenium", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("playwright", StringComparison.OrdinalIgnoreCase) ||
            content.Contains("webdriver", StringComparison.OrdinalIgnoreCase))
        {
            evidence[ProjectCapabilityKind.BrowserAutomation].Add(relativePath);
        }
    }

    private static bool ShouldInspectContent(string name, long length) =>
        length is >= 0 and <= MaximumContentBytes &&
        (name.Equals("requirements.txt", StringComparison.OrdinalIgnoreCase) ||
         name.Equals("pyproject.toml", StringComparison.OrdinalIgnoreCase) ||
         name.Equals("package.json", StringComparison.OrdinalIgnoreCase) ||
         name.Equals("composer.json", StringComparison.OrdinalIgnoreCase) ||
         Path.GetExtension(name).Equals(".py", StringComparison.OrdinalIgnoreCase) ||
         Path.GetExtension(name).Equals(".js", StringComparison.OrdinalIgnoreCase) ||
         Path.GetExtension(name).Equals(".mjs", StringComparison.OrdinalIgnoreCase) ||
         Path.GetExtension(name).Equals(".cjs", StringComparison.OrdinalIgnoreCase) ||
         Path.GetExtension(name).Equals(".ts", StringComparison.OrdinalIgnoreCase) ||
         Path.GetExtension(name).Equals(".tsx", StringComparison.OrdinalIgnoreCase));

    private static string ReadSmallUtf8(string path)
    {
        try
        {
            using var reader = new StreamReader(path, new UTF8Encoding(false, true), detectEncodingFromByteOrderMarks: true);
            var buffer = new char[MaximumContentBytes];
            var count = reader.ReadBlock(buffer, 0, buffer.Length);
            return new string(buffer, 0, count);
        }
        catch (Exception exception) when (exception is DecoderFallbackException or IOException or UnauthorizedAccessException)
        {
            return string.Empty;
        }
    }

    private static ProjectCapabilitySnapshot ToSnapshot(
        string projectId,
        IReadOnlyDictionary<ProjectCapabilityKind, HashSet<string>> evidence) =>
        new(
            projectId,
            evidence
                .Where(pair => pair.Value.Count > 0)
                .OrderBy(pair => pair.Key)
                .Select(pair => new ProjectCapabilityEvidence(
                    pair.Key,
                    pair.Value.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).Take(8).ToArray()))
                .ToArray());
}
