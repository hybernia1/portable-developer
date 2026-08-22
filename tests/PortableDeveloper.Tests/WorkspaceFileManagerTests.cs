using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Workspace;
using PortableDeveloper.Infrastructure.Projects;

namespace PortableDeveloper.Tests;

public sealed class WorkspaceFileManagerTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void File_operations_support_normal_project_workflow()
    {
        var service = new WorkspaceFileManager(new PortablePathResolver(_testRoot));

        service.CreateDirectory(string.Empty, "public");
        service.CreateFile("public", "index.php");
        service.Rename("public/index.php", "home.php");

        var file = Assert.Single(service.List("public"));
        Assert.Equal("home.php", file.Name);
        Assert.False(file.IsDirectory);
        Assert.True(file.IsSafe);

        service.Delete("public");
        Assert.Empty(service.List(string.Empty));
    }

    [Fact]
    public void Operations_refuse_escape_and_project_root_deletion()
    {
        var service = new WorkspaceFileManager(new PortablePathResolver(_testRoot));

        Assert.Throws<ArgumentException>(() => service.List("../state"));
        Assert.Throws<ArgumentException>(() => service.CreateFile(string.Empty, "../app.exe"));
        Assert.Throws<InvalidOperationException>(() => service.Delete(string.Empty));
    }

    [Fact]
    public void File_operations_follow_active_project_without_exposing_other_projects()
    {
        var paths = new PortablePathResolver(_testRoot);
        var projects = new JsonWebProjectCatalog(paths);
        var service = new WorkspaceFileManager(paths, projects);
        service.CreateFile(string.Empty, "default.txt");
        var second = projects.Create("Second app");

        service.CreateFile(string.Empty, "second.txt");

        Assert.Equal(second.ProjectRootRelativePath, service.RootRelativePath);
        Assert.Contains(service.List(string.Empty), entry => entry.Name == "second.txt");
        Assert.DoesNotContain(service.List(string.Empty), entry => entry.Name == "default.txt");
        projects.SetActive("default");
        Assert.Equal("default.txt", Assert.Single(service.List(string.Empty)).Name);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }
}
