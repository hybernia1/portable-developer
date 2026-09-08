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
        DialogHeader.Heading = record.TaskName;
        DialogHeader.Context = text.ScheduledTaskLogDetailsTitle;
        var summary = new List<string>();
        if (!string.IsNullOrWhiteSpace(record.Command))
        {
            summary.Add($"{text.ScheduledTaskCommand}: {record.Command}");
        }

        if (!string.IsNullOrWhiteSpace(record.Target))
        {
            summary.Add($"{text.ScheduledTaskTarget}: {record.Target}");
        }

        summary.Add($"{text.ScheduledTaskStarted}: {record.Started}");
        summary.Add($"{text.ScheduledTaskDuration}: {record.Duration}");
        summary.Add($"{text.ScheduledTaskTriggerLabel}: {record.Trigger}");
        summary.Add($"{text.ScheduledTaskResult}: {record.Result}");
        SummaryText.Text = string.Join(Environment.NewLine, summary);
        OutputLabel.Text = text.ScheduledTaskOutput;
        OutputTextBox.Text = string.IsNullOrWhiteSpace(record.Output)
            ? text.ScheduledTaskLogHasNoOutput
            : record.Output;
        CloseButtonText.Text = text.CloseScheduledTaskLog;
        Loaded += (_, _) => CloseButton.Focus();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
