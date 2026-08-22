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

    public PackageManagerPageViewModel(string projectRelativePath)
    {
        ProjectRelativePath = projectRelativePath;
        Packages = new ObservableCollection<ProjectPackageInfo>();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string ProjectRelativePath { get; }

    public ObservableCollection<ProjectPackageInfo> Packages { get; }

    public bool RuntimeReady => _runtimeReady;

    public string RuntimeVersion => _runtimeVersion;

    public string RuntimeDetail => _runtimeDetail;

    public string Status => _status;

    public bool IsBusy => _isBusy;

    public bool CanOperate => _runtimeReady && !_isBusy;

    public bool NoPackages => Packages.Count == 0;

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

    public void SetPackages(IEnumerable<ProjectPackageInfo> packages)
    {
        Packages.Clear();
        foreach (var package in packages)
        {
            Packages.Add(package);
        }

        OnPropertyChanged(nameof(NoPackages));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
