using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Workspace;
using PortableDeveloper.Infrastructure.Projects;
using PortableDeveloper.Application.Workspace;

namespace PortableDeveloper.Tests;

public sealed class WorkspaceFileManagerTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void File_operations_support_normal_project_workflow()
    {
        var service = new WorkspaceFileManager(new PortablePathResolver(_testRoot));

        service.CreateDirectory(string.Empty, "src");
        service.CreateFile("src", "index.php");
        service.Rename("src/index.php", "home.php");

        var file = Assert.Single(service.List("src"));
        Assert.Equal("home.php", file.Name);
        Assert.False(file.IsDirectory);
        Assert.True(file.IsSafe);

        service.Delete("src");
        Assert.Equal(["public", "seldownloads"], service.List(string.Empty).Select(entry => entry.Name));
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
        var defaultEntries = service.List(string.Empty);
        Assert.Contains(defaultEntries, entry => entry.Name == "default.txt");
        Assert.Contains(defaultEntries, entry => entry.Name == "seldownloads" && entry.IsDirectory);
    }

    [Fact]
    public void List_page_bounds_results_and_uses_natural_name_sorting()
    {
        var service = new WorkspaceFileManager(new PortablePathResolver(_testRoot));
        foreach (var name in new[] { "file10.php", "file2.php", "file1.php", "notes.md" })
        {
            service.CreateFile(string.Empty, name);
        }

        var first = service.ListPage(new WorkspacePageRequest(string.Empty, 1, 3));
        var second = service.ListPage(new WorkspacePageRequest(string.Empty, 2, 3));

        Assert.Equal(6, first.TotalCount);
        Assert.Equal(2, first.TotalPages);
        Assert.Equal(["public", "seldownloads", "file1.php"], first.Entries.Select(entry => entry.Name));
        Assert.Equal(["file2.php", "file10.php", "notes.md"], second.Entries.Select(entry => entry.Name));
        Assert.Equal(WorkspaceFileKind.Php, second.Entries[0].FileKind);
        Assert.Equal(WorkspaceFileKind.Markdown, second.Entries[2].FileKind);
    }

    [Fact]
    public void Normalize_directory_accepts_project_relative_navigation_but_refuses_escape()
    {
        var service = new WorkspaceFileManager(new PortablePathResolver(_testRoot));
        service.CreateDirectory("public", "assets");

        Assert.Equal("public/assets", service.NormalizeDirectory("public/./assets"));
        Assert.Equal("public", service.NormalizeDirectory("public/assets/.."));
        Assert.Throws<ArgumentException>(() => service.NormalizeDirectory("../../outside"));
    }

    [Fact]
    public void List_classifies_common_workspace_file_types()
    {
        var service = new WorkspaceFileManager(new PortablePathResolver(_testRoot));
        foreach (var name in new[] { "index.html", "tool.exe", "preview.png", "package.json", "notes.md", "worker.py", "server.jar", "notes.txt", "report.docx", "budget.xlsx", "import.csv" })
        {
            service.CreateFile(string.Empty, name);
        }

        var kinds = service.List(string.Empty).ToDictionary(entry => entry.Name, entry => entry.FileKind);

        Assert.Equal(WorkspaceFileKind.Html, kinds["index.html"]);
        Assert.Equal(WorkspaceFileKind.Executable, kinds["tool.exe"]);
        Assert.Equal(WorkspaceFileKind.Image, kinds["preview.png"]);
        Assert.Equal(WorkspaceFileKind.Json, kinds["package.json"]);
        Assert.Equal(WorkspaceFileKind.Markdown, kinds["notes.md"]);
        Assert.Equal(WorkspaceFileKind.Python, kinds["worker.py"]);
        Assert.Equal(WorkspaceFileKind.Archive, kinds["server.jar"]);
        Assert.Equal(WorkspaceFileKind.Text, kinds["notes.txt"]);
        Assert.Equal(WorkspaceFileKind.Document, kinds["report.docx"]);
        Assert.Equal(WorkspaceFileKind.Spreadsheet, kinds["budget.xlsx"]);
        Assert.Equal(WorkspaceFileKind.Spreadsheet, kinds["import.csv"]);
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }
}
