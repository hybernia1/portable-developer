using System.Windows;

namespace PortableDeveloper.App;

public partial class ConfirmationDialog : Window
{
    public ConfirmationDialog(
        Window owner,
        string title,
        string message,
        string confirmLabel,
        string cancelLabel)
    {
        InitializeComponent();
        Owner = owner;
        Title = title;
        HeadingText.Text = title;
        MessageText.Text = message;
        ConfirmButton.Content = confirmLabel;
        CancelButton.Content = cancelLabel;
        Loaded += (_, _) => CancelButton.Focus();
    }

    public static bool Show(
        Window owner,
        string title,
        string message,
        string confirmLabel,
        string cancelLabel) =>
        new ConfirmationDialog(owner, title, message, confirmLabel, cancelLabel).ShowDialog() == true;

    private void Confirm_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
