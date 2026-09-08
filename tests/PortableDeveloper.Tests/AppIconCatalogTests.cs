using System.Xml.Linq;

namespace PortableDeveloper.Tests;

public sealed class AppIconCatalogTests
{
    [Fact]
    public void Semantic_app_icons_are_centralized_and_rendered_through_the_shared_control()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var iconCatalogPath = Path.Combine(appRoot, "Assets", "Icons.xaml");
        var iconCatalog = XDocument.Load(iconCatalogPath);
        XNamespace xaml = "http://schemas.microsoft.com/winfx/2006/xaml";
        var keys = iconCatalog.Root!
            .Elements()
            .Select(element => (string?)element.Attribute(xaml + "Key"))
            .Where(key => key is not null)
            .Cast<string>()
            .ToArray();

        Assert.Equal(keys.Length, keys.Distinct(StringComparer.Ordinal).Count());
        Assert.All(
            new[]
            {
                "IconOpenFolder", "IconRefresh", "IconInstall", "IconDelete", "IconShield",
                "IconDependency", "IconPackage", "IconProject", "IconManage", "IconGlobe",
                "IconEdit", "IconCopy", "IconRemove", "IconAdd", "IconWarning", "IconSave", "IconInfo",
                "IconClose", "IconPlay", "IconStop", "IconClear", "IconSearch", "IconHistory",
                "IconDuration", "IconSuccess", "IconFailure", "IconDetails"
            },
            key => Assert.Contains(key, keys));

        var missingIconResources = Directory
            .EnumerateFiles(appRoot, "*.xaml", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                .SelectMany(path => System.Text.RegularExpressions.Regex.Matches(
                    File.ReadAllText(path),
                    "(?:Data|Icon|Value)=\"\\{StaticResource (Icon[A-Za-z0-9]+)\\}\"")
                .Select(match => new
                {
                    Path = Path.GetRelativePath(repositoryRoot, path),
                    Key = match.Groups[1].Value
                }))
            .Where(reference => !keys.Contains(reference.Key, StringComparer.Ordinal))
            .Select(reference => $"{reference.Path}: {reference.Key}")
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            missingIconResources.Length == 0,
            $"Every semantic icon reference must resolve through Assets/Icons.xaml.{Environment.NewLine}{string.Join(Environment.NewLine, missingIconResources)}");

        var packageManager = File.ReadAllText(Path.Combine(appRoot, "Controls", "PackageManagerView.xaml"));
        var sidebar = File.ReadAllText(Path.Combine(appRoot, "Controls", "AppSidebar.xaml"));
        var header = File.ReadAllText(Path.Combine(appRoot, "Controls", "WorkspaceHeader.xaml"));
        var projects = File.ReadAllText(Path.Combine(appRoot, "Views", "ProjectsPageView.xaml"));
        var dialogHeader = File.ReadAllText(Path.Combine(appRoot, "Controls", "DialogHeader.xaml"));
        var styles = File.ReadAllText(Path.Combine(appRoot, "Assets", "WorkspaceStyles.xaml"));

        Assert.Contains("<controls:AppIcon", packageManager, StringComparison.Ordinal);
        Assert.Contains("<controls:AppIcon", sidebar, StringComparison.Ordinal);
        Assert.Contains("Data=\"{StaticResource IconProject}\"", header, StringComparison.Ordinal);
        Assert.Contains("Data=\"{StaticResource IconManage}\"", header, StringComparison.Ordinal);
        Assert.Contains("Data=\"{StaticResource IconOpenFolder}\"", projects, StringComparison.Ordinal);
        Assert.Contains("Data=\"{StaticResource IconGlobe}\"", projects, StringComparison.Ordinal);
        Assert.Contains("Data=\"{StaticResource IconEdit}\"", projects, StringComparison.Ordinal);
        Assert.Contains("Data=\"{StaticResource IconRemove}\"", projects, StringComparison.Ordinal);
        Assert.Contains("<controls:AppIcon", dialogHeader, StringComparison.Ordinal);
        Assert.Contains("TargetType=\"{x:Type controls:AppIcon}\"", styles, StringComparison.Ordinal);
        Assert.DoesNotContain("<Path Width=\"16\" Height=\"16\"", sidebar, StringComparison.Ordinal);
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
