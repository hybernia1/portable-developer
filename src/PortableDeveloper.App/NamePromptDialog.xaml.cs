using System.Windows;
using System.Windows.Input;

namespace PortableDeveloper.App;

public partial class NamePromptDialog : Window
{
    private readonly string _validationMessage;

    public NamePromptDialog(
        Window owner,
        string title,
        string prompt,
        string confirmLabel,
        string cancelLabel,
        string validationMessage,
        string initialValue = "")
    {
        InitializeComponent();
        Owner = owner;
        Title = title;
        PromptText.Text = prompt;
        ConfirmButton.Content = confirmLabel;
        CancelButton.Content = cancelLabel;
        _validationMessage = validationMessage;
        NameTextBox.Text = initialValue;
        Loaded += (_, _) =>
        {
            NameTextBox.Focus();
            NameTextBox.SelectAll();
        };
    }

    public string ItemName => NameTextBox.Text.Trim();

    private void Confirm_Click(object sender, RoutedEventArgs e) => Confirm();

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private void NameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        Confirm();
    }

    private void Confirm()
    {
        if (string.IsNullOrWhiteSpace(NameTextBox.Text))
        {
            ValidationText.Text = _validationMessage;
            NameTextBox.Focus();
            return;
        }

        DialogResult = true;
    }
}
