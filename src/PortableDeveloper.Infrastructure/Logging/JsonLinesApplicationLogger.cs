using System.Text.Json;
using System.Text.Json.Serialization;
using PortableDeveloper.Application.Abstractions;

namespace PortableDeveloper.Infrastructure.Logging;

/// <summary>
/// Writes structured runtime events to a portable JSON Lines file below logs/.
/// </summary>
public sealed class JsonLinesApplicationLogger : IApplicationLogger, IAsyncDisposable
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
    private readonly IPortablePathResolver _paths;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public JsonLinesApplicationLogger(IPortablePathResolver paths)
    {
        _paths = paths;
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
        var logPath = Path.Combine(logDirectory, $"portable-developer-{timestamp:yyyy-MM-dd}.jsonl");
        var entry = new RuntimeLogEntry(timestamp, level, component, eventName, message);
        var line = JsonSerializer.Serialize(entry, SerializerOptions) + Environment.NewLine;

        await _writeLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
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

    private sealed record RuntimeLogEntry(
        DateTimeOffset TimestampUtc,
        ApplicationLogLevel Level,
        string Component,
        string EventName,
        string Message);
}
