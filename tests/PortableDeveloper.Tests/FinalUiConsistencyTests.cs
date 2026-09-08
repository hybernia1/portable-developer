namespace PortableDeveloper.Tests;

public sealed class FinalUiConsistencyTests
{
    [Fact]
    public void Scheduler_and_guide_headers_do_not_repeat_self_evident_behavior()
    {
        var appRoot = FindAppRoot();
        var scheduler = File.ReadAllText(Path.Combine(appRoot, "Views", "SchedulerPageView.xaml"));
        var guides = File.ReadAllText(Path.Combine(appRoot, "Views", "GuidesPageView.xaml"));
        var schedulerText = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "UiText.Scheduler.cs"));
        var guideText = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "UiText.Guides.cs"));

        Assert.DoesNotContain("SchedulerRunsOnlyWhileOpen", scheduler, StringComparison.Ordinal);
        Assert.DoesNotContain("SchedulerRunsOnlyWhileOpen", schedulerText, StringComparison.Ordinal);
        Assert.DoesNotContain("GuideBrowseHelp", guides, StringComparison.Ordinal);
        Assert.DoesNotContain("GuideBrowseHelp", guideText, StringComparison.Ordinal);
    }

    [Fact]
    public void Remaining_forms_and_information_surfaces_use_shared_components()
    {
        var appRoot = FindAppRoot();
        var packages = File.ReadAllText(Path.Combine(appRoot, "Controls", "PackageManagerView.xaml"));
        var packageCode = File.ReadAllText(Path.Combine(appRoot, "Controls", "PackageManagerView.xaml.cs"));
        var apache = File.ReadAllText(Path.Combine(appRoot, "Views", "ApachePageView.xaml"));
        var projects = File.ReadAllText(Path.Combine(appRoot, "Views", "ProjectsPageView.xaml"));

        Assert.Contains("<controls:FormSection", packages, StringComparison.Ordinal);
        Assert.Contains("<controls:SectionHeader", packages, StringComparison.Ordinal);
        Assert.Contains("CompactBreakpoint=\"900\"", packages, StringComparison.Ordinal);
        Assert.Contains("PackageNameInputProperty", packageCode, StringComparison.Ordinal);
        Assert.Contains("VersionConstraintInputProperty", packageCode, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"PackageNameTextBox\"", packages, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"VersionConstraintTextBox\"", packages, StringComparison.Ordinal);
        Assert.Contains("<controls:FormSection.HeaderActions>", packages, StringComparison.Ordinal);
        Assert.Contains("Click=\"ShowHelp_Click\"", packages, StringComparison.Ordinal);
        Assert.DoesNotContain("Description=\"{Binding HelpText", packages, StringComparison.Ordinal);
        Assert.DoesNotContain("Grid.Row=\"1\" MaxHeight=\"460\"", packages, StringComparison.Ordinal);
        Assert.Contains("<ScrollViewer Margin=\"0,8,0,0\" MaxHeight=\"300\"", packages, StringComparison.Ordinal);

        Assert.Contains("<controls:SectionHeader", apache, StringComparison.Ordinal);
        Assert.Contains("CompactBreakpoint=\"620\"", apache, StringComparison.Ordinal);

        Assert.Equal(2, CountOccurrences(projects, "<controls:FormSection MaxWidth"));
        Assert.Contains("Text=\"{Binding SelectedTemplateDescription}\"", projects, StringComparison.Ordinal);
        Assert.DoesNotContain("x:Name=\"TemplateSelector\"", projects, StringComparison.Ordinal);
    }

    [Fact]
    public void Optional_explanations_use_explicit_information_dialogs()
    {
        var appRoot = FindAppRoot();
        var selenium = File.ReadAllText(Path.Combine(appRoot, "Views", "SeleniumPageView.xaml"));
        var seleniumCode = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Selenium.cs"));
        var packageCode = File.ReadAllText(Path.Combine(appRoot, "MainWindow.PackageManagement.cs"));
        var dialog = File.ReadAllText(Path.Combine(appRoot, "InformationDialog.xaml"));

        Assert.Contains("Click=\"ShowProfileHelp_Click\"", selenium, StringComparison.Ordinal);
        Assert.Contains("Click=\"ShowCookieVaultHelp_Click\"", selenium, StringComparison.Ordinal);
        Assert.DoesNotContain("Description=\"{Binding Text.SeleniumProfilesHelp}\"", selenium, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding Text.CreateCleanMasterHelp}\"", selenium, StringComparison.Ordinal);
        Assert.DoesNotContain("Description=\"{Binding Text.CookieVaultHelp}\"", selenium, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"{Binding Text.CookieVaultAutomaticProtectionHelp}\"", selenium, StringComparison.Ordinal);
        Assert.Contains("InformationDialog.Show(", seleniumCode, StringComparison.Ordinal);
        Assert.Contains("InformationDialog.Show(", packageCode, StringComparison.Ordinal);
        Assert.Contains("<controls:DialogHeader", dialog, StringComparison.Ordinal);
        Assert.Contains("Icon=\"{StaticResource IconInfo}\"", dialog, StringComparison.Ordinal);
    }

    [Fact]
    public void Catalog_creation_uses_header_commands_and_transient_native_dialogs()
    {
        var appRoot = FindAppRoot();
        var selenium = File.ReadAllText(Path.Combine(appRoot, "Views", "SeleniumPageView.xaml"));
        var seleniumCode = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Selenium.cs"));
        var seleniumPageModel = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "SeleniumPageViewModel.cs"));
        var databases = File.ReadAllText(Path.Combine(appRoot, "Views", "DatabasesPageView.xaml"));
        var databaseCode = File.ReadAllText(Path.Combine(appRoot, "MainWindow.Services.cs"));
        var databasePageModel = File.ReadAllText(Path.Combine(appRoot, "ViewModels", "DatabasesPageViewModel.cs"));

        Assert.Contains("Text.AddSeleniumProfile", selenium, StringComparison.Ordinal);
        Assert.Contains("Text.AddCookieVault", selenium, StringComparison.Ordinal);
        Assert.Contains("Click=\"CreateCleanSeleniumProfile_Click\"", selenium, StringComparison.Ordinal);
        Assert.Contains("Click=\"ImportCookieVault_Click\"", selenium, StringComparison.Ordinal);
        Assert.DoesNotContain("CleanProfileName", selenium, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectedCookieFileDisplay", selenium, StringComparison.Ordinal);
        Assert.Contains("new SeleniumProfileDialog", seleniumCode, StringComparison.Ordinal);
        Assert.Contains("new CookieVaultImportDialog", seleniumCode, StringComparison.Ordinal);
        Assert.DoesNotContain("CleanProfileName", seleniumPageModel, StringComparison.Ordinal);
        Assert.DoesNotContain("SelectedCookieFilePath", seleniumPageModel, StringComparison.Ordinal);

        Assert.Contains("Heading=\"{Binding Text.DatabaseOverview}\"", databases, StringComparison.Ordinal);
        Assert.Contains("Click=\"CreateDatabase_Click\"", databases, StringComparison.Ordinal);
        Assert.DoesNotContain("{Binding NewDatabaseName", databases, StringComparison.Ordinal);
        Assert.Contains("new NamePromptDialog", databaseCode, StringComparison.Ordinal);
        Assert.DoesNotContain("NewDatabaseName", databasePageModel, StringComparison.Ordinal);
    }

    [Fact]
    public void Feature_views_do_not_reintroduce_tables_or_page_local_section_headings()
    {
        var appRoot = FindAppRoot();
        var files = Directory.GetFiles(Path.Combine(appRoot, "Views"), "*.xaml")
            .Append(Path.Combine(appRoot, "Controls", "PackageManagerView.xaml"));

        foreach (var file in files)
        {
            var view = File.ReadAllText(file);
            Assert.DoesNotContain("<DataGrid", view, StringComparison.Ordinal);
            Assert.DoesNotContain("FontSize=\"17\" FontWeight=\"SemiBold\"", view, StringComparison.Ordinal);
            Assert.DoesNotContain("Background=\"#", view, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Foreground=\"#", view, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public void Steady_state_pages_keep_only_actionable_guidance_and_diagnostics()
    {
        var appRoot = FindAppRoot();
        var modules = File.ReadAllText(Path.Combine(appRoot, "Views", "ModulesPageView.xaml"));
        var projects = File.ReadAllText(Path.Combine(appRoot, "Views", "ProjectsPageView.xaml"));
        var ports = File.ReadAllText(Path.Combine(appRoot, "Views", "PortsPageView.xaml"));
        var apache = File.ReadAllText(Path.Combine(appRoot, "Views", "ApachePageView.xaml"));
        var databases = File.ReadAllText(Path.Combine(appRoot, "Views", "DatabasesPageView.xaml"));
        var selenium = File.ReadAllText(Path.Combine(appRoot, "Views", "SeleniumPageView.xaml"));
        var php = File.ReadAllText(Path.Combine(appRoot, "Views", "PhpPageView.xaml"));
        var runtimeHeader = File.ReadAllText(Path.Combine(appRoot, "Controls", "RuntimeHeader.xaml"));

        Assert.DoesNotContain("ModulesIntroduction", modules, StringComparison.Ordinal);
        Assert.DoesNotContain("ProjectTemplateNotice", projects, StringComparison.Ordinal);
        Assert.DoesNotContain("PortManagerHelp", ports, StringComparison.Ordinal);
        Assert.DoesNotContain("PortReadOnlyNotice", ports, StringComparison.Ordinal);
        Assert.Contains("Detail=\"{Binding ApacheHeaderDetail}\"", apache, StringComparison.Ordinal);
        Assert.Contains("Detail=\"{Binding MariaDbHeaderDetail}\"", databases, StringComparison.Ordinal);
        Assert.DoesNotContain("PhpMyAdminDescription", databases, StringComparison.Ordinal);
        Assert.Contains("Detail=\"{Binding SeleniumHeaderDetail}\"", selenium, StringComparison.Ordinal);
        Assert.DoesNotContain("Metadata=\"{Binding SeleniumHubUrl}\"", selenium, StringComparison.Ordinal);
        Assert.Contains("Visibility=\"{Binding SeleniumIsRunning", selenium, StringComparison.Ordinal);
        Assert.DoesNotContain("SessionTimeoutHelp", selenium, StringComparison.Ordinal);
        Assert.DoesNotContain("SeleniumDownloadsHelp", selenium, StringComparison.Ordinal);
        Assert.DoesNotContain("SeleniumDriversHelp", selenium, StringComparison.Ordinal);
        Assert.DoesNotContain("PhpExtensionsHelp", php, StringComparison.Ordinal);
        Assert.Contains("Visibility=\"{Binding Detail, ElementName=Root", runtimeHeader, StringComparison.Ordinal);
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
