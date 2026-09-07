using PortableDeveloper.App.Shell;
using PortableDeveloper.App.ViewModels;

namespace PortableDeveloper.Tests;

public sealed class WorkspaceNavigationPolicyTests
{
    [Fact]
    public void Navigating_to_a_page_selects_its_first_section()
    {
        var section = WorkspaceNavigationPolicy.ResolveSection(
            NavigationPage.Projects,
            NavigationSection.SeleniumSessions,
            preserveValidSelection: false);

        Assert.Equal(NavigationSection.ProjectsOverview, section);
    }

    [Fact]
    public void Refreshing_a_page_preserves_a_valid_section()
    {
        var section = WorkspaceNavigationPolicy.ResolveSection(
            NavigationPage.Ports,
            NavigationSection.PortsListeners,
            preserveValidSelection: true);

        Assert.Equal(NavigationSection.PortsListeners, section);
    }

    [Fact]
    public void Refreshing_a_page_replaces_an_unrelated_section()
    {
        var section = WorkspaceNavigationPolicy.ResolveSection(
            NavigationPage.Settings,
            NavigationSection.SeleniumDrivers,
            preserveValidSelection: true);

        Assert.Equal(NavigationSection.SettingsGeneral, section);
    }

    [Fact]
    public void A_page_without_sections_resolves_to_none()
    {
        var section = WorkspaceNavigationPolicy.ResolveSection(
            NavigationPage.Composer,
            NavigationSection.ProjectsOverview,
            preserveValidSelection: true);

        Assert.Equal(NavigationSection.None, section);
        Assert.Empty(WorkspaceNavigationPolicy.GetSections(NavigationPage.Composer));
    }

    [Theory]
    [InlineData(NavigationPage.Projects, 3)]
    [InlineData(NavigationPage.Php, 2)]
    [InlineData(NavigationPage.Databases, 3)]
    [InlineData(NavigationPage.Selenium, 5)]
    [InlineData(NavigationPage.Ports, 2)]
    [InlineData(NavigationPage.Scheduler, 2)]
    [InlineData(NavigationPage.Settings, 3)]
    public void Sectioned_pages_have_an_explicit_navigation_contract(NavigationPage page, int expectedCount)
    {
        var sections = WorkspaceNavigationPolicy.GetSections(page);

        Assert.Equal(expectedCount, sections.Count);
        Assert.DoesNotContain(NavigationSection.None, sections);
        Assert.Equal(sections.Count, sections.Distinct().Count());
    }
}
