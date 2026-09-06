namespace PortableDeveloper.Tests;

public sealed class WorkspaceFileManagerPresentationTests
{
    [Fact]
    public void File_manager_uses_type_aware_icons_with_targeted_styled_context_actions()
    {
        var repositoryRoot = FindRepositoryRoot();
        var window = File.ReadAllText(Path.Combine(repositoryRoot, "src", "PortableDeveloper.App", "MainWindow.xaml"));
        var filesStart = window.IndexOf("<!-- Files -->", StringComparison.Ordinal);
        var filesEnd = window.IndexOf("<!-- Guides -->", filesStart, StringComparison.Ordinal);
        var fileManager = window[filesStart..filesEnd];

        Assert.Contains("x:Name=\"WorkspaceEntriesListBox\"", fileManager, StringComparison.Ordinal);
        Assert.Contains("SelectionMode=\"Extended\"", fileManager, StringComparison.Ordinal);
        Assert.Contains("PreviewKeyDown=\"WorkspaceEntriesListBox_PreviewKeyDown\"", fileManager, StringComparison.Ordinal);
        Assert.Contains("WorkspaceContextNewFileMenuItem", fileManager, StringComparison.Ordinal);
        Assert.Contains("WorkspaceContextNewFolderMenuItem", fileManager, StringComparison.Ordinal);
        Assert.Contains("WorkspaceBackgroundContextMenu_Opened", fileManager, StringComparison.Ordinal);
        Assert.Contains("WorkspaceItemContextMenu_Opened", fileManager, StringComparison.Ordinal);
        Assert.Contains("InputGestureText=\"Ctrl+C\"", fileManager, StringComparison.Ordinal);
        Assert.Contains("InputGestureText=\"Ctrl+X\"", fileManager, StringComparison.Ordinal);
        Assert.Contains("InputGestureText=\"Ctrl+V\"", fileManager, StringComparison.Ordinal);
        Assert.Contains("Value=\"{StaticResource IconFile}\"", fileManager, StringComparison.Ordinal);
        Assert.Contains("Value=\"{StaticResource IconFolder}\"", fileManager, StringComparison.Ordinal);
        Assert.Contains("FileKind}\" Value=\"Html\"", fileManager, StringComparison.Ordinal);
        Assert.Contains("Value=\"{StaticResource IconHtml}\"", fileManager, StringComparison.Ordinal);
        Assert.Contains("FileKind}\" Value=\"StyleSheet\"", fileManager, StringComparison.Ordinal);
        Assert.Contains("FileKind}\" Value=\"Php\"", fileManager, StringComparison.Ordinal);
        Assert.Contains("FileKind}\" Value=\"Archive\"", fileManager, StringComparison.Ordinal);
        Assert.Contains("Value=\"{StaticResource IconArchive}\"", fileManager, StringComparison.Ordinal);
        Assert.Contains("FileKind}\" Value=\"Spreadsheet\"", fileManager, StringComparison.Ordinal);
        Assert.Contains("Value=\"{StaticResource IconSpreadsheet}\"", fileManager, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AppContextMenuStyle}\"", fileManager, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource AppContextMenuItemStyle}\"", fileManager, StringComparison.Ordinal);
        Assert.Contains("PreviewMouseLeftButtonUp=\"WorkspaceName_PreviewMouseLeftButtonUp\"", fileManager, StringComparison.Ordinal);
        Assert.Contains("PreviewMouseRightButtonDown=\"WorkspaceName_PreviewMouseRightButtonDown\"", fileManager, StringComparison.Ordinal);
        Assert.Contains("PreviewMouseMove=\"WorkspaceEntriesListBox_PreviewMouseMove\"", fileManager, StringComparison.Ordinal);
        Assert.Contains("PreviewDragOver=\"WorkspaceFileList_DragOver\"", fileManager, StringComparison.Ordinal);
        Assert.Contains("Drop=\"WorkspaceFileList_Drop\"", fileManager, StringComparison.Ordinal);
        Assert.Contains("FileConflictDialog.Show", File.ReadAllText(Path.Combine(repositoryRoot, "src", "PortableDeveloper.App", "MainWindow.FileManager.cs")), StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding EditName, UpdateSourceTrigger=PropertyChanged}\"", fileManager, StringComparison.Ordinal);
        Assert.DoesNotContain("OpenWorkspaceItem_Click", fileManager, StringComparison.Ordinal);
        Assert.DoesNotContain("RenameWorkspaceItem_Click", fileManager, StringComparison.Ordinal);
        Assert.DoesNotContain("DeleteWorkspaceItem_Click", fileManager, StringComparison.Ordinal);
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
