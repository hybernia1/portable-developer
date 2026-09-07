using System.Collections.ObjectModel;
using PortableDeveloper.App.Shell;
using PortableDeveloper.App.ViewModels;
using PortableDeveloper.Application.MariaDb;
using PortableDeveloper.Application.Php;
using PortableDeveloper.Application.Ports;
using PortableDeveloper.Application.Projects;
using PortableDeveloper.Application.Selenium;
using PortableDeveloper.Application.Settings;
using PortableDeveloper.Application.Workspace;

namespace PortableDeveloper.Tests;

public sealed class FeaturePageStateTests
{
    [Fact]
    public void Files_page_owns_workspace_entries_and_paging_state()
    {
        var text = new UiText(new InMemorySettingsStore());
        var page = new FilesPageViewModel(text);
        var entry = new WorkspaceEntry(
            "long-project-file-name.txt",
            "nested/long-project-file-name.txt",
            IsDirectory: false,
            SizeBytes: 128,
            LastWriteTime: new DateTime(2026, 9, 7, 12, 0, 0, DateTimeKind.Local),
            IsSafe: true,
            WorkspaceFileKind.Text);

        page.SetWorkspacePage(new WorkspacePage([entry], PageNumber: 2, PageSize: 25, TotalCount: 30));

        var projected = Assert.Single(page.WorkspaceEntries);
        Assert.Equal(entry.Name, projected.Name);
        Assert.Equal(2, page.WorkspacePageNumber);
        Assert.Equal(2, page.WorkspaceTotalPages);
        Assert.Equal(30, page.WorkspaceTotalCount);
        Assert.Equal(25, page.WorkspacePageSize);
        Assert.True(page.WorkspaceHasPreviousPage);
        Assert.False(page.WorkspaceHasNextPage);
        Assert.Equal(text.WorkspacePageSummary(26, 30, 30), page.WorkspacePageSummary);
        Assert.False(page.NoWorkspaceEntries);
    }

    [Fact]
    public void Scheduler_page_owns_task_and_history_collections()
    {
        var text = new UiText(new InMemorySettingsStore());
        var shell = CreateShell(text);
        var page = new SchedulerPageViewModel(text, shell);
        var task = new ScheduledTaskViewModel(
            "task-1",
            "Task",
            "PHP",
            "script.php",
            "Daily",
            "Next",
            "Last",
            "Enabled",
            IsRunning: false,
            IsEnabled: true);
        var run = new ScheduledTaskRunViewModel(
            "run-1",
            "Task",
            "Started",
            "1 s",
            "Manual",
            "Succeeded",
            "output",
            IsSuccess: true);

        Assert.True(page.NoScheduledTasks);
        Assert.True(page.NoScheduledTaskHistory);

        page.SetScheduledTasks([task], [run]);

        Assert.Same(task, Assert.Single(page.ScheduledTasks));
        Assert.Same(run, Assert.Single(page.ScheduledTaskHistory));
        Assert.False(page.NoScheduledTasks);
        Assert.False(page.NoScheduledTaskHistory);
        Assert.True(page.HasScheduledTaskHistory);

        page.HistoryFilterText = "succeed";
        Assert.Same(run, Assert.Single(page.ScheduledTaskHistory));

        page.HistoryFilterText = "missing";
        Assert.Empty(page.ScheduledTaskHistory);
        Assert.True(page.NoMatchingScheduledTaskHistory);
        Assert.False(page.NoScheduledTaskHistory);

        page.ClearHistoryFilter();
        Assert.Same(run, Assert.Single(page.ScheduledTaskHistory));
    }

    [Fact]
    public void Ports_page_owns_listener_projection_and_runtime_snapshot()
    {
        var text = new UiText(new InMemorySettingsStore());
        var shell = CreateShell(text);
        var page = new PortsPageViewModel(text, shell);
        var settings = new PortSettings(8080, 9070, 3307, 4445);

        page.SetRuntimeState(
            settings,
            apacheRunning: true,
            mariaDbRunning: false,
            seleniumRunning: false,
            settingsEnabled: false,
            text.PortSettingsRequireStoppedServices);
        page.SetTcpListeners([new TcpPortListenerInfo("127.0.0.1", 8080)]);
        page.SetPortSettings(settings);
        page.UpdateInputStatuses(page.TcpListeners
            .Select(listener => new TcpPortListenerInfo(listener.Address, listener.Port))
            .ToArray(), _ => true);

        var listener = Assert.Single(page.TcpListeners);
        Assert.Equal("127.0.0.1", listener.Address);
        Assert.Equal(8080, listener.Port);
        Assert.Equal(text.TcpListenerCount(1), page.TcpListenerCount);
        Assert.False(page.PortSettingsEnabled);
        Assert.Equal(text.PortSettingsRequireStoppedServices, page.PortSettingsAvailability);
        Assert.Equal(text.PortUsedByApplication, page.ApachePortStatus);
    }

    [Fact]
    public void Php_page_owns_extensions_and_runtime_snapshot()
    {
        var text = new UiText(new InMemorySettingsStore());
        var page = new PhpPageViewModel(text, CreateShell(text));
        var extension = new PhpExtensionViewModel("curl", isRequired: false, isAvailable: true, isEnabled: true);

        page.SetRuntimeState(settingsEnabled: true, "Save", "8.4.12");
        page.SetExtensions([extension]);
        page.SetSettings(PhpSettings.Default with { EnabledExtensions = ["curl"] });

        Assert.Same(extension, Assert.Single(page.PhpExtensions));
        Assert.True(extension.IsEnabled);
        Assert.True(page.PhpSettingsEnabled);
        Assert.Equal("Save", page.PhpSettingsActionLabel);
        Assert.Equal("8.4.12", page.PhpRuntimeVersion);
    }

    [Fact]
    public void Databases_page_owns_database_projection_and_runtime_snapshot()
    {
        var text = new UiText(new InMemorySettingsStore());
        var page = new DatabasesPageViewModel(text, CreateShell(text));
        var service = new ServiceCardViewModel("MariaDB", "Database", "Ready", "Running");

        page.SetRuntimeState(new DatabasesPageRuntimeState(
            service,
            MariaDbActionEnabled: true,
            "Stop",
            DatabaseActionsEnabled: true,
            MariaDbPort: 3307,
            "Configured",
            "Change password",
            PhpMyAdminInstalled: true,
            "http://127.0.0.1:8080/phpmyadmin/",
            PhpMyAdminActionEnabled: true,
            "Ready"));
        page.SetDatabases(
        [
            new DatabaseInfo("portable_dev", 2048),
            new DatabaseInfo("project_db", 1536)
        ]);

        var database = page.Databases.Single(item => item.Name == "project_db");
        Assert.Equal("project_db", database.Name);
        Assert.Equal(1536, database.ApproximateSizeBytes);
        Assert.True(database.CanDelete);
        Assert.False(page.Databases.Single(item => item.Name == "portable_dev").CanDelete);
        Assert.Same(service, page.MariaDbService);
        Assert.Equal(3307, page.MariaDbPort);
        Assert.True(page.DatabaseActionsEnabled);
        Assert.Equal(text.DatabaseCount(2), page.DatabaseCount);
    }

    [Fact]
    public void Selenium_page_owns_environment_profile_session_and_vault_projections()
    {
        var text = new UiText(new InMemorySettingsStore());
        var page = new SeleniumPageViewModel(
            text,
            CreateShell(text),
            new ObservableCollection<RuntimePackageViewModel>());
        var driver = new SeleniumDriverInfo("chrome", "ChromeDriver", "140", "drivers/chrome.exe", IsBundled: true);
        var environment = new SeleniumBrowserEnvironmentInfo(
            "chrome-140",
            "chrome",
            "Chrome for Testing",
            "140",
            "browsers/chrome.exe",
            IsManagedBrowser: true,
            SeleniumBrowserSource.Managed,
            driver,
            SeleniumBrowserEnvironmentState.Ready,
            "Ready");
        var profile = new SeleniumProfileInfo(
            "profile-1",
            "Master",
            SeleniumProfileBrowser.Chrome,
            "profiles/master",
            DateTimeOffset.UtcNow,
            2048,
            2,
            SeleniumProfileLayout.ChromiumUserData,
            "Default",
            "140",
            SeleniumProfileVerificationState.Verified,
            string.Empty);

        page.SetRuntimeState(new SeleniumPageRuntimeState(
            new ServiceCardViewModel("Selenium", "Grid", "Ready", "Running"),
            "http://127.0.0.1:4445/",
            SeleniumIsRunning: true,
            SeleniumActionEnabled: true,
            SeleniumSettingsEnabled: false,
            SeleniumProfileActionsEnabled: true,
            SeleniumSessionActionsEnabled: true,
            "Stop",
            MaximumSessions: 4));
        page.SetEnvironments([environment]);
        page.SetProfiles([profile]);
        page.SetSessions([new SeleniumSessionInfo(
            "session-1",
            "chrome",
            "140",
            "Windows 11",
            DateTimeOffset.UtcNow,
            TimeSpan.FromSeconds(90))]);
        page.SetCookieVaults([new SeleniumCookieVaultInfo(
            "vault-1",
            "Login",
            3,
            ["example.test"],
            DateTimeOffset.UtcNow,
            IsDamaged: false,
            string.Empty)]);

        Assert.Equal(1, page.ReadyEnvironmentCount);
        Assert.Single(page.SeleniumDrivers);
        Assert.Single(page.SeleniumBrowserChoices);
        Assert.Equal(text.VerifiedProfile, Assert.Single(page.SeleniumProfiles).Verification);
        Assert.Single(page.SeleniumSessions);
        Assert.Single(page.SeleniumCookieVaults);
        Assert.Equal(text.SeleniumSessionCount(1, 4), page.SeleniumSessionCount);
        Assert.False(page.NoSeleniumProfiles);
        Assert.False(page.NoSeleniumCookieVaults);
    }

    [Fact]
    public void Projects_page_owns_project_templates_registration_and_runtime_projection()
    {
        var text = new UiText(new InMemorySettingsStore());
        var shell = CreateShell(text);
        var page = new ProjectsPageViewModel(text, shell, Path.GetTempPath());
        var project = new PortableProject("project-1", "Project", "missing/project-1");
        var capabilities = new Dictionary<string, ProjectCapabilitySnapshot>(StringComparer.OrdinalIgnoreCase)
        {
            [project.Id] = new ProjectCapabilitySnapshot(
                project.Id,
                [new ProjectCapabilityEvidence(ProjectCapabilityKind.NodeJs, ["package.json"])])
        };

        page.SetRuntimeState(ProjectsPageRuntimeState.Default);
        page.SetProjects([project], project.Id, capabilities);
        page.SetRegistrableProjectDirectories([
            new ManagedProjectDirectoryCandidate("existing", "instances/default/projects/existing")
        ]);

        var projected = Assert.Single(page.Projects);
        Assert.Same(projected, page.SelectedProject);
        Assert.Equal(project.Id, shell.ActiveProjectId);
        Assert.Contains("Node.js", projected.RuntimeReadiness, StringComparison.Ordinal);
        Assert.NotEmpty(page.ProjectTemplates);
        Assert.Single(page.RegistrableProjectDirectories);
        Assert.False(page.NoRegistrableProjectDirectories);

        page.SetRuntimeState(ProjectsPageRuntimeState.Default with { NodeReady = true });

        Assert.Equal(text.SharedRuntimesReady, Assert.Single(page.Projects).RuntimeReadiness);
        Assert.Equal(project.Id, page.SelectedProject?.Id);
    }

    [Fact]
    public void Apache_page_owns_active_web_project_projection_and_runtime_snapshot()
    {
        var text = new UiText(new InMemorySettingsStore());
        var page = new ApachePageViewModel(text);
        var service = new ServiceCardViewModel("Apache", "Web server", "Ready", "Running");
        var project = new WebProject(
            "sample",
            "Sample",
            "instances/default/projects/sample",
            "public",
            IsEnabled: true);

        page.SetRuntimeState(new ApachePageRuntimeState(
            service,
            ApacheActionEnabled: true,
            "Stop",
            ApachePort: 8080));
        page.SetWebProjects([project], project.Id);

        Assert.Same(service, page.ApacheService);
        Assert.True(page.ApacheActionEnabled);
        Assert.Equal(8080, page.ApachePort);
        Assert.Equal(project.Name, page.ActiveWebProjectName);
        Assert.Equal(Path.Combine(project.ProjectRootRelativePath, project.WebRootRelativePath), page.ActiveDocumentRoot);
    }

    [Fact]
    public void Settings_page_owns_application_identity_and_port_snapshot()
    {
        var text = new UiText(new InMemorySettingsStore());
        var page = new SettingsPageViewModel(text, CreateShell(text), "1.30.0");
        var ports = new PortSettings(8081, 9001, 3308, 4446);

        page.SetPorts(ports);

        Assert.Equal("1.30.0", page.ApplicationVersion);
        Assert.Equal(ports.ApachePort, page.ApachePort);
        Assert.Equal(ports.PhpFastCgiPort, page.PhpFastCgiPort);
        Assert.Equal(ports.MariaDbPort, page.MariaDbPort);
        Assert.Equal(ports.SeleniumPort, page.SeleniumPort);
    }

    private static WorkspaceShellViewModel CreateShell(UiText text) => new(
        text,
        "test",
        new ObservableCollection<ProjectViewModel>(),
        new GlobalOperationViewModel());

    private sealed class InMemorySettingsStore : IApplicationSettingsStore
    {
        private ApplicationSettings _settings = ApplicationSettings.Default;

        public ApplicationSettings Load() => _settings;

        public void Save(ApplicationSettings settings) => _settings = settings;
    }
}
