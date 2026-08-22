using PortableDeveloper.Application.Workspace;

namespace PortableDeveloper.App.ViewModels;

public sealed record WorkspaceEntryViewModel(
    string Name,
    string RelativePath,
    bool IsDirectory,
    string Kind,
    string Size,
    string Modified,
    bool IsSafe)
{
    public static WorkspaceEntryViewModel From(WorkspaceEntry entry, UiText text) => new(
        entry.Name,
        entry.RelativePath,
        entry.IsDirectory,
        entry.IsDirectory ? text.Folder : text.File,
        entry.SizeBytes is { } bytes ? FormatSize(bytes) : "—",
        entry.LastWriteTime.ToString("g"),
        entry.IsSafe);

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB"];
        double size = bytes;
        var unit = 0;
        while (size >= 1024 && unit < units.Length - 1)
        {
            size /= 1024;
            unit++;
        }

        return $"{size:0.#} {units[unit]}";
    }
}
