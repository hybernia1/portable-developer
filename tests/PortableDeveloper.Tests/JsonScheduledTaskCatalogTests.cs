using PortableDeveloper.Application.Scheduling;
using PortableDeveloper.Infrastructure.Paths;
using PortableDeveloper.Infrastructure.Scheduling;

namespace PortableDeveloper.Tests;

public sealed class JsonScheduledTaskCatalogTests : IDisposable
{
    private readonly string _testRoot = Path.Combine(Path.GetTempPath(), $"PortableDeveloperTests-{Guid.NewGuid():N}");

    [Fact]
    public void Catalog_persists_valid_tasks_inside_the_portable_instance()
    {
        var paths = new PortablePathResolver(_testRoot);
        var catalog = new JsonScheduledTaskCatalog(paths);
        var task = CreateTask();

        catalog.Add(task);

        var reloaded = new JsonScheduledTaskCatalog(paths).GetRequired(task.Id);
        Assert.Equal(task, reloaded);
        Assert.True(File.Exists(Path.Combine(_testRoot, "instances", "default", "config", "scheduled-tasks.json")));
    }

    [Fact]
    public void Catalog_update_and_remove_are_persisted()
    {
        var paths = new PortablePathResolver(_testRoot);
        var catalog = new JsonScheduledTaskCatalog(paths);
        var task = CreateTask();
        catalog.Add(task);

        catalog.Update(task with { Name = "Updated", IsEnabled = false });
        Assert.Equal("Updated", new JsonScheduledTaskCatalog(paths).GetRequired(task.Id).Name);

        catalog.Remove(task.Id);
        Assert.Empty(new JsonScheduledTaskCatalog(paths).Tasks);
    }

    [Theory]
    [InlineData("../outside.py")]
    [InlineData("C:\\outside.py")]
    [InlineData("scripts//job.py")]
    public void Catalog_rejects_nonportable_script_paths(string target)
    {
        var catalog = new JsonScheduledTaskCatalog(new PortablePathResolver(_testRoot));

        Assert.Throws<ArgumentException>(() => catalog.Add(CreateTask() with { Target = target }));
        Assert.Empty(catalog.Tasks);
    }

    [Fact]
    public void Catalog_recovers_to_empty_state_from_malformed_json()
    {
        var config = Path.Combine(_testRoot, "instances", "default", "config");
        Directory.CreateDirectory(config);
        File.WriteAllText(Path.Combine(config, "scheduled-tasks.json"), "{broken");

        var catalog = new JsonScheduledTaskCatalog(new PortablePathResolver(_testRoot));

        Assert.Empty(catalog.Tasks);
    }

    [Fact]
    public void Catalog_rejects_a_reparse_point_in_its_storage_path()
    {
        var outside = Path.Combine(_testRoot, "outside");
        Directory.CreateDirectory(outside);
        Directory.CreateDirectory(_testRoot);
        try
        {
            Directory.CreateSymbolicLink(Path.Combine(_testRoot, "instances"), outside);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return;
        }

        Assert.Throws<InvalidDataException>(() => new JsonScheduledTaskCatalog(new PortablePathResolver(_testRoot)));
        Assert.False(File.Exists(Path.Combine(outside, "default", "config", "scheduled-tasks.json")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            Directory.Delete(_testRoot, recursive: true);
        }
    }

    private static PortableScheduledTask CreateTask() => new(
        "nightly",
        "default",
        "Nightly task",
        ScheduledTaskCommandKind.PythonScript,
        Path.Combine("scripts", "nightly.py"),
        "--quiet",
        new ScheduledTaskSchedule(ScheduledTaskScheduleKind.Daily, Hour: 23, Minute: 30));
}
