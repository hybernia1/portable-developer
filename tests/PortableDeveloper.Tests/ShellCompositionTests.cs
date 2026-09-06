namespace PortableDeveloper.Tests;

public sealed class ShellCompositionTests
{
    [Fact]
    public void Main_shell_uses_shared_components_and_one_workspace_style_dictionary()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var app = File.ReadAllText(Path.Combine(appRoot, "App.xaml"));
        var window = File.ReadAllText(Path.Combine(appRoot, "MainWindow.xaml"));
        var theme = File.ReadAllText(Path.Combine(appRoot, "Assets", "Theme.xaml"));
        var workspaceStyles = File.ReadAllText(Path.Combine(appRoot, "Assets", "WorkspaceStyles.xaml"));

        Assert.Contains("Assets/WorkspaceStyles.xaml", app, StringComparison.Ordinal);
        Assert.Contains("Assets/FileManagerStyles.xaml", app, StringComparison.Ordinal);
        Assert.Contains("Assets/GuideStyles.xaml", app, StringComparison.Ordinal);
        Assert.Contains("<controls:AppSidebar", window, StringComparison.Ordinal);
        Assert.Contains("<controls:WorkspaceHeader", window, StringComparison.Ordinal);
        Assert.DoesNotContain("<Window.Resources>", window, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Key=\"GroupedNavigation\"", window, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Key=\"PanelCardStyle\"", window, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Key=\"NavigationItemStyle\"", window, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"PanelCardStyle\"", workspaceStyles, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"NavigationItemStyle\"", workspaceStyles, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"SectionTabControlStyle\"", workspaceStyles, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"AppContextMenuStyle\"", workspaceStyles, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"VirtualizedListBoxStyle\"", workspaceStyles, StringComparison.Ordinal);
        Assert.Contains("Color=\"#0D1117\"", theme, StringComparison.Ordinal);
        Assert.Contains("Color=\"#161B22\"", theme, StringComparison.Ordinal);
        Assert.DoesNotContain("Color=\"#F6F8FC\"", theme, StringComparison.Ordinal);
    }

    [Fact]
    public void Large_feature_handlers_are_kept_in_focused_main_window_partials()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var mainCode = File.ReadAllText(Path.Combine(appRoot, "MainWindow.xaml.cs"));
        var partials = new Dictionary<string, string>
        {
            ["MainWindow.Guides.cs"] = "private void ApplyGuideFilters",
            ["MainWindow.PackageManagement.cs"] = "private async Task InstallPackageAsync",
            ["MainWindow.FileManager.cs"] = "private async void RefreshWorkspaceFiles",
            ["MainWindow.Projects.cs"] = "private async void CreateGeneralProject_Click",
            ["MainWindow.Scheduler.cs"] = "private void RefreshScheduledTaskBindings",
            ["MainWindow.Selenium.cs"] = "private async Task ToggleSeleniumAsync",
            ["MainWindow.Services.cs"] = "private async Task ToggleApacheAsync",
            ["MainWindow.Storage.cs"] = "private async Task RefreshStorageUsageAsync",
            ["MainWindow.Terminal.cs"] = "private async Task ExecuteTerminalCommandAsync"
        };

        Assert.DoesNotContain("private void ApplyGuideFilters", mainCode, StringComparison.Ordinal);
        Assert.DoesNotContain("private async Task InstallPackageAsync", mainCode, StringComparison.Ordinal);
        Assert.True(mainCode.Split('\n').Length < 700, "MainWindow.xaml.cs should remain shell composition and lifecycle code.");
        foreach (var partial in partials)
        {
            var code = File.ReadAllText(Path.Combine(appRoot, partial.Key));
            Assert.Contains(partial.Value, code, StringComparison.Ordinal);
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
