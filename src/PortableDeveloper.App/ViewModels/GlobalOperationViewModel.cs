using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PortableDeveloper.App.ViewModels;

public sealed class GlobalOperationViewModel : INotifyPropertyChanged
{
    private bool _isBusy;
    private string _status = string.Empty;
    private bool _isIndeterminate;
    private int _progress;

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

    public void Begin(string status, bool isIndeterminate = true, int progress = 0)
    {
        Status = status;
        IsIndeterminate = isIndeterminate;
        Progress = Math.Clamp(progress, 0, 100);
        IsBusy = true;
    }

    public void Update(string status, bool isIndeterminate, int progress)
    {
        Status = status;
        IsIndeterminate = isIndeterminate;
        Progress = Math.Clamp(progress, 0, 100);
    }

    public void End()
    {
        IsBusy = false;
        IsIndeterminate = false;
        Progress = 0;
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
