using System.Windows;
using System.Windows.Controls;

namespace PortableDeveloper.App.Controls;

public partial class WorkspaceHeader : UserControl
{
    public WorkspaceHeader()
    {
        InitializeComponent();
    }

    public event SelectionChangedEventHandler? ProjectSelectionChanged;

    public event RoutedEventHandler? ManageProjectsRequested;

    private void ProjectSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ProjectSelectionChanged?.Invoke(sender, e);

    private void ManageProjectsButton_Click(object sender, RoutedEventArgs e) =>
        ManageProjectsRequested?.Invoke(sender, e);
}
