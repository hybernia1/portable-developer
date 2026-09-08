namespace PortableDeveloper.Tests;

public sealed class ModuleCatalogPresentationTests
{
    [Fact]
    public void Module_groups_share_identity_counts_and_icon_first_actions()
    {
        var view = File.ReadAllText(Path.Combine(FindAppRoot(), "Views", "ModulesPageView.xaml"));

        Assert.Equal(3, view.Split("<controls:SectionHeader Icon", StringSplitOptions.None).Length - 1);
        Assert.Contains("Metadata=\"{Binding WebStackPackages.Count}\"", view, StringComparison.Ordinal);
        Assert.Contains("Metadata=\"{Binding DevelopmentPackages.Count}\"", view, StringComparison.Ordinal);
        Assert.Contains("Metadata=\"{Binding AutomationPackages.Count}\"", view, StringComparison.Ordinal);
        Assert.Contains("<controls:BrandLogo", view, StringComparison.Ordinal);
        Assert.Contains("IconInstall", view, StringComparison.Ordinal);
        Assert.Contains("IconSuccess", view, StringComparison.Ordinal);
        Assert.Contains("CompactBreakpoint=\"680\"", view, StringComparison.Ordinal);
        Assert.Contains("InstallRuntimePackage_Click", view, StringComparison.Ordinal);
        Assert.DoesNotContain("FontSize=\"17\" FontWeight=\"SemiBold\"", view, StringComparison.Ordinal);
        Assert.DoesNotContain("<DataGrid", view, StringComparison.Ordinal);
        Assert.DoesNotContain("<Path", view, StringComparison.Ordinal);
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
