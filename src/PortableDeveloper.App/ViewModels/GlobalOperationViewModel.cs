using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PortableDeveloper.App.ViewModels;

public sealed class GlobalOperationViewModel : INotifyPropertyChanged
{
    private bool _isBusy;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetField(ref _isBusy, value))
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsIdle)));
            }
        }
    }

    public bool IsIdle => !IsBusy;

    public void Begin() => IsBusy = true;

    public void End()
    {
        IsBusy = false;
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
