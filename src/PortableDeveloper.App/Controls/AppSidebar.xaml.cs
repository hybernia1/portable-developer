using System.Windows.Controls;

namespace PortableDeveloper.App.Controls;

public partial class AppSidebar : UserControl
{
    public AppSidebar()
    {
        InitializeComponent();
    }

    public event SelectionChangedEventHandler? NavigationSelectionChanged;

    public event SelectionChangedEventHandler? LanguageSelectionChanged;

    public string? SelectedLanguage
    {
        get => LanguageSelector.SelectedValue as string;
        set => LanguageSelector.SelectedValue = value;
    }

    private void NavigationList_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        NavigationSelectionChanged?.Invoke(sender, e);

    private void LanguageSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        LanguageSelectionChanged?.Invoke(sender, e);
}
