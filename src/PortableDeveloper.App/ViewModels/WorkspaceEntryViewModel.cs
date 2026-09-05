using System.ComponentModel;
using System.Runtime.CompilerServices;
using PortableDeveloper.Application.Workspace;

namespace PortableDeveloper.App.ViewModels;

public sealed class WorkspaceEntryViewModel : INotifyPropertyChanged
{
    private string _editName;
    private bool _isRenaming;

    public WorkspaceEntryViewModel(
        string name,
        string relativePath,
        bool isDirectory,
        string kind,
        string size,
        string modified,
        bool isSafe,
        WorkspaceFileKind fileKind)
    {
        Name = name;
        RelativePath = relativePath;
        IsDirectory = isDirectory;
        Kind = kind;
        Size = size;
        Modified = modified;
        IsSafe = isSafe;
        FileKind = fileKind;
        _editName = name;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name { get; }

    public string RelativePath { get; }

    public bool IsDirectory { get; }

    public string Kind { get; }

    public string Size { get; }

    public string Modified { get; }

    public bool IsSafe { get; }

    public WorkspaceFileKind FileKind { get; }

    public string EditName
    {
        get => _editName;
        set => SetProperty(ref _editName, value);
    }

    public bool IsRenaming
    {
        get => _isRenaming;
        set => SetProperty(ref _isRenaming, value);
    }

    public static WorkspaceEntryViewModel From(WorkspaceEntry entry, UiText text) => new(
        entry.Name,
        entry.RelativePath,
        entry.IsDirectory,
        text.WorkspaceKindLabel(entry.FileKind),
        entry.SizeBytes is { } bytes ? FormatSize(bytes) : "—",
        entry.LastWriteTime.ToString("g"),
        entry.IsSafe,
        entry.FileKind);

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

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
