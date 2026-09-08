namespace PortableDeveloper.Tests;

public sealed class ServiceStatusAndDialogPresentationTests
{
    [Fact]
    public void Server_navigation_shows_running_state_dots_only_for_controllable_services()
    {
        var repositoryRoot = FindRepositoryRoot();
        var sidebar = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "PortableDeveloper.App",
            "Controls",
            "AppSidebar.xaml"));
        var shell = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "PortableDeveloper.App",
            "Shell",
            "WorkspaceShellViewModel.cs"));

        Assert.Contains("SystemFillColorCriticalBrush", sidebar, StringComparison.Ordinal);
        Assert.Contains("SystemFillColorSuccessBrush", sidebar, StringComparison.Ordinal);
        Assert.Contains("{Binding HasStatus", sidebar, StringComparison.Ordinal);
        Assert.Contains("{Binding IsRunning}", sidebar, StringComparison.Ordinal);
        Assert.Contains("{Binding StatusText}", sidebar, StringComparison.Ordinal);
        Assert.DoesNotContain("ApacheIsRunning", sidebar, StringComparison.Ordinal);
        Assert.DoesNotContain("MariaDbIsRunning", sidebar, StringComparison.Ordinal);
        Assert.DoesNotContain("SeleniumIsRunning", sidebar, StringComparison.Ordinal);
        Assert.DoesNotContain("AncestorType=Window", sidebar, StringComparison.Ordinal);
        Assert.Contains("SetServiceStatus", shell, StringComparison.Ordinal);
    }

    [Fact]
    public void Every_application_modal_uses_native_window_chrome()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var dialogs = new[]
        {
            "ConfirmationDialog.xaml",
            "InformationDialog.xaml",
            "FileConflictDialog.xaml",
            "NamePromptDialog.xaml",
            "ProjectWebSettingsDialog.xaml",
            "ScheduledTaskDialog.xaml",
            "SeleniumProfileDialog.xaml",
            "CookieVaultImportDialog.xaml"
        };

        foreach (var dialog in dialogs)
        {
            var xaml = File.ReadAllText(Path.Combine(appRoot, dialog));
            Assert.Contains("ResizeMode=\"NoResize\"", xaml, StringComparison.Ordinal);
            Assert.Contains("Icon=\"Assets/portable-developer.ico\"", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("WindowStyle=\"None\"", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("AppTitleBar", xaml, StringComparison.Ordinal);
            Assert.Contains("<controls:DialogHeader", xaml, StringComparison.Ordinal);
            Assert.Contains("DialogFooterStyle", xaml, StringComparison.Ordinal);
            Assert.DoesNotContain("<Path", xaml, StringComparison.Ordinal);
        }

        var detailsDialog = File.ReadAllText(Path.Combine(appRoot, "ScheduledTaskRunDetailsDialog.xaml"));
        Assert.Contains("ResizeMode=\"CanResizeWithGrip\"", detailsDialog, StringComparison.Ordinal);
        Assert.Contains("<controls:DialogHeader", detailsDialog, StringComparison.Ordinal);
        Assert.Contains("DialogFooterStyle", detailsDialog, StringComparison.Ordinal);

        var scheduledTaskDialog = File.ReadAllText(Path.Combine(appRoot, "ScheduledTaskDialog.xaml"));
        Assert.True(
            scheduledTaskDialog.IndexOf("</ScrollViewer>", StringComparison.Ordinal) <
            scheduledTaskDialog.IndexOf("DialogFooterStyle", StringComparison.Ordinal));
        Assert.Contains("DialogPrimaryButtonStyle", scheduledTaskDialog, StringComparison.Ordinal);
        Assert.Contains("IconSave", scheduledTaskDialog, StringComparison.Ordinal);
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
