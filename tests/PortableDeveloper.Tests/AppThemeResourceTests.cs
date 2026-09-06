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
        var globalOperation = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "GlobalOperationViewModel.cs"));
        var packageManager = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "PackageManagerPageViewModel.cs"));

        Assert.DoesNotContain("IsIndeterminate=\"True\"", mainWindow, StringComparison.Ordinal);
        Assert.Contains("SeleniumProfileProgressBar.IsIndeterminate = visible;", File.ReadAllText(Path.Combine(appRoot, "MainWindow.Selenium.cs")), StringComparison.Ordinal);
        Assert.DoesNotContain("private bool _isIndeterminate = true;", globalOperation, StringComparison.Ordinal);
        Assert.DoesNotContain("private bool _operationIndeterminate = true;", packageManager, StringComparison.Ordinal);
        Assert.Contains("IsIndeterminate = false;", globalOperation, StringComparison.Ordinal);
    }

    [Fact]
    public void AppBrandAssetsCoverWindowsAndInAppTitleBars()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var assetRoot = Path.Combine(appRoot, "Assets");
        var png = File.ReadAllBytes(Path.Combine(assetRoot, "portable-developer.png"));
        var ico = File.ReadAllBytes(Path.Combine(assetRoot, "portable-developer.ico"));

        Assert.True(png.Length > 26 && png.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }));
        Assert.Equal(6, png[25]); // PNG truecolor with alpha.

        Assert.True(ico.Length > 6);
        Assert.Equal((ushort)1, System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(ico.AsSpan(2, 2)));
        Assert.True(System.Buffers.Binary.BinaryPrimitives.ReadUInt16LittleEndian(ico.AsSpan(4, 2)) >= 7);

        var titleBar = File.ReadAllText(Path.Combine(appRoot, "Controls", "AppTitleBar.xaml"));
        Assert.Contains("Assets/portable-developer.png", titleBar, StringComparison.Ordinal);

        var project = File.ReadAllText(Path.Combine(appRoot, "PortableDeveloper.App.csproj"));
        Assert.Contains("<ApplicationIcon>Assets\\portable-developer.ico</ApplicationIcon>", project, StringComparison.Ordinal);
        Assert.Contains("<Resource Include=\"Assets\\portable-developer.png\" />", project, StringComparison.Ordinal);
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
