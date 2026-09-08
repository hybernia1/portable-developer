namespace PortableDeveloper.Tests;

public sealed class TerminalAndGuidesPresentationTests
{
    [Fact]
    public void Terminal_is_one_bounded_workspace_with_state_and_safe_commands()
    {
        var appRoot = FindAppRoot();
        var view = File.ReadAllText(Path.Combine(appRoot, "Views", "TerminalPageView.xaml"));
        var viewCode = File.ReadAllText(Path.Combine(appRoot, "Views", "TerminalPageView.xaml.cs"));
        var routing = File.ReadAllText(Path.Combine(appRoot, "MainWindow.PageRouting.cs"));
        var handler = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Terminal.cs"));

        Assert.Contains("Style=\"{StaticResource PanelCardStyle}\"", view, StringComparison.Ordinal);
        Assert.Contains("<controls:SectionHeader", view, StringComparison.Ordinal);
        Assert.Contains("Metadata=\"{Binding StateText}\"", view, StringComparison.Ordinal);
        Assert.DoesNotContain("TerminalHelp", view, StringComparison.Ordinal);
        Assert.Contains("IconTerminal", view, StringComparison.Ordinal);
        Assert.Contains("IconStop", view, StringComparison.Ordinal);
        Assert.Contains("IconClear", view, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ConsoleTextBox\"", view, StringComparison.Ordinal);
        Assert.Contains("Click=\"StopButton_Click\"", view, StringComparison.Ordinal);
        Assert.Contains("Click=\"ClearButton_Click\"", view, StringComparison.Ordinal);
        Assert.Contains("ClearRequestedEvent", viewCode, StringComparison.Ordinal);
        Assert.Contains("TerminalPageView.ClearRequestedEvent", routing, StringComparison.Ordinal);
        Assert.Contains("TerminalPage_ClearRequested", handler, StringComparison.Ordinal);
        Assert.DoesNotContain("<DataGrid", view, StringComparison.Ordinal);
    }

    [Fact]
    public void Guides_use_filter_master_detail_and_coherent_empty_states()
    {
        var view = File.ReadAllText(Path.Combine(FindAppRoot(), "Views", "GuidesPageView.xaml"));

        Assert.Contains("<controls:SectionHeader", view, StringComparison.Ordinal);
        Assert.Contains("IconSearch", view, StringComparison.Ordinal);
        Assert.Contains("IconGuide", view, StringComparison.Ordinal);
        Assert.Contains("Metadata=\"{Binding Articles.Count}\"", view, StringComparison.Ordinal);
        Assert.Contains("IsEnabled=\"{Binding HasFilter}\"", view, StringComparison.Ordinal);
        Assert.Contains("Click=\"ClearGuideFilter_Click\"", view, StringComparison.Ordinal);
        Assert.Contains("ItemContainerStyle=\"{StaticResource GuideArticleItemStyle}\"", view, StringComparison.Ordinal);
        Assert.Contains("CompactBreakpoint=\"860\"", view, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(view, "x:Name=\"DocumentViewer\""));
        Assert.DoesNotContain("AccentTextFillColorPrimaryBrush", view, StringComparison.Ordinal);
        Assert.DoesNotContain("<DataGrid", view, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string text, string value)
    {
        var count = 0;
        var offset = 0;
        while ((offset = text.IndexOf(value, offset, StringComparison.Ordinal)) >= 0)
        {
            count++;
            offset += value.Length;
        }

        return count;
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
