using PortableDeveloper.Application.Projects;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Projects;

namespace PortableDeveloper.Tests;

public sealed class ProjectContextTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void Activate_persists_one_change_and_emits_one_coherent_event()
    {
        var catalog = CreateCatalog();
        var project = AddProject(catalog, "second");
        catalog.SetActive(ProjectCatalogDefaults.DefaultProjectId);
        var context = new ProjectContext(catalog);
        ProjectContextChangedEventArgs? observed = null;
        var eventCount = 0;
        context.Changed += (_, args) =>
        {
            eventCount++;
            observed = args;
        };

        var result = context.Activate(project.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(project.Id, context.ActiveProject.Id);
        Assert.Equal(1, eventCount);
        Assert.Equal(ProjectCatalogDefaults.DefaultProjectId, observed?.PreviousProject.Id);
        Assert.Equal(project.Id, observed?.ActiveProject.Id);
        Assert.Equal(project.Id, new JsonProjectCatalog(new PortablePathResolver(_testRoot)).ActiveProjectId);
    }

    [Theory]
    [InlineData(ProjectSwitchBlockReason.ProjectOperation)]
    [InlineData(ProjectSwitchBlockReason.InteractiveTerminal)]
    public void Activate_refuses_switch_while_project_scoped_work_is_active(ProjectSwitchBlockReason reason)
    {
        var catalog = CreateCatalog();
        var project = AddProject(catalog, "second");
        catalog.SetActive(ProjectCatalogDefaults.DefaultProjectId);
        var context = new ProjectContext(catalog);
        context.SetBlockReasonProvider(() => reason);
        var eventCount = 0;
        context.Changed += (_, _) => eventCount++;

        var result = context.Activate(project.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(reason, result.FailureReason);
        Assert.Equal(ProjectCatalogDefaults.DefaultProjectId, context.ActiveProject.Id);
        Assert.Equal(0, eventCount);
    }

    [Fact]
    public void Activating_current_project_is_idempotent_even_when_switching_is_blocked()
    {
        var context = new ProjectContext(CreateCatalog());
        context.SetBlockReasonProvider(() => ProjectSwitchBlockReason.InteractiveTerminal);
        var eventCount = 0;
        context.Changed += (_, _) => eventCount++;

        var result = context.Activate(ProjectCatalogDefaults.DefaultProjectId);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProjectSwitchBlockReason.None, result.FailureReason);
        Assert.Equal(0, eventCount);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private JsonProjectCatalog CreateCatalog() => new(new PortablePathResolver(_testRoot));

    private static PortableProject AddProject(JsonProjectCatalog catalog, string id)
    {
        var project = new PortableProject(
            id,
            "Second",
            Path.Combine("instances", "default", "projects", id));
        catalog.Add(project, makeActive: false);
        return project;
    }
}
