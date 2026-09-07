using System.Windows;
using System.Windows.Controls;
using PortableDeveloper.App.ViewModels;

namespace PortableDeveloper.App.Views;

public partial class ModulesPageView : UserControl
{
    public static readonly RoutedEvent InstallRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(InstallRequested),
        RoutingStrategy.Bubble,
        typeof(EventHandler<RuntimePackageInstallRequestedEventArgs>),
        typeof(ModulesPageView));

    public ModulesPageView()
    {
        InitializeComponent();
    }

    public event EventHandler<RuntimePackageInstallRequestedEventArgs> InstallRequested
    {
        add => AddHandler(InstallRequestedEvent, value);
        remove => RemoveHandler(InstallRequestedEvent, value);
    }

    private void InstallRuntimePackage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: RuntimePackageViewModel package })
        {
            RaiseEvent(new RuntimePackageInstallRequestedEventArgs(InstallRequestedEvent, package));
        }
    }
}

public sealed class RuntimePackageInstallRequestedEventArgs(
    RoutedEvent routedEvent,
    RuntimePackageViewModel package) : RoutedEventArgs(routedEvent)
{
    public RuntimePackageViewModel Package { get; } = package;
}
