using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace PortableDeveloper.App;

public partial class CookieVaultImportDialog : Window
{
    private readonly string _nameValidationMessage;
    private readonly string _fileValidationMessage;
    private readonly string _filePickerTitle;

    public CookieVaultImportDialog(
        Window owner,
        string title,
        string vaultNameLabel,
        string cookieFileLabel,
        string chooseFileLabel,
        string noFileSelectedLabel,
        string confirmLabel,
        string cancelLabel,
        string nameValidationMessage,
        string fileValidationMessage)
    {
        InitializeComponent();
        if (owner.IsLoaded)
        {
            Owner = owner;
        }
        Title = title;
        DialogHeader.Heading = title;
        VaultNameLabel.Text = vaultNameLabel;
        CookieFileLabel.Text = cookieFileLabel;
        ChooseFileButtonText.Text = chooseFileLabel;
        SelectedFileText.Text = noFileSelectedLabel;
        ConfirmButtonText.Text = confirmLabel;
        CancelButton.Content = cancelLabel;
        _nameValidationMessage = nameValidationMessage;
        _fileValidationMessage = fileValidationMessage;
        _filePickerTitle = chooseFileLabel;

        Loaded += (_, _) => VaultNameTextBox.Focus();
    }

    public string VaultName { get; private set; } = string.Empty;

    public string? SelectedFilePath { get; private set; }

    private void ChooseFile_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = _filePickerTitle,
            Filter = "JSON (*.json)|*.json|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false
        };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        SelectedFilePath = dialog.FileName;
        SelectedFileText.Text = Path.GetFileName(dialog.FileName);
        SelectedFileText.ToolTip = dialog.FileName;
        ValidationText.Text = string.Empty;
    }

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        var name = VaultNameTextBox.Text.Trim();
        if (name.Length is < 1 or > 80 || name.Any(char.IsControl))
        {
            ValidationText.Text = _nameValidationMessage;
            VaultNameTextBox.Focus();
            return;
        }

        if (SelectedFilePath is not { } filePath || !File.Exists(filePath))
        {
            ValidationText.Text = _fileValidationMessage;
            ChooseFileButton.Focus();
            return;
        }

        VaultName = name;
        DialogResult = true;
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
