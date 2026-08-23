using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PortableDeveloper.Application.ProjectTools;

namespace PortableDeveloper.App.ViewModels;

public sealed class PackageManagerPageViewModel : INotifyPropertyChanged
{
    private bool _runtimeReady;
    private string _runtimeVersion = string.Empty;
    private string _runtimeDetail = string.Empty;
    private string _status = string.Empty;
    private bool _isBusy;
    private bool _operationVisible;
    private string _operationStatus = string.Empty;
    private bool _operationIndeterminate;
    private int _operationPercentage;

    public PackageManagerPageViewModel(string projectRelativePath)
    {
        ProjectRelativePath = projectRelativePath;
        Packages = new ObservableCollection<ProjectPackageInfo>();
        DirectPackages = new ObservableCollection<ProjectPackageInfo>();
        TransitivePackages = new ObservableCollection<ProjectPackageInfo>();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ProjectRelativePath { get; private set; }

    public ObservableCollection<ProjectPackageInfo> Packages { get; }

    public ObservableCollection<ProjectPackageInfo> DirectPackages { get; }

    public ObservableCollection<ProjectPackageInfo> TransitivePackages { get; }

    public bool RuntimeReady => _runtimeReady;

    public string RuntimeVersion => _runtimeVersion;

    public string RuntimeDetail => _runtimeDetail;

    public string Status => _status;

    public bool IsBusy => _isBusy;

    public bool OperationVisible => _operationVisible;

    public string OperationStatus => _operationStatus;

    public bool OperationIndeterminate => _operationIndeterminate;

    public int OperationPercentage => _operationPercentage;

    public bool CanOperate => _runtimeReady && !_isBusy;

    public bool NoPackages => Packages.Count == 0;

    public bool HasTransitivePackages => TransitivePackages.Count > 0;

    public void SetProjectRelativePath(string projectRelativePath)
    {
        ProjectRelativePath = projectRelativePath;
        OnPropertyChanged(nameof(ProjectRelativePath));
    }

    public void SetRuntime(PortableToolRuntimeInfo runtime)
    {
        _runtimeReady = runtime.IsReady;
        _runtimeVersion = runtime.Version;
        _runtimeDetail = runtime.Detail;
        _status = runtime.Detail;
        OnPropertyChanged(nameof(RuntimeReady));
        OnPropertyChanged(nameof(RuntimeVersion));
        OnPropertyChanged(nameof(RuntimeDetail));
        OnPropertyChanged(nameof(Status));
        OnPropertyChanged(nameof(CanOperate));
    }

    public void SetStatus(string status)
    {
        _status = status;
        OnPropertyChanged(nameof(Status));
    }

    public void SetBusy(bool isBusy)
    {
        _isBusy = isBusy;
        OnPropertyChanged(nameof(IsBusy));
        OnPropertyChanged(nameof(CanOperate));
    }

    public void SetOperationProgress(ProjectPackageOperationProgress progress, string localizedStatus)
    {
        _operationVisible = true;
        _operationStatus = localizedStatus;
        _operationIndeterminate = progress.IsIndeterminate;
        _operationPercentage = progress.Percentage;
        OnPropertyChanged(nameof(OperationVisible));
        OnPropertyChanged(nameof(OperationStatus));
        OnPropertyChanged(nameof(OperationIndeterminate));
        OnPropertyChanged(nameof(OperationPercentage));
    }

    public void SetOperationResult(string localizedStatus, bool isSuccess)
    {
        _operationVisible = true;
        _operationStatus = localizedStatus;
        _operationIndeterminate = false;
        _operationPercentage = isSuccess ? 100 : 0;
        OnPropertyChanged(nameof(OperationVisible));
        OnPropertyChanged(nameof(OperationStatus));
        OnPropertyChanged(nameof(OperationIndeterminate));
        OnPropertyChanged(nameof(OperationPercentage));
    }

    public void SetPackages(IEnumerable<ProjectPackageInfo> packages)
    {
        Packages.Clear();
        DirectPackages.Clear();
        TransitivePackages.Clear();
        foreach (var package in packages)
        {
            Packages.Add(package);
            (package.IsDirectDependency ? DirectPackages : TransitivePackages).Add(package);
        }

        OnPropertyChanged(nameof(NoPackages));
        OnPropertyChanged(nameof(HasTransitivePackages));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
