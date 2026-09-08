using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using PortableDeveloper.App.ViewModels;
using PortableDeveloper.App.Views;
using PortableDeveloper.Application.Projects;
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
        var taskSnapshots = _taskScheduler.GetTasks(projectId);
        var taskDefinitions = taskSnapshots.ToDictionary(
            snapshot => snapshot.Definition.Id,
            snapshot => snapshot.Definition,
            StringComparer.OrdinalIgnoreCase);
        var tasks = taskSnapshots.Select(snapshot =>
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
                : snapshot.LastRun.StartedAtUtc.ToLocalTime().ToString("g", culture);
            var lastResult = snapshot.LastRun is null
                ? string.Empty
                : _dashboard.Text.ScheduledTaskOutcomeLabel(snapshot.LastRun.Outcome);
            return new ScheduledTaskViewModel(
                snapshot.Definition.Id,
                snapshot.Definition.Name,
                _dashboard.Text.ScheduledTaskCommandLabel(snapshot.Definition.CommandKind),
                GetScheduledTaskBrand(snapshot.Definition.CommandKind),
                snapshot.Definition.Target,
                _dashboard.Text.ScheduledTaskScheduleLabel(snapshot.Definition.Schedule),
                next,
                last,
                lastResult,
                snapshot.LastRun?.Outcome == ScheduledTaskOutcome.Succeeded,
                status,
                snapshot.IsRunning,
                snapshot.Definition.IsEnabled);
        });
        var history = _taskScheduler.GetHistory(projectId).Select(record =>
        {
            var duration = record.FinishedAtUtc - record.StartedAtUtc;
            taskDefinitions.TryGetValue(record.TaskId, out var currentDefinition);
            var commandKind = record.CommandKind ?? currentDefinition?.CommandKind;
            var target = string.IsNullOrWhiteSpace(record.Target)
                ? currentDefinition?.Target ?? string.Empty
                : record.Target;
            return new ScheduledTaskRunViewModel(
                record.Id,
                record.TaskName,
                commandKind is { } kind
                    ? _dashboard.Text.ScheduledTaskCommandLabel(kind)
                    : string.Empty,
                GetScheduledTaskBrand(commandKind),
                target,
                record.StartedAtUtc.ToLocalTime().ToString("g", culture),
                duration.TotalMinutes >= 1
                    ? $"{duration.TotalMinutes:0.0} min"
                    : $"{duration.TotalSeconds:0.0} s",
                _dashboard.Text.ScheduledTaskTrigger(record.Trigger),
                _dashboard.Text.ScheduledTaskOutcomeLabel(record.Outcome),
                record.Output,
                record.Outcome == ScheduledTaskOutcome.Succeeded);
        });
        _dashboard.SchedulerPage.SetScheduledTasks(tasks, history);
    }

    private static string GetScheduledTaskBrand(ScheduledTaskCommandKind? commandKind) => commandKind switch
    {
        ScheduledTaskCommandKind.PhpScript => "php",
        ScheduledTaskCommandKind.PythonScript => "python",
        ScheduledTaskCommandKind.NodeScript or ScheduledTaskCommandKind.NpmScript => "nodejs",
        _ => string.Empty
    };

    private void NewScheduledTask_Click(object sender, RoutedEventArgs e)
    {
        var activeProject = _projectContext.ActiveProject;
        var projectName = string.Equals(activeProject.Id, ProjectCatalogDefaults.DefaultProjectId, StringComparison.OrdinalIgnoreCase)
            ? _dashboard.Text.DefaultProjectName
            : activeProject.Name;
        var dialog = new ScheduledTaskDialog(this, _dashboard.Text, activeProject.Id, projectName);
        if (dialog.ShowDialog() != true || dialog.Task is null)
        {
            return;
        }

        try
        {
            _taskScheduler.Add(dialog.Task);
            _dashboard.SchedulerPage.SetStatus(string.Empty);
            ShowTransientNotification(_dashboard.Text.ScheduledTaskSaved);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or InvalidOperationException or UnauthorizedAccessException)
        {
            _dashboard.SchedulerPage.SetStatus(_dashboard.Text.ScheduledTaskOperationFailed(exception.Message));
        }
    }

    private async void SchedulerPage_TaskActionRequested(object? sender, ScheduledTaskActionRequestedEventArgs e)
    {
        e.Handled = true;
        switch (e.Action)
        {
            case ScheduledTaskAction.Run:
                await RunScheduledTaskAsync(e.TaskId);
                break;
            case ScheduledTaskAction.Edit:
                EditScheduledTask(e.TaskId);
                break;
            case ScheduledTaskAction.Delete:
                DeleteScheduledTask(e.TaskId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(e.Action), e.Action, null);
        }
    }

    private void SchedulerPage_HistoryActionRequested(object? sender, ScheduledTaskHistoryActionRequestedEventArgs e)
    {
        e.Handled = true;
        switch (e.Action)
        {
            case ScheduledTaskHistoryAction.View when e.RecordId is not null:
                ShowScheduledTaskLog(e.RecordId);
                break;
            case ScheduledTaskHistoryAction.Delete when e.RecordId is not null:
                DeleteScheduledTaskLog(e.RecordId);
                break;
            case ScheduledTaskHistoryAction.ClearAll:
                ClearScheduledTaskHistory();
                break;
        }
    }

    private void ShowScheduledTaskLog(string recordId)
    {
        var record = FindScheduledTaskRun(recordId);
        if (record is not null)
        {
            _ = new ScheduledTaskRunDetailsDialog(this, _dashboard.Text, record).ShowDialog();
        }
    }

    private void DeleteScheduledTaskLog(string recordId)
    {
        var record = FindScheduledTaskRun(recordId);
        if (record is null || !ConfirmationDialog.Show(
                this,
                _dashboard.Text.DeleteScheduledTaskLog,
                _dashboard.Text.ScheduledTaskLogDeleteConfirmation(record.TaskName, record.Started),
                _dashboard.Text.DeleteScheduledTaskLog,
                _dashboard.Text.Cancel))
        {
            return;
        }

        try
        {
            if (_taskScheduler.RemoveHistoryRecord(_projectContext.ActiveProject.Id, record.Id))
            {
                _dashboard.SchedulerPage.SetStatus(string.Empty);
                ShowTransientNotification(_dashboard.Text.ScheduledTaskLogDeleted);
            }
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or InvalidOperationException or UnauthorizedAccessException)
        {
            _dashboard.SchedulerPage.SetStatus(_dashboard.Text.ScheduledTaskOperationFailed(exception.Message));
        }
    }

    private void ClearScheduledTaskHistory()
    {
        var count = _dashboard.SchedulerPage.ScheduledTaskHistoryTotalCount;
        if (count == 0 || !ConfirmationDialog.Show(
                this,
                _dashboard.Text.ClearScheduledTaskHistory,
                _dashboard.Text.ScheduledTaskHistoryClearConfirmation(count),
                _dashboard.Text.ClearScheduledTaskHistory,
                _dashboard.Text.Cancel))
        {
            return;
        }

        try
        {
            var removedCount = _taskScheduler.ClearHistory(_projectContext.ActiveProject.Id);
            if (removedCount > 0)
            {
                _dashboard.SchedulerPage.SetStatus(string.Empty);
                ShowTransientNotification(_dashboard.Text.ScheduledTaskHistoryCleared(removedCount));
            }
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or InvalidOperationException or UnauthorizedAccessException)
        {
            _dashboard.SchedulerPage.SetStatus(_dashboard.Text.ScheduledTaskOperationFailed(exception.Message));
        }
    }

    private ScheduledTaskRunViewModel? FindScheduledTaskRun(string recordId) =>
        _dashboard.SchedulerPage.ScheduledTaskHistory.FirstOrDefault(
            record => string.Equals(record.Id, recordId, StringComparison.OrdinalIgnoreCase));

    private void EditScheduledTask(string taskId)
    {
        var snapshot = _taskScheduler.GetTasks(_projectContext.ActiveProject.Id)
            .FirstOrDefault(item => string.Equals(item.Definition.Id, taskId, StringComparison.OrdinalIgnoreCase));
        if (snapshot is null || snapshot.IsRunning)
        {
            return;
        }

        var activeProject = _projectContext.ActiveProject;
        var projectName = string.Equals(activeProject.Id, ProjectCatalogDefaults.DefaultProjectId, StringComparison.OrdinalIgnoreCase)
            ? _dashboard.Text.DefaultProjectName
            : activeProject.Name;
        var dialog = new ScheduledTaskDialog(
            this,
            _dashboard.Text,
            snapshot.Definition.ProjectId,
            projectName,
            snapshot.Definition);
        if (dialog.ShowDialog() != true || dialog.Task is null)
        {
            return;
        }

        try
        {
            _taskScheduler.Update(dialog.Task);
            _dashboard.SchedulerPage.SetStatus(string.Empty);
            ShowTransientNotification(_dashboard.Text.ScheduledTaskSaved);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or InvalidOperationException or UnauthorizedAccessException)
        {
            _dashboard.SchedulerPage.SetStatus(_dashboard.Text.ScheduledTaskOperationFailed(exception.Message));
        }
    }

    private void DeleteScheduledTask(string taskId)
    {
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
            _dashboard.SchedulerPage.SetStatus(string.Empty);
            ShowTransientNotification(_dashboard.Text.ScheduledTaskDeleted);
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or InvalidOperationException or UnauthorizedAccessException)
        {
            _dashboard.SchedulerPage.SetStatus(_dashboard.Text.ScheduledTaskOperationFailed(exception.Message));
        }
    }

    private async Task RunScheduledTaskAsync(string taskId)
    {
        try
        {
            var record = await _taskScheduler.RunNowAsync(taskId, _applicationLifetime.Token);
            _dashboard.SchedulerPage.SetStatus(string.Empty);
            ShowTransientNotification(
                _dashboard.Text.ScheduledTaskCompleted(record.Outcome),
                TransientNotificationIntent.Information);
        }
        catch (OperationCanceledException) when (_applicationLifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is ArgumentException or IOException or InvalidDataException or InvalidOperationException or UnauthorizedAccessException)
        {
            _dashboard.SchedulerPage.SetStatus(_dashboard.Text.ScheduledTaskOperationFailed(exception.Message));
        }
    }
}
