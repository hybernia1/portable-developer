using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.Json;
using PortableDeveloper.Application.Settings;

namespace PortableDeveloper.App.Guides;

internal sealed class BuiltInGuideLibrary
{
    private const string CatalogResourceName = "PortableDeveloper.App.Guides.catalog.json";
    private const int MaximumCatalogBytes = 64 * 1024;
    private const int MaximumArticleBytes = 128 * 1024;
    private const int MaximumCategories = 20;
    private const int MaximumArticles = 100;
    private readonly Assembly _assembly;
    private readonly GuideCatalogDocument _catalog;
    private readonly Dictionary<(string Language, string ArticleId), string> _articleCache = new();

    private BuiltInGuideLibrary(Assembly assembly, GuideCatalogDocument catalog)
    {
        _assembly = assembly;
        _catalog = catalog;
    }

    public static BuiltInGuideLibrary Load()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var json = ReadResource(assembly, CatalogResourceName, MaximumCatalogBytes);
        var catalog = JsonSerializer.Deserialize<GuideCatalogDocument>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidDataException("The built-in guide catalog is empty.");
        Validate(catalog, assembly);
        return new BuiltInGuideLibrary(assembly, catalog);
    }

    public IReadOnlyList<GuideCategoryItem> GetCategories(ApplicationLanguage language)
    {
        var languageCode = GetLanguageCode(language);
        return _catalog.Categories
            .OrderBy(category => category.Order)
            .Select(category => new GuideCategoryItem(category.Id, category.Titles[languageCode]))
            .ToArray();
    }

    public IReadOnlyList<GuideArticleItem> FindArticles(
        ApplicationLanguage language,
        string? categoryId,
        string? query)
    {
        var languageCode = GetLanguageCode(language);
        var categoryTitles = _catalog.Categories.ToDictionary(
            category => category.Id,
            category => category.Titles[languageCode],
            StringComparer.Ordinal);
        var terms = (query ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Take(8)
            .ToArray();

        return _catalog.Articles
            .Where(article => string.IsNullOrEmpty(categoryId)
                || string.Equals(article.CategoryId, categoryId, StringComparison.Ordinal))
            .OrderBy(article => article.Order)
            .Select(article => CreateArticleItem(article, languageCode, categoryTitles[article.CategoryId]))
            .Where(article => terms.Length == 0 || MatchesSearch(article, languageCode, terms))
            .ToArray();
    }

    public GuideArticleContent GetArticle(
        string articleId,
        ApplicationLanguage language,
        int apachePort,
        int mariaDbPort,
        int seleniumPort)
    {
        var languageCode = GetLanguageCode(language);
        var article = _catalog.Articles.FirstOrDefault(candidate =>
            string.Equals(candidate.Id, articleId, StringComparison.Ordinal))
            ?? throw new ArgumentException($"Unknown built-in guide article '{articleId}'.", nameof(articleId));
        var category = _catalog.Categories.First(candidate =>
            string.Equals(candidate.Id, article.CategoryId, StringComparison.Ordinal));
        var item = CreateArticleItem(article, languageCode, category.Titles[languageCode]);
        var markdown = LoadArticleMarkdown(article, languageCode)
            .Replace("{{APACHE_PORT}}", apachePort.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{{MARIADB_PORT}}", mariaDbPort.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal)
            .Replace("{{SELENIUM_PORT}}", seleniumPort.ToString(CultureInfo.InvariantCulture), StringComparison.Ordinal);
        return new GuideArticleContent(item, markdown);
    }

    private static string GetLanguageCode(ApplicationLanguage language) =>
        language == ApplicationLanguage.Czech ? "cs" : "en";

    private GuideArticleItem CreateArticleItem(
        GuideArticleDefinition article,
        string languageCode,
        string categoryTitle)
    {
        var translation = article.Translations[languageCode];
        return new GuideArticleItem(
            article.Id,
            article.CategoryId,
            categoryTitle,
            translation.Title,
            translation.Tags);
    }

    private bool MatchesSearch(GuideArticleItem article, string languageCode, IReadOnlyList<string> terms)
    {
        var definition = _catalog.Articles.First(candidate =>
            string.Equals(candidate.Id, article.Id, StringComparison.Ordinal));
        var searchable = string.Join(
            '\n',
            article.Title,
            article.CategoryTitle,
            string.Join(' ', article.Tags),
            LoadArticleMarkdown(definition, languageCode));
        return terms.All(term => searchable.Contains(term, StringComparison.CurrentCultureIgnoreCase));
    }

    private string LoadArticleMarkdown(GuideArticleDefinition article, string languageCode)
    {
        var key = (languageCode, article.Id);
        if (_articleCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        var resourceName = GetArticleResourceName(languageCode, article.Slug);
        var markdown = ReadResource(_assembly, resourceName, MaximumArticleBytes);
        _articleCache.Add(key, markdown);
        return markdown;
    }

    private static void Validate(GuideCatalogDocument catalog, Assembly assembly)
    {
        if (catalog.SchemaVersion != 1)
        {
            throw new InvalidDataException($"Unsupported built-in guide catalog schema {catalog.SchemaVersion}.");
        }

        if (catalog.Categories.Count is < 1 or > MaximumCategories
            || catalog.Articles.Count is < 1 or > MaximumArticles)
        {
            throw new InvalidDataException("The built-in guide catalog has an invalid item count.");
        }

        var categoryIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var category in catalog.Categories)
        {
            ValidateIdentifier(category.Id, "category");
            if (!categoryIds.Add(category.Id)
                || category.Order < 0
                || !HasValidLocalizedValues(category.Titles, 80))
            {
                throw new InvalidDataException($"The built-in guide category '{category.Id}' is invalid.");
            }
        }

        var articleIds = new HashSet<string>(StringComparer.Ordinal);
        var slugs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var article in catalog.Articles)
        {
            ValidateIdentifier(article.Id, "article");
            ValidateIdentifier(article.Slug, "article slug");
            if (!articleIds.Add(article.Id)
                || !slugs.Add(article.Slug)
                || !categoryIds.Contains(article.CategoryId)
                || article.Order < 0
                || article.Translations.Count != 2)
            {
                throw new InvalidDataException($"The built-in guide article '{article.Id}' is invalid.");
            }

            foreach (var languageCode in new[] { "cs", "en" })
            {
                if (!article.Translations.TryGetValue(languageCode, out var translation)
                    || string.IsNullOrWhiteSpace(translation.Title)
                    || translation.Title.Length > 120
                    || translation.Tags.Count is < 1 or > 12
                    || translation.Tags.Any(tag => string.IsNullOrWhiteSpace(tag) || tag.Length > 32)
                    || translation.Tags.Distinct(StringComparer.OrdinalIgnoreCase).Count() != translation.Tags.Count)
                {
                    throw new InvalidDataException($"The built-in guide article translation '{article.Id}/{languageCode}' is invalid.");
                }

                var resourceName = GetArticleResourceName(languageCode, article.Slug);
                using var stream = assembly.GetManifestResourceStream(resourceName)
                    ?? throw new InvalidDataException($"The built-in guide resource '{resourceName}' is missing.");
                if (stream.Length is < 1 or > MaximumArticleBytes)
                {
                    throw new InvalidDataException($"The built-in guide resource '{resourceName}' has an invalid size.");
                }
            }
        }
    }

    private static bool HasValidLocalizedValues(IReadOnlyDictionary<string, string> values, int maximumLength) =>
        values.Count == 2
        && values.TryGetValue("cs", out var czech)
        && values.TryGetValue("en", out var english)
        && !string.IsNullOrWhiteSpace(czech)
        && !string.IsNullOrWhiteSpace(english)
        && czech.Length <= maximumLength
        && english.Length <= maximumLength;

    private static void ValidateIdentifier(string value, string kind)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > 64
            || value.Any(character => !char.IsAsciiLetterOrDigit(character) && character != '-'))
        {
            throw new InvalidDataException($"The built-in guide {kind} identifier '{value}' is invalid.");
        }
    }

    private static string GetArticleResourceName(string languageCode, string slug) =>
        $"PortableDeveloper.App.Guides.Articles.{languageCode}.{slug}.md";

    private static string ReadResource(Assembly assembly, string resourceName, int maximumBytes)
    {
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidDataException($"The built-in guide resource '{resourceName}' is missing.");
        if (stream.Length < 1 || stream.Length > maximumBytes)
        {
            throw new InvalidDataException($"The built-in guide resource '{resourceName}' has an invalid size.");
        }

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var content = reader.ReadToEnd();
        if (Encoding.UTF8.GetByteCount(content) > maximumBytes)
        {
            throw new InvalidDataException($"The built-in guide resource '{resourceName}' exceeds its size limit.");
        }

        return content;
    }

    private sealed class GuideCatalogDocument
    {
        public int SchemaVersion { get; init; }

        public List<GuideCategoryDefinition> Categories { get; init; } = [];

        public List<GuideArticleDefinition> Articles { get; init; } = [];
    }

    private sealed class GuideCategoryDefinition
    {
        public string Id { get; init; } = string.Empty;

        public int Order { get; init; }

        public Dictionary<string, string> Titles { get; init; } = [];
    }

    private sealed class GuideArticleDefinition
    {
        public string Id { get; init; } = string.Empty;

        public string Slug { get; init; } = string.Empty;

        public string CategoryId { get; init; } = string.Empty;

        public int Order { get; init; }

        public Dictionary<string, GuideArticleTranslation> Translations { get; init; } = [];
    }

    private sealed class GuideArticleTranslation
    {
        public string Title { get; init; } = string.Empty;

        public List<string> Tags { get; init; } = [];
    }
}

internal sealed record GuideCategoryItem(string Id, string Title);

internal sealed record GuideArticleItem(
    string Id,
    string CategoryId,
    string CategoryTitle,
    string Title,
    IReadOnlyList<string> Tags);

internal sealed record GuideArticleContent(GuideArticleItem Article, string Markdown);
