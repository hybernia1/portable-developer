using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Workspace;

namespace PortableDeveloper.Tests;

public sealed class WorkspaceFileManagerTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void File_operations_stay_inside_www_and_support_normal_project_workflow()
    {
        var service = new WorkspaceFileManager(new PortablePathResolver(_testRoot));

        service.CreateDirectory(string.Empty, "public");
        service.CreateFile("public", "index.php");
        service.Rename("public/index.php", "home.php");

        var entries = service.List("public");
        var file = Assert.Single(entries);
        Assert.Equal("home.php", file.Name);
        Assert.False(file.IsDirectory);
        Assert.True(file.IsSafe);

        service.Delete("public");
        Assert.Empty(service.List(string.Empty));
    }

    [Fact]
    public void Operations_refuse_escape_and_workspace_root_deletion()
    {
        var service = new WorkspaceFileManager(new PortablePathResolver(_testRoot));

        Assert.Throws<ArgumentException>(() => service.List("../state"));
        Assert.Throws<ArgumentException>(() => service.CreateFile(string.Empty, "../app.exe"));
        Assert.Throws<InvalidOperationException>(() => service.Delete(string.Empty));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }
}
