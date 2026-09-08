using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using PortableDeveloper.App.Guides;
using PortableDeveloper.App.ViewModels;
using PortableDeveloper.Application.Settings;

namespace PortableDeveloper.App.Views;

public partial class GuidesPageView : UserControl
{
    private GuidesPageViewModel? _page;

    public GuidesPageView()
    {
        InitializeComponent();
        DataContextChanged += GuidesPageView_DataContextChanged;
        Loaded += GuidesPageView_Loaded;
        Unloaded += GuidesPageView_Unloaded;
    }

    private void GuidesPageView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        DetachPage();
        if (IsLoaded)
        {
            AttachPage(e.NewValue as GuidesPageViewModel);
        }

        RenderSelectedArticle();
    }

    private void GuidesPageView_Loaded(object sender, RoutedEventArgs e) =>
        AttachPage(DataContext as GuidesPageViewModel);

    private void GuidesPageView_Unloaded(object sender, RoutedEventArgs e) => DetachPage();

    private void Page_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (string.IsNullOrEmpty(e.PropertyName) || e.PropertyName == nameof(GuidesPageViewModel.SelectedContent))
        {
            RenderSelectedArticle();
        }
    }

    private void GuideTag_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string tag })
        {
            _page?.ApplyTag(tag);
        }
    }

    private void ClearGuideFilter_Click(object sender, RoutedEventArgs e) => _page?.ClearFilters();

    private void RenderSelectedArticle()
    {
        DocumentViewer.Document = _page?.SelectedContent is { } content
            ? MarkdownGuideRenderer.Render(
                content.Markdown,
                _page.Text.CurrentLanguage == ApplicationLanguage.Czech)
            : null;
    }

    private void DetachPage()
    {
        if (_page is not null)
        {
            _page.PropertyChanged -= Page_PropertyChanged;
            _page = null;
        }
    }

    private void AttachPage(GuidesPageViewModel? page)
    {
        if (ReferenceEquals(_page, page))
        {
            return;
        }

        DetachPage();
        _page = page;
        if (_page is not null)
        {
            _page.PropertyChanged += Page_PropertyChanged;
        }

        RenderSelectedArticle();
    }
}
