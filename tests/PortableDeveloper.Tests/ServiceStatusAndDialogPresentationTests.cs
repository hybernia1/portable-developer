namespace PortableDeveloper.Tests;

public sealed class ServiceStatusAndDialogPresentationTests
{
    [Fact]
    public void Server_navigation_shows_running_state_dots_only_for_controllable_services()
    {
        var repositoryRoot = FindRepositoryRoot();
        var window = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "PortableDeveloper.App",
            "Controls",
            "AppSidebar.xaml"));

        Assert.Contains("AppDangerBorderBrush", window, StringComparison.Ordinal);
        Assert.Contains("AppSuccessBrush", window, StringComparison.Ordinal);
        Assert.Contains("DataContext.ApacheIsRunning", window, StringComparison.Ordinal);
        Assert.Contains("DataContext.MariaDbIsRunning", window, StringComparison.Ordinal);
        Assert.Contains("DataContext.SeleniumIsRunning", window, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_application_modal_uses_the_shared_visible_border_style()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var theme = File.ReadAllText(Path.Combine(appRoot, "Assets", "Theme.xaml"));
        var dialogs = new[]
        {
            "ConfirmationDialog.xaml",
            "FileConflictDialog.xaml",
            "NamePromptDialog.xaml",
            "ProjectWebSettingsDialog.xaml",
            "ScheduledTaskDialog.xaml"
        };

        Assert.Contains("x:Key=\"AppDialogWindowStyle\"", theme, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"BorderBrush\" Value=\"{StaticResource AppBorderBrush}\" />", theme, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"BorderThickness\" Value=\"1\" />", theme, StringComparison.Ordinal);
        foreach (var dialog in dialogs)
        {
            var xaml = File.ReadAllText(Path.Combine(appRoot, dialog));
            Assert.Contains("Style=\"{StaticResource AppDialogWindowStyle}\"", xaml, StringComparison.Ordinal);
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "PortableDeveloper.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("PortableDeveloper.slnx was not found above the test output directory.");
    }
}
