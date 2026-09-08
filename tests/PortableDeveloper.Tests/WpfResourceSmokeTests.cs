using System.Collections.ObjectModel;
using System.Runtime.ExceptionServices;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using PortableDeveloper.App.Controls;
using PortableDeveloper.App.Shell;
using PortableDeveloper.App.ViewModels;
using PortableDeveloper.Application.Selenium;
using PortableDeveloper.App.Views;
using PortableDeveloper.Application.Settings;

namespace PortableDeveloper.Tests;

public sealed class WpfResourceSmokeTests
{
    private static void AssertPackageManagerForwardsVisibleInputForEveryManager()
    {
        (PackageManagerKind Manager, PortableDeveloper.Application.ProjectTools.PortableToolKind Tool, string Name, string Constraint)[] cases =
        [
            (PackageManagerKind.Python, PortableDeveloper.Application.ProjectTools.PortableToolKind.Python, "selenium", ">=4"),
            (PackageManagerKind.Node, PortableDeveloper.Application.ProjectTools.PortableToolKind.Node, "@types/node", "^24"),
            (PackageManagerKind.Composer, PortableDeveloper.Application.ProjectTools.PortableToolKind.Composer, "vendor/package", "^1")
        ];

        foreach (var testCase in cases)
        {
            var page = new PackageManagerPageViewModel(testCase.Manager, "project");
            page.SetRuntime(new PortableDeveloper.Application.ProjectTools.PortableToolRuntimeInfo(
                testCase.Tool,
                true,
                "current",
                "runtime.exe",
                string.Empty));
            var view = new PackageManagerView { Page = page };
            view.Measure(new Size(1200, 800));
            view.Arrange(new Rect(0, 0, 1200, 800));
            view.UpdateLayout();

            var inputs = FindVisualChildren<TextBox>(view).Take(2).ToArray();
            Assert.Equal(2, inputs.Length);
            inputs[0].Text = testCase.Name;
            inputs[1].Text = testCase.Constraint;
            inputs[0].GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
            inputs[1].GetBindingExpression(TextBox.TextProperty)?.UpdateSource();

            PackageInstallRequestedEventArgs? request = null;
            view.InstallRequested += (_, args) => request = args;
            typeof(PackageManagerView)
                .GetMethod("InstallPackage_Click", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(view, [view, new RoutedEventArgs()]);

            Assert.NotNull(request);
            Assert.Equal(testCase.Name, request.PackageName);
            Assert.Equal(testCase.Constraint, request.VersionConstraint);
        }
    }

    private static void AssertSchedulerMaterializesPopulatedTask()
    {
        var text = new UiText(new InMemorySettingsStore());
        var shell = new WorkspaceShellViewModel(
            text,
            "test",
            new ObservableCollection<ProjectViewModel>(),
            new GlobalOperationViewModel());
        shell.SetAvailablePages([NavigationPage.Projects, NavigationPage.Scheduler]);
        shell.SelectedPage = NavigationPage.Scheduler;
        var page = new SchedulerPageViewModel(text, shell);
        page.SetScheduledTasks(
        [
            new ScheduledTaskViewModel(
                "task-id",
                "Test task",
                "Python",
                "python",
                "test.py",
                "Every 60 min",
                "Soon",
                "Earlier",
                "Success",
                true,
                "Enabled",
                false,
                true)
        ],
        [
            new ScheduledTaskRunViewModel(
                "run-id",
                "Test task",
                "Python",
                "python",
                "test.py",
                "Earlier",
                "1.0 s",
                "Manual",
                "Success",
                "ok",
                true)
        ]);

        var view = new SchedulerPageView { DataContext = page };
        view.Measure(new Size(1200, 800));
        view.Arrange(new Rect(0, 0, 1200, 800));
        view.UpdateLayout();

        Assert.Contains(FindVisualChildren<TextBlock>(view), item => item.Text == "Test task");

        shell.SelectedSection = NavigationSection.SchedulerHistory;
        view.UpdateLayout();

        Assert.Contains(FindVisualChildren<TextBlock>(view), item => item.Text == "test.py");
    }

    private static void AssertSeleniumMaterializesPopulatedProfile()
    {
        var text = new UiText(new InMemorySettingsStore());
        var shell = new WorkspaceShellViewModel(
            text,
            "test",
            new ObservableCollection<ProjectViewModel>(),
            new GlobalOperationViewModel());
        shell.SetAvailablePages([NavigationPage.Projects, NavigationPage.Selenium]);
        shell.SelectedPage = NavigationPage.Selenium;
        var page = new SeleniumPageViewModel(text, shell, []);
        page.SetProfiles(
        [
            new SeleniumProfileInfo(
                "profile-id",
                "Signed-in Chrome",
                SeleniumProfileBrowser.Chrome,
                "profiles/signed-in-chrome",
                DateTimeOffset.UtcNow,
                4096,
                1,
                SeleniumProfileLayout.ChromiumUserData,
                "Default",
                "140",
                SeleniumProfileVerificationState.Verified,
                string.Empty)
        ]);
        shell.SelectedSection = NavigationSection.SeleniumBrowserProfiles;

        var view = new SeleniumPageView { DataContext = page };
        view.Measure(new Size(1200, 800));
        view.Arrange(new Rect(0, 0, 1200, 800));
        view.UpdateLayout();

        Assert.Contains(FindVisualChildren<TextBlock>(view), item => item.Text == "Signed-in Chrome");
    }

    [Fact]
    public void Application_resources_and_shell_controls_load_on_an_sta_thread()
    {
        RunOnStaThread(() =>
        {
            var application = new PortableDeveloper.App.App();
            try
            {
                application.InitializeComponent();
                AssertPageHostSurvivesRuntimeThemeChanges(application);
                AssertPackageManagerForwardsVisibleInputForEveryManager();
                AssertSchedulerMaterializesPopulatedTask();
                AssertSeleniumMaterializesPopulatedProfile();
                AssertCatalogCreationDialogsMaterialize();

                Assert.NotNull(application.TryFindResource("PanelCardStyle"));
                Assert.NotNull(application.TryFindResource("PageHostContentControlStyle"));
                Assert.NotNull(application.TryFindResource("AppIconBaseStyle"));
                Assert.NotNull(application.TryFindResource("PageScrollViewerStyle"));
                Assert.NotNull(application.TryFindResource("TrimmingGroupBoxStyle"));
                Assert.NotNull(application.TryFindResource("CatalogRowStyle"));
                Assert.NotNull(application.TryFindResource("InlineInfoPanelStyle"));
                Assert.NotNull(application.TryFindResource("SidebarNavigationItemStyle"));
                Assert.NotNull(application.TryFindResource("SectionNavigationItemStyle"));
                Assert.NotNull(application.TryFindResource("MasterListItemStyle"));
                Assert.NotNull(application.TryFindResource("FileListItemStyle"));
                Assert.NotNull(application.TryFindResource("DetailsHeaderButtonStyle"));
                Assert.NotNull(application.TryFindResource("DialogFieldLabelStyle"));
                Assert.NotNull(application.TryFindResource("DialogHelpTextStyle"));
                Assert.NotNull(application.TryFindResource("DialogValidationTextStyle"));
                Assert.NotNull(application.TryFindResource("DialogFooterStyle"));
                Assert.NotNull(application.TryFindResource("DialogPrimaryButtonStyle"));
                Assert.NotNull(application.TryFindResource("DialogSecondaryButtonStyle"));
                Assert.NotNull(application.TryFindResource("RuntimePrimaryButtonStyle"));
                Assert.NotNull(application.TryFindResource("RuntimeSecondaryButtonStyle"));
                Assert.NotNull(application.TryFindResource("FormFieldLabelStyle"));
                Assert.NotNull(application.TryFindResource("FormHelpTextStyle"));
                Assert.NotNull(application.TryFindResource("FormActionFooterStyle"));
                Assert.NotNull(application.TryFindResource("FormPrimaryButtonStyle"));
                Assert.NotNull(application.TryFindResource("FormSecondaryButtonStyle"));
                Assert.NotNull(application.TryFindResource("SectionActionButtonStyle"));
                Assert.NotNull(application.TryFindResource("LayoutModeVisibilityConverter"));
                Assert.NotNull(application.TryFindResource("IconInstall"));
                Assert.NotNull(application.TryFindResource("IconDelete"));
                Assert.NotNull(application.TryFindResource("IconProject"));
                Assert.NotNull(application.TryFindResource("IconManage"));
                Assert.NotNull(application.TryFindResource("IconGlobe"));
                Assert.NotNull(application.TryFindResource("IconEdit"));
                Assert.NotNull(application.TryFindResource("IconCopy"));
                Assert.NotNull(application.TryFindResource("IconRemove"));
                Assert.NotNull(application.TryFindResource("IconSave"));
                Assert.NotNull(application.TryFindResource("IconInfo"));
                Assert.NotNull(application.TryFindResource("IconClose"));
                Assert.NotNull(application.TryFindResource("IconPlay"));
                Assert.NotNull(application.TryFindResource("IconStop"));
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
                Assert.NotNull(new AppIcon());
                Assert.NotNull(new DialogHeader());
                Assert.NotNull(new RuntimeHeader());
                Assert.NotNull(new FormSection());
                Assert.NotNull(new SectionHeader());
                Assert.NotNull(new TransientNotificationHost());
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
                    new SectionHeader(),
                    new ApachePageView(),
                    new PackageManagerView(),
                    new PortsPageView(),
                    new SettingsPageView(),
                    new PhpPageView(),
                    new DatabasesPageView(),
                    new ProjectsPageView(),
                    new SchedulerPageView(),
                    new GuidesPageView(),
                    new FilesPageView(),
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

    private static void AssertCatalogCreationDialogsMaterialize()
    {
        var owner = new Window();
        try
        {
            var profileDialog = new PortableDeveloper.App.SeleniumProfileDialog(
                owner,
                "Add profile",
                "Profile name",
                "Managed browser",
                "Add profile",
                "Cancel",
                "Enter a profile name.",
                "Select a browser.",
                [new SeleniumBrowserChoiceViewModel("firefox", "Mozilla Firefox", "154.0")]);
            var vaultDialog = new PortableDeveloper.App.CookieVaultImportDialog(
                owner,
                "Add vault",
                "Vault name",
                "Cookie JSON file",
                "Choose file…",
                "No file selected.",
                "Add vault",
                "Cancel",
                "Enter a vault name.",
                "Choose a file.");

            Assert.NotNull(profileDialog.Content);
            Assert.NotNull(vaultDialog.Content);
            profileDialog.Close();
            vaultDialog.Close();
        }
        finally
        {
            owner.Close();
        }
    }

    private static void AssertPageHostSurvivesRuntimeThemeChanges(PortableDeveloper.App.App application)
    {
        var text = new UiText(new InMemorySettingsStore());
        var shell = new WorkspaceShellViewModel(
            text,
            "test",
            new ObservableCollection<ProjectViewModel>(),
            new GlobalOperationViewModel());
        shell.SetAvailablePages([NavigationPage.Projects, NavigationPage.Guides]);
        shell.SelectedPage = NavigationPage.Guides;
        var page = new GuidesPageViewModel(text);
        page.Refresh(8080, 3307, 4444, resetSearch: true);
        var host = new ContentControl
        {
            Style = (Style)application.FindResource("PageHostContentControlStyle"),
            Content = page
        };
        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(250) });
        root.ColumnDefinitions.Add(new ColumnDefinition());
        var sidebar = new AppSidebar { DataContext = shell };
        var workspace = new Grid();
        workspace.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        workspace.RowDefinitions.Add(new RowDefinition());
        var header = new WorkspaceHeader { DataContext = shell };
        Grid.SetRow(host, 1);
        workspace.Children.Add(header);
        workspace.Children.Add(host);
        Grid.SetColumn(workspace, 1);
        root.Children.Add(sidebar);
        root.Children.Add(workspace);
        var primaryIcon = new AppIcon
        {
            Data = (Geometry)application.FindResource("IconPlay")
        };
        var primaryButton = new Button
        {
            Style = (Style)application.FindResource("FormPrimaryButtonStyle"),
            Content = primaryIcon
        };
        Grid.SetColumn(primaryButton, 1);
        root.Children.Add(primaryButton);
        var window = new Window
        {
            Width = 1200,
            Height = 800,
            Left = -10000,
            Top = -10000,
            ShowActivated = false,
            ShowInTaskbar = false,
            Content = root
        };
        try
        {
            window.Show();
            host.UpdateLayout();

            var themeManager = typeof(System.Windows.Application).Assembly.GetType("System.Windows.ThemeManager")!;
            var currentState = themeManager.GetField(
                "s_currentFluentThemeState",
                BindingFlags.Static | BindingFlags.NonPublic)!;
            currentState.SetValue(null, Activator.CreateInstance(currentState.FieldType));
            themeManager.GetMethod(
                    "OnSystemThemeChanged",
                    BindingFlags.Static | BindingFlags.NonPublic)!
                .Invoke(null, null);
            host.UpdateLayout();

#pragma warning disable WPF0001
            application.ThemeMode = ThemeMode.Light;
            host.UpdateLayout();
            application.ThemeMode = ThemeMode.Dark;
            host.UpdateLayout();
            application.ThemeMode = ThemeMode.System;
#pragma warning restore WPF0001
            host.UpdateLayout();

            Assert.IsType<GuidesPageViewModel>(host.Content);
            Assert.Equal(DependencyProperty.UnsetValue, primaryIcon.ReadLocalValue(Control.ForegroundProperty));
            Assert.Equal(primaryButton.Foreground, primaryIcon.Foreground);
        }
        finally
        {
            window.Close();
        }
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

            var locallyAdaptivePanel = new AdaptiveSplitPanel
            {
                Gap = 18,
                SecondaryWeight = 0,
                CompactBreakpoint = 520
            };
            var localContent = new Border { Height = 40 };
            var localActions = new Border { Width = 120, Height = 34 };
            locallyAdaptivePanel.Children.Add(localContent);
            locallyAdaptivePanel.Children.Add(localActions);
            WorkspaceLayout.SetMode(locallyAdaptivePanel, WorkspaceLayoutMode.Wide);
            locallyAdaptivePanel.Measure(new Size(480, double.PositiveInfinity));
            locallyAdaptivePanel.Arrange(new Rect(0, 0, 480, locallyAdaptivePanel.DesiredSize.Height));

            Assert.Equal(58, VisualTreeHelper.GetOffset(localActions).Y);
            Assert.Equal(480, localContent.RenderSize.Width);

            locallyAdaptivePanel.Measure(new Size(600, double.PositiveInfinity));
            locallyAdaptivePanel.Arrange(new Rect(0, 0, 600, locallyAdaptivePanel.DesiredSize.Height));

            Assert.Equal(480, VisualTreeHelper.GetOffset(localActions).X);
            Assert.Equal(462, localContent.RenderSize.Width);

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

        Assert.True(thread.Join(TimeSpan.FromSeconds(30)), "The WPF smoke-test thread did not finish.");
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

    private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
        {
            var child = VisualTreeHelper.GetChild(parent, index);
            if (child is T match)
            {
                yield return match;
            }

            foreach (var descendant in FindVisualChildren<T>(child))
            {
                yield return descendant;
            }
        }
    }

    private sealed class InMemorySettingsStore : IApplicationSettingsStore
    {
        private ApplicationSettings _settings = ApplicationSettings.Default;

        public ApplicationSettings Load() => _settings;

        public void Save(ApplicationSettings settings) => _settings = settings;
    }
}
