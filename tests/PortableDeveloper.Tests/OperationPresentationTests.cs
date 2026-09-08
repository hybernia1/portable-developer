using System.Xml.Linq;

namespace PortableDeveloper.Tests;

public sealed class OperationPresentationTests
{
    [Fact]
    public void BrandAssetsAndLocalOperationDetailsAreWiredIntoTheAppShell()
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
        var app = File.ReadAllText(Path.Combine(appRoot, "App.xaml"));
        var modulesView = File.ReadAllText(Path.Combine(appRoot, "Views", "ModulesPageView.xaml"));
        var databasesView = File.ReadAllText(Path.Combine(appRoot, "Views", "DatabasesPageView.xaml"));
        var packageManagerView = File.ReadAllText(Path.Combine(appRoot, "Controls", "PackageManagerView.xaml"));
        var packageManagementCode = File.ReadAllText(Path.Combine(appRoot, "MainWindow.PackageManagement.cs"));
        var serviceCode = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Services.cs"));
        var storageCode = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Storage.cs"));
        var projectCode = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Projects.cs"));
        var sidebar = File.ReadAllText(Path.Combine(appRoot, "Controls", "AppSidebar.xaml"));
        var text = ReadUiTextSources(Path.Combine(appRoot, "ViewModels"));

        Assert.Contains("Assets\\Logos\\*.svg", project, StringComparison.Ordinal);
        Assert.Contains("resources\\logos", project, StringComparison.Ordinal);
        Assert.Contains("BrandLogo", navigation, StringComparison.Ordinal);
        Assert.Contains("PrimaryBrandLogo", runtimePackage, StringComparison.Ordinal);
        Assert.Contains("public bool IsIdle => !IsBusy;", globalOperation, StringComparison.Ordinal);
        Assert.DoesNotContain("GlobalOperation.", window, StringComparison.Ordinal);
        Assert.Contains("DataType=\"{x:Type viewModels:PackageManagerHostViewModel}\"", app, StringComparison.Ordinal);
        Assert.DoesNotContain("<controls:PackageManagerView", window, StringComparison.Ordinal);
        Assert.Contains("ContentTemplate=\"{StaticResource OperationProgressTemplate}\"", packageManagerView, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding PackageNameInput, RelativeSource={RelativeSource AncestorType={x:Type controls:PackageManagerView}}, Mode=TwoWay", packageManagerView, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding VersionConstraintInput, RelativeSource={RelativeSource AncestorType={x:Type controls:PackageManagerView}}, Mode=TwoWay", packageManagerView, StringComparison.Ordinal);
        Assert.Contains("<ItemsControl ItemsSource=\"{Binding Page.DirectPackages, ElementName=Root}\">", packageManagerView, StringComparison.Ordinal);
        Assert.DoesNotContain("<DataGrid", packageManagerView, StringComparison.Ordinal);
        Assert.Contains("Click=\"RemovePackage_Click\"", packageManagerView, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding DataContext.Text.InstallPackage", packageManagerView, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding DataContext.Text.RemovePackage", packageManagerView, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding Page.OperationStatus", packageManagerView, StringComparison.Ordinal);
        Assert.Contains("Data=\"{StaticResource IconInstall}\"", packageManagerView, StringComparison.Ordinal);
        Assert.Contains("Data=\"{StaticResource IconDelete}\"", packageManagerView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource InlineInfoPanelStyle}\"", packageManagerView, StringComparison.Ordinal);
        Assert.DoesNotContain("ComposerPackageNameTextBox", window, StringComparison.Ordinal);
        Assert.DoesNotContain("NodePackageNameTextBox", window, StringComparison.Ordinal);
        Assert.DoesNotContain("PythonPackageNameTextBox", window, StringComparison.Ordinal);
        Assert.Contains("GetPackageManagerService(page.Kind)", packageManagementCode, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding DownloadDetail}\"", modulesView, StringComparison.Ordinal);
        Assert.Contains("PackageOperationDetail", text, StringComparison.Ordinal);
        Assert.Contains("public void ClearOperation()", packageManager, StringComparison.Ordinal);
        Assert.Contains("page.ClearOperation();", packageManagementCode, StringComparison.Ordinal);
        Assert.Contains("page.IsBusy || _dashboard.GlobalOperation.IsBusy", packageManagementCode, StringComparison.Ordinal);
        Assert.Contains("|| _dashboard.GlobalOperation.IsBusy", serviceCode, StringComparison.Ordinal);
        Assert.Contains("|| _dashboard.GlobalOperation.IsBusy", storageCode, StringComparison.Ordinal);
        Assert.Contains("|| _dashboard.Python.IsBusy", projectCode, StringComparison.Ordinal);
        Assert.DoesNotContain("package.IsInstalled ? string.Empty : Text.PackageMissingComponents", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding Text.LocalOnly}\"", window, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding Text.ApplicationTitle}\"", window, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding ApplicationVersion, StringFormat=v{0}}\"", sidebar, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding Text.PhpSettingsHelp}\"", window, StringComparison.Ordinal);
        Assert.DoesNotContain("public string PhpSettingsHelp", text, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding Composer.RuntimeDetail}\"", window, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding Node.RuntimeDetail}\"", window, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding Python.RuntimeDetail}\"", window, StringComparison.Ordinal);
        var document = XDocument.Parse(databasesView);
        var phpMyAdminCard = document.Descendants()
            .Single(element => string.Equals((string?)element.Attribute("Heading"), "phpMyAdmin 5.2.3", StringComparison.Ordinal))
            .Ancestors()
            .First(element => string.Equals(element.Name.LocalName, "Border", StringComparison.Ordinal));
        Assert.DoesNotContain("{Binding NewDatabaseName", databasesView, StringComparison.Ordinal);
        Assert.Contains("Click=\"CreateDatabase_Click\"", databasesView, StringComparison.Ordinal);
        Assert.Equal(
            "{Binding PhpMyAdminInstalled, Converter={StaticResource BooleanToVisibilityConverter}}",
            (string?)phpMyAdminCard.Attribute("Visibility"));
        Assert.Contains("<controls:AdaptiveSplitPanel", databasesView, StringComparison.Ordinal);
        Assert.DoesNotContain("Grid.ColumnSpan", databasesView, StringComparison.Ordinal);
        Assert.Contains("<ItemsControl ItemsSource=\"{Binding Databases}\">", databasesView, StringComparison.Ordinal);
        Assert.DoesNotContain("<DataGrid", databasesView, StringComparison.Ordinal);
        Assert.Contains("Click=\"ManageDatabase_Click\"", databasesView, StringComparison.Ordinal);
        Assert.Contains("Click=\"DeleteDatabase_Click\"", databasesView, StringComparison.Ordinal);
        Assert.DoesNotContain("<Expander Style=\"{StaticResource AppNavigationGroupExpanderStyle}\" IsExpanded=\"True\" Header=\"{Binding Name}\"", window, StringComparison.Ordinal);
        Assert.Contains("<controls:AppSidebar", window, StringComparison.Ordinal);
        Assert.Contains("<controls:WorkspaceHeader", window, StringComparison.Ordinal);
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

    private static string ReadUiTextSources(string viewModelsRoot) => string.Join(
        Environment.NewLine,
        Directory.GetFiles(viewModelsRoot, "UiText*.cs").Order(StringComparer.Ordinal).Select(File.ReadAllText));
}
