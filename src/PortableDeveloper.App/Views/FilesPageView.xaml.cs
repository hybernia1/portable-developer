using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PortableDeveloper.App.Views;

public partial class FilesPageView : UserControl
{
    public static readonly RoutedEvent InteractionRequestedEvent = EventManager.RegisterRoutedEvent(
        nameof(InteractionRequested),
        RoutingStrategy.Bubble,
        typeof(EventHandler<FilesInteractionRequestedEventArgs>),
        typeof(FilesPageView));

    public FilesPageView()
    {
        InitializeComponent();
    }

    public event EventHandler<FilesInteractionRequestedEventArgs> InteractionRequested
    {
        add => AddHandler(InteractionRequestedEvent, value);
        remove => RemoveHandler(InteractionRequestedEvent, value);
    }

    internal ListBox EntriesListBox => WorkspaceEntriesListBox;

    internal TextBox PathTextBox => WorkspacePathTextBox;

    internal MenuItem PasteMenuItem => WorkspaceContextPasteMenuItem;

    internal MenuItem NewFileMenuItem => WorkspaceContextNewFileMenuItem;

    internal MenuItem NewFolderMenuItem => WorkspaceContextNewFolderMenuItem;

    private void FilesPageView_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.L)
        {
            e.Handled = true;
            PathTextBox.Focus();
            PathTextBox.SelectAll();
        }
    }

    private void RefreshWorkspace_Click(object sender, RoutedEventArgs e) => Forward(FilesInteraction.Refresh, sender, e);

    private void WorkspaceBack_Click(object sender, RoutedEventArgs e) => Forward(FilesInteraction.Back, sender, e);

    private void WorkspacePathTextBox_KeyDown(object sender, KeyEventArgs e) => Forward(FilesInteraction.PathKeyDown, sender, e);

    private void WorkspacePathTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) =>
        Forward(FilesInteraction.PathGotKeyboardFocus, sender, e);

    private void WorkspaceSort_Click(object sender, RoutedEventArgs e) => Forward(FilesInteraction.Sort, sender, e);

    private void WorkspacePageSizeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e) =>
        Forward(FilesInteraction.PageSizeChanged, sender, e);

    private void WorkspacePage_Click(object sender, RoutedEventArgs e) => Forward(FilesInteraction.Page, sender, e);

    private void CreateWorkspaceFile_Click(object sender, RoutedEventArgs e) => Forward(FilesInteraction.CreateFile, sender, e);

    private void CreateWorkspaceFolder_Click(object sender, RoutedEventArgs e) => Forward(FilesInteraction.CreateFolder, sender, e);

    private void WorkspaceEntry_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        Forward(FilesInteraction.EntryMouseLeftButtonDown, sender, e);

    private void WorkspaceEntriesListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e) =>
        Forward(FilesInteraction.ListPreviewMouseLeftButtonDown, sender, e);

    private void WorkspaceEntriesListBox_PreviewMouseMove(object sender, MouseEventArgs e) =>
        Forward(FilesInteraction.ListPreviewMouseMove, sender, e);

    private void WorkspaceName_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e) =>
        Forward(FilesInteraction.NamePreviewMouseRightButtonDown, sender, e);

    private void WorkspaceFileList_DragOver(object sender, DragEventArgs e) => Forward(FilesInteraction.DragOver, sender, e);

    private void WorkspaceFileList_Drop(object sender, DragEventArgs e) => Forward(FilesInteraction.Drop, sender, e);

    private void WorkspaceEntriesListBox_PreviewKeyDown(object sender, KeyEventArgs e) =>
        Forward(FilesInteraction.ListPreviewKeyDown, sender, e);

    private void WorkspaceBackgroundContextMenu_Opened(object sender, RoutedEventArgs e) =>
        Forward(FilesInteraction.BackgroundContextMenuOpened, sender, e);

    private void WorkspaceItemContextMenu_Opened(object sender, RoutedEventArgs e) =>
        Forward(FilesInteraction.ItemContextMenuOpened, sender, e);

    private void WorkspaceContextOpen_Click(object sender, RoutedEventArgs e) => Forward(FilesInteraction.Open, sender, e);

    private void WorkspaceContextRename_Click(object sender, RoutedEventArgs e) => Forward(FilesInteraction.Rename, sender, e);

    private void WorkspaceContextCopy_Click(object sender, RoutedEventArgs e) => Forward(FilesInteraction.Copy, sender, e);

    private void WorkspaceContextCut_Click(object sender, RoutedEventArgs e) => Forward(FilesInteraction.Cut, sender, e);

    private void WorkspaceContextPaste_Click(object sender, RoutedEventArgs e) => Forward(FilesInteraction.Paste, sender, e);

    private void WorkspaceContextDelete_Click(object sender, RoutedEventArgs e) => Forward(FilesInteraction.Delete, sender, e);

    private void WorkspaceRenameTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) =>
        Forward(FilesInteraction.RenameVisibilityChanged, sender, e);

    private void WorkspaceRenameTextBox_KeyDown(object sender, KeyEventArgs e) =>
        Forward(FilesInteraction.RenameKeyDown, sender, e);

    private void WorkspaceRenameTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) =>
        Forward(FilesInteraction.RenameLostKeyboardFocus, sender, e);

    private void Forward(FilesInteraction interaction, object sender, object eventArgs) =>
        RaiseEvent(new FilesInteractionRequestedEventArgs(
            InteractionRequestedEvent,
            this,
            interaction,
            sender,
            eventArgs));
}

public sealed class FilesInteractionRequestedEventArgs(
    RoutedEvent routedEvent,
    FilesPageView view,
    FilesInteraction interaction,
    object originalSender,
    object interactionEventArgs) : RoutedEventArgs(routedEvent)
{
    public FilesPageView View { get; } = view;

    public FilesInteraction Interaction { get; } = interaction;

    public object OriginalSender { get; } = originalSender;

    public object InteractionEventArgs { get; } = interactionEventArgs;
}

public enum FilesInteraction
{
    Refresh,
    Back,
    PathKeyDown,
    PathGotKeyboardFocus,
    Sort,
    PageSizeChanged,
    Page,
    CreateFile,
    CreateFolder,
    EntryMouseLeftButtonDown,
    ListPreviewMouseLeftButtonDown,
    ListPreviewMouseMove,
    NamePreviewMouseRightButtonDown,
    DragOver,
    Drop,
    ListPreviewKeyDown,
    BackgroundContextMenuOpened,
    ItemContextMenuOpened,
    Open,
    Rename,
    Copy,
    Cut,
    Paste,
    Delete,
    RenameVisibilityChanged,
    RenameKeyDown,
    RenameLostKeyboardFocus
}
