using System.Windows;
using PortableDeveloper.App.ViewModels;

namespace PortableDeveloper.App;

public partial class ScheduledTaskRunDetailsDialog : Window
{
    public ScheduledTaskRunDetailsDialog(
        Window owner,
        UiText text,
        ScheduledTaskRunViewModel record)
    {
        ArgumentNullException.ThrowIfNull(owner);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(record);

        InitializeComponent();
        Owner = owner;
        Title = text.ScheduledTaskLogDetailsTitle;
        HeadingText.Text = record.TaskName;
        SummaryText.Text = string.Join(
            Environment.NewLine,
            $"{text.ScheduledTaskStarted}: {record.Started}",
            $"{text.ScheduledTaskDuration}: {record.Duration}",
            $"{text.ScheduledTaskTriggerLabel}: {record.Trigger}",
            $"{text.ScheduledTaskResult}: {record.Result}");
        OutputLabel.Text = text.ScheduledTaskOutput;
        OutputTextBox.Text = string.IsNullOrWhiteSpace(record.Output)
            ? text.ScheduledTaskLogHasNoOutput
            : record.Output;
        CloseButton.Content = text.CloseScheduledTaskLog;
        Loaded += (_, _) => CloseButton.Focus();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
