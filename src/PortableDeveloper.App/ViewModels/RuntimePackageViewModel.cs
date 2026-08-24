using System.ComponentModel;
using System.Runtime.CompilerServices;
using PortableDeveloper.Application.Packages;

namespace PortableDeveloper.App.ViewModels;

public sealed class RuntimePackageViewModel : INotifyPropertyChanged
{
    private bool _isInstalled;
    private bool _isBusy;
    private bool _managerBusy;
    private int _progress;
    private string _status;
    private string _downloadDetail = string.Empty;

    public RuntimePackageViewModel(
        RuntimePackageKind kind,
        string name,
        string description,
        string version,
        bool isInstalled,
        string status)
    {
        Kind = kind;
        Name = name;
        Description = description;
        Version = version;
        _isInstalled = isInstalled;
        _status = status;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public RuntimePackageKind Kind { get; }

    public string Name { get; }

    public string Description { get; }

    public string Version { get; }

    public bool IsInstalled
    {
        get => _isInstalled;
        private set => SetField(ref _isInstalled, value);
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(CanInstall));
            }
        }
    }

    public bool CanInstall => !IsInstalled && !IsBusy && !_managerBusy;

    public int Progress
    {
        get => _progress;
        private set => SetField(ref _progress, value);
    }

    public string Status
    {
        get => _status;
        private set
        {
            if (SetField(ref _status, value))
            {
                OnPropertyChanged(nameof(HasStatus));
            }
        }
    }

    public bool HasStatus => !string.IsNullOrWhiteSpace(Status);

    public string IconKind => Kind.ToString();

    public string? PrimaryBrandLogo => Kind switch
    {
        RuntimePackageKind.WebStack => "apache",
        RuntimePackageKind.Database => "mariadb",
        RuntimePackageKind.Selenium => "selenium",
        RuntimePackageKind.Composer => "composer",
        RuntimePackageKind.Python => "python",
        RuntimePackageKind.Editor => "notepadplusplus",
        RuntimePackageKind.PhpMyAdmin => "phpmyadmin",
        RuntimePackageKind.SeleniumChromeEnvironment => "googlechrome",
        RuntimePackageKind.SeleniumFirefoxEnvironment => "firefox",
        _ => null
    };

    public string? SecondaryBrandLogo => Kind == RuntimePackageKind.WebStack ? "php" : null;

    public bool HasPrimaryBrandLogo => PrimaryBrandLogo is not null;

    public bool HasSecondaryBrandLogo => SecondaryBrandLogo is not null;

    public string DownloadDetail
    {
        get => _downloadDetail;
        private set => SetField(ref _downloadDetail, value);
    }

    public void SetProgress(int percentage, string status, string downloadDetail = "")
    {
        IsBusy = true;
        Progress = Math.Clamp(percentage, 0, 100);
        Status = status;
        DownloadDetail = downloadDetail;
    }

    public void SetManagerBusy(bool busy)
    {
        if (_managerBusy == busy)
        {
            return;
        }

        _managerBusy = busy;
        OnPropertyChanged(nameof(CanInstall));
    }

    public void Complete(bool installed, string status)
    {
        IsBusy = false;
        IsInstalled = installed;
        Progress = installed ? 100 : 0;
        Status = status;
        DownloadDetail = string.Empty;
        OnPropertyChanged(nameof(CanInstall));
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
