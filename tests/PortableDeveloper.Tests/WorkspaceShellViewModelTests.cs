using System.Collections.ObjectModel;
using PortableDeveloper.App.Shell;
using PortableDeveloper.App.ViewModels;
using PortableDeveloper.Application.Settings;

namespace PortableDeveloper.Tests;

public sealed class WorkspaceShellViewModelTests
{
    [Fact]
    public void Navigation_items_receive_presentation_ready_service_status()
    {
        var shell = CreateShell();
        shell.SetServiceStatus(NavigationPage.Apache, isRunning: false);
        shell.SetServiceStatus(NavigationPage.Databases, isRunning: true);
        shell.SetServiceStatus(NavigationPage.Selenium, isRunning: false);
        shell.SetAvailablePages(
        [
            NavigationPage.Projects,
            NavigationPage.Apache,
            NavigationPage.Databases,
            NavigationPage.Selenium,
            NavigationPage.Composer
        ]);

        var apache = shell.NavigationItems.Single(item => item.Page == NavigationPage.Apache);
        var database = shell.NavigationItems.Single(item => item.Page == NavigationPage.Databases);
        var composer = shell.NavigationItems.Single(item => item.Page == NavigationPage.Composer);

        Assert.True(apache.HasStatus);
        Assert.False(apache.IsRunning);
        Assert.Equal("Stopped", apache.StatusText);
        Assert.True(database.HasStatus);
        Assert.True(database.IsRunning);
        Assert.Equal("Running", database.StatusText);
        Assert.False(composer.HasStatus);
        Assert.Equal(NavigationIconKind.Package, composer.IconKind);

        shell.SetServiceStatus(NavigationPage.Apache, isRunning: true);
        Assert.True(apache.IsRunning);
        Assert.Equal("Running", apache.StatusText);
    }

    [Fact]
    public void Shell_owns_page_and_section_transitions()
    {
        var shell = CreateShell();
        shell.SetAvailablePages([NavigationPage.Projects, NavigationPage.Selenium]);

        shell.SelectedPage = NavigationPage.Selenium;
        Assert.Equal(NavigationSection.SeleniumSettings, shell.SelectedSection);

        shell.SelectedSection = NavigationSection.SeleniumCookieVaults;
        shell.SetAvailablePages([NavigationPage.Projects, NavigationPage.Selenium]);

        Assert.Equal(NavigationPage.Selenium, shell.SelectedPage);
        Assert.Equal(NavigationSection.SeleniumCookieVaults, shell.SelectedSection);
        Assert.Equal(
            shell.Text.NavigationSectionLabel(NavigationSection.SeleniumCookieVaults),
            shell.NavigationSections.Single(item => item.Section == shell.SelectedSection).Label);
    }

    [Fact]
    public void Operation_coordinator_exposes_narrow_shell_availability()
    {
        var operation = new GlobalOperationViewModel();
        var shell = CreateShell(operation);
        var changes = new List<string?>();
        shell.PropertyChanged += (_, args) => changes.Add(args.PropertyName);

        operation.Begin();

        Assert.False(shell.CanNavigate);
        Assert.False(shell.CanChangeProject);
        Assert.Contains(nameof(WorkspaceShellViewModel.CanNavigate), changes);
        Assert.Contains(nameof(WorkspaceShellViewModel.CanChangeProject), changes);

        operation.End();
        Assert.True(shell.CanNavigate);
        Assert.True(shell.CanChangeProject);
    }

    [Fact]
    public void Active_project_identity_is_shell_owned()
    {
        var shell = CreateShell();

        shell.SetActiveProjectId("second-project");

        Assert.Equal("second-project", shell.ActiveProjectId);
    }

    [Fact]
    public void Active_project_identity_is_reapplied_after_the_project_collection_is_rebuilt()
    {
        var shell = CreateShell();
        var changes = new List<string?>();
        shell.PropertyChanged += (_, args) => changes.Add(args.PropertyName);

        shell.SetActiveProjectId("default");

        Assert.Contains(nameof(WorkspaceShellViewModel.ActiveProjectId), changes);
    }

    [Fact]
    public void Project_context_feedback_is_owned_beside_the_shell_selector()
    {
        var shell = CreateShell();

        shell.SetProjectContextStatus("Project unavailable");

        Assert.Equal("Project unavailable", shell.ProjectContextStatus);
    }

    [Theory]
    [InlineData(759, WorkspaceLayoutMode.Compact)]
    [InlineData(760, WorkspaceLayoutMode.Wide)]
    public void Shell_derives_one_layout_mode_from_the_workspace_width(
        double width,
        WorkspaceLayoutMode expected)
    {
        var shell = CreateShell();

        shell.SetWorkspaceWidth(width);

        Assert.Equal(expected, shell.LayoutMode);
    }

    private static WorkspaceShellViewModel CreateShell(GlobalOperationViewModel? operation = null)
    {
        var settings = new MemorySettingsStore(
            ApplicationSettings.Default with { Language = ApplicationLanguage.English });
        return new WorkspaceShellViewModel(
            new UiText(settings),
            "1.30.0",
            new ObservableCollection<ProjectViewModel>(),
            operation ?? new GlobalOperationViewModel());
    }

    private sealed class MemorySettingsStore(ApplicationSettings settings) : IApplicationSettingsStore
    {
        private ApplicationSettings _settings = settings;

        public ApplicationSettings Load() => _settings;

        public void Save(ApplicationSettings settings) => _settings = settings;
    }
}
