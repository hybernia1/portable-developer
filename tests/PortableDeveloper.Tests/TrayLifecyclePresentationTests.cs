namespace PortableDeveloper.Tests;

public sealed class TrayLifecyclePresentationTests
{
    [Fact]
    public void Window_close_hides_to_tray_and_tray_exit_requires_confirmation()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var project = File.ReadAllText(Path.Combine(appRoot, "PortableDeveloper.App.csproj"));
        var window = File.ReadAllText(Path.Combine(appRoot, "MainWindow.xaml.cs"));

        Assert.Contains("<UseWindowsForms>true</UseWindowsForms>", project, StringComparison.Ordinal);
        Assert.Contains("Forms.NotifyIcon", window, StringComparison.Ordinal);
        Assert.Contains("HideToTray();", window, StringComparison.Ordinal);
        Assert.Contains("public void RestoreFromTray()", window, StringComparison.Ordinal);
        Assert.Contains("ConfirmationDialog.Show(", window, StringComparison.Ordinal);
        Assert.Contains("SessionEnding", window, StringComparison.Ordinal);
    }

    [Fact]
    public void Tray_actions_and_explanation_are_available_in_czech_and_english()
    {
        var repositoryRoot = FindRepositoryRoot();
        var text = File.ReadAllText(Path.Combine(repositoryRoot, "src", "PortableDeveloper.App", "ViewModels", "UiText.cs"));

        Assert.Contains("Otevřít Portable Developer", text, StringComparison.Ordinal);
        Assert.Contains("Open Portable Developer", text, StringComparison.Ordinal);
        Assert.Contains("Ukončit Portable Developer", text, StringComparison.Ordinal);
        Assert.Contains("Exit Portable Developer", text, StringComparison.Ordinal);
        Assert.Contains("oznamovací oblasti", text, StringComparison.Ordinal);
        Assert.Contains("notification area", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Single_instance_activation_restores_a_hidden_main_window()
    {
        var repositoryRoot = FindRepositoryRoot();
        var app = File.ReadAllText(Path.Combine(repositoryRoot, "src", "PortableDeveloper.App", "App.xaml.cs"));

        Assert.Contains("mainWindow.RestoreFromTray();", app, StringComparison.Ordinal);
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
