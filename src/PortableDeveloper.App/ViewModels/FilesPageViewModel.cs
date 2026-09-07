using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PortableDeveloper.Application.Workspace;

namespace PortableDeveloper.App.ViewModels;

public sealed class FilesPageViewModel : INotifyPropertyChanged
{
    private string _modifiedSortHeader = string.Empty;
    private string _nameSortHeader = string.Empty;
    private string _pageSizeText = "50";
    private string _pathText = string.Empty;
    private string _sizeSortHeader = string.Empty;
    private string _statusText = string.Empty;
    private string _typeSortHeader = string.Empty;
    private int _workspacePageNumber = 1;
    private int _workspacePageSize = 50;
    private int _workspaceTotalCount;
    private int _workspaceTotalPages = 1;

    public FilesPageViewModel(UiText text)
    {
        Text = text;
        WorkspaceEntries = [];
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UiText Text { get; }

    public ObservableCollection<WorkspaceEntryViewModel> WorkspaceEntries { get; }

    public bool NoWorkspaceEntries => WorkspaceEntries.Count == 0;

    public int WorkspacePageNumber => _workspacePageNumber;

    public int WorkspaceTotalPages => _workspaceTotalPages;

    public int WorkspaceTotalCount => _workspaceTotalCount;

    public int WorkspacePageSize => _workspacePageSize;

    public bool WorkspaceHasPreviousPage => WorkspacePageNumber > 1;

    public bool WorkspaceHasNextPage => WorkspacePageNumber < WorkspaceTotalPages;

    public string WorkspacePageSummary => Text.WorkspacePageSummary(
        WorkspaceTotalCount == 0 ? 0 : ((WorkspacePageNumber - 1) * WorkspacePageSize) + 1,
        Math.Min(WorkspacePageNumber * WorkspacePageSize, WorkspaceTotalCount),
        WorkspaceTotalCount);

    public string PathText
    {
        get => _pathText;
        set => SetField(ref _pathText, value);
    }

    public string PageSizeText
    {
        get => _pageSizeText;
        set => SetField(ref _pageSizeText, value);
    }

    public string NameSortHeader
    {
        get => _nameSortHeader;
        private set => SetField(ref _nameSortHeader, value);
    }

    public string TypeSortHeader
    {
        get => _typeSortHeader;
        private set => SetField(ref _typeSortHeader, value);
    }

    public string SizeSortHeader
    {
        get => _sizeSortHeader;
        private set => SetField(ref _sizeSortHeader, value);
    }

    public string ModifiedSortHeader
    {
        get => _modifiedSortHeader;
        private set => SetField(ref _modifiedSortHeader, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public void SetSortHeaders(string name, string type, string size, string modified)
    {
        NameSortHeader = name;
        TypeSortHeader = type;
        SizeSortHeader = size;
        ModifiedSortHeader = modified;
    }

    public void SetWorkspacePage(WorkspacePage page)
    {
        WorkspaceEntries.Clear();
        foreach (var entry in page.Entries)
        {
            WorkspaceEntries.Add(WorkspaceEntryViewModel.From(entry, Text));
        }

        _workspacePageNumber = page.PageNumber;
        _workspaceTotalPages = page.TotalPages;
        _workspaceTotalCount = page.TotalCount;
        _workspacePageSize = page.PageSize;
        OnPropertyChanged(nameof(NoWorkspaceEntries));
        OnPropertyChanged(nameof(WorkspacePageNumber));
        OnPropertyChanged(nameof(WorkspaceTotalPages));
        OnPropertyChanged(nameof(WorkspaceTotalCount));
        OnPropertyChanged(nameof(WorkspacePageSize));
        OnPropertyChanged(nameof(WorkspaceHasPreviousPage));
        OnPropertyChanged(nameof(WorkspaceHasNextPage));
        OnPropertyChanged(nameof(WorkspacePageSummary));
    }

    public void SetStatus(string status) => StatusText = status;

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
