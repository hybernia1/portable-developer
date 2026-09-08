namespace PortableDeveloper.Tests;

public sealed class RuntimeHeaderPresentationTests
{
    [Fact]
    public void Running_services_share_one_flat_runtime_header_composition()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var header = File.ReadAllText(Path.Combine(appRoot, "Controls", "RuntimeHeader.xaml"));

        Assert.Contains("<controls:AdaptiveSplitPanel", header, StringComparison.Ordinal);
        Assert.Contains("<controls:BrandLogo", header, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.Name=\"{Binding State, ElementName=Root}\"", header, StringComparison.Ordinal);
        Assert.Contains("{Binding IsRunning, ElementName=Root}", header, StringComparison.Ordinal);
        Assert.Contains("SystemFillColorCriticalBrush", header, StringComparison.Ordinal);
        Assert.Contains("SystemFillColorSuccessBrush", header, StringComparison.Ordinal);
        Assert.Contains("Content=\"{Binding Actions, ElementName=Root}\"", header, StringComparison.Ordinal);
        Assert.Contains("DividerStrokeColorDefaultBrush", header, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding State, ElementName=Root}\"", header, StringComparison.Ordinal);
        Assert.DoesNotContain("PanelCardStyle", header, StringComparison.Ordinal);
        Assert.DoesNotContain("<Path", header, StringComparison.Ordinal);

        var pages = new[]
        {
            (File: "ApachePageView.xaml", Brand: "apache", Toggle: "ToggleApache_Click"),
            (File: "DatabasesPageView.xaml", Brand: "mariadb", Toggle: "ToggleMariaDb_Click"),
            (File: "SeleniumPageView.xaml", Brand: "selenium", Toggle: "ToggleSelenium_Click")
        };

        foreach (var page in pages)
        {
            var xaml = File.ReadAllText(Path.Combine(appRoot, "Views", page.File));
            Assert.Equal(1, xaml.Split("<controls:RuntimeHeader Margin", StringSplitOptions.None).Length - 1);
            Assert.Contains($"Brand=\"{page.Brand}\"", xaml, StringComparison.Ordinal);
            Assert.Contains("IsRunning=\"{Binding", xaml, StringComparison.Ordinal);
            Assert.Contains("Version=\"{Binding", xaml, StringComparison.Ordinal);
            Assert.Contains("RuntimePrimaryButtonStyle", xaml, StringComparison.Ordinal);
            Assert.Contains("IconPlay", xaml, StringComparison.Ordinal);
            Assert.Contains("IconStop", xaml, StringComparison.Ordinal);
            Assert.Contains(page.Toggle, xaml, StringComparison.Ordinal);
        }

        var selenium = File.ReadAllText(Path.Combine(appRoot, "Views", "SeleniumPageView.xaml"));
        Assert.Equal(1, selenium.Split("Feedback=\"{Binding StatusText}\"", StringSplitOptions.None).Length - 1);
        Assert.Contains("RuntimeSecondaryButtonStyle", selenium, StringComparison.Ordinal);
        Assert.Contains("IconGlobe", selenium, StringComparison.Ordinal);
        Assert.Contains("Visibility=\"{Binding SeleniumIsRunning", selenium, StringComparison.Ordinal);
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
