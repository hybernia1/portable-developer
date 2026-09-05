using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Scheduling;

namespace PortableDeveloper.Infrastructure.Scheduling;

public sealed class JsonScheduledTaskCatalog : IScheduledTaskCatalog
{
    private const int CurrentSchemaVersion = 1;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IPortablePathResolver _paths;
    private ScheduledTaskDocument _document;

    public JsonScheduledTaskCatalog(IPortablePathResolver paths)
    {
        _paths = paths;
        _document = Load();
    }

    public IReadOnlyList<PortableScheduledTask> Tasks => _document.Tasks;

    public PortableScheduledTask GetRequired(string taskId) =>
        _document.Tasks.FirstOrDefault(task => string.Equals(task.Id, taskId?.Trim(), StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException("The scheduled task does not exist.", nameof(taskId));

    public void Add(PortableScheduledTask task)
    {
        task = ScheduledTaskValidator.Validate(task);
        if (_document.Tasks.Any(existing => string.Equals(existing.Id, task.Id, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"A scheduled task with the ID '{task.Id}' already exists.");
        }

        Save(_document with { Tasks = _document.Tasks.Append(task).ToArray() });
    }

    public void Update(PortableScheduledTask task)
    {
        task = ScheduledTaskValidator.Validate(task);
        _ = GetRequired(task.Id);
        Save(_document with
        {
            Tasks = _document.Tasks.Select(existing =>
                string.Equals(existing.Id, task.Id, StringComparison.OrdinalIgnoreCase) ? task : existing).ToArray()
        });
    }

    public void Remove(string taskId)
    {
        var task = GetRequired(taskId);
        Save(_document with
        {
            Tasks = _document.Tasks.Where(existing =>
                !string.Equals(existing.Id, task.Id, StringComparison.OrdinalIgnoreCase)).ToArray()
        });
    }

    private ScheduledTaskDocument Load()
    {
        var path = GetPath();
        ScheduledTaskStoragePaths.RefuseReparsePoint(path);
        if (!File.Exists(path))
        {
            return new(CurrentSchemaVersion, []);
        }

        try
        {
            var document = JsonSerializer.Deserialize<ScheduledTaskDocument>(File.ReadAllText(path), SerializerOptions);
            return ValidateDocument(document);
        }
        catch (Exception exception) when (exception is JsonException or ArgumentException or InvalidDataException or IOException)
        {
            return new(CurrentSchemaVersion, []);
        }
    }

    private void Save(ScheduledTaskDocument document)
    {
        document = ValidateDocument(document);
        var path = GetPath();
        var temporaryPath = path + ".part";
        ScheduledTaskStoragePaths.RefuseReparsePoint(path);
        ScheduledTaskStoragePaths.RefuseReparsePoint(temporaryPath);
        if (File.Exists(temporaryPath))
        {
            File.Delete(temporaryPath);
        }

        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(document, SerializerOptions), new UTF8Encoding(false));
        var verified = JsonSerializer.Deserialize<ScheduledTaskDocument>(File.ReadAllText(temporaryPath), SerializerOptions);
        _ = ValidateDocument(verified);
        File.Move(temporaryPath, path, overwrite: true);
        _document = document;
    }

    private static ScheduledTaskDocument ValidateDocument(ScheduledTaskDocument? document)
    {
        if (document is null || document.SchemaVersion != CurrentSchemaVersion || document.Tasks is null)
        {
            throw new InvalidDataException("The scheduled task catalog is invalid.");
        }

        var tasks = document.Tasks.Select(ScheduledTaskValidator.Validate).ToArray();
        if (tasks.GroupBy(task => task.Id, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
        {
            throw new InvalidDataException("The scheduled task catalog contains duplicate identifiers.");
        }

        return document with { Tasks = tasks };
    }

    private string GetPath()
    {
        var relativeDirectory = Path.Combine("instances", "default", "config");
        var directory = ScheduledTaskStoragePaths.EnsureSafeDirectory(_paths, relativeDirectory);
        return Path.Combine(directory, "scheduled-tasks.json");
    }

    private sealed record ScheduledTaskDocument(int SchemaVersion, IReadOnlyList<PortableScheduledTask> Tasks);
}
