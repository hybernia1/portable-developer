using System.Windows;
using System.Windows.Controls;

namespace PortableDeveloper.App.Views;

public partial class DatabasesPageView : UserControl
{
    public static readonly RoutedEvent ToggleRequestedEvent = RegisterEvent(nameof(ToggleRequested));
    public static readonly RoutedEvent CreateDatabaseRequestedEvent = RegisterEvent(nameof(CreateDatabaseRequested));
    public static readonly RoutedEvent RefreshRequestedEvent = RegisterEvent(nameof(RefreshRequested));
    public static readonly RoutedEvent OpenPhpMyAdminRequestedEvent = RegisterEvent(nameof(OpenPhpMyAdminRequested));
    public static readonly RoutedEvent DatabaseActionRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(DatabaseActionRequested),
        RoutingStrategy.Bubble,
        typeof(EventHandler<DatabaseActionRequestedEventArgs>),
        typeof(DatabasesPageView));
    public static readonly RoutedEvent ChangePasswordRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(ChangePasswordRequested),
        RoutingStrategy.Bubble,
        typeof(EventHandler<PasswordChangeRequestedEventArgs>),
        typeof(DatabasesPageView));

    public DatabasesPageView()
    {
        InitializeComponent();
    }

    public event RoutedEventHandler ToggleRequested
    {
        add => AddHandler(ToggleRequestedEvent, value);
        remove => RemoveHandler(ToggleRequestedEvent, value);
    }

    public event RoutedEventHandler CreateDatabaseRequested
    {
        add => AddHandler(CreateDatabaseRequestedEvent, value);
        remove => RemoveHandler(CreateDatabaseRequestedEvent, value);
    }

    public event RoutedEventHandler RefreshRequested
    {
        add => AddHandler(RefreshRequestedEvent, value);
        remove => RemoveHandler(RefreshRequestedEvent, value);
    }

    public event RoutedEventHandler OpenPhpMyAdminRequested
    {
        add => AddHandler(OpenPhpMyAdminRequestedEvent, value);
        remove => RemoveHandler(OpenPhpMyAdminRequestedEvent, value);
    }

    public event EventHandler<DatabaseActionRequestedEventArgs> DatabaseActionRequested
    {
        add => AddHandler(DatabaseActionRequestedEvent, value);
        remove => RemoveHandler(DatabaseActionRequestedEvent, value);
    }

    public event EventHandler<PasswordChangeRequestedEventArgs> ChangePasswordRequested
    {
        add => AddHandler(ChangePasswordRequestedEvent, value);
        remove => RemoveHandler(ChangePasswordRequestedEvent, value);
    }

    private static RoutedEvent RegisterEvent(string name) => EventManager.RegisterRoutedEvent(
        name,
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(DatabasesPageView));

    private void ToggleMariaDb_Click(object sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(ToggleRequestedEvent));

    private void CreateDatabase_Click(object sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(CreateDatabaseRequestedEvent));

    private void RefreshDatabases_Click(object sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(RefreshRequestedEvent));

    private void OpenPhpMyAdmin_Click(object sender, RoutedEventArgs e) => RaiseEvent(new RoutedEventArgs(OpenPhpMyAdminRequestedEvent));

    private void ManageDatabase_Click(object sender, RoutedEventArgs e) => RaiseDatabaseAction(sender, DatabaseAction.Manage);

    private void DeleteDatabase_Click(object sender, RoutedEventArgs e) => RaiseDatabaseAction(sender, DatabaseAction.Delete);

    private void RaiseDatabaseAction(object sender, DatabaseAction action)
    {
        if (sender is Button { Tag: string databaseName })
        {
            RaiseEvent(new DatabaseActionRequestedEventArgs(DatabaseActionRequestedEvent, databaseName, action));
        }
    }

    private void ChangeRootPassword_Click(object sender, RoutedEventArgs e) => RaiseEvent(
        new PasswordChangeRequestedEventArgs(
            ChangePasswordRequestedEvent,
            RootPasswordBox.Password,
            ConfirmRootPasswordBox.Password,
            ClearPasswordInputs));

    private void ClearPasswordInputs()
    {
        RootPasswordBox.Clear();
        ConfirmRootPasswordBox.Clear();
    }
}

public sealed class DatabaseActionRequestedEventArgs(
    RoutedEvent routedEvent,
    string databaseName,
    DatabaseAction action) : RoutedEventArgs(routedEvent)
{
    public string DatabaseName { get; } = databaseName;

    public DatabaseAction Action { get; } = action;
}

public enum DatabaseAction
{
    Manage,
    Delete
}

public sealed class PasswordChangeRequestedEventArgs(
    RoutedEvent routedEvent,
    string password,
    string confirmation,
    Action clearInputs) : RoutedEventArgs(routedEvent)
{
    public string Password { get; } = password;

    public string Confirmation { get; } = confirmation;

    public void ClearInputs() => clearInputs();
}
