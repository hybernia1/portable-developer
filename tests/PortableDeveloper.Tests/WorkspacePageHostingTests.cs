using System.Collections.ObjectModel;
using PortableDeveloper.App.ViewModels;
using PortableDeveloper.App.Views;
using PortableDeveloper.Application.Packages;
using PortableDeveloper.Application.Settings;

namespace PortableDeveloper.Tests;

public sealed class WorkspacePageHostingTests
{
    [Fact]
    public void Extracted_pages_use_one_typed_page_host_without_legacy_visibility_roots()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var app = File.ReadAllText(Path.Combine(appRoot, "App.xaml"));
        var window = File.ReadAllText(Path.Combine(appRoot, "MainWindow.xaml"));
        var pageRouting = File.ReadAllText(Path.Combine(appRoot, "MainWindow.PageRouting.cs"));
        var dashboard = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "DashboardViewModel.cs"));
        var modulesView = File.ReadAllText(Path.Combine(appRoot, "Views", "ModulesPageView.xaml"));
        var apacheView = File.ReadAllText(Path.Combine(appRoot, "Views", "ApachePageView.xaml"));
        var portsView = File.ReadAllText(Path.Combine(appRoot, "Views", "PortsPageView.xaml"));
        var settingsView = File.ReadAllText(Path.Combine(appRoot, "Views", "SettingsPageView.xaml"));
        var phpView = File.ReadAllText(Path.Combine(appRoot, "Views", "PhpPageView.xaml"));
        var databasesView = File.ReadAllText(Path.Combine(appRoot, "Views", "DatabasesPageView.xaml"));
        var projectsView = File.ReadAllText(Path.Combine(appRoot, "Views", "ProjectsPageView.xaml"));
        var schedulerView = File.ReadAllText(Path.Combine(appRoot, "Views", "SchedulerPageView.xaml"));
        var guidesView = File.ReadAllText(Path.Combine(appRoot, "Views", "GuidesPageView.xaml"));
        var terminalView = File.ReadAllText(Path.Combine(appRoot, "Views", "TerminalPageView.xaml"));
        var filesView = File.ReadAllText(Path.Combine(appRoot, "Views", "FilesPageView.xaml"));
        var seleniumView = File.ReadAllText(Path.Combine(appRoot, "Views", "SeleniumPageView.xaml"));

        Assert.Equal(1, window.Split("Content=\"{Binding CurrentPage}\"", StringSplitOptions.None).Length - 1);
        Assert.Contains("Content=\"{Binding CurrentPage}\"", window, StringComparison.Ordinal);
        Assert.DoesNotContain("PageView.", window, StringComparison.Ordinal);
        Assert.DoesNotContain("PackageManagerView.", window, StringComparison.Ordinal);
        Assert.Contains("RegisterPageActionHandlers();", File.ReadAllText(Path.Combine(appRoot, "MainWindow.xaml.cs")), StringComparison.Ordinal);
        Assert.Contains("AddHandler(ModulesPageView.InstallRequestedEvent", pageRouting, StringComparison.Ordinal);
        Assert.Contains("AddHandler(PackageManagerView.RemoveRequestedEvent", pageRouting, StringComparison.Ordinal);
        Assert.Contains("AddHandler(DatabasesPageView.DatabaseActionRequestedEvent", pageRouting, StringComparison.Ordinal);
        Assert.Contains("DataType=\"{x:Type viewModels:ModulesPageViewModel}\"", app, StringComparison.Ordinal);
        Assert.Contains("DataType=\"{x:Type viewModels:ApachePageViewModel}\"", app, StringComparison.Ordinal);
        Assert.Contains("DataType=\"{x:Type viewModels:PortsPageViewModel}\"", app, StringComparison.Ordinal);
        Assert.Contains("DataType=\"{x:Type viewModels:SettingsPageViewModel}\"", app, StringComparison.Ordinal);
        Assert.Contains("DataType=\"{x:Type viewModels:PhpPageViewModel}\"", app, StringComparison.Ordinal);
        Assert.Contains("DataType=\"{x:Type viewModels:DatabasesPageViewModel}\"", app, StringComparison.Ordinal);
        Assert.Contains("DataType=\"{x:Type viewModels:ProjectsPageViewModel}\"", app, StringComparison.Ordinal);
        Assert.Contains("DataType=\"{x:Type viewModels:SchedulerPageViewModel}\"", app, StringComparison.Ordinal);
        Assert.Contains("DataType=\"{x:Type viewModels:GuidesPageViewModel}\"", app, StringComparison.Ordinal);
        Assert.Contains("DataType=\"{x:Type viewModels:TerminalPageViewModel}\"", app, StringComparison.Ordinal);
        Assert.Contains("DataType=\"{x:Type viewModels:FilesPageViewModel}\"", app, StringComparison.Ordinal);
        Assert.Contains("DataType=\"{x:Type viewModels:SeleniumPageViewModel}\"", app, StringComparison.Ordinal);
        Assert.Contains("DataType=\"{x:Type viewModels:PackageManagerHostViewModel}\"", app, StringComparison.Ordinal);
        Assert.Contains("NavigationPage.Modules => ModulesPage", dashboard, StringComparison.Ordinal);
        Assert.Contains("NavigationPage.Apache => ApachePage", dashboard, StringComparison.Ordinal);
        Assert.Contains("NavigationPage.Ports => PortsPage", dashboard, StringComparison.Ordinal);
        Assert.Contains("NavigationPage.Settings => SettingsPage", dashboard, StringComparison.Ordinal);
        Assert.Contains("NavigationPage.Php => PhpPage", dashboard, StringComparison.Ordinal);
        Assert.Contains("NavigationPage.Databases => DatabasesPage", dashboard, StringComparison.Ordinal);
        Assert.Contains("NavigationPage.Projects => ProjectsPage", dashboard, StringComparison.Ordinal);
        Assert.Contains("NavigationPage.Scheduler => SchedulerPage", dashboard, StringComparison.Ordinal);
        Assert.Contains("NavigationPage.Guides => GuidesPage", dashboard, StringComparison.Ordinal);
        Assert.Contains("NavigationPage.Terminal => TerminalPage", dashboard, StringComparison.Ordinal);
        Assert.Contains("NavigationPage.Files => FilesPage", dashboard, StringComparison.Ordinal);
        Assert.Contains("NavigationPage.Selenium => SeleniumPage", dashboard, StringComparison.Ordinal);
        Assert.Contains("NavigationPage.Composer => ComposerHost", dashboard, StringComparison.Ordinal);
        Assert.Contains("NavigationPage.Node => NodeHost", dashboard, StringComparison.Ordinal);
        Assert.Contains("NavigationPage.Python => PythonHost", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("ConverterParameter=Modules", window, StringComparison.Ordinal);
        Assert.DoesNotContain("ConverterParameter=Apache", window, StringComparison.Ordinal);
        Assert.DoesNotContain("ConverterParameter=Ports", window, StringComparison.Ordinal);
        Assert.DoesNotContain("ConverterParameter=Settings", window, StringComparison.Ordinal);
        Assert.DoesNotContain("ConverterParameter=Php", window, StringComparison.Ordinal);
        Assert.DoesNotContain("ConverterParameter=Databases", window, StringComparison.Ordinal);
        Assert.DoesNotContain("ConverterParameter=Projects", window, StringComparison.Ordinal);
        Assert.DoesNotContain("ConverterParameter=Scheduler", window, StringComparison.Ordinal);
        Assert.DoesNotContain("ConverterParameter=Guides", window, StringComparison.Ordinal);
        Assert.DoesNotContain("ConverterParameter=Terminal", window, StringComparison.Ordinal);
        Assert.DoesNotContain("ConverterParameter=Files", window, StringComparison.Ordinal);
        Assert.DoesNotContain("ConverterParameter=Selenium", window, StringComparison.Ordinal);
        Assert.DoesNotContain("ConverterParameter=Composer", window, StringComparison.Ordinal);
        Assert.DoesNotContain("ConverterParameter=Node", window, StringComparison.Ordinal);
        Assert.DoesNotContain("ConverterParameter=Python", window, StringComparison.Ordinal);
        Assert.DoesNotContain("ItemsSource=\"{Binding RuntimePackages}\"", window, StringComparison.Ordinal);
        Assert.DoesNotContain("Apache HTTP Server", window, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding WebStackPackages}\"", modulesView, StringComparison.Ordinal);
        Assert.Contains("Apache HTTP Server", apacheView, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding TcpListeners}\"", portsView, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding StorageStatus}\"", settingsView, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding MemoryLimitText, UpdateSourceTrigger=PropertyChanged}\"", phpView, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding Databases}\"", databasesView, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding Projects}\"", projectsView, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding ScheduledTasks}\"", schedulerView, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding Articles}\"", guidesView, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding TextContent, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}\"", terminalView, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding WorkspaceEntries}\"", filesView, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding SeleniumProfiles}\"", seleniumView, StringComparison.Ordinal);
        Assert.DoesNotContain("ApachePortTextBox", window, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"EditorPreferenceSelector\"", window, StringComparison.Ordinal);
        Assert.DoesNotContain("PhpMemoryLimitTextBox", window, StringComparison.Ordinal);
        Assert.DoesNotContain("NewDatabaseNameTextBox", window, StringComparison.Ordinal);
        Assert.DoesNotContain("RootPasswordBox", window, StringComparison.Ordinal);
        Assert.DoesNotContain("GeneralProjectNameTextBox", window, StringComparison.Ordinal);
        Assert.DoesNotContain("ExistingProjectDirectorySelector", window, StringComparison.Ordinal);
        Assert.DoesNotContain("TerminalConsoleTextBox", window, StringComparison.Ordinal);
        Assert.DoesNotContain("WorkspaceEntriesListBox", window, StringComparison.Ordinal);
        Assert.DoesNotContain("SeleniumMaxSessionsTextBox", window, StringComparison.Ordinal);
        Assert.DoesNotContain("<controls:PackageManagerView", window, StringComparison.Ordinal);
        Assert.Contains("await ActivatePageAsync(item.Page)", File.ReadAllText(Path.Combine(appRoot, "MainWindow.xaml.cs")), StringComparison.Ordinal);
        Assert.Contains("shell:WorkspaceLayout.Mode=\"{Binding Shell.LayoutMode}\"", window, StringComparison.Ordinal);
        Assert.Contains("SizeChanged=\"WorkspaceSurface_SizeChanged\"", window, StringComparison.Ordinal);
    }

    [Fact]
    public void Modules_page_model_keeps_the_authoritative_runtime_collection()
    {
        var text = new UiText(new InMemorySettingsStore());
        var packages = new ObservableCollection<RuntimePackageViewModel>();
        var page = new ModulesPageViewModel(text, packages);
        var package = new RuntimePackageViewModel(
            RuntimePackageKind.Apache,
            "Apache",
            "HTTP server",
            "2.4",
            isInstalled: false,
            string.Empty);

        packages.Add(package);

        Assert.Same(text, page.Text);
        Assert.Same(packages, page.RuntimePackages);
        Assert.Same(package, Assert.Single(page.RuntimePackages));
        Assert.Same(package, Assert.Single(page.WebStackPackages));
        Assert.Empty(page.DevelopmentPackages);
        Assert.Empty(page.AutomationPackages);

        var development = new RuntimePackageViewModel(
            RuntimePackageKind.Node,
            "Node.js",
            "JavaScript runtime",
            "24",
            isInstalled: true,
            string.Empty);
        var automation = new RuntimePackageViewModel(
            RuntimePackageKind.Selenium,
            "Selenium",
            "Browser automation",
            "4",
            isInstalled: true,
            string.Empty);
        packages.Add(development);
        packages.Add(automation);

        Assert.Same(development, Assert.Single(page.DevelopmentPackages));
        Assert.Same(automation, Assert.Single(page.AutomationPackages));

        packages.Clear();
        Assert.Empty(page.WebStackPackages);
        Assert.Empty(page.DevelopmentPackages);
        Assert.Empty(page.AutomationPackages);
    }

    [Fact]
    public void Modules_use_grouped_catalog_rows_instead_of_a_wrapping_card_cloud()
    {
        var repositoryRoot = FindRepositoryRoot();
        var modulesView = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "PortableDeveloper.App",
            "Views",
            "ModulesPageView.xaml"));

        Assert.Contains("ItemsSource=\"{Binding WebStackPackages}\"", modulesView, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding DevelopmentPackages}\"", modulesView, StringComparison.Ordinal);
        Assert.Contains("ItemsSource=\"{Binding AutomationPackages}\"", modulesView, StringComparison.Ordinal);
        Assert.Contains("ItemTemplate=\"{StaticResource ModuleCatalogRowTemplate}\"", modulesView, StringComparison.Ordinal);
        Assert.DoesNotContain("<WrapPanel", modulesView, StringComparison.Ordinal);
        Assert.DoesNotContain("ModuleCardStyle", modulesView, StringComparison.Ordinal);
    }

    [Fact]
    public void Runtime_install_feedback_stays_in_the_owning_card()
    {
        var repositoryRoot = FindRepositoryRoot();
        var services = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "PortableDeveloper.App",
            "MainWindow.Services.cs"));
        var methodStart = services.IndexOf(
            "private async Task InstallRuntimePackageAsync",
            StringComparison.Ordinal);
        var methodEnd = services.IndexOf(
            "private void SetRuntimePackageManagerBusy",
            methodStart,
            StringComparison.Ordinal);
        var method = services[methodStart..methodEnd];

        Assert.Contains("package.SetProgress", method, StringComparison.Ordinal);
        Assert.Contains("package.Complete(false, failure)", method, StringComparison.Ordinal);
        Assert.Contains("installed.Complete(true", method, StringComparison.Ordinal);
        Assert.DoesNotContain("InstallationStatusText", method, StringComparison.Ordinal);
    }

    [Fact]
    public void Ports_and_storage_feedback_stays_in_the_owning_page_models()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var services = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Services.cs"));
        var storage = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Storage.cs"));

        var portsStart = services.IndexOf("private void RefreshPorts_Click", StringComparison.Ordinal);
        var portsEnd = services.IndexOf("private async void SavePhpSettings_Click", portsStart, StringComparison.Ordinal);
        var portsMethods = services[portsStart..portsEnd];

        Assert.Contains("_dashboard.PortsPage.SetStatus", portsMethods, StringComparison.Ordinal);
        Assert.DoesNotContain("InstallationStatusText", portsMethods, StringComparison.Ordinal);
        Assert.Contains("_dashboard.SettingsPage.SetStorageStatus", storage, StringComparison.Ordinal);
        Assert.DoesNotContain("InstallationStatusText", storage, StringComparison.Ordinal);
        Assert.DoesNotContain("StorageActionsPanel", storage, StringComparison.Ordinal);
    }

    [Fact]
    public void Php_and_database_feedback_stays_in_the_owning_page_models()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var services = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Services.cs"));

        var phpStart = services.IndexOf("private async void SavePhpSettings_Click", StringComparison.Ordinal);
        var phpEnd = services.IndexOf("private ApachePhpStackOptions CreateApachePhpOptions", phpStart, StringComparison.Ordinal);
        var phpMethods = services[phpStart..phpEnd];
        var databaseStart = services.IndexOf("private async Task BootstrapMariaDbAsync", StringComparison.Ordinal);
        var databaseMethods = services[databaseStart..];

        Assert.Contains("_dashboard.PhpPage.SetStatus", phpMethods, StringComparison.Ordinal);
        Assert.DoesNotContain("InstallationStatusText", phpMethods, StringComparison.Ordinal);
        Assert.Contains("_dashboard.DatabasesPage.SetStatus", databaseMethods, StringComparison.Ordinal);
        Assert.DoesNotContain("InstallationStatusText", databaseMethods, StringComparison.Ordinal);
        Assert.DoesNotContain("RootPasswordBox", services, StringComparison.Ordinal);
    }

    [Fact]
    public void Database_password_request_keeps_sensitive_input_view_local_and_explicitly_clearable()
    {
        var cleared = false;
        (PasswordValidationTarget Target, string Message)? validation = null;
        var request = new PasswordChangeRequestedEventArgs(
            DatabasesPageView.ChangePasswordRequestedEvent,
            "secret",
            "confirmation",
            () => cleared = true,
            (target, message) => validation = (target, message),
            () => validation = null);

        Assert.Equal("secret", request.Password);
        Assert.Equal("confirmation", request.Confirmation);
        Assert.False(cleared);
        Assert.Null(validation);

        request.ShowValidation(PasswordValidationTarget.Confirmation, "Mismatch");

        Assert.Equal((PasswordValidationTarget.Confirmation, "Mismatch"), validation);

        request.ClearInputs();

        Assert.True(cleared);

        request.ClearValidation();

        Assert.Null(validation);
    }

    [Fact]
    public void Database_password_feedback_uses_notifications_and_field_validation_instead_of_the_runtime_header()
    {
        var services = File.ReadAllText(Path.Combine(
            FindRepositoryRoot(),
            "src",
            "PortableDeveloper.App",
            "MainWindow.Services.cs"));
        var passwordStart = services.IndexOf("private async void ChangeRootPassword_Click", StringComparison.Ordinal);
        var passwordEnd = services.IndexOf("private void OpenPhpMyAdmin_Click", passwordStart, StringComparison.Ordinal);
        var passwordHandler = services[passwordStart..passwordEnd];

        Assert.DoesNotContain("_dashboard.DatabasesPage.SetStatus(_dashboard.Text.Password", passwordHandler, StringComparison.Ordinal);
        Assert.Contains("e.ShowValidation(PasswordValidationTarget.Password", passwordHandler, StringComparison.Ordinal);
        Assert.Contains("e.ShowValidation(PasswordValidationTarget.Confirmation", passwordHandler, StringComparison.Ordinal);
        Assert.Contains("TransientNotificationIntent.Error", passwordHandler, StringComparison.Ordinal);
    }

    [Fact]
    public void Project_actions_keep_the_project_identity_and_explicit_intent()
    {
        var request = new ProjectActionRequestedEventArgs(
            ProjectsPageView.ProjectActionRequestedEvent,
            "sample-project",
            ProjectAction.ConfigureWeb);

        Assert.Equal("sample-project", request.ProjectId);
        Assert.Equal(ProjectAction.ConfigureWeb, request.Action);
    }

    [Fact]
    public void Project_forms_and_action_feedback_stay_in_the_owning_page_model()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var projects = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Projects.cs"));

        var actionStart = projects.IndexOf(
            "private void ProjectsPage_ProjectActionRequested",
            StringComparison.Ordinal);
        var actionEnd = projects.IndexOf("private void ResetProjectTools", actionStart, StringComparison.Ordinal);
        var projectActions = projects[actionStart..actionEnd];

        Assert.Contains("_dashboard.ProjectsPage.SetStatus", projectActions, StringComparison.Ordinal);
        Assert.Contains("page.NewProjectName", projectActions, StringComparison.Ordinal);
        Assert.Contains("page.SelectedExistingDirectoryId", projectActions, StringComparison.Ordinal);
        Assert.DoesNotContain("InstallationStatusText", projectActions, StringComparison.Ordinal);
        Assert.DoesNotContain("GeneralProjectNameTextBox", projects, StringComparison.Ordinal);
        Assert.DoesNotContain("ExistingProjectDirectorySelector", projects, StringComparison.Ordinal);
    }

    [Fact]
    public void Project_switch_does_not_wait_for_package_inventory_refreshes()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var projects = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Projects.cs"));
        var window = File.ReadAllText(Path.Combine(appRoot, "MainWindow.xaml.cs"));

        var switchStart = projects.IndexOf("private bool SelectWebProject", StringComparison.Ordinal);
        var switchEnd = projects.IndexOf("private void ManageProjects_Click", switchStart, StringComparison.Ordinal);
        var projectSwitch = projects[switchStart..switchEnd];

        Assert.DoesNotContain("RefreshPackageManagerAsync", projectSwitch, StringComparison.Ordinal);
        Assert.Contains("statusSink(string.Empty);", projectSwitch, StringComparison.Ordinal);
        Assert.DoesNotContain("ProjectSelected", projectSwitch, StringComparison.Ordinal);
        Assert.Contains("case NavigationPage.Composer:", window, StringComparison.Ordinal);
        Assert.Contains("case NavigationPage.Node:", window, StringComparison.Ordinal);
        Assert.Contains("case NavigationPage.Python:", window, StringComparison.Ordinal);
        Assert.Contains("EnsurePackageManagerLoadedAsync(_composerPackageManager", window, StringComparison.Ordinal);
        Assert.Contains("EnsurePackageManagerLoadedAsync(_nodePackageManager", window, StringComparison.Ordinal);
        Assert.Contains("EnsurePackageManagerLoadedAsync(_pythonPackageManager", window, StringComparison.Ordinal);
    }

    [Fact]
    public void Package_inventory_is_lazy_and_reuses_the_loaded_page_snapshot()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var packageManagement = File.ReadAllText(Path.Combine(appRoot, "MainWindow.PackageManagement.cs"));
        var services = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Services.cs"));

        Assert.Contains("if (page.InventoryLoaded)", packageManagement, StringComparison.Ordinal);
        Assert.DoesNotContain("_dashboard.Composer.RuntimeReady", services, StringComparison.Ordinal);
        Assert.DoesNotContain("_dashboard.Node.RuntimeReady", services, StringComparison.Ordinal);
        Assert.DoesNotContain("_dashboard.Python.RuntimeReady", services, StringComparison.Ordinal);
    }

    [Fact]
    public void Database_actions_keep_the_database_identity_and_explicit_intent()
    {
        var request = new DatabaseActionRequestedEventArgs(
            DatabasesPageView.DatabaseActionRequestedEvent,
            "project_db",
            DatabaseAction.Delete);

        Assert.Equal("project_db", request.DatabaseName);
        Assert.Equal(DatabaseAction.Delete, request.Action);
    }

    [Fact]
    public void Terminal_and_file_feedback_no_longer_writes_to_the_shell_status()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var terminal = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Terminal.cs"));
        var files = File.ReadAllText(Path.Combine(appRoot, "MainWindow.FileManager.cs"));

        Assert.Contains("_dashboard.TerminalPage", terminal, StringComparison.Ordinal);
        Assert.DoesNotContain("TerminalConsoleTextBox", terminal, StringComparison.Ordinal);
        Assert.DoesNotContain("InstallationStatusText", terminal, StringComparison.Ordinal);
        Assert.Contains("_dashboard.FilesPage.SetStatus", files, StringComparison.Ordinal);
        Assert.DoesNotContain("InstallationStatusText", files, StringComparison.Ordinal);
    }

    [Fact]
    public void Selenium_settings_progress_and_feedback_belong_to_the_page_model_while_creation_inputs_are_modal()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var selenium = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Selenium.cs"));
        var page = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "SeleniumPageViewModel.cs"));

        Assert.Contains("_dashboard.SeleniumPage.SetStatus", selenium, StringComparison.Ordinal);
        Assert.Contains("_dashboard.SeleniumPage.SetProfileProgress", selenium, StringComparison.Ordinal);
        Assert.Contains("page.MaximumSessionsText", selenium, StringComparison.Ordinal);
        Assert.Contains("new SeleniumProfileDialog", selenium, StringComparison.Ordinal);
        Assert.Contains("new CookieVaultImportDialog", selenium, StringComparison.Ordinal);
        Assert.Contains("await SaveSeleniumSettingsAsync()", selenium, StringComparison.Ordinal);
        Assert.Contains("await RestartSeleniumAsync()", selenium, StringComparison.Ordinal);
        Assert.Contains("await _seleniumServer.StopAsync", selenium, StringComparison.Ordinal);
        Assert.Contains("await _seleniumServer.StartAsync", selenium, StringComparison.Ordinal);
        Assert.DoesNotContain("InstallationStatusText", selenium, StringComparison.Ordinal);
        Assert.Contains("public string StatusText", page, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectedCookieFilePath", page, StringComparison.Ordinal);
        Assert.DoesNotContain("CleanProfileName", page, StringComparison.Ordinal);
    }

    [Fact]
    public void Generic_shell_status_channel_has_no_remaining_definition_or_caller()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var sourceFiles = Directory.EnumerateFiles(appRoot, "*.*", SearchOption.AllDirectories)
            .Where(path => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
                || path.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase)
                && !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase));
        var source = string.Join(Environment.NewLine, sourceFiles.Select(File.ReadAllText));
        var header = File.ReadAllText(Path.Combine(appRoot, "Controls", "WorkspaceHeader.xaml"));

        Assert.DoesNotContain("InstallationStatusText", source, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding ProjectContextStatus}\"", header, StringComparison.Ordinal);
    }

    [Fact]
    public void Workspace_uses_one_adaptive_layout_contract_without_page_level_horizontal_scrolling()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var window = File.ReadAllText(Path.Combine(appRoot, "MainWindow.xaml"));
        var workspaceStyles = File.ReadAllText(Path.Combine(appRoot, "Assets", "WorkspaceStyles.xaml"));
        var adaptiveViews = new[]
        {
            Path.Combine("Controls", "WorkspaceHeader.xaml"),
            Path.Combine("Controls", "PackageManagerView.xaml"),
            Path.Combine("Controls", "RuntimeHeader.xaml"),
            Path.Combine("Controls", "SectionHeader.xaml"),
            Path.Combine("Views", "DatabasesPageView.xaml"),
            Path.Combine("Views", "GuidesPageView.xaml"),
            Path.Combine("Views", "PhpPageView.xaml"),
            Path.Combine("Views", "PortsPageView.xaml"),
            Path.Combine("Views", "ProjectsPageView.xaml"),
            Path.Combine("Views", "SchedulerPageView.xaml"),
            Path.Combine("Views", "SeleniumPageView.xaml")
        };

        Assert.Contains("shell:WorkspaceLayout.Mode=\"{Binding Shell.LayoutMode}\"", window, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"PageScrollViewerStyle\"", workspaceStyles, StringComparison.Ordinal);
        Assert.Contains("HorizontalScrollBarVisibility\" Value=\"Disabled\"", workspaceStyles, StringComparison.Ordinal);
        foreach (var relativePath in adaptiveViews)
        {
            var view = File.ReadAllText(Path.Combine(appRoot, relativePath));
            Assert.Contains("AdaptiveSplitPanel", view, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Dynamic_project_collection_owns_a_bounded_inner_scroll_viewport()
    {
        var repositoryRoot = FindRepositoryRoot();
        var projectsView = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "src",
            "PortableDeveloper.App",
            "Views",
            "ProjectsPageView.xaml"));

        Assert.Contains("<Border MinHeight=\"430\" MaxHeight=\"520\"", projectsView, StringComparison.Ordinal);
        Assert.Contains("ScrollViewer.VerticalScrollBarVisibility=\"Auto\"", projectsView, StringComparison.Ordinal);
        Assert.Contains("ScrollViewer.HorizontalScrollBarVisibility=\"Disabled\"", projectsView, StringComparison.Ordinal);
    }

    [Fact]
    public void Project_master_detail_uses_quiet_selection_and_one_sectioned_detail_surface()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var projectsView = File.ReadAllText(Path.Combine(appRoot, "Views", "ProjectsPageView.xaml"));
        var styles = File.ReadAllText(Path.Combine(appRoot, "Assets", "WorkspaceStyles.xaml"));

        Assert.Contains("ItemContainerStyle=\"{StaticResource MasterListItemStyle}\"", projectsView, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"ListBoxItemSelectedBackgroundThemeBrush\"", projectsView, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource SubtleFillColorSecondary}", projectsView, StringComparison.Ordinal);
        Assert.Contains("{DynamicResource AccentFillColorDefaultBrush}", projectsView, StringComparison.Ordinal);
        Assert.Contains("Content=\"{Binding SelectedProject}\"", projectsView, StringComparison.Ordinal);
        Assert.Contains("Data=\"{StaticResource IconProject}\"", projectsView, StringComparison.Ordinal);
        Assert.Contains("Click=\"OpenManagedProject_Click\"", projectsView, StringComparison.Ordinal);
        Assert.Contains("Click=\"OpenProjectFiles_Click\"", projectsView, StringComparison.Ordinal);
        Assert.Contains("Click=\"OpenProjectTerminal_Click\"", projectsView, StringComparison.Ordinal);
        Assert.Contains("Click=\"ConfigureProjectWeb_Click\"", projectsView, StringComparison.Ordinal);
        Assert.Contains("Click=\"RenameProject_Click\"", projectsView, StringComparison.Ordinal);
        Assert.Contains("Click=\"UnregisterProject_Click\"", projectsView, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"MasterListItemStyle\"", styles, StringComparison.Ordinal);
        Assert.DoesNotContain("Data=\"{StaticResource IconDelete}\"", projectsView, StringComparison.Ordinal);
    }

    [Fact]
    public void Potentially_long_identity_fields_have_an_explicit_overflow_policy()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var projectsView = File.ReadAllText(Path.Combine(appRoot, "Views", "ProjectsPageView.xaml"));
        var filesView = File.ReadAllText(Path.Combine(appRoot, "Views", "FilesPageView.xaml"));
        var packageView = File.ReadAllText(Path.Combine(appRoot, "Controls", "PackageManagerView.xaml"));
        var seleniumView = File.ReadAllText(Path.Combine(appRoot, "Views", "SeleniumPageView.xaml"));
        var styles = File.ReadAllText(Path.Combine(appRoot, "Assets", "WorkspaceStyles.xaml"));

        var normalizedProjectsView = projectsView.ReplaceLineEndings("\n");
        Assert.Contains("Text=\"{Binding Name}\"\n                                                           TextTrimming=\"CharacterEllipsis\" ToolTip=\"{Binding Name}\"", normalizedProjectsView, StringComparison.Ordinal);
        Assert.Contains("TextTrimming=\"CharacterEllipsis\" ToolTip=\"{Binding Name}\"", filesView, StringComparison.Ordinal);
        Assert.Contains("TextTrimming=\"CharacterEllipsis\" ToolTip=\"{Binding Name}\"", packageView, StringComparison.Ordinal);
        Assert.Equal(1, seleniumView.Split("Style=\"{StaticResource TrimmingGroupBoxStyle}\"", StringSplitOptions.None).Length - 1);
        Assert.Contains("ToolTip=\"{Binding CapabilityValue}\"", seleniumView, StringComparison.Ordinal);
        Assert.Contains("Brand=\"{Binding BrowserBrand}\"", seleniumView, StringComparison.Ordinal);
        Assert.Contains("Brand=\"{Binding PrimaryBrandLogo}\"", seleniumView, StringComparison.Ordinal);
        Assert.Contains("CompactBreakpoint=\"720\"", seleniumView, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource CatalogRowStyle}\"", seleniumView, StringComparison.Ordinal);
        Assert.Contains("x:Key=\"TrimmingGroupBoxStyle\"", styles, StringComparison.Ordinal);
        Assert.Contains("BasedOn=\"{StaticResource DefaultGroupBoxStyle}\"", styles, StringComparison.Ordinal);
    }

    [Fact]
    public void Files_and_scheduler_state_no_longer_lives_in_the_root_dashboard_model()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var dashboard = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "DashboardViewModel.cs"));
        var files = File.ReadAllText(Path.Combine(appRoot, "MainWindow.FileManager.cs"));
        var scheduler = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Scheduler.cs"));

        Assert.DoesNotContain("ObservableCollection<WorkspaceEntryViewModel>", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("ObservableCollection<ScheduledTaskViewModel>", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("ObservableCollection<ScheduledTaskRunViewModel>", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("void SetWorkspacePage", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("void SetScheduledTasks", dashboard, StringComparison.Ordinal);
        Assert.Contains("_dashboard.FilesPage.SetWorkspacePage", files, StringComparison.Ordinal);
        Assert.Contains("_dashboard.SchedulerPage.SetScheduledTasks", scheduler, StringComparison.Ordinal);
    }

    [Fact]
    public void Ports_and_php_collections_no_longer_live_in_the_root_dashboard_model()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var dashboard = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "DashboardViewModel.cs"));
        var portsPage = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "PortsPageViewModel.cs"));
        var phpPage = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "PhpPageViewModel.cs"));
        var services = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Services.cs"));

        Assert.DoesNotContain("ObservableCollection<TcpPortListenerViewModel>", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("ObservableCollection<PhpExtensionViewModel>", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("void SetTcpListeners", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("void SetPhpExtensions", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("DashboardViewModel", portsPage, StringComparison.Ordinal);
        Assert.DoesNotContain("DashboardViewModel", phpPage, StringComparison.Ordinal);
        Assert.Contains("_dashboard.PortsPage.SetTcpListeners", services, StringComparison.Ordinal);
        Assert.Contains("_dashboard.PhpPage.SetExtensions", services, StringComparison.Ordinal);
    }

    [Fact]
    public void Database_and_selenium_collections_no_longer_live_in_the_root_dashboard_model()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var dashboard = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "DashboardViewModel.cs"));
        var databasesPage = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "DatabasesPageViewModel.cs"));
        var seleniumPage = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "SeleniumPageViewModel.cs"));
        var services = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Services.cs"));
        var selenium = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Selenium.cs"));

        Assert.DoesNotContain("ObservableCollection<DatabaseCardViewModel>", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("ObservableCollection<SeleniumDriverCardViewModel>", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("ObservableCollection<SeleniumSessionCardViewModel>", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("ObservableCollection<SeleniumProfileCardViewModel>", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("ObservableCollection<SeleniumCookieVaultCardViewModel>", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("ObservableCollection<SeleniumBrowserChoiceViewModel>", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("DashboardViewModel", databasesPage, StringComparison.Ordinal);
        Assert.DoesNotContain("DashboardViewModel", seleniumPage, StringComparison.Ordinal);
        Assert.Contains("_dashboard.DatabasesPage.SetDatabases", services, StringComparison.Ordinal);
        Assert.Contains("_dashboard.SeleniumPage.SetEnvironments", selenium, StringComparison.Ordinal);
        Assert.Contains("_dashboard.SeleniumPage.SetProfiles", selenium, StringComparison.Ordinal);
        Assert.Contains("_dashboard.SeleniumPage.SetSessions", selenium, StringComparison.Ordinal);
        Assert.Contains("_dashboard.SeleniumPage.SetCookieVaults", selenium, StringComparison.Ordinal);
    }

    [Fact]
    public void Project_presentation_state_no_longer_lives_in_the_root_dashboard_model()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var dashboard = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "DashboardViewModel.cs"));
        var projectsPage = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "ProjectsPageViewModel.cs"));
        var projects = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Projects.cs"));

        Assert.DoesNotContain("public ObservableCollection<ProjectViewModel> Projects", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("ObservableCollection<ProjectTemplateChoiceViewModel>", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("ObservableCollection<ManagedProjectDirectoryCandidate>", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("void SetProjects", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("void SetRegistrableProjectDirectories", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("public sealed record ProjectViewModel", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("DashboardViewModel", projectsPage, StringComparison.Ordinal);
        Assert.Contains("_dashboard.ProjectsPage.SetProjects", projects, StringComparison.Ordinal);
        Assert.Contains("_dashboard.ProjectsPage.SetRegistrableProjectDirectories", projects, StringComparison.Ordinal);
    }

    [Fact]
    public void Apache_and_settings_page_models_no_longer_forward_the_root_dashboard()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var dashboard = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "DashboardViewModel.cs"));
        var apachePage = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "ApachePageViewModel.cs"));
        var settingsPage = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "SettingsPageViewModel.cs"));
        var projects = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Projects.cs"));

        Assert.DoesNotContain("DashboardViewModel", apachePage, StringComparison.Ordinal);
        Assert.DoesNotContain("DashboardViewModel", settingsPage, StringComparison.Ordinal);
        Assert.DoesNotContain("ObservableCollection<WebProjectViewModel>", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("public ObservableCollection<ServiceCardViewModel> Services", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("void SetWebProjects", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("public string RootPath", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("public string ApplicationVersion", dashboard, StringComparison.Ordinal);
        Assert.Contains("_dashboard.ApachePage.SetWebProjects", projects, StringComparison.Ordinal);
    }

    [Fact]
    public void Root_dashboard_only_composes_pages_shell_and_runtime_presentation()
    {
        var repositoryRoot = FindRepositoryRoot();
        var appRoot = Path.Combine(repositoryRoot, "src", "PortableDeveloper.App");
        var dashboard = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "DashboardViewModel.cs"));
        var runtime = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "WorkspaceRuntimeCoordinator.cs"));
        var services = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Services.cs"));

        Assert.Contains("public WorkspaceRuntimeCoordinator Runtime", dashboard, StringComparison.Ordinal);
        Assert.Contains("private void ApplyRuntimeState()", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("private ManagedProcessState", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("private readonly IModuleInventory", dashboard, StringComparison.Ordinal);
        Assert.DoesNotContain("PageViewModel", runtime, StringComparison.Ordinal);
        Assert.DoesNotContain("MainWindow", runtime, StringComparison.Ordinal);
        Assert.Contains("_dashboard.Runtime.SetApacheStatus", services, StringComparison.Ordinal);
        Assert.True(File.ReadLines(Path.Combine(appRoot, "ViewModels", "DashboardViewModel.cs")).Count() < 350);
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

    private sealed class InMemorySettingsStore : IApplicationSettingsStore
    {
        private ApplicationSettings _settings = ApplicationSettings.Default;

        public ApplicationSettings Load() => _settings;

        public void Save(ApplicationSettings settings) => _settings = settings;
    }
}
