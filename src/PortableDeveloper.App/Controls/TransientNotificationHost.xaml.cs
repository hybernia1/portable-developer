using System.ComponentModel;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using PortableDeveloper.App.ViewModels;

namespace PortableDeveloper.App.Controls;

public partial class TransientNotificationHost : UserControl
{
    public TransientNotificationHost()
    {
        InitializeComponent();
        DataContextChanged += Host_DataContextChanged;
    }

    private void Host_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is TransientNotificationViewModel previous)
        {
            previous.PropertyChanged -= Notification_PropertyChanged;
        }

        if (e.NewValue is TransientNotificationViewModel current)
        {
            current.PropertyChanged += Notification_PropertyChanged;
        }
    }

    private void Notification_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (sender is not TransientNotificationViewModel { IsVisible: true }
            || e.PropertyName is not (nameof(TransientNotificationViewModel.Message)
                or nameof(TransientNotificationViewModel.IsVisible)))
        {
            return;
        }

        Dispatcher.BeginInvoke(() =>
        {
            var peer = UIElementAutomationPeer.FromElement(LiveRegion)
                ?? UIElementAutomationPeer.CreatePeerForElement(LiveRegion);
            peer?.RaiseAutomationEvent(AutomationEvents.LiveRegionChanged);
        });
    }

    private void Host_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (DataContext is TransientNotificationViewModel notification)
        {
            notification.Pause();
        }
    }

    private void Host_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (DataContext is TransientNotificationViewModel notification)
        {
            notification.Resume();
        }
    }

    private void Dismiss_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is TransientNotificationViewModel notification)
        {
            notification.Dismiss();
        }
    }
}
