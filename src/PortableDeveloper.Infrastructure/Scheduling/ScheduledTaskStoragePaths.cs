using PortableDeveloper.Application.Abstractions;

namespace PortableDeveloper.Infrastructure.Scheduling;

internal static class ScheduledTaskStoragePaths
{
    public static string EnsureSafeDirectory(IPortablePathResolver paths, string relativePath)
    {
        Directory.CreateDirectory(paths.RootPath);
        var target = paths.Resolve(relativePath);
        var relative = Path.GetRelativePath(paths.RootPath, target);
        var current = paths.RootPath;
        RefuseReparsePoint(current);
        foreach (var segment in relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar))
        {
            current = Path.Combine(current, segment);
            if (File.Exists(current) && !Directory.Exists(current))
            {
                throw new IOException("Scheduled task storage is occupied by a file.");
            }

            if (Directory.Exists(current))
            {
                RefuseReparsePoint(current);
            }
            else
            {
                Directory.CreateDirectory(current);
            }
        }

        return target;
    }

    public static void RefuseReparsePoint(string path)
    {
        if ((File.Exists(path) || Directory.Exists(path)) &&
            (File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint)
        {
            throw new InvalidDataException("Scheduled task storage must not use links or reparse points.");
        }
    }
}
