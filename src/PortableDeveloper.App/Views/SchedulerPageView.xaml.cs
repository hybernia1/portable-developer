using System.Windows;
using System.Windows.Controls;
using PortableDeveloper.App.ViewModels;

namespace PortableDeveloper.App.Views;

public partial class SchedulerPageView : UserControl
{
    public static readonly RoutedEvent NewTaskRequestedEvent = RegisterEvent(nameof(NewTaskRequested));
    public static readonly RoutedEvent TaskActionRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(TaskActionRequested),
        RoutingStrategy.Bubble,
        typeof(EventHandler<ScheduledTaskActionRequestedEventArgs>),
        typeof(SchedulerPageView));
    public static readonly RoutedEvent HistoryActionRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(HistoryActionRequested),
        RoutingStrategy.Bubble,
        typeof(EventHandler<ScheduledTaskHistoryActionRequestedEventArgs>),
        typeof(SchedulerPageView));

    public SchedulerPageView()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler NewTaskRequested
    {
        add => AddHandler(NewTaskRequestedEvent, value);
        remove => RemoveHandler(NewTaskRequestedEvent, value);
    }

    public event EventHandler<ScheduledTaskActionRequestedEventArgs> TaskActionRequested
    {
        add => AddHandler(TaskActionRequestedEvent, value);
        remove => RemoveHandler(TaskActionRequestedEvent, value);
    }

    public event EventHandler<ScheduledTaskHistoryActionRequestedEventArgs> HistoryActionRequested
    {
        add => AddHandler(HistoryActionRequestedEvent, value);
        remove => RemoveHandler(HistoryActionRequestedEvent, value);
    }

    private static RoutedEvent RegisterEvent(string name) => EventManager.RegisterRoutedEvent(
        name,
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(SchedulerPageView));

    private void NewScheduledTask_Click(object sender, RoutedEventArgs e) =>
        RaiseEvent(new RoutedEventArgs(NewTaskRequestedEvent));

    private void RunScheduledTask_Click(object sender, RoutedEventArgs e) =>
        RaiseTaskAction(sender, ScheduledTaskAction.Run);

    private void EditScheduledTask_Click(object sender, RoutedEventArgs e) =>
        RaiseTaskAction(sender, ScheduledTaskAction.Edit);

    private void DeleteScheduledTask_Click(object sender, RoutedEventArgs e) =>
        RaiseTaskAction(sender, ScheduledTaskAction.Delete);

    private void ViewScheduledTaskLog_Click(object sender, RoutedEventArgs e) =>
        RaiseHistoryAction(sender, ScheduledTaskHistoryAction.View);

    private void DeleteScheduledTaskLog_Click(object sender, RoutedEventArgs e) =>
        RaiseHistoryAction(sender, ScheduledTaskHistoryAction.Delete);

    private void ClearScheduledTaskHistory_Click(object sender, RoutedEventArgs e) =>
        RaiseEvent(new ScheduledTaskHistoryActionRequestedEventArgs(
            HistoryActionRequestedEvent,
            null,
            ScheduledTaskHistoryAction.ClearAll));

    private void ClearHistoryFilter_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is SchedulerPageViewModel page)
        {
            page.ClearHistoryFilter();
            HistoryFilterTextBox.Focus();
        }
    }

    private void RaiseTaskAction(object sender, ScheduledTaskAction action)
    {
        if (sender is Button { Tag: string taskId })
        {
            RaiseEvent(new ScheduledTaskActionRequestedEventArgs(TaskActionRequestedEvent, taskId, action));
        }
    }

    private void RaiseHistoryAction(object sender, ScheduledTaskHistoryAction action)
    {
        if (sender is Button { Tag: string recordId })
        {
            RaiseEvent(new ScheduledTaskHistoryActionRequestedEventArgs(
                HistoryActionRequestedEvent,
                recordId,
                action));
        }
    }
}

public sealed class ScheduledTaskActionRequestedEventArgs(
    RoutedEvent routedEvent,
    string taskId,
    ScheduledTaskAction action) : RoutedEventArgs(routedEvent)
{
    public string TaskId { get; } = taskId;

    public ScheduledTaskAction Action { get; } = action;
}

public enum ScheduledTaskAction
{
    Run,
    Edit,
    Delete
}

public sealed class ScheduledTaskHistoryActionRequestedEventArgs(
    RoutedEvent routedEvent,
    string? recordId,
    ScheduledTaskHistoryAction action) : RoutedEventArgs(routedEvent)
{
    public string? RecordId { get; } = recordId;

    public ScheduledTaskHistoryAction Action { get; } = action;
}

public enum ScheduledTaskHistoryAction
{
    View,
    Delete,
    ClearAll
}
