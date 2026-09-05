using System.Windows;
using PortableDeveloper.App.Controls;
using PortableDeveloper.Application.Projects;

namespace PortableDeveloper.App;

public partial class ProjectWebSettingsDialog : Window
{
    private readonly string _validationMessage;
    private readonly bool _canDisable;

    public ProjectWebSettingsDialog(
        Window owner,
        string title,
        string rootPrompt,
        string enabledLabel,
        string htaccessLabel,
        string help,
        string defaultProjectNote,
        string saveLabel,
        string cancelLabel,
        string validationMessage,
        ProjectWebSettings initialSettings,
        bool canDisable)
    {
        AppWindowChrome.Apply(this);
        InitializeComponent();
        Owner = owner;
        Title = title;
        RootPromptText.Text = rootPrompt;
        WebEnabledCheckBox.Content = enabledLabel;
        HtaccessCheckBox.Content = htaccessLabel;
        HelpText.Text = help;
        SaveButton.Content = saveLabel;
        CancelButton.Content = cancelLabel;
        _validationMessage = validationMessage;
        _canDisable = canDisable;

        WebRootTextBox.Text = initialSettings.RootRelativePath;
        WebEnabledCheckBox.IsChecked = initialSettings.IsEnabled;
        WebEnabledCheckBox.IsEnabled = canDisable;
        HtaccessCheckBox.IsChecked = initialSettings.AllowHtaccess;
        DefaultProjectNoteText.Text = canDisable ? string.Empty : defaultProjectNote;
        DefaultProjectNoteText.Visibility = canDisable ? Visibility.Collapsed : Visibility.Visible;

        Loaded += (_, _) =>
        {
            WebRootTextBox.Focus();
            WebRootTextBox.SelectAll();
        };
    }

    public ProjectWebSettings Settings { get; private set; } = new(true);

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        var webRoot = WebRootTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(webRoot))
        {
            ValidationText.Text = _validationMessage;
            WebRootTextBox.Focus();
            return;
        }

        Settings = new ProjectWebSettings(
            !_canDisable || WebEnabledCheckBox.IsChecked == true,
            webRoot,
            HtaccessCheckBox.IsChecked == true);
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
