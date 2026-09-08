using System.Windows;
using PortableDeveloper.App.ViewModels;
using PortableDeveloper.Application.Selenium;

namespace PortableDeveloper.App;

public partial class SeleniumProfileDialog : Window
{
    private readonly string _profileNameValidationMessage;
    private readonly string _browserValidationMessage;

    public SeleniumProfileDialog(
        Window owner,
        string title,
        string profileNameLabel,
        string browserLabel,
        string confirmLabel,
        string cancelLabel,
        string profileNameValidationMessage,
        string browserValidationMessage,
        IReadOnlyList<SeleniumBrowserChoiceViewModel> browserChoices)
    {
        InitializeComponent();
        if (owner.IsLoaded)
        {
            Owner = owner;
        }
        Title = title;
        DialogHeader.Heading = title;
        ProfileNameLabel.Text = profileNameLabel;
        BrowserLabel.Text = browserLabel;
        ConfirmButtonText.Text = confirmLabel;
        CancelButton.Content = cancelLabel;
        _profileNameValidationMessage = profileNameValidationMessage;
        _browserValidationMessage = browserValidationMessage;
        BrowserComboBox.ItemsSource = browserChoices;
        BrowserComboBox.SelectedIndex = browserChoices.Count > 0 ? 0 : -1;

        Loaded += (_, _) => ProfileNameTextBox.Focus();
    }

    public string ProfileName { get; private set; } = string.Empty;

    public string SelectedBrowserEnvironmentId { get; private set; } = string.Empty;

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (!SeleniumProfileName.TryNormalize(ProfileNameTextBox.Text, out var profileName))
        {
            ValidationText.Text = _profileNameValidationMessage;
            ProfileNameTextBox.Focus();
            return;
        }

        if (BrowserComboBox.SelectedValue is not string browserEnvironmentId)
        {
            ValidationText.Text = _browserValidationMessage;
            BrowserComboBox.Focus();
            return;
        }

        ProfileName = profileName;
        SelectedBrowserEnvironmentId = browserEnvironmentId;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
