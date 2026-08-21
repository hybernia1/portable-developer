using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PortableDeveloper.Application.ApachePhp;
using PortableDeveloper.Application.MariaDb;
using PortableDeveloper.Application.Modules;
using PortableDeveloper.Application.Php;
using PortableDeveloper.Application.Settings;
using PortableDeveloper.Domain.Modules;
using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.App.ViewModels;

public sealed class DashboardViewModel : INotifyPropertyChanged
{
    private readonly IModuleInventory _moduleInventory;
    private readonly IModuleInstallationVerifier _moduleVerifier;
    private readonly IApacheRuntimePreflight _apacheRuntimePreflight;
    private readonly IPhpRuntimePreflight _phpRuntimePreflight;
    private ManagedProcessState _stackState = ManagedProcessState.Stopped;
    private string _stackErrorDetail = string.Empty;
    private MariaDbInstanceState _mariaDbState;
    private bool _mariaDbOperationInProgress;

    public DashboardViewModel(
        string rootPath,
        IModuleInventory moduleInventory,
        IModuleInstallationVerifier moduleVerifier,
        IApacheRuntimePreflight apacheRuntimePreflight,
        IPhpRuntimePreflight phpRuntimePreflight,
        MariaDbInstanceState mariaDbState,
        UiText text)
    {
        RootPath = rootPath;
        _moduleInventory = moduleInventory;
        _moduleVerifier = moduleVerifier;
        _apacheRuntimePreflight = apacheRuntimePreflight;
        _phpRuntimePreflight = phpRuntimePreflight;
        _mariaDbState = mariaDbState;
        Text = text;
        Services = new ObservableCollection<ServiceCardViewModel>();
        RefreshServices();
    }

    public string RootPath { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UiText Text { get; }

    public ObservableCollection<ServiceCardViewModel> Services { get; }

    public ManagedProcessState StackProcessState => _stackState;

    public string StackState => Text.StackStatus(_stackState);

    public string StackDetail => Text.StackSummary(_stackState, _stackErrorDetail);

    public string StackActionLabel => Text.StackAction(_stackState);

    public bool StackActionEnabled => _stackState is not ManagedProcessState.Starting and not ManagedProcessState.Stopping;

    public string StackActionBackground => _stackState == ManagedProcessState.Running ? "#6B3434" : "#2D6A4F";

    public string StackActionBorder => _stackState == ManagedProcessState.Running ? "#A25B5B" : "#4F9A70";

    public void SetLanguage(ApplicationLanguage language)
    {
        Text.SetLanguage(language);
        RefreshServices();
        NotifyStackProperties();
    }

    public void SetStackStatus(ManagedProcessState state, string detail)
    {
        _stackState = state;
        _stackErrorDetail = state == ManagedProcessState.Failed ? detail : string.Empty;
        RefreshServices();
        NotifyStackProperties();
    }

    public void SetMariaDbState(MariaDbInstanceState state)
    {
        _mariaDbState = state;
        RefreshServices();
    }

    public void SetMariaDbOperationInProgress(bool inProgress)
    {
        _mariaDbOperationInProgress = inProgress;
        RefreshServices();
    }

    private void RefreshServices()
    {
        Services.Clear();
        AddModuleCard(ModuleKind.Apache, "Apache", "apache");
        AddModuleCard(ModuleKind.Php, "PHP", "php");
        AddModuleCard(ModuleKind.MariaDb, "MariaDB", "mariadb");
        AddModuleCard(ModuleKind.Selenium, "Selenium", "selenium");
    }

    private void AddModuleCard(ModuleKind kind, string name, string descriptionKey)
    {
        var description = Text.ServiceDescription(descriptionKey);
        var installation = _moduleInventory.GetInstalled(kind).FirstOrDefault();
        if (installation is null)
        {
            Services.Add(new ServiceCardViewModel(name, description, Text.ModuleNotFound, Text.NotInstalled));
            return;
        }

        if (kind == ModuleKind.Apache)
        {
            var readiness = _apacheRuntimePreflight.Check(installation.ModuleRootRelativePath);
            if (!readiness.IsReady)
            {
                Services.Add(new ServiceCardViewModel(name, description, Text.RuntimeMissing(readiness.MissingFiles), Text.WaitingRuntime));
                return;
            }
        }
        else if (kind == ModuleKind.Php)
        {
            var readiness = _phpRuntimePreflight.Check(installation.ModuleRootRelativePath);
            if (!readiness.IsReady)
            {
                Services.Add(new ServiceCardViewModel(name, description, Text.RuntimeMissing(readiness.MissingFiles), Text.WaitingRuntime));
                return;
            }
        }

        var verification = _moduleVerifier.Verify(kind, name);
        if (!verification.IsVerified)
        {
            Services.Add(new ServiceCardViewModel(name, description, verification.Detail, Text.VerificationFailed));
            return;
        }

        Services.Add(kind switch
        {
            ModuleKind.Apache => CreateStackServiceCard(name, description, installation.Version, 8080),
            ModuleKind.Php => CreateStackServiceCard(name, description, installation.Version, 9000),
            ModuleKind.MariaDb => CreateMariaDbCard(name, description, installation.Version),
            ModuleKind.Selenium => new ServiceCardViewModel(
                name,
                description,
                Text.ControlNotAvailable(installation.Version),
                Text.Bundled),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        });
    }

    private ServiceCardViewModel CreateStackServiceCard(string name, string description, string version, int port)
    {
        var state = _stackState switch
        {
            ManagedProcessState.Running => Text.Running,
            ManagedProcessState.Starting => Text.Starting,
            ManagedProcessState.Stopping => Text.Stopping,
            ManagedProcessState.Failed => Text.Failed,
            _ => Text.Stopped
        };
        var detail = _stackState == ManagedProcessState.Running
            ? Text.RunningModule(version, port)
            : Text.VerifiedModule(version);
        return new ServiceCardViewModel(name, description, detail, state);
    }

    private ServiceCardViewModel CreateMariaDbCard(string name, string description, string version) => _mariaDbState switch
    {
        MariaDbInstanceState.Initialized => new(
            name,
            description,
            Text.MariaDbInstanceReady(version),
            Text.Initialized),
        MariaDbInstanceState.Incomplete => new(
            name,
            description,
            Text.MariaDbInstanceIncomplete,
            Text.NeedsAttention),
        _ => new(
            name,
            description,
            Text.MariaDbNeedsPreparation(version),
            Text.NeedsSetup,
            "initialize-mariadb",
            _mariaDbOperationInProgress ? Text.PreparingMariaDb : Text.PrepareMariaDb,
            !_mariaDbOperationInProgress)
    };

    private void NotifyStackProperties()
    {
        OnPropertyChanged(nameof(StackProcessState));
        OnPropertyChanged(nameof(StackState));
        OnPropertyChanged(nameof(StackDetail));
        OnPropertyChanged(nameof(StackActionLabel));
        OnPropertyChanged(nameof(StackActionEnabled));
        OnPropertyChanged(nameof(StackActionBackground));
        OnPropertyChanged(nameof(StackActionBorder));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record ServiceCardViewModel(
    string Name,
    string Description,
    string Detail,
    string State,
    string? ActionKey = null,
    string? ActionLabel = null,
    bool IsActionEnabled = true)
{
    public bool HasAction => !string.IsNullOrWhiteSpace(ActionKey);
}
