using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PortableDeveloper.App.ViewModels;

public sealed class GlobalOperationViewModel : INotifyPropertyChanged
{
    private bool _isBusy;
    private string _status = string.Empty;
    private bool _isIndeterminate;
    private int _progress;
    private string _detail = string.Empty;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetField(ref _isBusy, value);
    }

    public string Status
    {
        get => _status;
        private set => SetField(ref _status, value);
    }

    public bool IsIndeterminate
    {
        get => _isIndeterminate;
        private set => SetField(ref _isIndeterminate, value);
    }

    public int Progress
    {
        get => _progress;
        private set => SetField(ref _progress, value);
    }

    public string Detail
    {
        get => _detail;
        private set => SetField(ref _detail, value);
    }

    public void Begin(string status, bool isIndeterminate = true, int progress = 0, string detail = "")
    {
        Status = status;
        IsIndeterminate = isIndeterminate;
        Progress = Math.Clamp(progress, 0, 100);
        Detail = detail;
        IsBusy = true;
    }

    public void Update(string status, bool isIndeterminate, int progress, string detail = "")
    {
        Status = status;
        IsIndeterminate = isIndeterminate;
        Progress = Math.Clamp(progress, 0, 100);
        Detail = detail;
    }

    public void End()
    {
        IsBusy = false;
        IsIndeterminate = false;
        Progress = 0;
        Detail = string.Empty;
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
