using System.Windows;
using System.Windows.Controls;

namespace PortableDeveloper.App.Views;

public partial class PhpPageView : UserControl
{
    public static readonly RoutedEvent SaveRequestedEvent = RegisterEvent(nameof(SaveRequested));
    public static readonly RoutedEvent ResetRequestedEvent = RegisterEvent(nameof(ResetRequested));
    public static readonly RoutedEvent EditCustomIniRequestedEvent = RegisterEvent(nameof(EditCustomIniRequested));

    public PhpPageView()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler SaveRequested
    {
        add => AddHandler(SaveRequestedEvent, value);
        remove => RemoveHandler(SaveRequestedEvent, value);
    }

    public event RoutedEventHandler ResetRequested
    {
        add => AddHandler(ResetRequestedEvent, value);
        remove => RemoveHandler(ResetRequestedEvent, value);
    }

    public event RoutedEventHandler EditCustomIniRequested
    {
        add => AddHandler(EditCustomIniRequestedEvent, value);
        remove => RemoveHandler(EditCustomIniRequestedEvent, value);
    }

    private static RoutedEvent RegisterEvent(string name) => EventManager.RegisterRoutedEvent(
        name,
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(PhpPageView));

    private void SavePhpSettings_Click(object sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(SaveRequestedEvent));

    private void ResetPhpSettings_Click(object sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(ResetRequestedEvent));

    private void EditCustomPhpIni_Click(object sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(EditCustomIniRequestedEvent));
}
