using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using PortableDeveloper.App.ViewModels;
using PortableDeveloper.Application.Workspace;

namespace PortableDeveloper.App;

public partial class MainWindow
{

    private void RefreshWorkspace_Click(object sender, RoutedEventArgs e) => RefreshWorkspaceFiles();

    private void MainWindow_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_dashboard.SelectedPage == NavigationPage.Files &&
            Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.L)
        {
            e.Handled = true;
            WorkspacePathTextBox.Focus();
            WorkspacePathTextBox.SelectAll();
        }
    }

    private void WorkspacePathTextBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e) =>
        WorkspacePathTextBox.SelectAll();

    private void WorkspacePathTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            e.Handled = true;
            WorkspacePathTextBox.Text = DisplayTerminalPath(_workspaceDirectory);
            Keyboard.ClearFocus();
            return;
        }

        if (e.Key != Key.Enter)
        {
            return;
        }

        e.Handled = true;
        try
        {
            var requested = WorkspacePathTextBox.Text.Trim();
            var prefix = $"{_projectContext.ActiveProject.Id}:/";
            if (requested.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                requested = requested[prefix.Length..];
            }

            requested = requested.Replace('\\', '/').TrimStart('/');
            var normalized = _workspaceFileManager.NormalizeDirectory(requested);
            if (!string.Equals(normalized, _workspaceDirectory, StringComparison.OrdinalIgnoreCase))
            {
                _workspaceHistory.Push(_workspaceDirectory);
                _workspaceDirectory = normalized;
                _workspacePageNumber = 1;
            }

            RefreshWorkspaceFiles();
            Keyboard.ClearFocus();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            WorkspacePathTextBox.Text = DisplayTerminalPath(_workspaceDirectory);
            InstallationStatusText.Text = _dashboard.Text.WorkspaceOperationFailed(exception.Message);
        }
    }

    private void WorkspaceSort_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string columnText } ||
            !Enum.TryParse<WorkspaceSortColumn>(columnText, out var column))
        {
            return;
        }

        if (_workspaceSortColumn == column)
        {
            _workspaceSortDirection = _workspaceSortDirection == WorkspaceSortDirection.Ascending
                ? WorkspaceSortDirection.Descending
                : WorkspaceSortDirection.Ascending;
        }
        else
        {
            _workspaceSortColumn = column;
            _workspaceSortDirection = WorkspaceSortDirection.Ascending;
        }

        _workspacePageNumber = 1;
        UpdateWorkspaceSortHeaders();
        RefreshWorkspaceFiles();
    }

    private void UpdateWorkspaceSortHeaders()
    {
        WorkspaceNameSortButton.Content = GetWorkspaceSortHeader(_dashboard.Text.Name, WorkspaceSortColumn.Name);
        WorkspaceTypeSortButton.Content = GetWorkspaceSortHeader(_dashboard.Text.Type, WorkspaceSortColumn.Type);
        WorkspaceSizeSortButton.Content = GetWorkspaceSortHeader(_dashboard.Text.Size, WorkspaceSortColumn.Size);
        WorkspaceModifiedSortButton.Content = GetWorkspaceSortHeader(_dashboard.Text.Modified, WorkspaceSortColumn.Modified);
    }

    private string GetWorkspaceSortHeader(string label, WorkspaceSortColumn column)
    {
        if (_workspaceSortColumn != column)
        {
            return label;
        }

        return $"{label} {(_workspaceSortDirection == WorkspaceSortDirection.Ascending ? "↑" : "↓")}";
    }

    private void WorkspacePageSizeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded || sender is not ComboBox { SelectedValue: string value } || !int.TryParse(value, out var pageSize))
        {
            return;
        }

        _workspacePageSize = pageSize;
        _workspacePageNumber = 1;
        RefreshWorkspaceFiles();
    }

    private void WorkspacePage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string action })
        {
            return;
        }

        _workspacePageNumber = action switch
        {
            "First" => 1,
            "Previous" => Math.Max(1, _workspacePageNumber - 1),
            "Next" => Math.Min(_dashboard.WorkspaceTotalPages, _workspacePageNumber + 1),
            "Last" => _dashboard.WorkspaceTotalPages,
            _ => _workspacePageNumber
        };
        RefreshWorkspaceFiles();
    }

    private void WorkspaceBack_Click(object sender, RoutedEventArgs e)
    {
        if (_workspaceHistory.Count == 0)
        {
            return;
        }

        _workspaceDirectory = _workspaceHistory.Pop();
        _workspacePageNumber = 1;
        RefreshWorkspaceFiles();
    }

    private void CreateWorkspaceFile_Click(object sender, RoutedEventArgs e)
    {
        var name = PromptForWorkspaceName(
            _dashboard.Text.CreateFileTitle,
            _dashboard.Text.EnterFileName);
        if (name is null)
        {
            return;
        }

        RunWorkspaceOperation(() => _workspaceFileManager.CreateFile(_workspaceDirectory, name));
    }

    private void CreateWorkspaceFolder_Click(object sender, RoutedEventArgs e)
    {
        var name = PromptForWorkspaceName(
            _dashboard.Text.CreateFolderTitle,
            _dashboard.Text.EnterFolderName);
        if (name is null)
        {
            return;
        }

        RunWorkspaceOperation(() => _workspaceFileManager.CreateDirectory(_workspaceDirectory, name));
    }

    private async void WorkspaceEntry_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.OriginalSource is TextBox
            || e.ClickCount != 2
            || sender is not Border { Tag: WorkspaceEntryViewModel entry }
            || !entry.IsSafe)
        {
            return;
        }

        e.Handled = true;
        await OpenWorkspaceEntryAsync(entry);
    }

    private void WorkspaceEntriesListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _workspaceDragAnchor = null;
        _workspaceRenameCandidate = null;
        if (e.OriginalSource is TextBox || e.OriginalSource is not DependencyObject source)
        {
            return;
        }

        var item = ItemsControl.ContainerFromElement(WorkspaceEntriesListBox, source) as ListBoxItem;
        if (item?.DataContext is not WorkspaceEntryViewModel { IsSafe: true } entry)
        {
            return;
        }

        _workspaceDragStartPoint = e.GetPosition(WorkspaceEntriesListBox);
        _workspaceDragAnchor = entry;
        if (e.ClickCount == 1
            && source is TextBlock { Tag: "WorkspaceEntryName" }
            && WorkspaceEntriesListBox.SelectedItems.Count == 1
            && ReferenceEquals(WorkspaceEntriesListBox.SelectedItem, entry))
        {
            _workspaceRenameCandidate = entry;
        }
    }

    private void WorkspaceEntriesListBox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _workspaceDragAnchor is not { IsSafe: true } anchor)
        {
            return;
        }

        var current = e.GetPosition(WorkspaceEntriesListBox);
        if (Math.Abs(current.X - _workspaceDragStartPoint.X) < SystemParameters.MinimumHorizontalDragDistance
            && Math.Abs(current.Y - _workspaceDragStartPoint.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var entries = GetSelectedWorkspaceEntries();
        if (!entries.Contains(anchor))
        {
            entries = [anchor];
        }

        _workspaceDragAnchor = null;
        _workspaceRenameCandidate = null;
        try
        {
            var sourcePaths = entries
                .Select(entry => _workspaceFileManager.GetExportPath(entry.RelativePath))
                .ToArray();
            var data = new DataObject(DataFormats.FileDrop, sourcePaths);
            data.SetData(WorkspaceDragDataFormat, true);
            data.SetData(
                "Preferred DropEffect",
                new MemoryStream(BitConverter.GetBytes((int)DragDropEffects.Copy)));
            DragDrop.DoDragDrop(
                WorkspaceEntriesListBox,
                data,
                DragDropEffects.Copy | DragDropEffects.Move);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            InstallationStatusText.Text = _dashboard.Text.WorkspaceOperationFailed(exception.Message);
        }
    }

    private void WorkspaceName_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        var candidate = _workspaceRenameCandidate;
        _workspaceDragAnchor = null;
        _workspaceRenameCandidate = null;
        if (sender is not TextBlock { DataContext: WorkspaceEntryViewModel entry }
            || !ReferenceEquals(candidate, entry))
        {
            return;
        }

        e.Handled = true;
        BeginWorkspaceRename(entry);
    }

    private void WorkspaceFileList_DragOver(object sender, DragEventArgs e)
    {
        e.Effects = GetWorkspaceDropPaths(e.Data) is not { Length: > 0 }
            ? DragDropEffects.None
            : e.Data.GetDataPresent(WorkspaceDragDataFormat)
                ? DragDropEffects.Move
                : DragDropEffects.Copy;
        e.Handled = true;
    }

    private void WorkspaceFileList_Drop(object sender, DragEventArgs e)
    {
        var sourcePaths = GetWorkspaceDropPaths(e.Data);
        if (sourcePaths is not { Length: > 0 })
        {
            return;
        }

        var destinationDirectory = _workspaceDirectory;
        if (e.OriginalSource is DependencyObject source
            && ItemsControl.ContainerFromElement(WorkspaceEntriesListBox, source) is ListBoxItem
            {
                DataContext: WorkspaceEntryViewModel { IsDirectory: true, IsSafe: true } directory
            })
        {
            destinationDirectory = directory.RelativePath;
        }

        e.Handled = true;
        if (e.Data.GetDataPresent(WorkspaceDragDataFormat))
        {
            MoveDroppedWorkspaceEntries(sourcePaths, destinationDirectory);
            e.Effects = DragDropEffects.Move;
            return;
        }

        var importedCount = 0;
        var resolveConflict = CreateWorkspaceConflictResolver();
        RunWorkspaceOperation(
            () => importedCount = _workspaceFileManager.Import(
                sourcePaths,
                destinationDirectory,
                _dashboard.Text.WorkspaceCopyNameSuffix,
                resolveConflict),
            () => _dashboard.Text.WorkspaceItemsImported(importedCount));
        e.Effects = DragDropEffects.Copy;
    }

    private void MoveDroppedWorkspaceEntries(IReadOnlyList<string> sourcePaths, string destinationDirectory)
    {
        var relativePaths = new List<string>(sourcePaths.Count);
        foreach (var sourcePath in sourcePaths)
        {
            if (!_workspaceFileManager.TryGetRelativePath(sourcePath, out var relativePath))
            {
                InstallationStatusText.Text = _dashboard.Text.WorkspaceOperationFailed(
                    _dashboard.Text.WorkspaceDraggedItemUnavailable);
                return;
            }

            if (!IsWorkspaceDropNoOp(relativePath, destinationDirectory))
            {
                relativePaths.Add(relativePath);
            }
        }

        if (relativePaths.Count == 0)
        {
            InstallationStatusText.Text = string.Empty;
            return;
        }

        var movedCount = 0;
        var resolveConflict = CreateWorkspaceConflictResolver();
        RunWorkspaceOperation(
            () =>
            {
                foreach (var relativePath in relativePaths)
                {
                    if (_workspaceFileManager.Move(
                        relativePath,
                        destinationDirectory,
                        _dashboard.Text.WorkspaceCopyNameSuffix,
                        resolveConflict))
                    {
                        movedCount++;
                    }
                }
            },
            () => _dashboard.Text.WorkspaceItemsMoved(movedCount, relativePaths.Count));
    }

    private static bool IsWorkspaceDropNoOp(string sourceRelativePath, string destinationRelativeDirectory)
    {
        var normalizedSource = sourceRelativePath.Replace('\\', '/').Trim('/');
        var normalizedDestination = destinationRelativeDirectory.Replace('\\', '/').Trim('/');
        var separatorIndex = normalizedSource.LastIndexOf('/');
        var sourceParent = separatorIndex < 0 ? string.Empty : normalizedSource[..separatorIndex];
        return string.Equals(normalizedSource, normalizedDestination, StringComparison.OrdinalIgnoreCase)
            || string.Equals(sourceParent, normalizedDestination, StringComparison.OrdinalIgnoreCase);
    }

    private static string[]? GetWorkspaceDropPaths(IDataObject data) =>
        data.GetDataPresent(DataFormats.FileDrop)
            ? data.GetData(DataFormats.FileDrop) as string[]
            : null;

    private async void WorkspaceEntriesListBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.OriginalSource is TextBox)
        {
            return;
        }

        var controlPressed = (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control;
        if (controlPressed && e.Key == Key.A)
        {
            e.Handled = true;
            WorkspaceEntriesListBox.SelectAll();
            return;
        }

        if (controlPressed && e.Key == Key.V)
        {
            e.Handled = true;
            PasteWorkspaceClipboard();
            return;
        }

        var entries = GetSelectedWorkspaceEntries();
        if (entries.Count == 0)
        {
            return;
        }

        if (controlPressed && e.Key is Key.C or Key.X)
        {
            e.Handled = true;
            SetWorkspaceClipboard(entries, isCut: e.Key == Key.X);
            return;
        }

        var entry = entries[0];

        switch (e.Key)
        {
            case Key.F2 when entries.Count == 1 && entry.IsSafe:
                e.Handled = true;
                BeginWorkspaceRename(entry);
                break;
            case Key.Enter when entries.Count == 1 && entry.IsSafe:
                e.Handled = true;
                await OpenWorkspaceEntryAsync(entry);
                break;
            case Key.Delete when entries.All(candidate => candidate.IsSafe):
                e.Handled = true;
                DeleteWorkspaceEntries(entries);
                break;
        }
    }

    private void WorkspaceName_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not TextBlock { DataContext: WorkspaceEntryViewModel entry })
        {
            return;
        }

        if (!WorkspaceEntriesListBox.SelectedItems.Contains(entry))
        {
            WorkspaceEntriesListBox.UnselectAll();
            WorkspaceEntriesListBox.SelectedItem = entry;
        }
    }

    private void WorkspaceBackgroundContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        WorkspaceContextPasteMenuItem.Header = _dashboard.Text.Paste;
        WorkspaceContextPasteMenuItem.IsEnabled = CanPasteWorkspaceClipboard();
        WorkspaceContextNewFileMenuItem.Header = _dashboard.Text.NewFile;
        WorkspaceContextNewFolderMenuItem.Header = _dashboard.Text.NewFolder;
    }

    private void WorkspaceItemContextMenu_Opened(object sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu contextMenu
            || contextMenu.PlacementTarget is not FrameworkElement { DataContext: WorkspaceEntryViewModel entry })
        {
            return;
        }

        contextMenu.DataContext = entry;
        var entries = GetSelectedWorkspaceEntries();
        var hasSafeSelection = entries.Count > 0 && entries.All(candidate => candidate.IsSafe);
        var hasSingleSafeSelection = entries.Count == 1 && hasSafeSelection;
        foreach (var menuItem in contextMenu.Items.OfType<MenuItem>())
        {
            menuItem.Header = menuItem.Tag switch
            {
                "Open" => _dashboard.Text.Open,
                "Copy" => _dashboard.Text.Copy,
                "Cut" => _dashboard.Text.Cut,
                "Rename" => _dashboard.Text.Rename,
                "Delete" => _dashboard.Text.Delete,
                _ => string.Empty
            };
            menuItem.IsEnabled = menuItem.Tag is "Open" or "Rename"
                ? hasSingleSafeSelection
                : hasSafeSelection;
        }
    }

    private async void WorkspaceContextOpen_Click(object sender, RoutedEventArgs e)
    {
        var entries = GetSelectedWorkspaceEntries();
        if (entries is [{ IsSafe: true } entry])
        {
            await OpenWorkspaceEntryAsync(entry);
        }
    }

    private void WorkspaceContextRename_Click(object sender, RoutedEventArgs e)
    {
        var entries = GetSelectedWorkspaceEntries();
        if (entries is [{ IsSafe: true } entry])
        {
            BeginWorkspaceRename(entry);
        }
    }

    private void WorkspaceContextCopy_Click(object sender, RoutedEventArgs e)
    {
        var entries = GetSelectedWorkspaceEntries();
        if (entries.Count > 0 && entries.All(entry => entry.IsSafe))
        {
            SetWorkspaceClipboard(entries, isCut: false);
        }
    }

    private void WorkspaceContextCut_Click(object sender, RoutedEventArgs e)
    {
        var entries = GetSelectedWorkspaceEntries();
        if (entries.Count > 0 && entries.All(entry => entry.IsSafe))
        {
            SetWorkspaceClipboard(entries, isCut: true);
        }
    }

    private void WorkspaceContextPaste_Click(object sender, RoutedEventArgs e) => PasteWorkspaceClipboard();

    private void WorkspaceContextDelete_Click(object sender, RoutedEventArgs e)
    {
        var entries = GetSelectedWorkspaceEntries();
        if (entries.Count > 0 && entries.All(entry => entry.IsSafe))
        {
            DeleteWorkspaceEntries(entries);
        }
    }

    private IReadOnlyList<WorkspaceEntryViewModel> GetSelectedWorkspaceEntries() =>
        WorkspaceEntriesListBox.SelectedItems
            .OfType<WorkspaceEntryViewModel>()
            .ToArray();

    private void SetWorkspaceClipboard(IReadOnlyList<WorkspaceEntryViewModel> entries, bool isCut)
    {
        if (entries.Count == 0 || entries.Any(entry => !entry.IsSafe))
        {
            return;
        }

        _workspaceClipboard = new WorkspaceClipboardEntry(
            _projectContext.ActiveProject.Id,
            entries.Select(entry => new WorkspaceClipboardItem(entry.RelativePath)).ToArray(),
            isCut);
        InstallationStatusText.Text = entries.Count == 1
            ? isCut
                ? _dashboard.Text.WorkspaceItemCut(entries[0].Name)
                : _dashboard.Text.WorkspaceItemCopied(entries[0].Name)
            : isCut
                ? _dashboard.Text.WorkspaceItemsCut(entries.Count)
                : _dashboard.Text.WorkspaceItemsCopied(entries.Count);
    }

    private bool CanPasteWorkspaceClipboard() =>
        _workspaceClipboard is not null
        && string.Equals(
            _workspaceClipboard.ProjectId,
            _projectContext.ActiveProject.Id,
            StringComparison.OrdinalIgnoreCase);

    private void PasteWorkspaceClipboard()
    {
        if (_workspaceClipboard is not { } clipboard || !CanPasteWorkspaceClipboard())
        {
            if (_workspaceClipboard is not null)
            {
                InstallationStatusText.Text = _dashboard.Text.WorkspaceClipboardUnavailable;
            }

            return;
        }

        var transferredPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var resolveConflict = CreateWorkspaceConflictResolver();
        Action operation = () =>
        {
            foreach (var item in clipboard.Items)
            {
                var transferred = clipboard.IsCut
                    ? _workspaceFileManager.Move(
                        item.RelativePath,
                        _workspaceDirectory,
                        _dashboard.Text.WorkspaceCopyNameSuffix,
                        resolveConflict)
                    : _workspaceFileManager.Copy(
                        item.RelativePath,
                        _workspaceDirectory,
                        _dashboard.Text.WorkspaceCopyNameSuffix,
                        resolveConflict);
                if (transferred)
                {
                    transferredPaths.Add(item.RelativePath);
                }
            }
        };
        RunWorkspaceOperation(
            operation,
            () => clipboard.Items.Count == 1
                ? transferredPaths.Count == 1
                    ? _dashboard.Text.WorkspacePasteCompleted
                    : _dashboard.Text.WorkspaceTransferSkipped
                : _dashboard.Text.WorkspaceItemsPasteCompleted(transferredPaths.Count, clipboard.Items.Count),
            clipboard.IsCut
                ? () =>
                {
                    if (!ReferenceEquals(_workspaceClipboard, clipboard))
                    {
                        return;
                    }

                    var remaining = clipboard.Items
                        .Where(item => !transferredPaths.Contains(item.RelativePath))
                        .ToArray();
                    _workspaceClipboard = remaining.Length == 0
                        ? null
                        : clipboard with { Items = remaining };
                }
        : null);
    }

    private Func<WorkspaceConflict, WorkspaceConflictDecision> CreateWorkspaceConflictResolver()
    {
        WorkspaceConflictDecision? applyToRemaining = null;
        return conflict =>
        {
            if (applyToRemaining is not null)
            {
                return applyToRemaining;
            }

            var decision = Dispatcher.Invoke(() => FileConflictDialog.Show(
                this,
                _dashboard.Text.WorkspaceConflictTitle,
                _dashboard.Text.WorkspaceConflictMessage(conflict),
                _dashboard.Text.Overwrite,
                _dashboard.Text.RenameCopy,
                _dashboard.Text.Skip,
                _dashboard.Text.ApplyToRemainingConflicts));
            if (decision.ApplyToRemaining && decision.Action != WorkspaceConflictAction.Cancel)
            {
                applyToRemaining = decision with { ApplyToRemaining = false };
            }

            return decision;
        };
    }

    private void BeginWorkspaceRename(WorkspaceEntryViewModel entry)
    {
        foreach (var candidate in _dashboard.WorkspaceEntries.Where(candidate => candidate.IsRenaming && !ReferenceEquals(candidate, entry)))
        {
            candidate.EditName = candidate.Name;
            candidate.IsRenaming = false;
        }

        WorkspaceEntriesListBox.SelectedItem = entry;
        entry.EditName = entry.Name;
        entry.IsRenaming = true;
    }

    private void WorkspaceRenameTextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not TextBox textBox || e.NewValue is not true)
        {
            return;
        }

        Dispatcher.BeginInvoke(
            DispatcherPriority.Input,
            () =>
            {
                textBox.Focus();
                textBox.SelectAll();
            });
    }

    private void WorkspaceRenameTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (sender is not TextBox { DataContext: WorkspaceEntryViewModel entry })
        {
            return;
        }

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            CommitWorkspaceRename(entry);
        }
        else if (e.Key == Key.Escape)
        {
            e.Handled = true;
            CancelWorkspaceRename(entry);
            WorkspaceEntriesListBox.Focus();
        }
    }

    private void WorkspaceRenameTextBox_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        if (sender is TextBox { DataContext: WorkspaceEntryViewModel entry } && entry.IsRenaming)
        {
            CommitWorkspaceRename(entry);
        }
    }

    private void CommitWorkspaceRename(WorkspaceEntryViewModel entry)
    {
        if (!entry.IsRenaming)
        {
            return;
        }

        var newName = entry.EditName.Trim();
        entry.IsRenaming = false;
        if (newName.Length == 0)
        {
            entry.EditName = entry.Name;
            InstallationStatusText.Text = _dashboard.Text.WorkspaceItemNameRequired;
            return;
        }

        if (string.Equals(newName, entry.Name, StringComparison.Ordinal))
        {
            return;
        }

        RunWorkspaceOperation(() => _workspaceFileManager.Rename(entry.RelativePath, newName));
    }

    private static void CancelWorkspaceRename(WorkspaceEntryViewModel entry)
    {
        entry.EditName = entry.Name;
        entry.IsRenaming = false;
    }

    private void DeleteWorkspaceEntries(IReadOnlyList<WorkspaceEntryViewModel> entries)
    {
        if (entries.Count == 0)
        {
            return;
        }

        var confirmed = ConfirmationDialog.Show(
            this,
            _dashboard.Text.DeleteItemTitle,
            entries.Count == 1
                ? _dashboard.Text.DeleteItemQuestion(entries[0].Name)
                : _dashboard.Text.DeleteItemsQuestion(entries.Count),
            _dashboard.Text.Delete,
            _dashboard.Text.Cancel);
        if (confirmed)
        {
            RunWorkspaceOperation(() =>
            {
                foreach (var entry in entries)
                {
                    _workspaceFileManager.Delete(entry.RelativePath);
                }
            });
        }
    }

    private async void RunWorkspaceOperation(
        Action operation,
        Func<string>? successMessage = null,
        Action? onFinished = null)
    {
        try
        {
            await Task.Run(operation, _applicationLifetime.Token);
            RefreshWorkspaceFiles();
            InstallationStatusText.Text = successMessage?.Invoke() ?? string.Empty;
        }
        catch (OperationCanceledException)
        {
            RefreshWorkspaceFiles();
            InstallationStatusText.Text = _dashboard.Text.OperationCanceled;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            RefreshWorkspaceFiles();
            InstallationStatusText.Text = _dashboard.Text.WorkspaceOperationFailed(exception.Message);
        }
        finally
        {
            onFinished?.Invoke();
        }
    }

    private async Task OpenWorkspaceEntryAsync(WorkspaceEntryViewModel entry)
    {
        if (entry.IsDirectory)
        {
            _workspaceHistory.Push(_workspaceDirectory);
            _workspaceDirectory = entry.RelativePath;
            _workspacePageNumber = 1;
            RefreshWorkspaceFiles();
            return;
        }

        await OpenPortableFileAsync(
            Path.Combine(
                _workspaceFileManager.RootRelativePath,
                entry.RelativePath.Replace('/', Path.DirectorySeparatorChar)),
            _workspaceFileManager.RootRelativePath,
            PortableFileLaunchIntent.Open);
    }

    private async Task OpenPortableFileAsync(
        string relativeFilePath,
        string allowedRootRelativePath,
        PortableFileLaunchIntent intent,
        string? initialContent = null)
    {
        var result = await _fileLauncher.LaunchAsync(
            relativeFilePath,
            allowedRootRelativePath,
            intent,
            _dashboard.Text.CurrentLanguage,
            initialContent,
            _applicationLifetime.Token);
        InstallationStatusText.Text = result.Detail;
    }

    private string? PromptForWorkspaceName(string title, string prompt, string initialValue = "")
    {
        var dialog = new NamePromptDialog(
            this,
            title,
            prompt,
            _dashboard.Text.Confirm,
            _dashboard.Text.Cancel,
            _dashboard.Text.WorkspaceItemNameRequired,
            initialValue);
        return dialog.ShowDialog() == true ? dialog.ItemName : null;
    }

    private async void RefreshWorkspaceFiles()
    {
        _workspaceRefreshCancellation?.Cancel();
        _workspaceRefreshCancellation?.Dispose();
        _workspaceRefreshCancellation = CancellationTokenSource.CreateLinkedTokenSource(_applicationLifetime.Token);
        var cancellationToken = _workspaceRefreshCancellation.Token;
        try
        {
            var request = new WorkspacePageRequest(
                _workspaceDirectory,
                _workspacePageNumber,
                _workspacePageSize,
                _workspaceSortColumn,
                _workspaceSortDirection);
            var page = await Task.Run(() => _workspaceFileManager.ListPage(request), cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            _workspacePageNumber = page.PageNumber;
            _dashboard.SetWorkspacePage(page);
            WorkspacePathTextBox.Text = DisplayTerminalPath(_workspaceDirectory);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            _workspaceDirectory = string.Empty;
            _workspaceHistory.Clear();
            _workspacePageNumber = 1;
            _dashboard.SetWorkspacePage(new WorkspacePage([], 1, _workspacePageSize, 0));
            WorkspacePathTextBox.Text = $"{_projectContext.ActiveProject.Id}:/";
            InstallationStatusText.Text = _dashboard.Text.WorkspaceOperationFailed(exception.Message);
        }
    }


    private sealed record WorkspaceClipboardEntry(
        string ProjectId,
        IReadOnlyList<WorkspaceClipboardItem> Items,
        bool IsCut);

    private sealed record WorkspaceClipboardItem(string RelativePath);
}
