using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PortableDeveloper.App.Shell;
using PortableDeveloper.Application.Php;

namespace PortableDeveloper.App.ViewModels;

public sealed class PhpPageViewModel : INotifyPropertyChanged
{
    private readonly WorkspaceShellViewModel _shell;
    private bool _phpSettingsEnabled;
    private string _phpSettingsActionLabel = string.Empty;
    private string _phpRuntimeVersion = string.Empty;
    private string _memoryLimitText = string.Empty;
    private string _uploadLimitText = string.Empty;
    private string _postLimitText = string.Empty;
    private string _executionTimeText = string.Empty;
    private string _maximumInputVariablesText = string.Empty;
    private bool _displayErrors;
    private string _statusText = string.Empty;

    public PhpPageViewModel(UiText text, WorkspaceShellViewModel shell)
    {
        Text = text;
        _shell = shell;
        PhpExtensions = [];
        _shell.PropertyChanged += Shell_PropertyChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UiText Text { get; }

    public NavigationSection SelectedSection => _shell.SelectedSection;

    public bool PhpSettingsEnabled => _phpSettingsEnabled;

    public string PhpSettingsActionLabel => _phpSettingsActionLabel;

    public ObservableCollection<PhpExtensionViewModel> PhpExtensions { get; }

    public string PhpRuntimeVersion => _phpRuntimeVersion;

    public string MemoryLimitText
    {
        get => _memoryLimitText;
        set => SetField(ref _memoryLimitText, value);
    }

    public string UploadLimitText
    {
        get => _uploadLimitText;
        set => SetField(ref _uploadLimitText, value);
    }

    public string PostLimitText
    {
        get => _postLimitText;
        set => SetField(ref _postLimitText, value);
    }

    public string ExecutionTimeText
    {
        get => _executionTimeText;
        set => SetField(ref _executionTimeText, value);
    }

    public string MaximumInputVariablesText
    {
        get => _maximumInputVariablesText;
        set => SetField(ref _maximumInputVariablesText, value);
    }

    public bool DisplayErrors
    {
        get => _displayErrors;
        set => SetField(ref _displayErrors, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public void SetSettings(PhpSettings settings)
    {
        MemoryLimitText = settings.MemoryLimitMb.ToString();
        UploadLimitText = settings.UploadMaxFileSizeMb.ToString();
        PostLimitText = settings.PostMaxSizeMb.ToString();
        ExecutionTimeText = settings.MaxExecutionTimeSeconds.ToString();
        MaximumInputVariablesText = settings.MaxInputVariables.ToString();
        DisplayErrors = settings.DisplayErrors;
        var enabled = settings.EnabledExtensions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var extension in PhpExtensions)
        {
            extension.IsEnabled = enabled.Contains(extension.Name);
        }
    }

    public void SetRuntimeState(bool settingsEnabled, string actionLabel, string runtimeVersion)
    {
        if (_phpSettingsEnabled != settingsEnabled)
        {
            _phpSettingsEnabled = settingsEnabled;
            OnPropertyChanged(nameof(PhpSettingsEnabled));
        }

        if (!string.Equals(_phpSettingsActionLabel, actionLabel, StringComparison.Ordinal))
        {
            _phpSettingsActionLabel = actionLabel;
            OnPropertyChanged(nameof(PhpSettingsActionLabel));
        }

        if (!string.Equals(_phpRuntimeVersion, runtimeVersion, StringComparison.Ordinal))
        {
            _phpRuntimeVersion = runtimeVersion;
            OnPropertyChanged(nameof(PhpRuntimeVersion));
        }
    }

    public void SetExtensions(IEnumerable<PhpExtensionViewModel> extensions)
    {
        PhpExtensions.Clear();
        foreach (var extension in extensions)
        {
            PhpExtensions.Add(extension);
        }
    }

    public bool TryCreateSettings(out PhpSettings settings)
    {
        settings = PhpSettings.Default;
        if (!int.TryParse(MemoryLimitText.Trim(), out var memoryLimit)
            || !int.TryParse(UploadLimitText.Trim(), out var uploadLimit)
            || !int.TryParse(PostLimitText.Trim(), out var postLimit)
            || !int.TryParse(ExecutionTimeText.Trim(), out var executionTime)
            || !int.TryParse(MaximumInputVariablesText.Trim(), out var maximumInputVariables))
        {
            return false;
        }

        try
        {
            settings = PhpSettingsValidator.Normalize(new PhpSettings
            {
                MemoryLimitMb = memoryLimit,
                UploadMaxFileSizeMb = uploadLimit,
                PostMaxSizeMb = postLimit,
                MaxExecutionTimeSeconds = executionTime,
                MaxInputVariables = maximumInputVariables,
                DisplayErrors = DisplayErrors,
                EnabledExtensions = PhpExtensions
                    .Where(extension => extension.IsEnabled)
                    .Select(extension => extension.Name)
                    .ToArray()
            });
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public void SetStatus(string status) => StatusText = status;

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
