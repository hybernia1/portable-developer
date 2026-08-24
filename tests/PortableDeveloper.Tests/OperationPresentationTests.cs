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
            "python.svg", "selenium.svg", "notepadplusplus.svg", "googlechrome.svg", "firefox.svg"
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
        var window = File.ReadAllText(Path.Combine(appRoot, "MainWindow.xaml"));
        var text = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "UiText.cs"));

        Assert.Contains("Assets\\Logos\\*.svg", project, StringComparison.Ordinal);
        Assert.Contains("resources\\logos", project, StringComparison.Ordinal);
        Assert.Contains("BrandLogo", navigation, StringComparison.Ordinal);
        Assert.Contains("PrimaryBrandLogo", runtimePackage, StringComparison.Ordinal);
        Assert.Contains("public string Detail", globalOperation, StringComparison.Ordinal);
        Assert.Contains("GlobalOperation.Detail", window, StringComparison.Ordinal);
        Assert.Contains("PackageOperationDetail", text, StringComparison.Ordinal);
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
