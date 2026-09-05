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
        var cleanup = File.ReadAllText(Path.Combine(repositoryRoot, "scripts", "Cleanup-Releases.ps1"));
        var seedBuilder = File.ReadAllText(Path.Combine(repositoryRoot, "scripts", "New-PortableSeedArchive.ps1"));
        var releaseWorkflow = File.ReadAllText(Path.Combine(repositoryRoot, ".github", "workflows", "release.yml"));

        Assert.Contains("-p:IncludeNativeLibrariesForSelfExtract=true", onlinePublish, StringComparison.Ordinal);
        Assert.Contains("-p:IncludeNativeLibrariesForSelfExtract=true", offlinePublish, StringComparison.Ordinal);
        Assert.Contains("Test-ReleaseLayout.ps1", onlinePublish, StringComparison.Ordinal);
        Assert.Contains("Test-ReleaseLayout.ps1", offlinePublish, StringComparison.Ordinal);
        Assert.Contains("PortableDeveloper.exe", layoutTest, StringComparison.Ordinal);
        Assert.Contains("resources", layoutTest, StringComparison.Ordinal);
        Assert.Contains("SingleExecutable", onlinePublish, StringComparison.Ordinal);
        Assert.Contains("New-PortableSeedArchive.ps1", onlinePublish, StringComparison.Ordinal);
        Assert.Contains("PortableSeedArchive", onlinePublish, StringComparison.Ordinal);
        Assert.Contains("entries.Count -ne 1", layoutTest, StringComparison.Ordinal);
        Assert.Contains("$resolvedOutput.exe", onlinePublish, StringComparison.Ordinal);
        Assert.Contains(".exe.sha256", cleanup, StringComparison.Ordinal);
        Assert.Contains("-SingleExecutable", releaseWorkflow, StringComparison.Ordinal);
        Assert.Contains("PortableDeveloper-win-x64-*.exe", releaseWorkflow, StringComparison.Ordinal);
        Assert.DoesNotContain("PortableDeveloper-win-x64-*.zip", releaseWorkflow, StringComparison.Ordinal);
        Assert.Contains("$archive.CreateEntry(", seedBuilder, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateFromDirectory", seedBuilder, StringComparison.Ordinal);
        Assert.Contains(
            "(Join-Path $releaseDocumentsPath \"bundle-manifest.json\")",
            File.ReadAllText(Path.Combine(repositoryRoot, "scripts", "Bundle-OfflineDependencies.ps1")),
            StringComparison.Ordinal);
    }

    [Fact]
    public void Native_runtime_packaging_extracts_verified_cabinets_without_Wix()
    {
        var repositoryRoot = FindRepositoryRoot();
        var onlineBundle = File.ReadAllText(
            Path.Combine(repositoryRoot, "scripts", "Bundle-PortableNativeRuntime.ps1"));
        var offlineBundle = File.ReadAllText(
            Path.Combine(repositoryRoot, "scripts", "Bundle-OfflineDependencies.ps1"));
        var extraction = File.ReadAllText(
            Path.Combine(repositoryRoot, "scripts", "VerifiedVcRuntimeExtraction.ps1"));
        using var toolManifest = System.Text.Json.JsonDocument.Parse(
            File.ReadAllText(Path.Combine(repositoryRoot, ".config", "dotnet-tools.json")));

        Assert.False(toolManifest.RootElement.GetProperty("tools").TryGetProperty("wix", out _));
        Assert.Contains("VerifiedVcRuntimeExtraction.ps1", onlineBundle, StringComparison.Ordinal);
        Assert.Contains("VerifiedVcRuntimeExtraction.ps1", offlineBundle, StringComparison.Ordinal);
        Assert.Contains("Expand-PdVerifiedVcRuntime", onlineBundle, StringComparison.Ordinal);
        Assert.Contains("Expand-PdVerifiedVcRuntime", offlineBundle, StringComparison.Ordinal);
        Assert.DoesNotContain("dotnet tool run wix", onlineBundle, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("dotnet tool run wix", offlineBundle, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Get-AuthenticodeSignature", extraction, StringComparison.Ordinal);
        Assert.Contains("ExpectedInstallerSha256", extraction, StringComparison.Ordinal);
        Assert.Contains("O=Microsoft Corporation", extraction, StringComparison.Ordinal);
        Assert.Contains("System32\\expand.exe", extraction, StringComparison.Ordinal);
        Assert.Contains("$startInfo.Arguments", extraction, StringComparison.Ordinal);
        Assert.Contains("$process.Kill($true)", extraction, StringComparison.Ordinal);
        Assert.Contains("512MB", extraction, StringComparison.Ordinal);
        Assert.DoesNotContain("::IndexOf[", extraction, StringComparison.Ordinal);
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
