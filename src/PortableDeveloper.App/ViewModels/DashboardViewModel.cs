using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PortableDeveloper.Application.ApachePhp;
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
    private string _stackDetail = string.Empty;

    public DashboardViewModel(
        string rootPath,
        IModuleInventory moduleInventory,
        IModuleInstallationVerifier moduleVerifier,
        IApacheRuntimePreflight apacheRuntimePreflight,
        IPhpRuntimePreflight phpRuntimePreflight,
        UiText text)
    {
        RootPath = rootPath;
        _moduleInventory = moduleInventory;
        _moduleVerifier = moduleVerifier;
        _apacheRuntimePreflight = apacheRuntimePreflight;
        _phpRuntimePreflight = phpRuntimePreflight;
        Text = text;
        Services = new ObservableCollection<ServiceCardViewModel>();
        RefreshModules();
    }

    public string RootPath { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UiText Text { get; }

    public ObservableCollection<ServiceCardViewModel> Services { get; }

    public string StackState => Text.StackStatus(_stackState);

    public string StackDetail => _stackDetail;

    public void RefreshModules()
    {
        Services.Clear();
        AddModuleCard(ModuleKind.Apache, "Apache", "apache");
        AddModuleCard(ModuleKind.Php, "PHP", "php");
        AddModuleCard(ModuleKind.MariaDb, "MariaDB", "mariadb");
        AddModuleCard(ModuleKind.Selenium, "Selenium", "selenium");
    }

    public void SetLanguage(ApplicationLanguage language)
    {
        Text.SetLanguage(language);
        RefreshModules();
        OnPropertyChanged(nameof(StackState));
    }

    public void SetStackStatus(ManagedProcessState state, string detail)
    {
        _stackState = state;
        _stackDetail = detail;
        OnPropertyChanged(nameof(StackState));
        OnPropertyChanged(nameof(StackDetail));
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
                Services.Add(new ServiceCardViewModel(
                    name,
                    description,
                    Text.RuntimeMissing(readiness.MissingFiles),
                    Text.WaitingRuntime));
                return;
            }
        }
        else if (kind == ModuleKind.Php)
        {
            var readiness = _phpRuntimePreflight.Check(installation.ModuleRootRelativePath);
            if (!readiness.IsReady)
            {
                Services.Add(new ServiceCardViewModel(
                    name,
                    description,
                    Text.RuntimeMissing(readiness.MissingFiles),
                    Text.WaitingRuntime));
                return;
            }
        }

        var verification = _moduleVerifier.Verify(kind, name);
        if (!verification.IsVerified)
        {
            Services.Add(new ServiceCardViewModel(name, description, verification.Detail, Text.VerificationFailed));
            return;
        }

        Services.Add(new ServiceCardViewModel(name, description, Text.ReadyModule(installation.Version), Text.Ready));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed record ServiceCardViewModel(string Name, string Description, string Detail, string State);
