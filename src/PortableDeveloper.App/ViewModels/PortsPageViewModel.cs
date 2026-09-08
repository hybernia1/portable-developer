using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PortableDeveloper.App.Shell;
using PortableDeveloper.Application.Ports;

namespace PortableDeveloper.App.ViewModels;

public sealed class PortsPageViewModel : INotifyPropertyChanged
{
    private readonly WorkspaceShellViewModel _shell;
    private IReadOnlyList<TcpPortListenerInfo> _listeners = [];
    private PortSettings _currentSettings = PortSettings.Default;
    private bool _apacheRunning;
    private bool _mariaDbRunning;
    private bool _seleniumRunning;
    private bool _portSettingsEnabled;
    private string _portSettingsAvailability = string.Empty;
    private string _apachePortText = string.Empty;
    private string _phpFastCgiPortText = string.Empty;
    private string _mariaDbPortText = string.Empty;
    private string _seleniumPortText = string.Empty;
    private string _apachePortStatus = string.Empty;
    private string _phpPortStatus = string.Empty;
    private string _mariaDbPortStatus = string.Empty;
    private string _seleniumPortStatus = string.Empty;
    private string _statusText = string.Empty;

    public PortsPageViewModel(UiText text, WorkspaceShellViewModel shell)
    {
        Text = text;
        _shell = shell;
        TcpListeners = [];
        _shell.PropertyChanged += Shell_PropertyChanged;
        Text.PropertyChanged += Text_PropertyChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UiText Text { get; }

    public NavigationSection SelectedSection => _shell.SelectedSection;

    public bool PortSettingsEnabled => _portSettingsEnabled;

    public string PortSettingsAvailability => _portSettingsAvailability;

    public ObservableCollection<TcpPortListenerViewModel> TcpListeners { get; }

    public string ApachePortText
    {
        get => _apachePortText;
        set => SetField(ref _apachePortText, value);
    }

    public string PhpFastCgiPortText
    {
        get => _phpFastCgiPortText;
        set => SetField(ref _phpFastCgiPortText, value);
    }

    public string MariaDbPortText
    {
        get => _mariaDbPortText;
        set => SetField(ref _mariaDbPortText, value);
    }

    public string SeleniumPortText
    {
        get => _seleniumPortText;
        set => SetField(ref _seleniumPortText, value);
    }

    public string ApachePortStatus
    {
        get => _apachePortStatus;
        private set => SetField(ref _apachePortStatus, value);
    }

    public string PhpPortStatus
    {
        get => _phpPortStatus;
        private set => SetField(ref _phpPortStatus, value);
    }

    public string MariaDbPortStatus
    {
        get => _mariaDbPortStatus;
        private set => SetField(ref _mariaDbPortStatus, value);
    }

    public string SeleniumPortStatus
    {
        get => _seleniumPortStatus;
        private set => SetField(ref _seleniumPortStatus, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public void SetPortSettings(PortSettings settings)
    {
        ApachePortText = settings.ApachePort.ToString();
        PhpFastCgiPortText = settings.PhpFastCgiPort.ToString();
        MariaDbPortText = settings.MariaDbPort.ToString();
        SeleniumPortText = settings.SeleniumPort.ToString();
    }

    public void SetRuntimeState(
        PortSettings currentSettings,
        bool apacheRunning,
        bool mariaDbRunning,
        bool seleniumRunning,
        bool settingsEnabled,
        string settingsAvailability)
    {
        _currentSettings = currentSettings;
        _apacheRunning = apacheRunning;
        _mariaDbRunning = mariaDbRunning;
        _seleniumRunning = seleniumRunning;
        _portSettingsEnabled = settingsEnabled;
        _portSettingsAvailability = settingsAvailability;
        OnPropertyChanged(nameof(PortSettingsEnabled));
        OnPropertyChanged(nameof(PortSettingsAvailability));
    }

    public void SetTcpListeners(IEnumerable<TcpPortListenerInfo> listeners)
    {
        _listeners = listeners.ToArray();
        RefreshTcpListeners();
    }

    public bool TryCreateSettings(out PortSettings settings)
    {
        settings = PortSettings.Default;
        if (!int.TryParse(ApachePortText.Trim(), out var apachePort)
            || !int.TryParse(PhpFastCgiPortText.Trim(), out var phpPort)
            || !int.TryParse(MariaDbPortText.Trim(), out var mariaDbPort)
            || !int.TryParse(SeleniumPortText.Trim(), out var seleniumPort))
        {
            return false;
        }

        try
        {
            settings = PortSettingsValidator.Validate(new PortSettings(
                apachePort,
                phpPort,
                mariaDbPort,
                seleniumPort));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public void UpdateInputStatuses(
        IReadOnlyCollection<TcpPortListenerInfo> listeners,
        Func<int, bool> isAvailable)
    {
        var inputs = new[]
        {
            (Text: ApachePortText, CurrentPort: _currentSettings.ApachePort, Owned: _apacheRunning),
            (Text: PhpFastCgiPortText, CurrentPort: _currentSettings.PhpFastCgiPort, Owned: _apacheRunning),
            (Text: MariaDbPortText, CurrentPort: _currentSettings.MariaDbPort, Owned: _mariaDbRunning),
            (Text: SeleniumPortText, CurrentPort: _currentSettings.SeleniumPort, Owned: _seleniumRunning)
        };
        var parsed = inputs.Select(input => int.TryParse(input.Text.Trim(), out var port) ? port : -1).ToArray();
        var statuses = new string[inputs.Length];
        for (var index = 0; index < inputs.Length; index++)
        {
            var port = parsed[index];
            statuses[index] = PortInputStatusPolicy.Resolve(
                Text,
                port,
                parsed,
                inputs[index].CurrentPort,
                inputs[index].Owned,
                listeners,
                isAvailable);
        }

        ApachePortStatus = statuses[0];
        PhpPortStatus = statuses[1];
        MariaDbPortStatus = statuses[2];
        SeleniumPortStatus = statuses[3];
    }

    public void SetStatus(string status) => StatusText = status;

    private void RefreshTcpListeners()
    {
        TcpListeners.Clear();
        foreach (var listener in _listeners)
        {
            TcpListeners.Add(new TcpPortListenerViewModel(
                listener.Address,
                listener.Port,
                Text.TcpListenerEndpoint(listener.Address, listener.Port)));
        }

    }

    private void Shell_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(WorkspaceShellViewModel.SelectedSection))
        {
            OnPropertyChanged(nameof(SelectedSection));
        }
    }

    private void Text_PropertyChanged(object? sender, PropertyChangedEventArgs e) => RefreshTcpListeners();

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

public sealed record TcpPortListenerViewModel(string Address, int Port, string Endpoint);
