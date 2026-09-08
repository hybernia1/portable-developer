namespace PortableDeveloper.Tests;

public sealed class FormSectionPresentationTests
{
    [Fact]
    public void Shared_form_section_owns_header_actions_body_feedback_and_optional_footer()
    {
        var appRoot = FindAppRoot();
        var section = File.ReadAllText(Path.Combine(appRoot, "Controls", "FormSection.xaml"));

        Assert.Contains("PanelCardStyle", section, StringComparison.Ordinal);
        Assert.Contains("<controls:AppIcon", section, StringComparison.Ordinal);
        Assert.Contains("Content=\"{Binding HeaderActions, ElementName=Root}\"", section, StringComparison.Ordinal);
        Assert.Contains("Binding=\"{Binding HeaderActions, ElementName=Root}\" Value=\"{x:Null}\"", section, StringComparison.Ordinal);
        Assert.Contains("Content=\"{Binding Body, ElementName=Root}\"", section, StringComparison.Ordinal);
        Assert.Contains("Text=\"{Binding Feedback, ElementName=Root}\"", section, StringComparison.Ordinal);
        Assert.Contains("FormActionFooterStyle", section, StringComparison.Ordinal);
        Assert.Contains("Binding=\"{Binding Actions, ElementName=Root}\" Value=\"{x:Null}\"", section, StringComparison.Ordinal);
        Assert.DoesNotContain("<ControlTemplate", section, StringComparison.Ordinal);
        Assert.DoesNotContain("<Path", section, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("SeleniumPageView.xaml", "IconGear", "SaveSeleniumSettings_Click")]
    [InlineData("PhpPageView.xaml", "IconCode", "SavePhpSettings_Click")]
    [InlineData("SettingsPageView.xaml", "IconCode", "EditorPreferenceSelector_SelectionChanged")]
    public void Settings_workspaces_use_the_shared_form_grammar(string file, string icon, string action)
    {
        var view = File.ReadAllText(Path.Combine(FindAppRoot(), "Views", file));

        Assert.Contains("<controls:FormSection", view, StringComparison.Ordinal);
        Assert.Contains($"{icon}", view, StringComparison.Ordinal);
        Assert.Contains(action, view, StringComparison.Ordinal);
    }

    [Fact]
    public void Password_form_keeps_page_namescope_and_shared_visual_contract()
    {
        var view = File.ReadAllText(Path.Combine(FindAppRoot(), "Views", "DatabasesPageView.xaml"));

        Assert.Contains("x:Name=\"RootPasswordBox\"", view, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ConfirmRootPasswordBox\"", view, StringComparison.Ordinal);
        Assert.Contains("PasswordChanged=\"PasswordInput_PasswordChanged\"", view, StringComparison.Ordinal);
        Assert.Contains("Icon=\"{StaticResource IconShield}\"", view, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource FormActionFooterStyle}\"", view, StringComparison.Ordinal);
        Assert.Contains("Style=\"{StaticResource FormPrimaryButtonStyle}\"", view, StringComparison.Ordinal);
        Assert.Contains("ChangeRootPassword_Click", view, StringComparison.Ordinal);

        var viewCode = File.ReadAllText(Path.Combine(FindAppRoot(), "Views", "DatabasesPageView.xaml.cs"));
        Assert.Contains("SystemFillColorCriticalBrush", viewCode, StringComparison.Ordinal);
        Assert.Contains("AutomationProperties.SetHelpText", viewCode, StringComparison.Ordinal);
    }

    [Fact]
    public void Form_controls_remain_native_fluent_controls()
    {
        var appRoot = FindAppRoot();
        var views = new[]
        {
            "SeleniumPageView.xaml",
            "PhpPageView.xaml",
            "SettingsPageView.xaml",
            "DatabasesPageView.xaml"
        };

        foreach (var file in views)
        {
            var view = File.ReadAllText(Path.Combine(appRoot, "Views", file));
            Assert.DoesNotContain("<ControlTemplate", view, StringComparison.Ordinal);
        }
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
