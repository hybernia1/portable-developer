namespace PortableDeveloper.App.ViewModels;

public sealed partial class UiText
{
    public string GuideBrowse => IsCzech ? "Najít návod" : "Find a guide";

    public string GuideArticles => IsCzech ? "Články" : "Articles";

    public string GuideCategories => IsCzech ? "Kategorie" : "Categories";

    public string GuideSearch => IsCzech ? "Hledat v návodech" : "Search guides";

    public string ClearGuideFilter => IsCzech ? "Zrušit filtr" : "Clear filter";

    public string GuideAllCategories => IsCzech ? "Vše" : "All";

    public string GuideNoArticles => IsCzech
        ? "Tomuto filtru neodpovídá žádný článek."
        : "No articles match this filter.";

    public string GuideSelectArticle => IsCzech
        ? "Vyberte článek ze seznamu."
        : "Select an article from the list.";

}
