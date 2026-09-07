namespace PortableDeveloper.Tests;

using PortableDeveloper.App.Views;

public sealed class ScheduledTaskPresentationTests
{
    [Fact]
    public void Scheduler_navigation_and_page_are_wired_into_the_shell()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var window = File.ReadAllText(Path.Combine(appRoot, "MainWindow.xaml"));
        var view = File.ReadAllText(Path.Combine(appRoot, "Views", "SchedulerPageView.xaml"));
        var code = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Scheduler.cs"));
        var detailsDialog = File.ReadAllText(Path.Combine(appRoot, "ScheduledTaskRunDetailsDialog.xaml"));
        var navigation = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "NavigationPage.cs"));

        Assert.Contains("Scheduler,", navigation, StringComparison.Ordinal);
        Assert.DoesNotContain("ConverterParameter=Scheduler", window, StringComparison.Ordinal);
        Assert.Contains("NewScheduledTask_Click", view, StringComparison.Ordinal);
        Assert.Contains("RunScheduledTask_Click", view, StringComparison.Ordinal);
        Assert.Contains("ScheduledTaskHistory", view, StringComparison.Ordinal);
        Assert.Contains("<ItemsControl ItemsSource=\"{Binding ScheduledTasks}\"", view, StringComparison.Ordinal);
        Assert.Contains("<ItemsControl ItemsSource=\"{Binding ScheduledTaskHistory}\"", view, StringComparison.Ordinal);
        Assert.Contains("HistoryFilterText", view, StringComparison.Ordinal);
        Assert.Contains("ViewScheduledTaskLog_Click", view, StringComparison.Ordinal);
        Assert.Contains("DeleteScheduledTaskLog_Click", view, StringComparison.Ordinal);
        Assert.Contains("ClearScheduledTaskHistory_Click", view, StringComparison.Ordinal);
        Assert.Contains("<controls:AdaptiveSplitPanel", view, StringComparison.Ordinal);
        Assert.DoesNotContain("<DataGrid", view, StringComparison.Ordinal);
        Assert.DoesNotContain("RowDetails", view, StringComparison.Ordinal);
        Assert.Contains("OutputTextBox", detailsDialog, StringComparison.Ordinal);
        Assert.Contains("ScheduledTaskRunDetailsDialog", code, StringComparison.Ordinal);
        Assert.Contains("RemoveHistoryRecord", code, StringComparison.Ordinal);
        Assert.Contains("ClearHistory", code, StringComparison.Ordinal);
        Assert.Contains("_dashboard.SchedulerPage.SetStatus", code, StringComparison.Ordinal);
        Assert.DoesNotContain("InstallationStatusText", code, StringComparison.Ordinal);
    }

    [Fact]
    public void Scheduler_copy_is_available_in_czech_and_english()
    {
        var repositoryRoot = FindRepositoryRoot();
        var viewModelsRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App", "ViewModels");
        var text = string.Join(
            Environment.NewLine,
            Directory.GetFiles(viewModelsRoot, "UiText*.cs").Order(StringComparer.Ordinal).Select(File.ReadAllText));

        Assert.Contains("Plánovač", text, StringComparison.Ordinal);
        Assert.Contains("Scheduler", text, StringComparison.Ordinal);
        Assert.Contains("Při spuštění aplikace", text, StringComparison.Ordinal);
        Assert.Contains("Application start", text, StringComparison.Ordinal);
        Assert.Contains("while Portable Developer is open", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Scheduler_row_requests_keep_task_identity_and_action_intent()
    {
        var request = new ScheduledTaskActionRequestedEventArgs(
            SchedulerPageView.TaskActionRequestedEvent,
            "task-id",
            ScheduledTaskAction.Run);

        Assert.Equal("task-id", request.TaskId);
        Assert.Equal(ScheduledTaskAction.Run, request.Action);
    }

    [Fact]
    public void Scheduler_history_requests_keep_record_identity_and_action_intent()
    {
        var request = new ScheduledTaskHistoryActionRequestedEventArgs(
            SchedulerPageView.HistoryActionRequestedEvent,
            "run-id",
            ScheduledTaskHistoryAction.Delete);

        Assert.Equal("run-id", request.RecordId);
        Assert.Equal(ScheduledTaskHistoryAction.Delete, request.Action);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "PortableDeveloper.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("PortableDeveloper.slnx was not found above the test output directory.");
    }

}
