namespace PortableDeveloper.Tests;

public sealed class BuiltInGuideContentTests
{
    [Fact]
    public void CzechAndEnglishGuidesDocumentPortableIntegrationContracts()
    {
        var appRoot = Path.Combine(FindRepositoryRoot(), "src", "PortableDeveloper.App");
        var czech = File.ReadAllText(Path.Combine(appRoot, "Guides", "cs.md"));
        var english = File.ReadAllText(Path.Combine(appRoot, "Guides", "en.md"));

        foreach (var guide in new[] { czech, english })
        {
            Assert.Contains("{{SELENIUM_PORT}}", guide, StringComparison.Ordinal);
            Assert.Contains("{{MARIADB_PORT}}", guide, StringComparison.Ordinal);
            Assert.Contains("portable:profile", guide, StringComparison.Ordinal);
            Assert.Contains("portable:vault", guide, StringComparison.Ordinal);
            Assert.Contains("seldownloads", guide, StringComparison.Ordinal);
            Assert.Contains("php-webdriver/webdriver", guide, StringComparison.Ordinal);
            Assert.Contains("driver.quit()", guide, StringComparison.Ordinal);
            Assert.Contains("## 7.", guide, StringComparison.Ordinal);
            Assert.Contains("## 8.", guide, StringComparison.Ordinal);
            Assert.Contains("Ctrl+C", guide, StringComparison.Ordinal);
            Assert.Contains("rmdir", guide, StringComparison.Ordinal);
        }

        Assert.Contains("přidejte přímý balíček selenium", czech, StringComparison.Ordinal);
        Assert.Contains("Štítky: selenium, python", czech, StringComparison.Ordinal);
        Assert.Contains("nepotřebuje účet v prohlížeči ani cloudovou synchronizaci", czech, StringComparison.Ordinal);
        Assert.Contains("add the direct selenium package", english, StringComparison.Ordinal);
        Assert.Contains("Tags: selenium, php", english, StringComparison.Ordinal);
        Assert.Contains("does not require a browser account or cloud synchronization", english, StringComparison.Ordinal);
    }

    [Fact]
    public void GuideMarkdownIsEmbeddedInTheApplication()
    {
        var project = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "PortableDeveloper.App",
            "PortableDeveloper.App.csproj"));

        Assert.Contains("<EmbeddedResource Include=\"Guides\\cs.md\" />", project, StringComparison.Ordinal);
        Assert.Contains("<EmbeddedResource Include=\"Guides\\en.md\" />", project, StringComparison.Ordinal);
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
