using System.ComponentModel;
using System.Runtime.CompilerServices;
using PortableDeveloper.App.Shell;
using PortableDeveloper.Application.Ports;
using PortableDeveloper.Application.Settings;
using PortableDeveloper.Application.Storage;

namespace PortableDeveloper.App.ViewModels;

public sealed class SettingsPageViewModel : INotifyPropertyChanged
{
    private readonly WorkspaceShellViewModel _shell;
    private PortSettings _portSettings = PortSettings.Default;
    private string _editorPreferenceName = FileEditorPreference.PortableWhenAvailable.ToString();
    private string _editorStatus = string.Empty;
    private bool _storageActionsEnabled = true;
    private string _runtimePackageCacheSize = "—";
    private string _composerCacheSize = "—";
    private string _npmCacheSize = "—";
    private string _pipCacheSize = "—";
    private string _totalCacheSize = "—";
    private string _installedRuntimeSize = "—";
    private string _persistentDataSize = "—";
    private string _storageStatus = string.Empty;
    private bool _canClearRuntimePackageCache;
    private bool _canClearComposerCache;
    private bool _canClearNpmCache;
    private bool _canClearPipCache;
    private bool _canClearAllCaches;

    public SettingsPageViewModel(UiText text, WorkspaceShellViewModel shell, string applicationVersion)
    {
        Text = text;
        _shell = shell;
        ApplicationVersion = applicationVersion;
        _shell.PropertyChanged += Shell_PropertyChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UiText Text { get; }

    public NavigationSection SelectedSection => _shell.SelectedSection;

    public string ApplicationVersion { get; }

    public int ApachePort => _portSettings.ApachePort;

    public int PhpFastCgiPort => _portSettings.PhpFastCgiPort;

    public int MariaDbPort => _portSettings.MariaDbPort;

    public int SeleniumPort => _portSettings.SeleniumPort;

    public string EditorPreferenceName
    {
        get => _editorPreferenceName;
        private set => SetField(ref _editorPreferenceName, value);
    }

    public string EditorStatus
    {
        get => _editorStatus;
        private set => SetField(ref _editorStatus, value);
    }

    public bool StorageActionsEnabled
    {
        get => _storageActionsEnabled;
        private set => SetField(ref _storageActionsEnabled, value);
    }

    public string RuntimePackageCacheSize
    {
        get => _runtimePackageCacheSize;
        private set => SetField(ref _runtimePackageCacheSize, value);
    }

    public string ComposerCacheSize
    {
        get => _composerCacheSize;
        private set => SetField(ref _composerCacheSize, value);
    }

    public string NpmCacheSize
    {
        get => _npmCacheSize;
        private set => SetField(ref _npmCacheSize, value);
    }

    public string PipCacheSize
    {
        get => _pipCacheSize;
        private set => SetField(ref _pipCacheSize, value);
    }

    public string TotalCacheSize
    {
        get => _totalCacheSize;
        private set => SetField(ref _totalCacheSize, value);
    }

    public string InstalledRuntimeSize
    {
        get => _installedRuntimeSize;
        private set => SetField(ref _installedRuntimeSize, value);
    }

    public string PersistentDataSize
    {
        get => _persistentDataSize;
        private set => SetField(ref _persistentDataSize, value);
    }

    public string StorageStatus
    {
        get => _storageStatus;
        private set => SetField(ref _storageStatus, value);
    }

    public bool CanClearRuntimePackageCache
    {
        get => _canClearRuntimePackageCache;
        private set => SetField(ref _canClearRuntimePackageCache, value);
    }

    public bool CanClearComposerCache
    {
        get => _canClearComposerCache;
        private set => SetField(ref _canClearComposerCache, value);
    }

    public bool CanClearNpmCache
    {
        get => _canClearNpmCache;
        private set => SetField(ref _canClearNpmCache, value);
    }

    public bool CanClearPipCache
    {
        get => _canClearPipCache;
        private set => SetField(ref _canClearPipCache, value);
    }

    public bool CanClearAllCaches
    {
        get => _canClearAllCaches;
        private set => SetField(ref _canClearAllCaches, value);
    }

    public void SetPorts(PortSettings settings)
    {
        if (_portSettings == settings)
        {
            return;
        }

        _portSettings = settings;
        OnPropertyChanged(nameof(ApachePort));
        OnPropertyChanged(nameof(PhpFastCgiPort));
        OnPropertyChanged(nameof(MariaDbPort));
        OnPropertyChanged(nameof(SeleniumPort));
    }

    public void SetEditorPreference(FileEditorPreference preference) =>
        EditorPreferenceName = preference.ToString();

    public void SetEditorStatus(string status) => EditorStatus = status;

    public void SetStorageActionsEnabled(bool enabled) => StorageActionsEnabled = enabled;

    public void SetStorageStatus(string status) => StorageStatus = status;

    public void ApplyStorageUsage(StorageUsageSnapshot usage, Func<long, string> formatSize)
    {
        RuntimePackageCacheSize = formatSize(usage.RuntimePackageCacheBytes);
        ComposerCacheSize = formatSize(usage.ComposerCacheBytes);
        NpmCacheSize = formatSize(usage.NpmCacheBytes);
        PipCacheSize = formatSize(usage.PipCacheBytes);
        TotalCacheSize = formatSize(usage.TotalCacheBytes);
        InstalledRuntimeSize = formatSize(usage.InstalledRuntimeBytes);
        PersistentDataSize = formatSize(usage.PersistentDataBytes);
        CanClearRuntimePackageCache = usage.RuntimePackageCacheBytes > 0;
        CanClearComposerCache = usage.ComposerCacheBytes > 0;
        CanClearNpmCache = usage.NpmCacheBytes > 0;
        CanClearPipCache = usage.PipCacheBytes > 0;
        CanClearAllCaches = usage.TotalCacheBytes > 0;
    }

    private void Shell_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName)
            || e.PropertyName == nameof(WorkspaceShellViewModel.SelectedSection))
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
