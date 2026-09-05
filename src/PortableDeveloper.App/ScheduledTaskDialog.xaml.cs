using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using PortableDeveloper.App.Controls;
using PortableDeveloper.App.ViewModels;
using PortableDeveloper.Application.Scheduling;

namespace PortableDeveloper.App;

public partial class ScheduledTaskDialog : Window
{
    private readonly UiText _text;
    private readonly string _projectId;
    private readonly string _taskId;

    public ScheduledTaskDialog(Window owner, UiText text, string projectId, PortableScheduledTask? initial = null)
    {
        AppWindowChrome.Apply(this);
        InitializeComponent();
        Owner = owner;
        _text = text;
        _projectId = projectId;
        _taskId = initial?.Id ?? $"task-{Guid.NewGuid():N}";
        Title = text.ScheduledTaskDialogTitle(initial is not null);

        NameLabel.Text = text.ScheduledTaskName;
        CommandLabel.Text = text.ScheduledTaskCommand;
        ScheduleLabel.Text = text.ScheduledTaskSchedule;
        TargetLabel.Text = text.ScheduledTaskTarget;
        TargetHelpText.Text = text.ScheduledTaskTargetHelp;
        ArgumentsLabel.Text = text.ScheduledTaskArguments;
        ArgumentsHelpText.Text = text.ScheduledTaskArgumentsHelp;
        IntervalLabel.Text = text.ScheduledTaskIntervalMinutes;
        TimeLabel.Text = text.ScheduledTaskTime;
        DayLabel.Text = text.ScheduledTaskDay;
        TimeoutLabel.Text = text.ScheduledTaskTimeout;
        EnabledCheckBox.Content = text.ScheduledTaskEnabled;
        SaveButton.Content = text.SaveScheduledTask;
        CancelButton.Content = text.Cancel;

        CommandComboBox.ItemsSource = Enum.GetValues<ScheduledTaskCommandKind>()
            .Select(kind => new ScheduledTaskChoice<ScheduledTaskCommandKind>(kind, text.ScheduledTaskCommandLabel(kind)))
            .ToArray();
        ScheduleComboBox.ItemsSource = Enum.GetValues<ScheduledTaskScheduleKind>()
            .Select(kind => new ScheduledTaskChoice<ScheduledTaskScheduleKind>(kind, text.ScheduledTaskScheduleKindLabel(kind)))
            .ToArray();
        DayComboBox.ItemsSource = Enum.GetValues<DayOfWeek>()
            .Select(day => new ScheduledTaskChoice<DayOfWeek>(day, text.ScheduledTaskDayLabel(day)))
            .ToArray();

        var schedule = initial?.Schedule ?? new ScheduledTaskSchedule(ScheduledTaskScheduleKind.Interval);
        NameTextBox.Text = initial?.Name ?? string.Empty;
        TargetTextBox.Text = initial?.Target ?? string.Empty;
        ArgumentsTextBox.Text = initial?.Arguments ?? string.Empty;
        CommandComboBox.SelectedValue = initial?.CommandKind ?? ScheduledTaskCommandKind.PythonScript;
        ScheduleComboBox.SelectedValue = schedule.Kind;
        IntervalTextBox.Text = schedule.IntervalMinutes.ToString(CultureInfo.InvariantCulture);
        TimeTextBox.Text = $"{schedule.Hour:00}:{schedule.Minute:00}";
        DayComboBox.SelectedValue = schedule.DayOfWeek;
        TimeoutTextBox.Text = (initial?.TimeoutMinutes ?? 10).ToString(CultureInfo.InvariantCulture);
        EnabledCheckBox.IsChecked = initial?.IsEnabled ?? true;
        UpdateScheduleFields();

        Loaded += (_, _) =>
        {
            NameTextBox.Focus();
            NameTextBox.SelectAll();
        };
    }

    public PortableScheduledTask? Task { get; private set; }

    private void ScheduleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e) => UpdateScheduleFields();

    private void UpdateScheduleFields()
    {
        if (ScheduleComboBox.SelectedValue is not ScheduledTaskScheduleKind kind)
        {
            return;
        }

        IntervalPanel.Visibility = kind == ScheduledTaskScheduleKind.Interval ? Visibility.Visible : Visibility.Collapsed;
        TimePanel.Visibility = kind is ScheduledTaskScheduleKind.Daily or ScheduledTaskScheduleKind.Weekly
            ? Visibility.Visible
            : Visibility.Collapsed;
        DayPanel.Visibility = kind == ScheduledTaskScheduleKind.Weekly ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (CommandComboBox.SelectedValue is not ScheduledTaskCommandKind command ||
                ScheduleComboBox.SelectedValue is not ScheduledTaskScheduleKind scheduleKind ||
                DayComboBox.SelectedValue is not DayOfWeek day ||
                !int.TryParse(IntervalTextBox.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var interval) ||
                !int.TryParse(TimeoutTextBox.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var timeout))
            {
                throw new ArgumentException(_text.ScheduledTaskNotScheduled);
            }

            var hour = 9;
            var minute = 0;
            if (scheduleKind is ScheduledTaskScheduleKind.Daily or ScheduledTaskScheduleKind.Weekly)
            {
                if (!TimeOnly.TryParseExact(TimeTextBox.Text.Trim(), "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var time))
                {
                    throw new ArgumentException(_text.ScheduledTaskTime);
                }

                hour = time.Hour;
                minute = time.Minute;
            }

            Task = ScheduledTaskValidator.Validate(new PortableScheduledTask(
                _taskId,
                _projectId,
                NameTextBox.Text,
                command,
                TargetTextBox.Text,
                ArgumentsTextBox.Text,
                new ScheduledTaskSchedule(scheduleKind, interval, hour, minute, day),
                timeout,
                EnabledCheckBox.IsChecked == true));
            DialogResult = true;
        }
        catch (ArgumentException)
        {
            ValidationText.Text = _text.ScheduledTaskValidationFailed;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
}
