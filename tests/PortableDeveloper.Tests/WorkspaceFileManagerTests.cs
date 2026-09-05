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
        var service = CreateService();

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
    public void Copy_and_move_support_files_directories_and_same_folder_copy_names()
    {
        var service = CreateService();
        service.CreateDirectory(string.Empty, "src");
        service.CreateFile("src", "index.php");

        var conflictResolverCalled = false;
        Assert.True(service.Move(
            "src/index.php",
            "src",
            " - copy",
            _ =>
            {
                conflictResolverCalled = true;
                return new WorkspaceConflictDecision(WorkspaceConflictAction.Overwrite);
            }));
        Assert.False(conflictResolverCalled);

        service.Copy("src", string.Empty, " - copy", RenameConflict);
        Assert.Contains(service.List(string.Empty), entry => entry.Name == "src - copy" && entry.IsDirectory);
        Assert.Contains(service.List("src - copy"), entry => entry.Name == "index.php" && !entry.IsDirectory);

        service.Copy("src/index.php", "src", " - copy", RenameConflict);
        service.Move("src - copy/index.php", "public", " - copy", OverwriteConflict);

        Assert.Contains(service.List("src"), entry => entry.Name == "index - copy.php" && !entry.IsDirectory);
        Assert.Contains(service.List("public"), entry => entry.Name == "index.php" && !entry.IsDirectory);
        Assert.Empty(service.List("src - copy"));
    }

    [Fact]
    public void Copy_and_move_refuse_a_directory_destination_inside_itself()
    {
        var service = CreateService();
        service.CreateDirectory(string.Empty, "src");
        service.CreateDirectory("src", "nested");

        Assert.Throws<IOException>(() => service.Copy("src", "src/nested", " - copy", RenameConflict));
        Assert.Throws<IOException>(() => service.Move("src", "src/nested", " - copy", OverwriteConflict));
    }

    [Fact]
    public void Drag_import_and_export_copy_external_files_and_directories_inside_the_project()
    {
        var service = CreateService();
        var incoming = Path.Combine(_testRoot, "incoming");
        Directory.CreateDirectory(Path.Combine(incoming, "assets"));
        File.WriteAllText(Path.Combine(incoming, "readme.md"), "hello");
        File.WriteAllText(Path.Combine(incoming, "assets", "site.css"), "body {}");

        service.Import(
            [Path.Combine(incoming, "readme.md"), Path.Combine(incoming, "assets")],
            "public",
            " - copy",
            RenameConflict);

        var importedFile = Assert.Single(service.List("public"), entry => entry.Name == "readme.md");
        var importedDirectory = Assert.Single(service.List("public"), entry => entry.Name == "assets" && entry.IsDirectory);
        Assert.Equal(
            Path.GetFullPath(Path.Combine(_testRoot, service.RootRelativePath, importedFile.RelativePath)),
            service.GetExportPath(importedFile.RelativePath));
        Assert.True(service.TryGetRelativePath(service.GetExportPath(importedFile.RelativePath), out var exportedRelativePath));
        Assert.Equal(importedFile.RelativePath, exportedRelativePath);
        Assert.Equal(
            Path.GetFullPath(Path.Combine(_testRoot, service.RootRelativePath, importedDirectory.RelativePath)),
            service.GetExportPath(importedDirectory.RelativePath));
        Assert.Contains(service.List("public/assets"), entry => entry.Name == "site.css");
    }

    [Fact]
    public void Import_conflicts_can_overwrite_merge_rename_or_skip()
    {
        var service = CreateService();
        var incoming = Path.Combine(_testRoot, "conflicts");
        Directory.CreateDirectory(Path.Combine(incoming, "assets"));
        File.WriteAllText(Path.Combine(incoming, "assets", "site.css"), "new");
        service.CreateDirectory("public", "assets");
        service.CreateFile("public/assets", "site.css");
        service.CreateFile("public/assets", "keep.txt");
        File.WriteAllText(service.GetExportPath("public/assets/site.css"), "old");

        var overwritten = service.Import(
            [Path.Combine(incoming, "assets")],
            "public",
            " - copy",
            OverwriteConflict);
        var renamed = service.Import(
            [Path.Combine(incoming, "assets")],
            "public",
            " - copy",
            RenameConflict);
        var skipped = service.Import(
            [Path.Combine(incoming, "assets")],
            "public",
            " - copy",
            _ => new WorkspaceConflictDecision(WorkspaceConflictAction.Skip));

        Assert.Equal(1, overwritten);
        Assert.Equal(1, renamed);
        Assert.Equal(0, skipped);
        Assert.Equal("new", File.ReadAllText(service.GetExportPath("public/assets/site.css")));
        Assert.Contains(service.List("public/assets"), entry => entry.Name == "keep.txt");
        Assert.Contains(service.List("public"), entry => entry.Name == "assets - copy" && entry.IsDirectory);
    }

    [Fact]
    public void Operations_refuse_escape_and_project_root_deletion()
    {
        var service = CreateService();

        Assert.Throws<ArgumentException>(() => service.List("../state"));
        Assert.Throws<ArgumentException>(() => service.CreateFile(string.Empty, "../app.exe"));
        Assert.Throws<InvalidOperationException>(() => service.Delete(string.Empty));
    }

    [Fact]
    public void File_operations_follow_active_project_without_exposing_other_projects()
    {
        var paths = new PortablePathResolver(_testRoot);
        var projects = new JsonWebProjectCatalog(paths);
        var service = new WorkspaceFileManager(paths, CreateContext(projects));
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
        var service = CreateService();
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
        var service = CreateService();
        service.CreateDirectory("public", "assets");

        Assert.Equal("public/assets", service.NormalizeDirectory("public/./assets"));
        Assert.Equal("public", service.NormalizeDirectory("public/assets/.."));
        Assert.Throws<ArgumentException>(() => service.NormalizeDirectory("../../outside"));
    }

    [Fact]
    public void List_classifies_common_workspace_file_types()
    {
        var service = CreateService();
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

    private WorkspaceFileManager CreateService()
    {
        var paths = new PortablePathResolver(_testRoot);
        return new WorkspaceFileManager(paths, CreateContext(new JsonWebProjectCatalog(paths)));
    }

    private static ProjectContext CreateContext(JsonWebProjectCatalog projects) =>
        new(new LegacyWebProjectCatalogAdapter(projects));

    private static WorkspaceConflictDecision RenameConflict(WorkspaceConflict _) =>
        new(WorkspaceConflictAction.Rename);

    private static WorkspaceConflictDecision OverwriteConflict(WorkspaceConflict _) =>
        new(WorkspaceConflictAction.Overwrite);
}
