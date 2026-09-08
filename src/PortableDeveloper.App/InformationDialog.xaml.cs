using System.Windows;
using System.Windows.Markup;

namespace PortableDeveloper.App;

public partial class InformationDialog : Window
{
    public InformationDialog(Window owner, string title, string message, string closeLabel)
    {
        InitializeComponent();
        Owner = owner;
        Title = title;
        DialogHeader.Heading = title;
        MessageText.Text = message;
        CloseButton.Content = closeLabel;
        Loaded += (_, _) => CloseButton.Focus();
    }

    public static void Show(Window owner, string title, string message, string closeLabel)
    {
        try
        {
            _ = new InformationDialog(owner, title, message, closeLabel).ShowDialog();
        }
        catch (XamlParseException)
        {
            MessageBox.Show(owner, message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
