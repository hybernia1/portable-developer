using System.Windows;
using System.Windows.Controls;

namespace PortableDeveloper.App.Views;

public partial class ApachePageView : UserControl
{
    public static readonly RoutedEvent ToggleRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(ToggleRequested),
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(ApachePageView));

    public ApachePageView()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler ToggleRequested
    {
        add => AddHandler(ToggleRequestedEvent, value);
        remove => RemoveHandler(ToggleRequestedEvent, value);
    }

    private void ToggleApache_Click(object sender, RoutedEventArgs e) =>
        RaiseEvent(new RoutedEventArgs(ToggleRequestedEvent));
}
