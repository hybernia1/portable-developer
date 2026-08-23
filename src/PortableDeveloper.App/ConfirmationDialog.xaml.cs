using System.Windows;
using System.Windows.Markup;
using PortableDeveloper.App.Controls;
using PortableDeveloper.Application.Abstractions;

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
        AppWindowChrome.Apply(this);
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
        string cancelLabel)
    {
        try
        {
            return new ConfirmationDialog(owner, title, message, confirmLabel, cancelLabel).ShowDialog() == true;
        }
        catch (XamlParseException exception)
        {
            try
            {
                ((App)System.Windows.Application.Current).Logger.LogAsync(
                        ApplicationLogLevel.Error,
                        "confirmation-dialog",
                        "confirmation-dialog.load.failed",
                        exception.ToString())
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();
            }
            catch
            {
                // A failed diagnostic write must not turn a fail-closed confirmation into another crash.
            }

            MessageBox.Show(
                owner,
                "Potvrzovací dialog se nepodařilo otevřít. Nebyla provedena žádná změna.\n\n" +
                "The confirmation dialog could not be opened. No change was made.",
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return false;
        }
    }

    private void Confirm_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
