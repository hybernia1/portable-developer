using System.Windows;
using System.Windows.Markup;
using PortableDeveloper.App.Controls;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Workspace;

namespace PortableDeveloper.App;

public partial class FileConflictDialog : Window
{
    private WorkspaceConflictDecision _decision = new(WorkspaceConflictAction.Cancel);

    public FileConflictDialog(
        Window owner,
        string title,
        string message,
        string overwriteLabel,
        string renameLabel,
        string skipLabel,
        string applyToRemainingLabel)
    {
        AppWindowChrome.Apply(this);
        InitializeComponent();
        Owner = owner;
        Title = title;
        HeadingText.Text = title;
        MessageText.Text = message;
        OverwriteButton.Content = overwriteLabel;
        RenameButton.Content = renameLabel;
        SkipButton.Content = skipLabel;
        ApplyToRemainingCheckBox.Content = applyToRemainingLabel;
        Loaded += (_, _) => RenameButton.Focus();
    }

    public static WorkspaceConflictDecision Show(
        Window owner,
        string title,
        string message,
        string overwriteLabel,
        string renameLabel,
        string skipLabel,
        string applyToRemainingLabel)
    {
        try
        {
            var dialog = new FileConflictDialog(
                owner,
                title,
                message,
                overwriteLabel,
                renameLabel,
                skipLabel,
                applyToRemainingLabel);
            return dialog.ShowDialog() == true
                ? dialog._decision
                : new WorkspaceConflictDecision(WorkspaceConflictAction.Cancel);
        }
        catch (XamlParseException exception)
        {
            try
            {
                ((App)System.Windows.Application.Current).Logger.LogAsync(
                        ApplicationLogLevel.Error,
                        "file-conflict-dialog",
                        "file-conflict-dialog.load.failed",
                        exception.ToString())
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();
            }
            catch
            {
                // A failed diagnostic write must not turn a canceled transfer into another crash.
            }

            MessageBox.Show(
                owner,
                "Dialog kolize souborů se nepodařilo otevřít. Operace byla zrušena.\n\n" +
                "The file conflict dialog could not be opened. The operation was cancelled.",
                title,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            return new WorkspaceConflictDecision(WorkspaceConflictAction.Cancel);
        }
    }

    private void Overwrite_Click(object sender, RoutedEventArgs e) => Complete(WorkspaceConflictAction.Overwrite);

    private void Rename_Click(object sender, RoutedEventArgs e) => Complete(WorkspaceConflictAction.Rename);

    private void Skip_Click(object sender, RoutedEventArgs e) => Complete(WorkspaceConflictAction.Skip);

    private void Complete(WorkspaceConflictAction action)
    {
        _decision = new WorkspaceConflictDecision(action, ApplyToRemainingCheckBox.IsChecked == true);
        DialogResult = true;
    }
}
