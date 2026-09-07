using System.Windows;
using System.Windows.Controls;

namespace PortableDeveloper.App.Views;

public partial class PortsPageView : UserControl
{
    public static readonly RoutedEvent RefreshRequestedEvent = RegisterEvent(nameof(RefreshRequested));
    public static readonly RoutedEvent SaveRequestedEvent = RegisterEvent(nameof(SaveRequested));
    public static readonly RoutedEvent InputsChangedEvent = RegisterEvent(nameof(InputsChanged));

    public PortsPageView()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler RefreshRequested
    {
        add => AddHandler(RefreshRequestedEvent, value);
        remove => RemoveHandler(RefreshRequestedEvent, value);
    }

    public event RoutedEventHandler SaveRequested
    {
        add => AddHandler(SaveRequestedEvent, value);
        remove => RemoveHandler(SaveRequestedEvent, value);
    }

    public event RoutedEventHandler InputsChanged
    {
        add => AddHandler(InputsChangedEvent, value);
        remove => RemoveHandler(InputsChangedEvent, value);
    }

    private static RoutedEvent RegisterEvent(string name) => EventManager.RegisterRoutedEvent(
        name,
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(PortsPageView));

    private void RefreshPorts_Click(object sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(RefreshRequestedEvent));

    private void SavePorts_Click(object sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(SaveRequestedEvent));

    private void PortTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        }

        RaiseEvent(new RoutedEventArgs(InputsChangedEvent));
    }
}
