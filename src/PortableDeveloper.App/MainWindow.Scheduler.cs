using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using PortableDeveloper.App.ViewModels;
using PortableDeveloper.Application.Settings;
using PortableDeveloper.Application.Scheduling;

namespace PortableDeveloper.App;

public partial class MainWindow
{

    private void TaskScheduler_Changed(object? sender, EventArgs e)
    {
        if (Dispatcher.HasShutdownStarted || Dispatcher.HasShutdownFinished)
        {
            return;
        }

        _ = Dispatcher.BeginInvoke(DispatcherPriority.Background, RefreshScheduledTaskBindings);
    }

    private void RefreshScheduledTaskBindings()
    {
        var projectId = _projectContext.ActiveProject.Id;
        var culture = _dashboard.Text.CurrentLanguage == ApplicationLanguage.Czech
            ? CultureInfo.GetCultureInfo("cs-CZ")
            : CultureInfo.GetCultureInfo("en-US");
        var tasks = _taskScheduler.GetTasks(projectId).Select(snapshot =>
        {
            var status = snapshot.IsRunning
                ? _dashboard.Text.ScheduledTaskRunning
                : snapshot.Definition.IsEnabled
                    ? _dashboard.Text.ScheduledTaskEnabled
                    : _dashboard.Text.ScheduledTaskDisabled;
            var next = snapshot.NextRunUtc is null
                ? _dashboard.Text.ScheduledTaskNotScheduled
                : snapshot.NextRunUtc.Value.ToLocalTime().ToString("g", culture);
            var last = snapshot.LastRun is null
                ? _dashboard.Text.ScheduledTaskNever
                : $"{snapshot.LastRun.StartedAtUtc.ToLocalTime().ToString("g", culture)} · {_dashboard.Text.ScheduledTaskOutcomeLabel(snapshot.LastRun.Outcome)}";
            return new ScheduledTaskViewModel(
                snapshot.Definition.Id,
                snapshot.Definition.Name,
                _dashboard.Text.ScheduledTaskCommandLabel(snapshot.Definition.CommandKind),
                snapshot.Definition.Target,
                _dashboard.Text.ScheduledTaskScheduleLabel(snapshot.Definition.Schedule),
                $"{_dashboard.Text.ScheduledTaskNextRun}: {next}",
                $"{_dashboard.Text.ScheduledTaskLastRun}: {last}",
                status,
                snapshot.IsRunning,
                snapshot.Definition.IsEnabled);
        });
        var history = _taskScheduler.GetHistory(projectId).Select(record =>
        {
            var duration = record.FinishedAtUtc - record.StartedAtUtc;
            return new ScheduledTaskRunViewModel(
                record.TaskName,
                record.StartedAtUtc.ToLocalTime().ToString("g", culture),
                duration.TotalMinutes >= 1
                    ? $"{duration.TotalMinutes:0.0} min"
                    : $"{duration.TotalSeconds:0.0} s",
                _dashboard.Text.ScheduledTaskTrigger(record.Trigger),
                _dashboard.Text.ScheduledTaskOutcomeLabel(record.Outcome),
                record.Output,
                record.Outcome == ScheduledTaskOutcome.Succeeded);
        });
        _dashboard.SetScheduledTasks(tasks, history);
    }

    private void NewScheduledTask_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new ScheduledTaskDialog(this, _dashboard.Text, _projectContext.ActiveProject.Id);
        if (dialog.ShowDialog() != true || dialog.Task is null)
        {
            return;
        }

        try
        {
            _taskScheduler.Add(dialog.Task);
            InstallationStatusText.Text = _dashboard.Text.ScheduledTaskSaved;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or InvalidOperationException or UnauthorizedAccessException)
        {
            InstallationStatusText.Text = _dashboard.Text.ScheduledTaskOperationFailed(exception.Message);
        }
    }

    private void EditScheduledTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string taskId })
        {
            return;
        }

        var snapshot = _taskScheduler.GetTasks(_projectContext.ActiveProject.Id)
            .FirstOrDefault(item => string.Equals(item.Definition.Id, taskId, StringComparison.OrdinalIgnoreCase));
        if (snapshot is null || snapshot.IsRunning)
        {
            return;
        }

        var dialog = new ScheduledTaskDialog(this, _dashboard.Text, snapshot.Definition.ProjectId, snapshot.Definition);
        if (dialog.ShowDialog() != true || dialog.Task is null)
        {
            return;
        }

        try
        {
            _taskScheduler.Update(dialog.Task);
            InstallationStatusText.Text = _dashboard.Text.ScheduledTaskSaved;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or InvalidOperationException or UnauthorizedAccessException)
        {
            InstallationStatusText.Text = _dashboard.Text.ScheduledTaskOperationFailed(exception.Message);
        }
    }

    private void DeleteScheduledTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string taskId })
        {
            return;
        }

        var snapshot = _taskScheduler.GetTasks(_projectContext.ActiveProject.Id)
            .FirstOrDefault(item => string.Equals(item.Definition.Id, taskId, StringComparison.OrdinalIgnoreCase));
        if (snapshot is null || snapshot.IsRunning || !ConfirmationDialog.Show(
                this,
                _dashboard.Text.DeleteScheduledTask,
                _dashboard.Text.ScheduledTaskDeleteConfirmation(snapshot.Definition.Name),
                _dashboard.Text.DeleteScheduledTask,
                _dashboard.Text.Cancel))
        {
            return;
        }

        try
        {
            _taskScheduler.Remove(taskId);
            InstallationStatusText.Text = _dashboard.Text.ScheduledTaskDeleted;
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or InvalidOperationException or UnauthorizedAccessException)
        {
            InstallationStatusText.Text = _dashboard.Text.ScheduledTaskOperationFailed(exception.Message);
        }
    }

    private async void RunScheduledTask_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string taskId })
        {
            return;
        }

        try
        {
            var record = await _taskScheduler.RunNowAsync(taskId, _applicationLifetime.Token);
            InstallationStatusText.Text = _dashboard.Text.ScheduledTaskCompleted(record.Outcome);
        }
        catch (OperationCanceledException) when (_applicationLifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or InvalidOperationException or UnauthorizedAccessException)
        {
            InstallationStatusText.Text = _dashboard.Text.ScheduledTaskOperationFailed(exception.Message);
        }
    }
}
