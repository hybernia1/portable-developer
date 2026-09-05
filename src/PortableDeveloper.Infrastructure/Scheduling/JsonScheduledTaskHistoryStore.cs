using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Scheduling;

namespace PortableDeveloper.Infrastructure.Scheduling;

public sealed class JsonScheduledTaskHistoryStore : IScheduledTaskHistoryStore
{
    private const int MaximumRetainedRecords = 200;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly IPortablePathResolver _paths;
    private readonly object _sync = new();

    public JsonScheduledTaskHistoryStore(IPortablePathResolver paths)
    {
        _paths = paths;
    }

    public IReadOnlyList<ScheduledTaskRunRecord> ReadRecent(int maximumCount = MaximumRetainedRecords)
    {
        maximumCount = Math.Clamp(maximumCount, 1, MaximumRetainedRecords);
        lock (_sync)
        {
            return Load().Records
                .OrderByDescending(record => record.StartedAtUtc)
                .Take(maximumCount)
                .ToArray();
        }
    }

    public void Append(ScheduledTaskRunRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        lock (_sync)
        {
            var document = Load();
            var records = document.Records
                .Append(record)
                .OrderByDescending(item => item.StartedAtUtc)
                .Take(MaximumRetainedRecords)
                .ToArray();
            Save(new(1, records));
        }
    }

    private HistoryDocument Load()
    {
        var path = GetPath();
        ScheduledTaskStoragePaths.RefuseReparsePoint(path);
        if (!File.Exists(path))
        {
            return new(1, []);
        }

        try
        {
            var document = JsonSerializer.Deserialize<HistoryDocument>(File.ReadAllText(path), SerializerOptions);
            return document is { SchemaVersion: 1, Records: not null } ? document : new(1, []);
        }
        catch (Exception exception) when (exception is JsonException or IOException)
        {
            return new(1, []);
        }
    }

    private void Save(HistoryDocument document)
    {
        var path = GetPath();
        var temporaryPath = path + ".part";
        ScheduledTaskStoragePaths.RefuseReparsePoint(path);
        ScheduledTaskStoragePaths.RefuseReparsePoint(temporaryPath);
        if (File.Exists(temporaryPath))
        {
            File.Delete(temporaryPath);
        }

        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(document, SerializerOptions), new UTF8Encoding(false));
        File.Move(temporaryPath, path, overwrite: true);
    }

    private string GetPath()
    {
        var directory = ScheduledTaskStoragePaths.EnsureSafeDirectory(
            _paths,
            Path.Combine("instances", "default", "scheduler"));
        return Path.Combine(directory, "history.json");
    }

    private sealed record HistoryDocument(int SchemaVersion, IReadOnlyList<ScheduledTaskRunRecord> Records);
}
