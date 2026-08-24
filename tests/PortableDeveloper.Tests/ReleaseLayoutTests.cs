namespace PortableDeveloper.Tests;

public sealed class ReleaseLayoutTests
{
    [Fact]
    public void PublishScripts_bundleNativeLibraries_and_validate_the_clean_release_root()
    {
        var repositoryRoot = FindRepositoryRoot();
        var onlinePublish = File.ReadAllText(Path.Combine(repositoryRoot, "scripts", "Publish-Online-Windows.ps1"));
        var offlinePublish = File.ReadAllText(Path.Combine(repositoryRoot, "scripts", "Publish-Windows.ps1"));
        var layoutTest = File.ReadAllText(Path.Combine(repositoryRoot, "scripts", "Test-ReleaseLayout.ps1"));

        Assert.Contains("-p:IncludeNativeLibrariesForSelfExtract=true", onlinePublish, StringComparison.Ordinal);
        Assert.Contains("-p:IncludeNativeLibrariesForSelfExtract=true", offlinePublish, StringComparison.Ordinal);
        Assert.Contains("Test-ReleaseLayout.ps1", onlinePublish, StringComparison.Ordinal);
        Assert.Contains("Test-ReleaseLayout.ps1", offlinePublish, StringComparison.Ordinal);
        Assert.Contains("PortableDeveloper.exe", layoutTest, StringComparison.Ordinal);
        Assert.Contains("resources", layoutTest, StringComparison.Ordinal);
        Assert.Contains(
            "(Join-Path $releaseDocumentsPath \"bundle-manifest.json\")",
            File.ReadAllText(Path.Combine(repositoryRoot, "scripts", "Bundle-OfflineDependencies.ps1")),
            StringComparison.Ordinal);
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
