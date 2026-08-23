using System.Text.Json;
using System.Text.Json.Serialization;
using PortableDeveloper.Application.Abstractions;

namespace PortableDeveloper.Infrastructure.Logging;

/// <summary>
/// Writes structured runtime events to a portable JSON Lines file below logs/.
/// </summary>
public sealed class JsonLinesApplicationLogger : IApplicationLogger, IAsyncDisposable
{
    private const long DefaultMaximumFileBytes = 10 * 1024 * 1024;
    private const long DefaultMaximumTotalBytes = 100 * 1024 * 1024;
    private const int DefaultRetentionDays = 14;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
    private readonly IPortablePathResolver _paths;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private readonly long _maximumFileBytes;
    private readonly long _maximumTotalBytes;
    private readonly TimeSpan _retention;

    public JsonLinesApplicationLogger(
        IPortablePathResolver paths,
        long maximumFileBytes = DefaultMaximumFileBytes,
        long maximumTotalBytes = DefaultMaximumTotalBytes,
        int retentionDays = DefaultRetentionDays)
    {
        _paths = paths;
        _maximumFileBytes = maximumFileBytes > 0
            ? maximumFileBytes
            : throw new ArgumentOutOfRangeException(nameof(maximumFileBytes));
        _maximumTotalBytes = maximumTotalBytes >= maximumFileBytes
            ? maximumTotalBytes
            : throw new ArgumentOutOfRangeException(nameof(maximumTotalBytes));
        _retention = retentionDays > 0
            ? TimeSpan.FromDays(retentionDays)
            : throw new ArgumentOutOfRangeException(nameof(retentionDays));
    }

    public async ValueTask LogAsync(
        ApplicationLogLevel level,
        string component,
        string eventName,
        string message,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(component);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);
        ArgumentNullException.ThrowIfNull(message);

        var timestamp = DateTimeOffset.UtcNow;
        var logDirectory = _paths.EnsureDirectory("logs");
        var entry = new RuntimeLogEntry(timestamp, level, component, eventName, message);
        var line = JsonSerializer.Serialize(entry, SerializerOptions) + Environment.NewLine;
        var lineByteCount = System.Text.Encoding.UTF8.GetByteCount(line);

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            PruneLogs(logDirectory, timestamp, lineByteCount);
            var logPath = SelectLogPath(logDirectory, timestamp, lineByteCount);
            await File.AppendAllTextAsync(logPath, line, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public ValueTask DisposeAsync()
    {
        _writeLock.Dispose();
        return ValueTask.CompletedTask;
    }

    private string SelectLogPath(string logDirectory, DateTimeOffset timestamp, int lineByteCount)
    {
        var stem = $"portable-developer-{timestamp:yyyy-MM-dd}";
        var basePath = Path.Combine(logDirectory, $"{stem}.jsonl");
        if (CanAppend(basePath, lineByteCount))
        {
            return basePath;
        }

        for (var segment = 1; segment < int.MaxValue; segment++)
        {
            var segmentPath = Path.Combine(logDirectory, $"{stem}-{segment:000}.jsonl");
            if (CanAppend(segmentPath, lineByteCount))
            {
                return segmentPath;
            }
        }

        throw new IOException("A new portable log segment could not be allocated.");
    }

    private bool CanAppend(string path, int lineByteCount) =>
        !File.Exists(path) || new FileInfo(path).Length + lineByteCount <= _maximumFileBytes;

    private void PruneLogs(string logDirectory, DateTimeOffset timestamp, int incomingByteCount)
    {
        var logFiles = Directory.EnumerateFiles(logDirectory, "portable-developer-*.jsonl", SearchOption.TopDirectoryOnly)
            .Select(path => new FileInfo(path))
            .OrderBy(file => file.LastWriteTimeUtc)
            .ToList();
        var oldestAllowed = timestamp.UtcDateTime - _retention;

        foreach (var file in logFiles.Where(file => file.LastWriteTimeUtc < oldestAllowed).ToArray())
        {
            TryDelete(file);
            logFiles.Remove(file);
        }

        var totalBytes = logFiles.Sum(file => file.Exists ? file.Length : 0);
        foreach (var file in logFiles)
        {
            if (totalBytes + incomingByteCount <= _maximumTotalBytes)
            {
                break;
            }

            var length = file.Exists ? file.Length : 0;
            if (TryDelete(file))
            {
                totalBytes -= length;
            }
        }
    }

    private static bool TryDelete(FileInfo file)
    {
        try
        {
            file.Delete();
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private sealed record RuntimeLogEntry(
        DateTimeOffset TimestampUtc,
        ApplicationLogLevel Level,
        string Component,
        string EventName,
        string Message);
}
