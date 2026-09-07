using System.Windows;

namespace PortableDeveloper.App.Shell;

public static class WorkspaceLayout
{
    public static readonly DependencyProperty ModeProperty = DependencyProperty.RegisterAttached(
        "Mode",
        typeof(WorkspaceLayoutMode),
        typeof(WorkspaceLayout),
        new FrameworkPropertyMetadata(
            WorkspaceLayoutMode.Wide,
            FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static WorkspaceLayoutMode GetMode(DependencyObject element) =>
        (WorkspaceLayoutMode)element.GetValue(ModeProperty);

    public static void SetMode(DependencyObject element, WorkspaceLayoutMode value) =>
        element.SetValue(ModeProperty, value);
}
