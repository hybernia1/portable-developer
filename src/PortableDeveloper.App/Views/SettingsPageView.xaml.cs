using System.Windows;
using System.Windows.Controls;
using PortableDeveloper.Application.Settings;
using PortableDeveloper.Application.Storage;

namespace PortableDeveloper.App.Views;

public partial class SettingsPageView : UserControl
{
    public static readonly RoutedEvent EditorPreferenceChangedEvent = EventManager.RegisterRoutedEvent(
        nameof(EditorPreferenceChanged),
        RoutingStrategy.Bubble,
        typeof(EventHandler<EditorPreferenceChangedEventArgs>),
        typeof(SettingsPageView));
    public static readonly RoutedEvent RefreshStorageRequestedEvent = RegisterEvent(nameof(RefreshStorageRequested));
    public static readonly RoutedEvent ClearAllCachesRequestedEvent = RegisterEvent(nameof(ClearAllCachesRequested));
    public static readonly RoutedEvent ClearCacheRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(ClearCacheRequested),
        RoutingStrategy.Bubble,
        typeof(EventHandler<StorageCacheRequestedEventArgs>),
        typeof(SettingsPageView));

    public SettingsPageView()
    {
        InitializeComponent();
    }

    public event EventHandler<EditorPreferenceChangedEventArgs> EditorPreferenceChanged
    {
        add => AddHandler(EditorPreferenceChangedEvent, value);
        remove => RemoveHandler(EditorPreferenceChangedEvent, value);
    }

    public event RoutedEventHandler RefreshStorageRequested
    {
        add => AddHandler(RefreshStorageRequestedEvent, value);
        remove => RemoveHandler(RefreshStorageRequestedEvent, value);
    }

    public event RoutedEventHandler ClearAllCachesRequested
    {
        add => AddHandler(ClearAllCachesRequestedEvent, value);
        remove => RemoveHandler(ClearAllCachesRequestedEvent, value);
    }

    public event EventHandler<StorageCacheRequestedEventArgs> ClearCacheRequested
    {
        add => AddHandler(ClearCacheRequestedEvent, value);
        remove => RemoveHandler(ClearCacheRequestedEvent, value);
    }

    private static RoutedEvent RegisterEvent(string name) => EventManager.RegisterRoutedEvent(
        name,
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(SettingsPageView));

    private void EditorPreferenceSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is ComboBox { SelectedValue: string preferenceName }
            && Enum.TryParse<FileEditorPreference>(preferenceName, out var preference)
            && Enum.IsDefined(preference))
        {
            RaiseEvent(new EditorPreferenceChangedEventArgs(EditorPreferenceChangedEvent, preference));
        }
    }

    private void RefreshStorageUsage_Click(object sender, RoutedEventArgs e) =>
        RaiseEvent(new RoutedEventArgs(RefreshStorageRequestedEvent));

    private void ClearAllStorageCaches_Click(object sender, RoutedEventArgs e) =>
        RaiseEvent(new RoutedEventArgs(ClearAllCachesRequestedEvent));

    private void ClearStorageCache_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string cacheName }
            && Enum.TryParse<StorageCacheKind>(cacheName, out var cache))
        {
            RaiseEvent(new StorageCacheRequestedEventArgs(ClearCacheRequestedEvent, cache));
        }
    }
}

public sealed class EditorPreferenceChangedEventArgs(
    RoutedEvent routedEvent,
    FileEditorPreference preference) : RoutedEventArgs(routedEvent)
{
    public FileEditorPreference Preference { get; } = preference;
}

public sealed class StorageCacheRequestedEventArgs(
    RoutedEvent routedEvent,
    StorageCacheKind cache) : RoutedEventArgs(routedEvent)
{
    public StorageCacheKind Cache { get; } = cache;
}
