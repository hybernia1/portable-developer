using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PortableDeveloper.App.ViewModels;

public sealed class PhpExtensionViewModel : INotifyPropertyChanged
{
    private bool _isEnabled;

    public PhpExtensionViewModel(string name, bool isRequired, bool isAvailable, bool isEnabled)
    {
        Name = name;
        IsRequired = isRequired;
        IsAvailable = isAvailable;
        _isEnabled = isRequired || isEnabled;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name { get; }

    public bool IsRequired { get; }

    public bool IsAvailable { get; }

    public bool CanToggle => IsAvailable && !IsRequired;

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            var normalized = IsRequired || (IsAvailable && value);
            if (_isEnabled == normalized)
            {
                return;
            }

            _isEnabled = normalized;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
