using PortableDeveloper.App.ViewModels;

namespace PortableDeveloper.Tests;

public sealed class TransientNotificationViewModelTests
{
    [Fact]
    public void Show_replaces_the_current_notification_and_dismiss_clears_it()
    {
        var notification = new TransientNotificationViewModel();

        notification.Show("Copied", TransientNotificationIntent.Information);
        notification.Show("Saved", TransientNotificationIntent.Success);

        Assert.True(notification.IsVisible);
        Assert.Equal("Saved", notification.Message);
        Assert.Equal(TransientNotificationIntent.Success, notification.Intent);

        notification.Dismiss();

        Assert.False(notification.IsVisible);
        Assert.Empty(notification.Message);
    }

    [Fact]
    public void Empty_message_does_not_replace_a_visible_notification()
    {
        var notification = new TransientNotificationViewModel();
        notification.Show("Saved");

        notification.Show("   ", TransientNotificationIntent.Information);

        Assert.True(notification.IsVisible);
        Assert.Equal("Saved", notification.Message);
        Assert.Equal(TransientNotificationIntent.Success, notification.Intent);
        notification.Dismiss();
    }

    [Fact]
    public void Error_notification_uses_the_critical_visual_intent()
    {
        var notification = new TransientNotificationViewModel();
        notification.Show("Invalid password", TransientNotificationIntent.Error);

        Assert.Equal(TransientNotificationIntent.Error, notification.Intent);

        var host = File.ReadAllText(Path.Combine(FindAppRoot(), "Controls", "TransientNotificationHost.xaml"));
        Assert.Contains("TransientNotificationIntent.Error", host, StringComparison.Ordinal);
        Assert.Contains("SystemFillColorCriticalBrush", host, StringComparison.Ordinal);
        notification.Dismiss();
    }

    [Fact]
    public void Notification_host_declares_polite_live_region_and_manual_dismissal()
    {
        var appRoot = FindAppRoot();
        var host = File.ReadAllText(Path.Combine(appRoot, "Controls", "TransientNotificationHost.xaml"));
        var hostCode = File.ReadAllText(Path.Combine(appRoot, "Controls", "TransientNotificationHost.xaml.cs"));

        Assert.Contains("AutomationProperties.LiveSetting=\"Polite\"", host, StringComparison.Ordinal);
        Assert.Contains("Click=\"Dismiss_Click\"", host, StringComparison.Ordinal);
        Assert.Contains("LiveRegionChanged", hostCode, StringComparison.Ordinal);
        Assert.Contains("notification.Pause()", hostCode, StringComparison.Ordinal);
        Assert.Contains("notification.Resume()", hostCode, StringComparison.Ordinal);
    }

    [Fact]
    public void Clipboard_confirmation_uses_transient_information_feedback()
    {
        var selenium = File.ReadAllText(Path.Combine(FindAppRoot(), "MainWindow.Selenium.cs"));

        Assert.Contains(
            "ShowTransientNotification(successMessage, TransientNotificationIntent.Information)",
            selenium,
            StringComparison.Ordinal);
        Assert.DoesNotContain("SetSeleniumStatus(successMessage)", selenium, StringComparison.Ordinal);
    }

    private static string FindAppRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var appRoot = Path.Combine(directory.FullName, "src", "PortableDeveloper.App");
            if (Directory.Exists(appRoot))
            {
                return appRoot;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("PortableDeveloper.App was not found above the test output directory.");
    }
}
