using System.Runtime.ExceptionServices;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PortableDeveloper.App.Controls;
using PortableDeveloper.App.Shell;
using PortableDeveloper.App.ViewModels;
using PortableDeveloper.App.Views;

namespace PortableDeveloper.Tests;

public sealed class WpfResourceSmokeTests
{
    [Fact]
    public void Application_resources_and_shell_controls_load_on_an_sta_thread()
    {
        RunOnStaThread(() =>
        {
            var application = new PortableDeveloper.App.App();
            try
            {
                application.InitializeComponent();

                Assert.NotNull(application.TryFindResource("PanelCardStyle"));
                Assert.NotNull(application.TryFindResource("PageScrollViewerStyle"));
                Assert.NotNull(application.TryFindResource("TrimmingGroupBoxStyle"));
                Assert.NotNull(application.TryFindResource(new DataTemplateKey(typeof(ModulesPageViewModel))));
                Assert.NotNull(application.TryFindResource(new DataTemplateKey(typeof(ApachePageViewModel))));
                Assert.NotNull(application.TryFindResource(new DataTemplateKey(typeof(PortsPageViewModel))));
                Assert.NotNull(application.TryFindResource(new DataTemplateKey(typeof(SettingsPageViewModel))));
                Assert.NotNull(application.TryFindResource(new DataTemplateKey(typeof(PhpPageViewModel))));
                Assert.NotNull(application.TryFindResource(new DataTemplateKey(typeof(DatabasesPageViewModel))));
                Assert.NotNull(application.TryFindResource(new DataTemplateKey(typeof(ProjectsPageViewModel))));
                Assert.NotNull(application.TryFindResource(new DataTemplateKey(typeof(SchedulerPageViewModel))));
                Assert.NotNull(application.TryFindResource(new DataTemplateKey(typeof(GuidesPageViewModel))));
                Assert.NotNull(application.TryFindResource(new DataTemplateKey(typeof(TerminalPageViewModel))));
                Assert.NotNull(application.TryFindResource(new DataTemplateKey(typeof(FilesPageViewModel))));
                Assert.NotNull(application.TryFindResource(new DataTemplateKey(typeof(SeleniumPageViewModel))));
                Assert.NotNull(application.TryFindResource(new DataTemplateKey(typeof(PackageManagerHostViewModel))));
                Assert.NotNull(new AppSidebar());
                Assert.NotNull(new ApachePageView());
                Assert.NotNull(new ModulesPageView());
                Assert.NotNull(new PackageManagerView());
                Assert.NotNull(new PortsPageView());
                Assert.NotNull(new SettingsPageView());
                Assert.NotNull(new PhpPageView());
                Assert.NotNull(new DatabasesPageView());
                Assert.NotNull(new ProjectsPageView());
                Assert.NotNull(new SchedulerPageView());
                Assert.NotNull(new GuidesPageView());
                Assert.NotNull(new TerminalPageView());
                Assert.NotNull(new FilesPageView());
                Assert.NotNull(new SeleniumPageView());
                Assert.NotNull(new WorkspaceHeader());

                FrameworkElement[] responsiveSurfaces =
                [
                    new WorkspaceHeader(),
                    new ApachePageView(),
                    new PackageManagerView(),
                    new PortsPageView(),
                    new SettingsPageView(),
                    new PhpPageView(),
                    new DatabasesPageView(),
                    new ProjectsPageView(),
                    new SchedulerPageView(),
                    new GuidesPageView(),
                    new SeleniumPageView()
                ];
                foreach (var surface in responsiveSurfaces)
                {
                    AssertFitsCompactWorkspace(surface);
                }
            }
            finally
            {
                application.Shutdown();
            }
        });
    }

    [Fact]
    public void Adaptive_split_panel_stacks_or_splits_the_same_children_from_the_inherited_mode()
    {
        RunOnStaThread(() =>
        {
            var panel = new AdaptiveSplitPanel { Gap = 18, PrimaryWeight = 1, SecondaryWeight = 1 };
            var first = new Border { Height = 100 };
            var second = new Border { Height = 80 };
            panel.Children.Add(first);
            panel.Children.Add(second);

            WorkspaceLayout.SetMode(panel, WorkspaceLayoutMode.Compact);
            panel.Measure(new Size(600, double.PositiveInfinity));
            panel.Arrange(new Rect(0, 0, 600, panel.DesiredSize.Height));

            Assert.Equal(198, panel.DesiredSize.Height);
            Assert.Equal(118, VisualTreeHelper.GetOffset(second).Y);
            Assert.Equal(600, first.RenderSize.Width);

            WorkspaceLayout.SetMode(panel, WorkspaceLayoutMode.Wide);
            panel.Measure(new Size(600, double.PositiveInfinity));
            panel.Arrange(new Rect(0, 0, 600, panel.DesiredSize.Height));

            Assert.Equal(309, VisualTreeHelper.GetOffset(second).X);
            Assert.Equal(291, first.RenderSize.Width);

            var autoSizedPanel = new AdaptiveSplitPanel { Gap = 18, SecondaryWeight = 0 };
            var flexibleContent = new Border();
            var fixedActions = new Border { Width = 120 };
            autoSizedPanel.Children.Add(flexibleContent);
            autoSizedPanel.Children.Add(fixedActions);
            WorkspaceLayout.SetMode(autoSizedPanel, WorkspaceLayoutMode.Wide);
            autoSizedPanel.Measure(new Size(600, double.PositiveInfinity));
            autoSizedPanel.Arrange(new Rect(0, 0, 600, autoSizedPanel.DesiredSize.Height));

            Assert.Equal(462, flexibleContent.RenderSize.Width);
            Assert.Equal(480, VisualTreeHelper.GetOffset(fixedActions).X);

            var fillPanel = new AdaptiveSplitPanel
            {
                Gap = 12,
                PrimaryWeight = 1,
                SecondaryWeight = 1,
                CompactFillAvailableHeight = true
            };
            var upper = new Border();
            var lower = new Border();
            fillPanel.Children.Add(upper);
            fillPanel.Children.Add(lower);
            WorkspaceLayout.SetMode(fillPanel, WorkspaceLayoutMode.Compact);
            fillPanel.Measure(new Size(600, 500));
            fillPanel.Arrange(new Rect(0, 0, 600, 500));

            Assert.Equal(244, upper.RenderSize.Height);
            Assert.Equal(256, VisualTreeHelper.GetOffset(lower).Y);
        });
    }

    private static void RunOnStaThread(Action action)
    {
        ExceptionDispatchInfo? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();

        Assert.True(thread.Join(TimeSpan.FromSeconds(15)), "The WPF smoke-test thread did not finish.");
        failure?.Throw();
    }

    private static void AssertFitsCompactWorkspace(FrameworkElement surface)
    {
        const double minimumWorkspaceWidth = 634d;
        WorkspaceLayout.SetMode(surface, WorkspaceLayoutMode.Compact);
        surface.Measure(new Size(minimumWorkspaceWidth, 560d));
        surface.Arrange(new Rect(0d, 0d, minimumWorkspaceWidth, 560d));

        Assert.True(
            surface.DesiredSize.Width <= minimumWorkspaceWidth,
            $"{surface.GetType().Name} requested {surface.DesiredSize.Width} DIP in a {minimumWorkspaceWidth} DIP workspace.");
    }
}
