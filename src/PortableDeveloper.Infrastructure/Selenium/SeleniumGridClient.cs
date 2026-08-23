using System.Net.Http.Json;
using System.Globalization;
using System.Text.Json;
using PortableDeveloper.Application.Selenium;

namespace PortableDeveloper.Infrastructure.Selenium;

public sealed class SeleniumGridClient : ISeleniumGridClient
{
    private const string SessionsQuery = "{ sessionsInfo { sessions { id capabilities startTime sessionDurationMillis } } }";
    private readonly HttpClient _client;

    public SeleniumGridClient(HttpClient? client = null)
    {
        _client = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
    }

    public async Task<bool> IsReadyAsync(int port, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _client.GetAsync(BuildUri(port, "/status"), cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
            return document.RootElement.TryGetProperty("value", out var value) &&
                value.TryGetProperty("ready", out var ready) &&
                ready.ValueKind == JsonValueKind.True;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            return false;
        }
    }

    public async Task<IReadOnlyList<SeleniumSessionInfo>> ListSessionsAsync(
        int port,
        CancellationToken cancellationToken = default)
    {
        using var response = await _client.PostAsJsonAsync(
            BuildUri(port, "/graphql"),
            new { query = SessionsQuery },
            cancellationToken);
        response.EnsureSuccessStatusCode();
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        var root = document.RootElement;
        if (root.TryGetProperty("errors", out var errors))
        {
            throw new InvalidDataException($"Selenium GraphQL returned an error: {errors}");
        }

        if (!root.TryGetProperty("data", out var data) ||
            !data.TryGetProperty("sessionsInfo", out var sessionsInfo) ||
            !sessionsInfo.TryGetProperty("sessions", out var sessions) ||
            sessions.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("Selenium did not return a readable session list.");
        }

        return sessions.EnumerateArray().Select(ParseSession).ToArray();
    }

    public async Task<SeleniumOperationResult> TerminateSessionAsync(
        int port,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId) || sessionId.Length > 128 ||
            sessionId.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '-' and not '_'))
        {
            return SeleniumOperationResult.Failure("The Selenium session ID is invalid.");
        }

        using var response = await _client.DeleteAsync(
            BuildUri(port, $"/session/{Uri.EscapeDataString(sessionId)}"),
            cancellationToken);
        return response.IsSuccessStatusCode
            ? SeleniumOperationResult.Success()
            : SeleniumOperationResult.Failure($"Selenium returned HTTP {(int)response.StatusCode} while terminating the session.");
    }

    private static SeleniumSessionInfo ParseSession(JsonElement session)
    {
        var id = GetString(session, "id", "unknown");
        var capabilities = ParseCapabilities(session);
        var browserName = GetString(capabilities, "browserName", "unknown");
        var browserVersion = GetString(capabilities, "browserVersion", string.Empty);
        var platformName = GetString(capabilities, "platformName", "Windows");
        var startedAt = ParseStartTime(session);
        var duration = TimeSpan.FromMilliseconds(Math.Max(0, ParseDurationMillis(session)));
        return new(id, browserName, browserVersion, platformName, startedAt, duration);
    }

    private static DateTimeOffset? ParseStartTime(JsonElement session)
    {
        if (!session.TryGetProperty("startTime", out var startTime) ||
            startTime.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        return DateTimeOffset.TryParse(
            startTime.GetString(),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var parsedStart)
                ? parsedStart
                : null;
    }

    private static long ParseDurationMillis(JsonElement session)
    {
        if (!session.TryGetProperty("sessionDurationMillis", out var duration))
        {
            return 0;
        }

        if (duration.ValueKind == JsonValueKind.Number && duration.TryGetInt64(out var numericValue))
        {
            return numericValue;
        }

        return duration.ValueKind == JsonValueKind.String &&
            long.TryParse(duration.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var stringValue)
                ? stringValue
                : 0;
    }

    private static JsonElement ParseCapabilities(JsonElement session)
    {
        if (!session.TryGetProperty("capabilities", out var capabilities))
        {
            return default;
        }

        if (capabilities.ValueKind == JsonValueKind.Object)
        {
            return capabilities.Clone();
        }

        if (capabilities.ValueKind == JsonValueKind.String)
        {
            try
            {
                using var document = JsonDocument.Parse(capabilities.GetString() ?? "{}");
                return document.RootElement.Clone();
            }
            catch (JsonException)
            {
                return default;
            }
        }

        return default;
    }

    private static string GetString(JsonElement element, string propertyName, string fallback) =>
        element.ValueKind == JsonValueKind.Object &&
        element.TryGetProperty(propertyName, out var property) &&
        property.ValueKind == JsonValueKind.String
            ? property.GetString() ?? fallback
            : fallback;

    private static Uri BuildUri(int port, string path) => new($"http://127.0.0.1:{port}{path}");
}
