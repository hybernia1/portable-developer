namespace PortableDeveloper.Tests;

public sealed class BuiltInGuideContentTests
{
    [Fact]
    public void CzechAndEnglishGuidesDocumentPortableIntegrationContracts()
    {
        var appRoot = Path.Combine(FindRepositoryRoot(), "src", "PortableDeveloper.App");
        var guideRoot = Path.Combine(appRoot, "Guides");
        var czech = ReadArticles(guideRoot, "cs");
        var english = ReadArticles(guideRoot, "en");
        var catalog = File.ReadAllText(Path.Combine(guideRoot, "catalog.json"));

        foreach (var guide in new[] { czech, english })
        {
            Assert.Contains("{{SELENIUM_PORT}}", guide, StringComparison.Ordinal);
            Assert.Contains("{{MARIADB_PORT}}", guide, StringComparison.Ordinal);
            Assert.Contains("portable:profile", guide, StringComparison.Ordinal);
            Assert.Contains("portable:vault", guide, StringComparison.Ordinal);
            Assert.Contains("seldownloads", guide, StringComparison.Ordinal);
            Assert.Contains("php-webdriver/webdriver", guide, StringComparison.Ordinal);
            Assert.Contains("driver.quit()", guide, StringComparison.Ordinal);
            Assert.Contains("Ctrl+C", guide, StringComparison.Ordinal);
            Assert.Contains("rmdir", guide, StringComparison.Ordinal);
        }

        Assert.Contains("přidejte přímý balíček selenium", czech, StringComparison.Ordinal);
        Assert.Contains("nepotřebuje účet v prohlížeči ani cloudovou synchronizaci", czech, StringComparison.Ordinal);
        Assert.Contains("add the direct selenium package", english, StringComparison.Ordinal);
        Assert.Contains("does not require a browser account or cloud synchronization", english, StringComparison.Ordinal);
        Assert.Contains("\"tags\": [\"selenium\", \"python\", \"master profil\"]", catalog, StringComparison.Ordinal);
        Assert.Contains("\"tags\": [\"selenium\", \"php\", \"composer\"]", catalog, StringComparison.Ordinal);
    }

    [Fact]
    public void GuideMarkdownIsEmbeddedInTheApplication()
    {
        var project = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "PortableDeveloper.App",
            "PortableDeveloper.App.csproj"));

        Assert.Contains("<EmbeddedResource Include=\"Guides\\catalog.json\" />", project, StringComparison.Ordinal);
        Assert.Contains("<EmbeddedResource Include=\"Guides\\Articles\\**\\*.md\" />", project, StringComparison.Ordinal);
    }

    private static string ReadArticles(string guideRoot, string language) => string.Join(
        '\n',
        Directory.GetFiles(Path.Combine(guideRoot, "Articles", language), "*.md")
            .OrderBy(path => path, StringComparer.Ordinal)
            .Select(File.ReadAllText));

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
