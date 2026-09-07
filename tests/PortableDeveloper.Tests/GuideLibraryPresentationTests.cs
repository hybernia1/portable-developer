using System.Text.Json;
using PortableDeveloper.App.ViewModels;
using PortableDeveloper.Application.Settings;

namespace PortableDeveloper.Tests;

public sealed class GuideLibraryPresentationTests
{
    [Fact]
    public void Guide_catalog_maps_every_article_to_czech_and_english_markdown()
    {
        var repositoryRoot = FindRepositoryRoot();
        var guideRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App", "Guides");
        using var catalog = JsonDocument.Parse(File.ReadAllText(Path.Combine(guideRoot, "catalog.json")));
        var root = catalog.RootElement;
        var categories = root.GetProperty("categories").EnumerateArray().ToArray();
        var articles = root.GetProperty("articles").EnumerateArray().ToArray();

        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(4, categories.Length);
        Assert.Equal(9, articles.Length);
        Assert.Equal(categories.Length, categories.Select(item => item.GetProperty("id").GetString()).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(articles.Length, articles.Select(item => item.GetProperty("id").GetString()).Distinct(StringComparer.Ordinal).Count());

        foreach (var article in articles)
        {
            var slug = article.GetProperty("slug").GetString();
            var translations = article.GetProperty("translations");
            foreach (var language in new[] { "cs", "en" })
            {
                var translation = translations.GetProperty(language);
                Assert.False(string.IsNullOrWhiteSpace(translation.GetProperty("title").GetString()));
                Assert.NotEmpty(translation.GetProperty("tags").EnumerateArray());
                var articlePath = Path.Combine(guideRoot, "Articles", language, $"{slug}.md");
                Assert.True(File.Exists(articlePath), $"Missing guide article: {articlePath}");
                Assert.False(string.IsNullOrWhiteSpace(File.ReadAllText(articlePath)));
            }
        }

        Assert.False(File.Exists(Path.Combine(guideRoot, "cs.md")));
        Assert.False(File.Exists(Path.Combine(guideRoot, "en.md")));
    }

    [Fact]
    public void Guides_page_uses_filters_and_renders_only_the_selected_article()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var window = File.ReadAllText(Path.Combine(appRoot, "MainWindow.xaml"));
        var view = File.ReadAllText(Path.Combine(appRoot, "Views", "GuidesPageView.xaml"));
        var viewCode = File.ReadAllText(Path.Combine(appRoot, "Views", "GuidesPageView.xaml.cs"));
        var pageModel = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "GuidesPageViewModel.cs"));
        var renderer = File.ReadAllText(Path.Combine(appRoot, "Guides", "MarkdownGuideRenderer.cs"));

        Assert.DoesNotContain("ConverterParameter=Guides", window, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding Categories}\"", view, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding SearchText, UpdateSourceTrigger=PropertyChanged}\"", view, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding Articles}\"", view, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(view, "x:Name=\"DocumentViewer\""));
        Assert.Contains("ApplyFilters", pageModel, StringComparison.Ordinal);
        Assert.Contains("LoadSelectedArticle", pageModel, StringComparison.Ordinal);
        Assert.Contains("MarkdownGuideRenderer.Render", viewCode, StringComparison.Ordinal);
        Assert.Contains("NormalizeCodeLanguage", renderer, StringComparison.Ordinal);
    }

    [Fact]
    public void Guides_page_model_keeps_filter_and_selected_content_as_durable_state()
    {
        var page = new GuidesPageViewModel(new UiText(new InMemorySettingsStore()));

        page.Refresh(apachePort: 8080, mariaDbPort: 3307, seleniumPort: 4444);

        Assert.NotEmpty(page.Categories);
        Assert.NotEmpty(page.Articles);
        Assert.NotNull(page.SelectedArticle);
        Assert.NotNull(page.SelectedContent);

        var tag = page.SelectedContent.Article.Tags[0];
        page.ApplyTag(tag);

        Assert.Equal(string.Empty, page.SelectedCategoryId);
        Assert.Equal(tag, page.SearchText);
        Assert.NotEmpty(page.Articles);

        page.SearchText = "query-that-does-not-exist-in-the-bundled-guides";

        Assert.True(page.NoArticles);
        Assert.True(page.NoSelectedArticle);
        Assert.Null(page.SelectedContent);
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var offset = 0;
        while ((offset = text.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }

        return count;
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

    private sealed class InMemorySettingsStore : IApplicationSettingsStore
    {
        private ApplicationSettings _settings = ApplicationSettings.Default;

        public ApplicationSettings Load() => _settings;

        public void Save(ApplicationSettings settings) => _settings = settings;
    }
}
