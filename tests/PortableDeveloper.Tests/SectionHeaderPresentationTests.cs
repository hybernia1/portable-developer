namespace PortableDeveloper.Tests;

public sealed class SectionHeaderPresentationTests
{
    [Fact]
    public void Shared_section_header_owns_identity_metadata_and_optional_actions()
    {
        var appRoot = FindAppRoot();
        var header = File.ReadAllText(Path.Combine(appRoot, "Controls", "SectionHeader.xaml"));

        Assert.Contains("<controls:AdaptiveSplitPanel", header, StringComparison.Ordinal);
        Assert.Contains("CompactBreakpoint=\"520\"", header, StringComparison.Ordinal);
        Assert.Contains("<controls:AppIcon", header, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding Heading, ElementName=Root}\"", header, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding Description, ElementName=Root}\"", header, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding Metadata, ElementName=Root}\"", header, StringComparison.Ordinal);
        Assert.Contains("Content=\"{Binding Actions, ElementName=Root}\"", header, StringComparison.Ordinal);
        Assert.DoesNotContain("PanelCardStyle", header, StringComparison.Ordinal);
        Assert.DoesNotContain("<ControlTemplate", header, StringComparison.Ordinal);
        Assert.DoesNotContain("<Path", header, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("SettingsPageView.xaml", 3)]
    [InlineData("DatabasesPageView.xaml", 4)]
    [InlineData("SeleniumPageView.xaml", 4)]
    public void Information_and_catalog_surfaces_use_the_shared_header(string file, int expectedCount)
    {
        var view = File.ReadAllText(Path.Combine(FindAppRoot(), "Views", file));

        Assert.Equal(
            expectedCount,
            view.Split("<controls:SectionHeader Icon", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("FontSize=\"17\" FontWeight=\"SemiBold\"", view, StringComparison.Ordinal);
    }

    [Fact]
    public void Section_commands_keep_original_handlers_and_add_semantic_icons()
    {
        var appRoot = FindAppRoot();
        var settings = File.ReadAllText(Path.Combine(appRoot, "Views", "SettingsPageView.xaml"));
        var databases = File.ReadAllText(Path.Combine(appRoot, "Views", "DatabasesPageView.xaml"));
        var selenium = File.ReadAllText(Path.Combine(appRoot, "Views", "SeleniumPageView.xaml"));

        Assert.Contains("RefreshStorageUsage_Click", settings, StringComparison.Ordinal);
        Assert.Contains("ClearAllStorageCaches_Click", settings, StringComparison.Ordinal);
        Assert.Contains("IconRefresh", settings, StringComparison.Ordinal);
        Assert.Contains("IconDelete", settings, StringComparison.Ordinal);
        Assert.Contains("IconPackage", settings, StringComparison.Ordinal);
        Assert.DoesNotContain("IconArchive", settings, StringComparison.Ordinal);

        Assert.Contains("OpenPhpMyAdmin_Click", databases, StringComparison.Ordinal);
        Assert.Contains("RefreshDatabases_Click", databases, StringComparison.Ordinal);
        Assert.Contains("IconGlobe", databases, StringComparison.Ordinal);
        Assert.Contains("IconDatabase", databases, StringComparison.Ordinal);

        Assert.Contains("ReloadSeleniumDrivers_Click", selenium, StringComparison.Ordinal);
        Assert.Contains("RefreshSeleniumSessions_Click", selenium, StringComparison.Ordinal);
        Assert.Contains("IconRefresh", selenium, StringComparison.Ordinal);
    }

    [Fact]
    public void Selenium_browser_catalog_is_the_single_browser_inventory_surface()
    {
        var selenium = File.ReadAllText(Path.Combine(FindAppRoot(), "Views", "SeleniumPageView.xaml"));

        Assert.Equal(1, selenium.Split("ItemsSource=\"{Binding SeleniumDriverPackages}\"", StringSplitOptions.None).Length - 1);
        Assert.DoesNotContain("ItemsSource=\"{Binding SeleniumDrivers}\"", selenium, StringComparison.Ordinal);
        Assert.DoesNotContain("InstalledSeleniumDrivers", selenium, StringComparison.Ordinal);
        Assert.DoesNotContain("SeleniumDriverCount", selenium, StringComparison.Ordinal);
    }

    private static string FindAppRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var appRoot = Path.Combine(directory.FullName, "src", "PortableDeveloper.App");
            if (Directory.Exists(appRoot))
            {
                return appRoot;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("PortableDeveloper.App was not found above the test output directory.");
    }
}
