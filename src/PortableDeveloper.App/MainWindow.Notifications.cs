using PortableDeveloper.App.ViewModels;

namespace PortableDeveloper.App;

public partial class MainWindow
{
    private void ShowTransientNotification(
        string message,
        TransientNotificationIntent intent = TransientNotificationIntent.Success) =>
        _dashboard.Notifications.Show(message, intent);
}
