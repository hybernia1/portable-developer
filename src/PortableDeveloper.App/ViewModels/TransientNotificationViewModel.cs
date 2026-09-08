using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;

namespace PortableDeveloper.App.ViewModels;

public enum TransientNotificationIntent
{
    Information,
    Success,
    Error
}

public sealed class TransientNotificationViewModel : INotifyPropertyChanged
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(7);
    private readonly DispatcherTimer _dismissTimer;
    private string _message = string.Empty;
    private TransientNotificationIntent _intent;
    private bool _isVisible;
    private DateTimeOffset _expiresAt;
    private TimeSpan _remaining = DefaultDuration;

    public TransientNotificationViewModel()
    {
        _dismissTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = DefaultDuration
        };
        _dismissTimer.Tick += DismissTimer_Tick;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Message
    {
        get => _message;
        private set => SetField(ref _message, value);
    }

    public TransientNotificationIntent Intent
    {
        get => _intent;
        private set => SetField(ref _intent, value);
    }

    public bool IsVisible
    {
        get => _isVisible;
        private set => SetField(ref _isVisible, value);
    }

    public void Show(
        string message,
        TransientNotificationIntent intent = TransientNotificationIntent.Success,
        TimeSpan? duration = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        Message = message;
        Intent = intent;
        IsVisible = true;
        _remaining = duration is { } requested && requested > TimeSpan.Zero
            ? requested
            : DefaultDuration;
        RestartTimer(_remaining);
    }

    public void Pause()
    {
        if (!_dismissTimer.IsEnabled || !IsVisible)
        {
            return;
        }

        _remaining = _expiresAt - DateTimeOffset.UtcNow;
        if (_remaining < TimeSpan.Zero)
        {
            _remaining = TimeSpan.Zero;
        }

        _dismissTimer.Stop();
    }

    public void Resume()
    {
        if (!IsVisible || _dismissTimer.IsEnabled)
        {
            return;
        }

        if (_remaining <= TimeSpan.Zero)
        {
            Dismiss();
            return;
        }

        RestartTimer(_remaining);
    }

    public void Dismiss()
    {
        _dismissTimer.Stop();
        IsVisible = false;
        Message = string.Empty;
        _remaining = DefaultDuration;
    }

    private void RestartTimer(TimeSpan duration)
    {
        _dismissTimer.Stop();
        _dismissTimer.Interval = duration;
        _expiresAt = DateTimeOffset.UtcNow + duration;
        _dismissTimer.Start();
    }

    private void DismissTimer_Tick(object? sender, EventArgs e) => Dismiss();

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
