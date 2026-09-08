namespace PortableDeveloper.Tests;

public sealed class PortsAndPhpCatalogPresentationTests
{
    [Fact]
    public void Port_settings_and_listeners_use_form_and_catalog_grammar()
    {
        var appRoot = FindAppRoot();
        var view = File.ReadAllText(Path.Combine(appRoot, "Views", "PortsPageView.xaml"));
        var code = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Services.cs"));

        Assert.Contains("<controls:FormSection", view, StringComparison.Ordinal);
        Assert.Contains("<controls:SectionHeader", view, StringComparison.Ordinal);
        Assert.Contains("Metadata=\"{Binding TcpListeners.Count}\"", view, StringComparison.Ordinal);
        Assert.Contains("IconPorts", view, StringComparison.Ordinal);
        Assert.Contains("IconRefresh", view, StringComparison.Ordinal);
        Assert.Contains("IconSave", view, StringComparison.Ordinal);
        Assert.Contains("<controls:BrandLogo", view, StringComparison.Ordinal);
        Assert.Contains("TextChanged=\"PortTextBox_TextChanged\"", view, StringComparison.Ordinal);
        Assert.Contains("Click=\"RefreshPorts_Click\"", view, StringComparison.Ordinal);
        Assert.Contains("Click=\"SavePorts_Click\"", view, StringComparison.Ordinal);
        Assert.Contains("if (RefreshPortUsage())", code, StringComparison.Ordinal);
        Assert.Contains("SetStatus(string.Empty)", code, StringComparison.Ordinal);
        Assert.DoesNotContain("FontSize=\"17\" FontWeight=\"SemiBold\"", view, StringComparison.Ordinal);
        Assert.DoesNotContain("<DataGrid", view, StringComparison.Ordinal);
    }

    [Fact]
    public void Php_extensions_keep_native_toggles_and_expose_both_save_and_advanced_edit()
    {
        var view = File.ReadAllText(Path.Combine(FindAppRoot(), "Views", "PhpPageView.xaml"));

        Assert.Contains("<controls:SectionHeader", view, StringComparison.Ordinal);
        Assert.Contains("Metadata=\"{Binding PhpExtensions.Count}\"", view, StringComparison.Ordinal);
        Assert.Contains("IconDependency", view, StringComparison.Ordinal);
        Assert.Contains("IconShield", view, StringComparison.Ordinal);
        Assert.Contains("IconSave", view, StringComparison.Ordinal);
        Assert.Contains("IconEdit", view, StringComparison.Ordinal);
        Assert.Contains("IsChecked=\"{Binding IsEnabled, Mode=TwoWay}\"", view, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding CanToggle}\"", view, StringComparison.Ordinal);
        Assert.Contains("Click=\"SavePhpSettings_Click\"", view, StringComparison.Ordinal);
        Assert.Contains("Click=\"EditCustomPhpIni_Click\"", view, StringComparison.Ordinal);
        Assert.Contains("Feedback=\"{Binding StatusText}\"", view, StringComparison.Ordinal);
        Assert.DoesNotContain("temp/generated/default/apache-php/php.ini", view, StringComparison.Ordinal);
        Assert.DoesNotContain("FontSize=\"17\" FontWeight=\"SemiBold\"", view, StringComparison.Ordinal);
        Assert.DoesNotContain("<DataGrid", view, StringComparison.Ordinal);
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
