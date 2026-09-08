using System.Windows;
using System.Windows.Controls;
using PortableDeveloper.App.ViewModels;

namespace PortableDeveloper.App.Views;

public partial class SeleniumPageView : UserControl
{
    public static readonly RoutedEvent ActionRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(ActionRequested),
        RoutingStrategy.Bubble,
        typeof(EventHandler<SeleniumActionRequestedEventArgs>),
        typeof(SeleniumPageView));

    public SeleniumPageView()
    {
        InitializeComponent();
    }

    public event EventHandler<SeleniumActionRequestedEventArgs> ActionRequested
    {
        add => AddHandler(ActionRequestedEvent, value);
        remove => RemoveHandler(ActionRequestedEvent, value);
    }

    private void Request(SeleniumAction action, object? payload = null) =>
        RaiseEvent(new SeleniumActionRequestedEventArgs(ActionRequestedEvent, action, payload));

    private void ToggleSelenium_Click(object sender, RoutedEventArgs e) => Request(SeleniumAction.ToggleServer);
    private void OpenSeleniumHub_Click(object sender, RoutedEventArgs e) => Request(SeleniumAction.OpenHub);
    private void SaveSeleniumSettings_Click(object sender, RoutedEventArgs e) => Request(SeleniumAction.SaveSettings);
    private void ReloadSeleniumDrivers_Click(object sender, RoutedEventArgs e) => Request(SeleniumAction.ReloadDrivers);
    private void CreateCleanSeleniumProfile_Click(object sender, RoutedEventArgs e) => Request(SeleniumAction.CreateProfile);
    private void ShowProfileHelp_Click(object sender, RoutedEventArgs e) => Request(SeleniumAction.ShowProfileHelp);
    private void ShowCookieVaultHelp_Click(object sender, RoutedEventArgs e) => Request(SeleniumAction.ShowCookieVaultHelp);
    private void ImportCookieVault_Click(object sender, RoutedEventArgs e) => Request(SeleniumAction.ImportCookieVault);
    private void RefreshSeleniumSessions_Click(object sender, RoutedEventArgs e) => Request(SeleniumAction.RefreshSessions);

    private void InstallRuntimePackage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { DataContext: RuntimePackageViewModel package })
        {
            Request(SeleniumAction.InstallDriver, package);
        }
    }

    private void EditSeleniumProfile_Click(object sender, RoutedEventArgs e) => RequestId(sender, SeleniumAction.EditProfile);
    private void CopySeleniumProfileId_Click(object sender, RoutedEventArgs e) => RequestId(sender, SeleniumAction.CopyProfileId);
    private void RemoveSeleniumProfile_Click(object sender, RoutedEventArgs e) => RequestId(sender, SeleniumAction.RemoveProfile);
    private void CopyCookieVaultId_Click(object sender, RoutedEventArgs e) => RequestId(sender, SeleniumAction.CopyCookieVaultId);
    private void RemoveCookieVault_Click(object sender, RoutedEventArgs e) => RequestId(sender, SeleniumAction.RemoveCookieVault);
    private void TerminateSeleniumSession_Click(object sender, RoutedEventArgs e) => RequestId(sender, SeleniumAction.TerminateSession);

    private void RequestId(object sender, SeleniumAction action)
    {
        if (sender is Button { Tag: string id })
        {
            Request(action, id);
        }
    }
}

public sealed class SeleniumActionRequestedEventArgs(
    RoutedEvent routedEvent,
    SeleniumAction action,
    object? payload) : RoutedEventArgs(routedEvent)
{
    public SeleniumAction Action { get; } = action;
    public object? Payload { get; } = payload;
}

public enum SeleniumAction
{
    ToggleServer,
    OpenHub,
    SaveSettings,
    ReloadDrivers,
    InstallDriver,
    CreateProfile,
    ShowProfileHelp,
    EditProfile,
    CopyProfileId,
    RemoveProfile,
    ShowCookieVaultHelp,
    ImportCookieVault,
    CopyCookieVaultId,
    RemoveCookieVault,
    RefreshSessions,
    TerminateSession
}
