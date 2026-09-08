using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PortableDeveloper.App.Shell;

namespace PortableDeveloper.App.ViewModels;

public sealed class SchedulerPageViewModel : INotifyPropertyChanged
{
    private readonly List<ScheduledTaskRunViewModel> _allScheduledTaskHistory = [];
    private readonly WorkspaceShellViewModel _shell;
    private string _historyFilterText = string.Empty;
    private string _statusText = string.Empty;

    public SchedulerPageViewModel(UiText text, WorkspaceShellViewModel shell)
    {
        Text = text;
        _shell = shell;
        ScheduledTasks = [];
        ScheduledTaskHistory = [];
        _shell.PropertyChanged += Shell_PropertyChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UiText Text { get; }

    public NavigationSection SelectedSection => _shell.SelectedSection;

    public ObservableCollection<ScheduledTaskViewModel> ScheduledTasks { get; }

    public ObservableCollection<ScheduledTaskRunViewModel> ScheduledTaskHistory { get; }

    public bool NoScheduledTasks => ScheduledTasks.Count == 0;

    public bool NoScheduledTaskHistory => _allScheduledTaskHistory.Count == 0;

    public bool HasScheduledTaskHistory => _allScheduledTaskHistory.Count > 0;

    public bool NoMatchingScheduledTaskHistory =>
        _allScheduledTaskHistory.Count > 0 && ScheduledTaskHistory.Count == 0;

    public int ScheduledTaskHistoryTotalCount => _allScheduledTaskHistory.Count;

    public bool HasHistoryFilter => !string.IsNullOrWhiteSpace(HistoryFilterText);

    public string HistoryFilterText
    {
        get => _historyFilterText;
        set
        {
            if (SetField(ref _historyFilterText, value ?? string.Empty))
            {
                OnPropertyChanged(nameof(HasHistoryFilter));
                ApplyHistoryFilter();
            }
        }
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public void SetStatus(string status) => StatusText = status;

    public void SetScheduledTasks(
        IEnumerable<ScheduledTaskViewModel> tasks,
        IEnumerable<ScheduledTaskRunViewModel> history)
    {
        ScheduledTasks.Clear();
        foreach (var task in tasks)
        {
            ScheduledTasks.Add(task);
        }

        _allScheduledTaskHistory.Clear();
        foreach (var record in history)
        {
            _allScheduledTaskHistory.Add(record);
        }

        ApplyHistoryFilter();
        OnPropertyChanged(nameof(NoScheduledTasks));
        OnPropertyChanged(nameof(HasScheduledTaskHistory));
        OnPropertyChanged(nameof(ScheduledTaskHistoryTotalCount));
    }

    public void ClearHistoryFilter() => HistoryFilterText = string.Empty;

    private void ApplyHistoryFilter()
    {
        var query = HistoryFilterText.Trim();
        ScheduledTaskHistory.Clear();
        foreach (var record in _allScheduledTaskHistory.Where(record => MatchesHistoryFilter(record, query)))
        {
            ScheduledTaskHistory.Add(record);
        }

        OnPropertyChanged(nameof(NoScheduledTaskHistory));
        OnPropertyChanged(nameof(NoMatchingScheduledTaskHistory));
    }

    private static bool MatchesHistoryFilter(ScheduledTaskRunViewModel record, string query) =>
        query.Length == 0
        || Contains(record.TaskName, query)
        || Contains(record.Command, query)
        || Contains(record.Target, query)
        || Contains(record.Started, query)
        || Contains(record.Duration, query)
        || Contains(record.Trigger, query)
        || Contains(record.Result, query)
        || Contains(record.Output, query);

    private static bool Contains(string value, string query) =>
        value.Contains(query, StringComparison.CurrentCultureIgnoreCase);

    private void Shell_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(WorkspaceShellViewModel.SelectedSection))
        {
            OnPropertyChanged(nameof(SelectedSection));
        }
    }

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
