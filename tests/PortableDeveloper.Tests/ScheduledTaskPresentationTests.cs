namespace PortableDeveloper.Tests;

public sealed class ScheduledTaskPresentationTests
{
    [Fact]
    public void Scheduler_navigation_and_page_are_wired_into_the_shell()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var window = File.ReadAllText(Path.Combine(appRoot, "MainWindow.xaml"));
        var navigation = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "NavigationPage.cs"));

        Assert.Contains("Scheduler,", navigation, StringComparison.Ordinal);
        Assert.Contains("ConverterParameter=Scheduler", window, StringComparison.Ordinal);
        Assert.Contains("NewScheduledTask_Click", window, StringComparison.Ordinal);
        Assert.Contains("RunScheduledTask_Click", window, StringComparison.Ordinal);
        Assert.Contains("ScheduledTaskHistory", window, StringComparison.Ordinal);
    }

    [Fact]
    public void Scheduler_copy_is_available_in_czech_and_english()
    {
        var repositoryRoot = FindRepositoryRoot();
        var text = File.ReadAllText(Path.Combine(repositoryRoot, "src", "PortableDeveloper.App", "ViewModels", "UiText.cs"));

        Assert.Contains("Plánovač", text, StringComparison.Ordinal);
        Assert.Contains("Scheduler", text, StringComparison.Ordinal);
        Assert.Contains("Při spuštění aplikace", text, StringComparison.Ordinal);
        Assert.Contains("Application start", text, StringComparison.Ordinal);
        Assert.Contains("while Portable Developer is open", text, StringComparison.Ordinal);
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
