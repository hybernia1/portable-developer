using System.Text.RegularExpressions;

namespace PortableDeveloper.Tests;

public sealed partial class AppThemeResourceTests
{
    [Fact]
    public void HiddenProgressIndicatorsDoNotAnimateByDefault()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var mainWindow = File.ReadAllText(Path.Combine(appRoot, "MainWindow.xaml"));
        var seleniumView = File.ReadAllText(Path.Combine(appRoot, "Views", "SeleniumPageView.xaml"));
        var packageManager = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "PackageManagerPageViewModel.cs"));

        Assert.DoesNotContain("IsIndeterminate=\"True\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("IsIndeterminate=\"{Binding ProfileProgressVisible}\"", seleniumView, StringComparison.Ordinal);
        Assert.DoesNotContain("private bool _operationIndeterminate = true;", packageManager, StringComparison.Ordinal);
    }

    [Fact]
    public void AppBrandAssetCoversNativeWindows()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var assetRoot = Path.Combine(appRoot, "Assets");
        var ico = File.ReadAllBytes(Path.Combine(assetRoot, "portable-developer.ico"));

        Assert.True(ico.Length > 6);
        Assert.Equal((ushort)1, System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(ico.AsSpan(2, 2)));
        Assert.True(System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(ico.AsSpan(4, 2)) >= 7);

        var mainWindow = File.ReadAllText(Path.Combine(appRoot, "MainWindow.xaml"));
        Assert.Contains("Icon=\"Assets/portable-developer.ico\"", mainWindow, StringComparison.Ordinal);

        var project = File.ReadAllText(Path.Combine(appRoot, "PortableDeveloper.App.csproj"));
        Assert.Contains("<ApplicationIcon>Assets\\portable-developer.ico</ApplicationIcon>", project, StringComparison.Ordinal);
    }

    [Fact]
    public void AppUiKeepsConcreteColorsInsideTheCentralThemeOnly()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var themePath = Path.GetFullPath(Path.Combine(appRoot, "Assets", "Theme.xaml"));
        var violations = Directory
            .EnumerateFiles(appRoot, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !string.Equals(Path.GetFullPath(path), themePath, StringComparison.OrdinalIgnoreCase))
            .SelectMany(path => File.ReadLines(path)
                .Select((line, index) => new { Path = path, Line = line, Number = index + 1 }))
            .Where(item => HexColor().IsMatch(item.Line) || NamedPropertyColor().IsMatch(item.Line))
            .Select(item => $"{Path.GetRelativePath(repositoryRoot, item.Path)}:{item.Number}: {item.Line.Trim()}")
            .ToArray();

        Assert.True(
            violations.Length == 0,
            $"Concrete UI colors must be declared only in Assets/Theme.xaml.{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void Keyed_resources_and_named_elements_have_source_consumers()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var sources = Directory
            .EnumerateFiles(appRoot, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Select(path => new { Path = path, Text = File.ReadAllText(path) })
            .ToArray();
        var combinedSource = string.Join(Environment.NewLine, sources.Select(source => source.Text));

        var unusedResources = sources
            .Where(source => source.Path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            .SelectMany(source => Regex.Matches(source.Text, "x:Key=\"([^\"]+)\"")
                .Select(match => new { Source = source.Path, Name = match.Groups[1].Value }))
            .Where(resource => Regex.Matches(combinedSource, Regex.Escape(resource.Name)).Count == 1)
            .Select(resource => $"{Path.GetRelativePath(repositoryRoot, resource.Source)}: {resource.Name}")
            .ToArray();
        var unusedNames = sources
            .Where(source => source.Path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            .SelectMany(source => Regex.Matches(source.Text, "x:Name=\"([^\"]+)\"")
                .Select(match => new { Source = source.Path, Name = match.Groups[1].Value }))
            .Where(element => Regex.Matches(combinedSource, $"\\b{Regex.Escape(element.Name)}\\b").Count == 1)
            .Select(element => $"{Path.GetRelativePath(repositoryRoot, element.Source)}: {element.Name}")
            .ToArray();

        Assert.True(
            unusedResources.Length == 0,
            $"Keyed resources must have a source consumer.{Environment.NewLine}{string.Join(Environment.NewLine, unusedResources)}");
        Assert.True(
            unusedNames.Length == 0,
            $"Named elements must have a source consumer.{Environment.NewLine}{string.Join(Environment.NewLine, unusedNames)}");
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

    [GeneratedRegex("#[0-9A-Fa-f]{3,8}\\b", RegexOptions.CultureInvariant)]
    private static partial Regex HexColor();

    [GeneratedRegex("(?:Foreground|Background|BorderBrush|Fill|Stroke)\\s*=\\s*\"(?:Black|White|Red|Green|Blue|Gray|Grey|Orange|Yellow)\"", RegexOptions.CultureInvariant)]
    private static partial Regex NamedPropertyColor();
}
