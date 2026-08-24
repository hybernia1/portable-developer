namespace PortableDeveloper.Tests;

public sealed class WorkspaceIconPresentationTests
{
    [Fact]
    public void File_manager_maps_distinct_file_kinds_to_shared_icon_resources()
    {
        var repositoryRoot = FindRepositoryRoot();
        var icons = File.ReadAllText(Path.Combine(repositoryRoot, "src", "PortableDeveloper.App", "Assets", "Icons.xaml"));
        var window = File.ReadAllText(Path.Combine(repositoryRoot, "src", "PortableDeveloper.App", "MainWindow.xaml"));

        Assert.Contains("x:Key=\"IconHtml\"", icons, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"IconText\"", icons, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"IconExecutable\"", icons, StringComparison.Ordinal);
        Assert.Contains("FileKind}\" Value=\"Html\"", window, StringComparison.Ordinal);
        Assert.Contains("IconHtml", window, StringComparison.Ordinal);
        Assert.Contains("FileKind}\" Value=\"Python\"", window, StringComparison.Ordinal);
        Assert.Contains("FileKind}\" Value=\"StyleSheet\"", window, StringComparison.Ordinal);
        Assert.Contains("FileKind}\" Value=\"Spreadsheet\"", window, StringComparison.Ordinal);
        Assert.Contains("IconSpreadsheet", window, StringComparison.Ordinal);
        Assert.Contains("FileKind}\" Value=\"Document\"", window, StringComparison.Ordinal);
        Assert.Contains("FileKind}\" Value=\"Executable\"", window, StringComparison.Ordinal);
        Assert.Contains("IconExecutable", window, StringComparison.Ordinal);
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
