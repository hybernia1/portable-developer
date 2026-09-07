namespace PortableDeveloper.App.ViewModels;

public sealed partial class UiText
{
    public string GuideCategories => IsCzech ? "Kategorie" : "Categories";

    public string GuideSearch => IsCzech ? "Hledat v návodech" : "Search guides";

    public string GuideAllCategories => IsCzech ? "Vše" : "All";

    public string GuideNoArticles => IsCzech
        ? "Tomuto filtru neodpovídá žádný článek."
        : "No articles match this filter.";

    public string GuideSelectArticle => IsCzech
        ? "Vyberte článek ze seznamu."
        : "Select an article from the list.";

    public string GuideArticleCount(int count) => IsCzech
        ? count == 1 ? "1 článek" : $"Články: {count}"
        : count == 1 ? "1 article" : $"{count} articles";

}
