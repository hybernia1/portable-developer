using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using PortableDeveloper.App.Shell;
using PortableDeveloper.Application.Selenium;

namespace PortableDeveloper.App.ViewModels;

public sealed class SeleniumPageViewModel : INotifyPropertyChanged
{
    private readonly WorkspaceShellViewModel _shell;
    private IReadOnlyList<SeleniumBrowserEnvironmentInfo> _environmentSnapshot = [];
    private IReadOnlyList<SeleniumSessionInfo> _sessionSnapshot = [];
    private IReadOnlyList<SeleniumProfileInfo> _profileSnapshot = [];
    private IReadOnlyList<SeleniumCookieVaultInfo> _cookieVaultSnapshot = [];
    private SeleniumPageRuntimeState _runtimeState;
    private string _cleanProfileName = string.Empty;
    private string _cookieVaultName = string.Empty;
    private bool _downloadsEnabled;
    private string _maximumSessionsText = string.Empty;
    private string _profileProgressMessage = string.Empty;
    private bool _profileProgressVisible;
    private string? _selectedBrowserEnvironmentId;
    private string? _selectedCookieFilePath;
    private string _sessionTimeoutText = string.Empty;
    private string _statusText = string.Empty;

    public SeleniumPageViewModel(
        UiText text,
        WorkspaceShellViewModel shell,
        ObservableCollection<RuntimePackageViewModel> driverPackages)
    {
        Text = text;
        _shell = shell;
        _runtimeState = SeleniumPageRuntimeState.CreateDefault(text);
        SeleniumDriverPackages = driverPackages;
        SeleniumDrivers = [];
        SeleniumSessions = [];
        SeleniumProfiles = [];
        SeleniumCookieVaults = [];
        SeleniumBrowserChoices = [];
        SelectedCookieFileDisplay = Text.NoCookieFileSelected;
        _shell.PropertyChanged += Shell_PropertyChanged;
        Text.PropertyChanged += Text_PropertyChanged;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UiText Text { get; }

    public NavigationSection SelectedSection => _shell.SelectedSection;

    public ServiceCardViewModel SeleniumService => _runtimeState.SeleniumService;

    public string SeleniumHubUrl => _runtimeState.SeleniumHubUrl;

    public bool SeleniumIsRunning => _runtimeState.SeleniumIsRunning;

    public bool SeleniumActionEnabled => _runtimeState.SeleniumActionEnabled;

    public bool SeleniumSettingsEnabled => _runtimeState.SeleniumSettingsEnabled;

    public bool SeleniumProfileActionsEnabled => _runtimeState.SeleniumProfileActionsEnabled;

    public bool SeleniumSessionActionsEnabled => _runtimeState.SeleniumSessionActionsEnabled;

    public string SeleniumActionLabel => _runtimeState.SeleniumActionLabel;

    public string SeleniumSessionCount => Text.SeleniumSessionCount(SeleniumSessions.Count, _runtimeState.MaximumSessions);

    public bool NoSeleniumSessions => SeleniumSessions.Count == 0;

    public bool NoSeleniumProfiles => SeleniumProfiles.Count == 0;

    public bool NoSeleniumCookieVaults => SeleniumCookieVaults.Count == 0;

    public string SeleniumDriverCount => Text.SeleniumDriverCount(ReadyEnvironmentCount);

    public string SeleniumProfileCount => Text.SeleniumProfileCount(SeleniumProfiles.Count);

    public string SeleniumCookieVaultCount => Text.CookieVaultCount(SeleniumCookieVaults.Count);

    public int ReadyEnvironmentCount => _environmentSnapshot.Count(environment => environment.IsReady);

    public ObservableCollection<RuntimePackageViewModel> SeleniumDriverPackages { get; }

    public ObservableCollection<SeleniumDriverCardViewModel> SeleniumDrivers { get; }

    public ObservableCollection<SeleniumSessionCardViewModel> SeleniumSessions { get; }

    public ObservableCollection<SeleniumProfileCardViewModel> SeleniumProfiles { get; }

    public ObservableCollection<SeleniumCookieVaultCardViewModel> SeleniumCookieVaults { get; }

    public ObservableCollection<SeleniumBrowserChoiceViewModel> SeleniumBrowserChoices { get; }

    public string MaximumSessionsText
    {
        get => _maximumSessionsText;
        set => SetField(ref _maximumSessionsText, value);
    }

    public string SessionTimeoutText
    {
        get => _sessionTimeoutText;
        set => SetField(ref _sessionTimeoutText, value);
    }

    public bool DownloadsEnabled
    {
        get => _downloadsEnabled;
        set => SetField(ref _downloadsEnabled, value);
    }

    public string CleanProfileName
    {
        get => _cleanProfileName;
        set => SetField(ref _cleanProfileName, value);
    }

    public string? SelectedBrowserEnvironmentId
    {
        get => _selectedBrowserEnvironmentId;
        set => SetField(ref _selectedBrowserEnvironmentId, value);
    }

    public string CookieVaultName
    {
        get => _cookieVaultName;
        set => SetField(ref _cookieVaultName, value);
    }

    public string? SelectedCookieFilePath
    {
        get => _selectedCookieFilePath;
        private set => SetField(ref _selectedCookieFilePath, value);
    }

    public string SelectedCookieFileDisplay { get; private set; }

    public bool ProfileProgressVisible
    {
        get => _profileProgressVisible;
        private set => SetField(ref _profileProgressVisible, value);
    }

    public string ProfileProgressMessage
    {
        get => _profileProgressMessage;
        private set => SetField(ref _profileProgressMessage, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetField(ref _statusText, value);
    }

    public void SetRuntimeState(SeleniumPageRuntimeState state)
    {
        if (_runtimeState == state)
        {
            return;
        }

        _runtimeState = state;
        OnPropertyChanged(nameof(SeleniumService));
        OnPropertyChanged(nameof(SeleniumHubUrl));
        OnPropertyChanged(nameof(SeleniumIsRunning));
        OnPropertyChanged(nameof(SeleniumActionEnabled));
        OnPropertyChanged(nameof(SeleniumSettingsEnabled));
        OnPropertyChanged(nameof(SeleniumProfileActionsEnabled));
        OnPropertyChanged(nameof(SeleniumSessionActionsEnabled));
        OnPropertyChanged(nameof(SeleniumActionLabel));
        OnPropertyChanged(nameof(SeleniumSessionCount));
    }

    public void SetEnvironments(IEnumerable<SeleniumBrowserEnvironmentInfo> environments)
    {
        _environmentSnapshot = environments.ToArray();
        RefreshEnvironments();
        RefreshProfiles();
    }

    public void SetSessions(IEnumerable<SeleniumSessionInfo> sessions)
    {
        _sessionSnapshot = sessions.ToArray();
        RefreshSessions();
    }

    public void SetProfiles(IEnumerable<SeleniumProfileInfo> profiles)
    {
        _profileSnapshot = profiles.ToArray();
        RefreshProfiles();
    }

    public void SetCookieVaults(IEnumerable<SeleniumCookieVaultInfo> vaults)
    {
        _cookieVaultSnapshot = vaults.ToArray();
        RefreshCookieVaults();
    }

    public void PopulateSettings(int maximumSessions, int sessionTimeoutSeconds, bool downloadsEnabled)
    {
        MaximumSessionsText = maximumSessions.ToString();
        SessionTimeoutText = sessionTimeoutSeconds.ToString();
        DownloadsEnabled = downloadsEnabled;
    }

    public void SelectFirstBrowserEnvironmentIfNeeded()
    {
        if (SelectedBrowserEnvironmentId is null || SeleniumBrowserChoices.All(item => item.Id != SelectedBrowserEnvironmentId))
        {
            SelectedBrowserEnvironmentId = SeleniumBrowserChoices.FirstOrDefault()?.Id;
        }
    }

    public void SetProfileProgress(bool visible, string message)
    {
        ProfileProgressVisible = visible;
        ProfileProgressMessage = message;
    }

    public void SetCookieFile(string path)
    {
        SelectedCookieFilePath = path;
        SelectedCookieFileDisplay = Path.GetFileName(path);
        OnPropertyChanged(nameof(SelectedCookieFileDisplay));
    }

    public void ClearCookieFile()
    {
        SelectedCookieFilePath = null;
        SelectedCookieFileDisplay = Text.NoCookieFileSelected;
        OnPropertyChanged(nameof(SelectedCookieFileDisplay));
    }

    public void RefreshLocalizedFileDisplay()
    {
        if (SelectedCookieFilePath is null)
        {
            SelectedCookieFileDisplay = Text.NoCookieFileSelected;
            OnPropertyChanged(nameof(SelectedCookieFileDisplay));
        }
    }

    public void SetStatus(string status) => StatusText = status;

    private void RefreshEnvironments()
    {
        SeleniumDrivers.Clear();
        SeleniumBrowserChoices.Clear();
        foreach (var environment in _environmentSnapshot)
        {
            SeleniumDrivers.Add(new SeleniumDriverCardViewModel(
                environment.DisplayName,
                environment.BrowserVersion,
                environment.BrowserExecutablePath,
                Text.SeleniumEnvironmentState(environment.State),
                environment.Detail,
                environment.IsReady));
            if (environment.IsReady)
            {
                SeleniumBrowserChoices.Add(new SeleniumBrowserChoiceViewModel(
                    environment.Id,
                    environment.DisplayName,
                    environment.BrowserVersion));
            }
        }

        OnPropertyChanged(nameof(SeleniumDriverCount));
        OnPropertyChanged(nameof(ReadyEnvironmentCount));
    }

    private void RefreshSessions()
    {
        SeleniumSessions.Clear();
        foreach (var session in _sessionSnapshot)
        {
            SeleniumSessions.Add(new SeleniumSessionCardViewModel(
                session.Id,
                string.IsNullOrWhiteSpace(session.BrowserVersion)
                    ? session.BrowserName
                    : $"{session.BrowserName} {session.BrowserVersion}",
                session.PlatformName,
                session.StartedAtUtc?.ToLocalTime().ToString("g") ?? "—",
                FormatDuration(session.Duration)));
        }

        OnPropertyChanged(nameof(SeleniumSessionCount));
        OnPropertyChanged(nameof(NoSeleniumSessions));
    }

    private void RefreshProfiles()
    {
        SeleniumProfiles.Clear();
        foreach (var profile in _profileSnapshot)
        {
            var browserName = profile.Browser switch
            {
                SeleniumProfileBrowser.Edge => "MicrosoftEdge",
                SeleniumProfileBrowser.Chrome => "chrome",
                SeleniumProfileBrowser.Firefox => "firefox",
                _ => string.Empty
            };
            var hasReadyEnvironment = _environmentSnapshot.Any(environment =>
                environment.IsReady && string.Equals(environment.BrowserName, browserName, StringComparison.OrdinalIgnoreCase));
            SeleniumProfiles.Add(new SeleniumProfileCardViewModel(
                profile.Id,
                profile.Name,
                Text.SeleniumProfileBrowserLabel(profile.Browser),
                FormatSize(profile.ApproximateSizeBytes),
                $"portable:profile = {profile.Id}",
                !profile.IsVerified
                    ? Text.DamagedProfile(profile.VerificationDetail)
                    : hasReadyEnvironment
                        ? Text.VerifiedProfile
                        : Text.ProfileBrowserUnavailable));
        }

        OnPropertyChanged(nameof(NoSeleniumProfiles));
        OnPropertyChanged(nameof(SeleniumProfileCount));
    }

    private void RefreshCookieVaults()
    {
        SeleniumCookieVaults.Clear();
        foreach (var vault in _cookieVaultSnapshot)
        {
            SeleniumCookieVaults.Add(new SeleniumCookieVaultCardViewModel(
                vault.Id,
                vault.Name,
                vault.Domains.Count == 0 ? Text.NoCookieDomains : string.Join(", ", vault.Domains),
                Text.CookieCount(vault.CookieCount),
                $"portable:vault = {vault.Id}",
                vault.IsDamaged
                    ? Text.DamagedVault(vault.Detail)
                    : Text.CookieVaultReady));
        }

        OnPropertyChanged(nameof(NoSeleniumCookieVaults));
        OnPropertyChanged(nameof(SeleniumCookieVaultCount));
    }

    private void Shell_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName)
            || e.PropertyName == nameof(WorkspaceShellViewModel.SelectedSection))
        {
            OnPropertyChanged(nameof(SelectedSection));
        }
    }

    private void Text_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        RefreshEnvironments();
        RefreshSessions();
        RefreshProfiles();
        RefreshCookieVaults();
        RefreshLocalizedFileDisplay();
    }

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

    private static string FormatDuration(TimeSpan duration) => duration.TotalHours >= 1
        ? $"{(int)duration.TotalHours}:{duration.Minutes:00}:{duration.Seconds:00}"
        : $"{duration.Minutes}:{duration.Seconds:00}";

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

public sealed record SeleniumPageRuntimeState(
    ServiceCardViewModel SeleniumService,
    string SeleniumHubUrl,
    bool SeleniumIsRunning,
    bool SeleniumActionEnabled,
    bool SeleniumSettingsEnabled,
    bool SeleniumProfileActionsEnabled,
    bool SeleniumSessionActionsEnabled,
    string SeleniumActionLabel,
    int MaximumSessions)
{
    public static SeleniumPageRuntimeState CreateDefault(UiText text) => new(
        new ServiceCardViewModel("Selenium", string.Empty, text.ModuleNotFound, text.NotInstalled),
        string.Empty,
        SeleniumIsRunning: false,
        SeleniumActionEnabled: false,
        SeleniumSettingsEnabled: false,
        SeleniumProfileActionsEnabled: false,
        SeleniumSessionActionsEnabled: false,
        string.Empty,
        MaximumSessions: 1);
}

public sealed record SeleniumDriverCardViewModel(
    string Name,
    string Version,
    string RelativePath,
    string Source,
    string Detail,
    bool IsReady);

public sealed record SeleniumBrowserChoiceViewModel(string Id, string Name, string Version)
{
    public string Label => string.IsNullOrWhiteSpace(Version) || Version == "unknown" ? Name : $"{Name} · {Version}";
}

public sealed record SeleniumSessionCardViewModel(
    string Id,
    string Browser,
    string Platform,
    string StartedAt,
    string Duration);
