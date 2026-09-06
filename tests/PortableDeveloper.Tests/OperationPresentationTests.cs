using System.Xml.Linq;

namespace PortableDeveloper.Tests;

public sealed class OperationPresentationTests
{
    [Fact]
    public void BrandAssetsAndSharedOperationDetailsAreWiredIntoTheAppShell()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var logoDirectory = Path.Combine(appRoot, "Assets", "Logos");
        var expectedLogos = new[]
        {
            "apache.svg", "composer.svg", "mariadb.svg", "php.svg", "phpmyadmin.svg",
            "python.svg", "nodejs.svg", "selenium.svg", "notepadplusplus.svg", "googlechrome.svg", "firefox.svg"
        };

        Assert.All(expectedLogos, logo =>
        {
            var asset = Path.Combine(logoDirectory, logo);
            Assert.True(File.Exists(asset), $"Missing logo asset: {logo}");
            Assert.Contains("viewBox=\"0 0 24 24\"", File.ReadAllText(asset), StringComparison.Ordinal);
        });

        var project = File.ReadAllText(Path.Combine(appRoot, "PortableDeveloper.App.csproj"));
        var navigation = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "NavigationPage.cs"));
        var runtimePackage = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "RuntimePackageViewModel.cs"));
        var globalOperation = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "GlobalOperationViewModel.cs"));
        var packageManager = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "PackageManagerPageViewModel.cs"));
        var dashboard = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "DashboardViewModel.cs"));
        var window = File.ReadAllText(Path.Combine(appRoot, "MainWindow.xaml"));
        var windowCode = File.ReadAllText(Path.Combine(appRoot, "MainWindow.xaml.cs"));
        var text = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "UiText.cs"));

        Assert.Contains("Assets\\Logos\\*.svg", project, StringComparison.Ordinal);
        Assert.Contains("resources\\logos", project, StringComparison.Ordinal);
        Assert.Contains("BrandLogo", navigation, StringComparison.Ordinal);
        Assert.Contains("PrimaryBrandLogo", runtimePackage, StringComparison.Ordinal);
        Assert.Contains("public string Detail", globalOperation, StringComparison.Ordinal);
        Assert.Contains("GlobalOperation.Detail", window, StringComparison.Ordinal);
        Assert.Contains("PackageOperationDetail", text, StringComparison.Ordinal);
        Assert.Contains("public void ClearOperation()", packageManager, StringComparison.Ordinal);
        Assert.Contains("page.ClearOperation();", windowCode, StringComparison.Ordinal);
        Assert.DoesNotContain("package.IsInstalled ? string.Empty : Text.PackageMissingComponents", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding Text.LocalOnly}\"", window, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding Text.ApplicationTitle}\"", window, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding ApplicationVersion, StringFormat=v{0}}\"", window, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding Text.PhpSettingsHelp}\"", window, StringComparison.Ordinal);
        Assert.DoesNotContain("public string PhpSettingsHelp", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding Composer.RuntimeDetail}\"", window, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding Node.RuntimeDetail}\"", window, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding Python.RuntimeDetail}\"", window, StringComparison.Ordinal);
        var document = XDocument.Parse(window);
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        var createDatabaseCard = document.Descendants().Single(element =>
            string.Equals((string?)element.Attribute(xaml + "Name"), "CreateDatabaseCard", StringComparison.Ordinal));
        var phpMyAdminCard = document.Descendants().Single(element =>
            string.Equals((string?)element.Attribute(xaml + "Name"), "PhpMyAdminCard", StringComparison.Ordinal));
        Assert.Null(createDatabaseCard.Attribute("Visibility"));
        Assert.Equal(
            "{Binding PhpMyAdminInstalled, Converter={StaticResource BooleanToVisibilityConverter}}",
            (string?)phpMyAdminCard.Attribute("Visibility"));
        Assert.Contains("<Setter Property=\"Grid.ColumnSpan\" Value=\"3\" />", window, StringComparison.Ordinal);
        Assert.DoesNotContain("<Expander Style=\"{StaticResource AppNavigationGroupExpanderStyle}\" IsExpanded=\"True\" Header=\"{Binding Name}\"", window, StringComparison.Ordinal);
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
