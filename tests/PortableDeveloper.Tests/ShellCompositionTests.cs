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
        var sidebar = File.ReadAllText(Path.Combine(appRoot, "Controls", "AppSidebar.xaml"));
        var workspaceHeader = File.ReadAllText(Path.Combine(appRoot, "Controls", "WorkspaceHeader.xaml"));
        var guideRenderer = File.ReadAllText(Path.Combine(appRoot, "Guides", "MarkdownGuideRenderer.cs"));
        var allXaml = string.Join(
            Environment.NewLine,
            Directory.EnumerateFiles(appRoot, "*.xaml", SearchOption.AllDirectories)
                .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .Select(File.ReadAllText));

        Assert.Contains("Assets/WorkspaceStyles.xaml", app, StringComparison.Ordinal);
        Assert.DoesNotContain("Assets/FileManagerStyles.xaml", app, StringComparison.Ordinal);
        Assert.DoesNotContain("Assets/GuideStyles.xaml", app, StringComparison.Ordinal);
        Assert.Contains("<controls:AppSidebar", window, StringComparison.Ordinal);
        Assert.Contains("<controls:WorkspaceHeader", window, StringComparison.Ordinal);
        Assert.DoesNotContain("<Window.Resources>", window, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Key=\"GroupedNavigation\"", window, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Key=\"PanelCardStyle\"", window, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Key=\"NavigationItemStyle\"", window, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"PanelCardStyle\"", workspaceStyles, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"NavigationItemStyle\"", workspaceStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Key=\"ReadOnlyDataGridStyle\"", workspaceStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Key=\"VirtualizedListBoxStyle\"", workspaceStyles, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"PageScrollViewerStyle\"", workspaceStyles, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"SectionVisibilityConverter\"", workspaceStyles, StringComparison.Ordinal);
        Assert.Contains("ThemeMode=\"System\"", app, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource CardBackgroundFillColorDefaultBrush}", workspaceStyles, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource TextFillColorSecondaryBrush}", allXaml, StringComparison.Ordinal);
        Assert.Contains("BasedOn=\"{StaticResource {x:Type ListBoxItem}}\"", workspaceStyles, StringComparison.Ordinal);
        Assert.Contains("BasedOn=\"{StaticResource {x:Type Button}}\"", workspaceStyles, StringComparison.Ordinal);
        Assert.Contains("BasedOn=\"{StaticResource {x:Type ScrollViewer}}\"", workspaceStyles, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Type DataGrid", workspaceStyles, StringComparison.Ordinal);
        Assert.Contains("MinHeight\" Value=\"32\"", workspaceStyles, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource PageScrollViewerStyle}\"", allXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<DataGrid", allXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<TabControl", allXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<TabItem", allXaml, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding NavigationSections}\"", workspaceHeader, StringComparison.Ordinal);
        Assert.Contains("SelectedValue=\"{Binding SelectedSection, Mode=TwoWay}\"", workspaceHeader, StringComparison.Ordinal);
        Assert.DoesNotContain("Breadcrumb", workspaceHeader, StringComparison.Ordinal);
        Assert.DoesNotContain("Text.ApplicationTitle", workspaceHeader, StringComparison.Ordinal);
        Assert.Contains("<ItemsControl ItemsSource=\"{Binding SeleniumProfiles}\">", allXaml, StringComparison.Ordinal);
        Assert.Contains("<ItemsControl ItemsSource=\"{Binding SeleniumCookieVaults}\">", allXaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"CopySeleniumProfileId_Click\"", allXaml, StringComparison.Ordinal);
        Assert.Contains("Click=\"CopyCookieVaultId_Click\"", allXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("AncestorType=DataGrid", allXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("HeadersVisibility=\"None\"", allXaml, StringComparison.Ordinal);
        Assert.Contains("<Border Background=\"{DynamicResource LayerFillColorDefaultBrush}\"", window, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"SidebarScrollViewer\"", window, StringComparison.Ordinal);
        Assert.Contains("MinHeight=\"{Binding ViewportHeight, ElementName=SidebarScrollViewer}\"", window, StringComparison.Ordinal);
        Assert.Contains("ScrollViewer.VerticalScrollBarVisibility=\"Disabled\"", sidebar, StringComparison.Ordinal);
        Assert.Contains("DataContext=\"{Binding Shell}\"", window, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding CanNavigate}\"", sidebar, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding CanChangeProject}\"", workspaceHeader, StringComparison.Ordinal);
        Assert.DoesNotContain("GlobalOperation", sidebar, StringComparison.Ordinal);
        Assert.DoesNotContain("GlobalOperation", workspaceHeader, StringComparison.Ordinal);
        Assert.DoesNotContain("Panel.ZIndex=\"100\"", window, StringComparison.Ordinal);
        Assert.DoesNotContain("GlobalOperation.", window, StringComparison.Ordinal);
        Assert.DoesNotContain("LayerFillColorDefaultBrush", sidebar, StringComparison.Ordinal);
        Assert.DoesNotContain("<ColumnDefinition Width=\"300\"", window, StringComparison.Ordinal);
        Assert.DoesNotContain("<ColumnDefinition Width=\"286\"", window, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AccentButtonStyle}\"", allXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("AppWindowBrush", theme, StringComparison.Ordinal);
        Assert.DoesNotContain("AppForegroundBrush", guideRenderer, StringComparison.Ordinal);
        Assert.DoesNotContain("AppOverlayBrush", allXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("AppButtonStyle", allXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("SectionTabControlStyle", allXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("AppContextMenuStyle", allXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Color=\"{DynamicResource", theme, StringComparison.Ordinal);
        Assert.DoesNotContain("<ControlTemplate", allXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<Style TargetType=\"{x:Type ListBoxItem}\">", allXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<Style TargetType=\"{x:Type Button}\">", allXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<Style TargetType=\"Button\">", allXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<Style TargetType=\"{x:Type TextBox}\">", allXaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<controls:AppTitleBar", window, StringComparison.Ordinal);
        Assert.DoesNotContain("WindowStyle=\"None\"", window, StringComparison.Ordinal);
        Assert.DoesNotContain("Color=\"#0D1117\"", theme, StringComparison.Ordinal);
        Assert.False(File.Exists(Path.Combine(appRoot, "Controls", "AppTitleBar.xaml")));
        Assert.False(File.Exists(Path.Combine(appRoot, "Controls", "AppWindowChrome.cs")));
        Assert.False(File.Exists(Path.Combine(appRoot, "Assets", "FileManagerStyles.xaml")));
        Assert.False(File.Exists(Path.Combine(appRoot, "Assets", "GuideStyles.xaml")));
        Assert.False(File.Exists(Path.Combine(appRoot, "Theming", "FluentThemeResourceBridge.cs")));
    }

    [Fact]
    public void Large_feature_handlers_are_kept_in_focused_main_window_partials()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var mainCode = File.ReadAllText(Path.Combine(appRoot, "MainWindow.xaml.cs"));
        Assert.DoesNotContain("private void ApplyGuideFilters", mainCode, StringComparison.Ordinal);
        Assert.DoesNotContain("private async Task InstallPackageAsync", mainCode, StringComparison.Ordinal);
        Assert.True(mainCode.Split('\n').Length < 700, "MainWindow.xaml.cs should remain shell composition and lifecycle code.");
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
