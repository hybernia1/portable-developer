using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PortableDeveloper.App.Shell;
using PortableDeveloper.Application.MariaDb;

namespace PortableDeveloper.App.ViewModels;

public sealed class DatabasesPageViewModel : INotifyPropertyChanged
{
    private readonly WorkspaceShellViewModel _shell;
    private IReadOnlyList<DatabaseInfo> _databaseSnapshot = [];
    private DatabasesPageRuntimeState _runtimeState;
    private string _statusText = string.Empty;

    public DatabasesPageViewModel(UiText text, WorkspaceShellViewModel shell)
    {
        Text = text;
        _shell = shell;
        _runtimeState = DatabasesPageRuntimeState.CreateDefault(text);
        Databases = [];
        _shell.PropertyChanged += Shell_PropertyChanged;
        Text.PropertyChanged += Text_PropertyChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UiText Text { get; }

    public NavigationSection SelectedSection => _shell.SelectedSection;

    public ServiceCardViewModel MariaDbService => _runtimeState.MariaDbService;

    public string MariaDbHeaderDetail => MariaDbService.State == Text.Failed
        || MariaDbService.State == Text.NeedsAttention
        || MariaDbService.State == Text.NeedsSetup
            ? MariaDbService.Detail
            : string.Empty;

    public bool MariaDbActionEnabled => _runtimeState.MariaDbActionEnabled;

    public bool MariaDbIsRunning => _runtimeState.MariaDbIsRunning;

    public string MariaDbActionLabel => _runtimeState.MariaDbActionLabel;

    public bool DatabaseActionsEnabled => _runtimeState.DatabaseActionsEnabled;

    public int MariaDbPort => _runtimeState.MariaDbPort;

    public string RootPasswordState => _runtimeState.RootPasswordState;

    public string RootPasswordActionLabel => _runtimeState.RootPasswordActionLabel;

    public bool PhpMyAdminInstalled => _runtimeState.PhpMyAdminInstalled;

    public string PhpMyAdminUrl => _runtimeState.PhpMyAdminUrl;

    public bool PhpMyAdminActionEnabled => _runtimeState.PhpMyAdminActionEnabled;

    public string PhpMyAdminDependencyState => _runtimeState.PhpMyAdminDependencyState;

    public string DatabaseCount => Text.DatabaseCount(Databases.Count);

    public ObservableCollection<DatabaseCardViewModel> Databases { get; }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public void SetRuntimeState(DatabasesPageRuntimeState state)
    {
        if (_runtimeState == state)
        {
            return;
        }

        _runtimeState = state;
        OnPropertyChanged(nameof(MariaDbService));
        OnPropertyChanged(nameof(MariaDbHeaderDetail));
        OnPropertyChanged(nameof(MariaDbActionEnabled));
        OnPropertyChanged(nameof(MariaDbIsRunning));
        OnPropertyChanged(nameof(MariaDbActionLabel));
        OnPropertyChanged(nameof(DatabaseActionsEnabled));
        OnPropertyChanged(nameof(MariaDbPort));
        OnPropertyChanged(nameof(RootPasswordState));
        OnPropertyChanged(nameof(RootPasswordActionLabel));
        OnPropertyChanged(nameof(PhpMyAdminInstalled));
        OnPropertyChanged(nameof(PhpMyAdminUrl));
        OnPropertyChanged(nameof(PhpMyAdminActionEnabled));
        OnPropertyChanged(nameof(PhpMyAdminDependencyState));
    }

    public void SetDatabases(IEnumerable<DatabaseInfo> databases)
    {
        _databaseSnapshot = databases.ToArray();
        RefreshDatabases();
    }

    public void SetStatus(string status) => StatusText = status;

    private void RefreshDatabases()
    {
        Databases.Clear();
        foreach (var database in _databaseSnapshot)
        {
            Databases.Add(new DatabaseCardViewModel(
                database.Name,
                FormatSize(database.ApproximateSizeBytes),
                database.ApproximateSizeBytes,
                !string.Equals(database.Name, "portable_dev", StringComparison.OrdinalIgnoreCase)));
        }

        OnPropertyChanged(nameof(DatabaseCount));
    }

    private void Shell_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName)
            || e.PropertyName == nameof(WorkspaceShellViewModel.SelectedSection))
        {
            OnPropertyChanged(nameof(SelectedSection));
        }
    }

    private void Text_PropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshDatabases();

    private static string FormatSize(long bytes)
    {
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var value = (double)Math.Max(0, bytes);
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024d;
            unit++;
        }

        return unit == 0 ? $"{value:0} {units[unit]}" : $"{value:0.##} {units[unit]}";
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

public sealed record DatabasesPageRuntimeState(
    ServiceCardViewModel MariaDbService,
    bool MariaDbActionEnabled,
    bool MariaDbIsRunning,
    string MariaDbActionLabel,
    bool DatabaseActionsEnabled,
    int MariaDbPort,
    string RootPasswordState,
    string RootPasswordActionLabel,
    bool PhpMyAdminInstalled,
    string PhpMyAdminUrl,
    bool PhpMyAdminActionEnabled,
    string PhpMyAdminDependencyState)
{
    public static DatabasesPageRuntimeState CreateDefault(UiText text) => new(
        new ServiceCardViewModel("MariaDB", string.Empty, text.ModuleNotFound, text.NotInstalled),
        MariaDbActionEnabled: false,
        MariaDbIsRunning: false,
        string.Empty,
        DatabaseActionsEnabled: false,
        MariaDbPort: 0,
        string.Empty,
        string.Empty,
        PhpMyAdminInstalled: false,
        string.Empty,
        PhpMyAdminActionEnabled: false,
        string.Empty);
}

public sealed record DatabaseCardViewModel(
    string Name,
    string ApproximateSize,
    long ApproximateSizeBytes,
    bool CanDelete);
