using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PortableDeveloper.App.Guides;
using PortableDeveloper.Application.Settings;

namespace PortableDeveloper.App.ViewModels;

public sealed class GuidesPageViewModel : INotifyPropertyChanged
{
    private readonly BuiltInGuideLibrary _library;
    private readonly UiText _text;
    private int _apachePort;
    private int _mariaDbPort;
    private int _seleniumPort;
    private bool _updating;
    private string _searchText = string.Empty;
    private string _selectedCategoryId = string.Empty;
    private GuideArticleItem? _selectedArticle;
    private GuideArticleContent? _selectedContent;

    public GuidesPageViewModel(UiText text)
        : this(text, BuiltInGuideLibrary.Load())
    {
    }

    internal GuidesPageViewModel(UiText text, BuiltInGuideLibrary library)
    {
        _text = text;
        _library = library;
        Categories = new ObservableCollection<GuideCategoryItem>();
        Articles = new ObservableCollection<GuideArticleItem>();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public UiText Text => _text;

    public ObservableCollection<GuideCategoryItem> Categories { get; }

    public ObservableCollection<GuideArticleItem> Articles { get; }

    public GuideArticleContent? SelectedContent
    {
        get => _selectedContent;
        private set
        {
            if (!SetField(ref _selectedContent, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasSelectedArticle));
            OnPropertyChanged(nameof(NoSelectedArticle));
        }
    }

    public GuideArticleItem? SelectedArticle
    {
        get => _selectedArticle;
        set
        {
            if (!SetField(ref _selectedArticle, value) || _updating)
            {
                return;
            }

            LoadSelectedArticle();
        }
    }

    public string SelectedCategoryId
    {
        get => _selectedCategoryId;
        set
        {
            if (!SetField(ref _selectedCategoryId, value) || _updating)
            {
                return;
            }

            OnPropertyChanged(nameof(HasFilter));
            ApplyFilters();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetField(ref _searchText, value) || _updating)
            {
                return;
            }

            OnPropertyChanged(nameof(HasFilter));
            ApplyFilters();
        }
    }

    public bool NoArticles => Articles.Count == 0;

    public bool HasFilter => !string.IsNullOrWhiteSpace(SearchText)
                             || !string.IsNullOrWhiteSpace(SelectedCategoryId);

    public bool HasSelectedArticle => SelectedContent is not null;

    public bool NoSelectedArticle => SelectedContent is null;

    public void Refresh(int apachePort, int mariaDbPort, int seleniumPort, bool resetSearch = false)
    {
        _apachePort = apachePort;
        _mariaDbPort = mariaDbPort;
        _seleniumPort = seleniumPort;
        _updating = true;
        try
        {
            if (resetSearch)
            {
                SearchText = string.Empty;
            }

            Categories.Clear();
            Categories.Add(new GuideCategoryItem(string.Empty, _text.GuideAllCategories));
            foreach (var category in _library.GetCategories(_text.CurrentLanguage))
            {
                Categories.Add(category);
            }

            if (Categories.All(category =>
                    !string.Equals(category.Id, SelectedCategoryId, StringComparison.Ordinal)))
            {
                SelectedCategoryId = string.Empty;
            }
        }
        finally
        {
            _updating = false;
        }

        OnPropertyChanged(nameof(HasFilter));
        ApplyFilters();
    }

    public void ApplyTag(string tag)
    {
        _updating = true;
        try
        {
            SelectedCategoryId = string.Empty;
            SearchText = tag;
        }
        finally
        {
            _updating = false;
        }

        OnPropertyChanged(nameof(HasFilter));
        ApplyFilters();
    }

    public void ClearFilters()
    {
        _updating = true;
        try
        {
            SelectedCategoryId = string.Empty;
            SearchText = string.Empty;
        }
        finally
        {
            _updating = false;
        }

        OnPropertyChanged(nameof(HasFilter));
        ApplyFilters();
    }

    private void ApplyFilters()
    {
        var matches = _library.FindArticles(_text.CurrentLanguage, SelectedCategoryId, SearchText);
        var selectedArticleId = matches.Any(article =>
            string.Equals(article.Id, SelectedArticle?.Id, StringComparison.Ordinal))
                ? SelectedArticle?.Id
                : matches.FirstOrDefault()?.Id;

        _updating = true;
        try
        {
            Articles.Clear();
            foreach (var article in matches)
            {
                Articles.Add(article);
            }

            SelectedArticle = Articles.FirstOrDefault(article =>
                string.Equals(article.Id, selectedArticleId, StringComparison.Ordinal));
        }
        finally
        {
            _updating = false;
        }

        OnPropertyChanged(nameof(NoArticles));
        LoadSelectedArticle();
    }

    private void LoadSelectedArticle()
    {
        SelectedContent = SelectedArticle is null
            ? null
            : _library.GetArticle(
                SelectedArticle.Id,
                _text.CurrentLanguage,
                _apachePort,
                _mariaDbPort,
                _seleniumPort);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
