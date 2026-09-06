using System.Windows;
using System.Windows.Controls;
using PortableDeveloper.App.Guides;
using PortableDeveloper.Application.Settings;

namespace PortableDeveloper.App;

public partial class MainWindow
{
    private void RefreshGuides(bool resetSearch = false)
    {
        _updatingGuides = true;
        try
        {
            if (resetSearch)
            {
                GuidesSearchTextBox.Text = string.Empty;
            }

            var categories = new[]
                {
                    new GuideCategoryItem(string.Empty, _dashboard.Text.GuideAllCategories)
                }
                .Concat(_guideLibrary.GetCategories(_dashboard.Text.CurrentLanguage))
                .ToArray();
            if (categories.All(category => !string.Equals(category.Id, _guideCategoryId, StringComparison.Ordinal)))
            {
                _guideCategoryId = string.Empty;
            }

            GuidesCategoryListBox.ItemsSource = categories;
            GuidesCategoryListBox.SelectedValue = _guideCategoryId;
        }
        finally
        {
            _updatingGuides = false;
        }

        ApplyGuideFilters();
    }

    private void ApplyGuideFilters()
    {
        var articles = _guideLibrary.FindArticles(
            _dashboard.Text.CurrentLanguage,
            _guideCategoryId,
            GuidesSearchTextBox.Text);
        var selectedArticleId = articles.Any(article =>
            string.Equals(article.Id, _guideArticleId, StringComparison.Ordinal))
                ? _guideArticleId
                : articles.FirstOrDefault()?.Id;

        _updatingGuides = true;
        try
        {
            GuidesArticleListBox.ItemsSource = articles;
            GuidesArticleListBox.SelectedValue = selectedArticleId;
            GuidesArticleCountText.Text = _dashboard.Text.GuideArticleCount(articles.Count);
            GuidesNoArticlesText.Visibility = articles.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        finally
        {
            _updatingGuides = false;
        }

        RenderGuideArticle(articles.FirstOrDefault(article =>
            string.Equals(article.Id, selectedArticleId, StringComparison.Ordinal)));
    }

    private void RenderGuideArticle(GuideArticleItem? article)
    {
        if (article is null)
        {
            _guideArticleId = null;
            GuidesArticleContentPanel.Visibility = Visibility.Collapsed;
            GuidesEmptyArticleText.Visibility = Visibility.Visible;
            GuidesDocumentViewer.Document = null;
            return;
        }

        _guideArticleId = article.Id;
        var content = _guideLibrary.GetArticle(
            article.Id,
            _dashboard.Text.CurrentLanguage,
            _dashboard.ApachePort,
            _dashboard.MariaDbPort,
            _dashboard.SeleniumPort);
        GuidesArticleCategoryText.Text = content.Article.CategoryTitle;
        GuidesArticleTitleText.Text = content.Article.Title;
        GuidesArticleTags.ItemsSource = content.Article.Tags;
        GuidesDocumentViewer.Document = MarkdownGuideRenderer.Render(
            content.Markdown,
            _dashboard.Text.CurrentLanguage == ApplicationLanguage.Czech);
        GuidesEmptyArticleText.Visibility = Visibility.Collapsed;
        GuidesArticleContentPanel.Visibility = Visibility.Visible;
    }

    private void GuidesCategoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_updatingGuides || GuidesCategoryListBox.SelectedValue is not string categoryId)
        {
            return;
        }

        _guideCategoryId = categoryId;
        ApplyGuideFilters();
    }

    private void GuidesSearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (!_updatingGuides)
        {
            ApplyGuideFilters();
        }
    }

    private void GuidesArticleListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_updatingGuides)
        {
            RenderGuideArticle(GuidesArticleListBox.SelectedItem as GuideArticleItem);
        }
    }

    private void GuideTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string tag })
        {
            return;
        }

        _updatingGuides = true;
        try
        {
            _guideCategoryId = string.Empty;
            GuidesCategoryListBox.SelectedValue = string.Empty;
            GuidesSearchTextBox.Text = tag;
        }
        finally
        {
            _updatingGuides = false;
        }

        ApplyGuideFilters();
    }
}
