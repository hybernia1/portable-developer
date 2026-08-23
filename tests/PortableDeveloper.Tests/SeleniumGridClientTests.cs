using System.Net;
using System.Text;
using PortableDeveloper.Infrastructure.Selenium;

namespace PortableDeveloper.Tests;

public sealed class SeleniumGridClientTests
{
    [Fact]
    public async Task ListSessionsAsync_parses_graphql_session_details()
    {
        var handler = new RecordingHandler(request => request.RequestUri!.AbsolutePath switch
        {
            "/graphql" => Json(HttpStatusCode.OK,
                """
                {"data":{"sessionsInfo":{"sessions":[{"id":"abc123","capabilities":"{\"browserName\":\"firefox\",\"browserVersion\":\"152.0\",\"platformName\":\"windows\"}","startTime":"2026-08-22T10:00:00Z","sessionDurationMillis":12500}]}}}
                """),
            _ => Json(HttpStatusCode.NotFound, "{}")
        });
        var client = new SeleniumGridClient(new HttpClient(handler));

        var sessions = await client.ListSessionsAsync(4444);

        var session = Assert.Single(sessions);
        Assert.Equal("abc123", session.Id);
        Assert.Equal("firefox", session.BrowserName);
        Assert.Equal("152.0", session.BrowserVersion);
        Assert.Equal(TimeSpan.FromSeconds(12.5), session.Duration);
    }

    [Fact]
    public async Task ListSessionsAsync_accepts_duration_returned_as_a_string()
    {
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.OK,
            """
            {"data":{"sessionsInfo":{"sessions":[{"id":"string-duration","capabilities":{"browserName":"chrome"},"startTime":"2026-08-23T10:00:00Z","sessionDurationMillis":"9876"}]}}}
            """));
        var client = new SeleniumGridClient(new HttpClient(handler));

        var session = Assert.Single(await client.ListSessionsAsync(4444));

        Assert.Equal(TimeSpan.FromMilliseconds(9876), session.Duration);
        Assert.Equal(DateTimeOffset.Parse("2026-08-23T10:00:00Z"), session.StartedAtUtc);
    }

    [Fact]
    public async Task ListSessionsAsync_uses_safe_defaults_for_unexpected_optional_value_types()
    {
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.OK,
            """
            {"data":{"sessionsInfo":{"sessions":[{"id":"unexpected-values","capabilities":null,"startTime":42,"sessionDurationMillis":{"value":1}}]}}}
            """));
        var client = new SeleniumGridClient(new HttpClient(handler));

        var session = Assert.Single(await client.ListSessionsAsync(4444));

        Assert.Null(session.StartedAtUtc);
        Assert.Equal(TimeSpan.Zero, session.Duration);
        Assert.Equal("unknown", session.BrowserName);
    }

    [Fact]
    public async Task TerminateSessionAsync_uses_standard_webdriver_delete_endpoint()
    {
        var handler = new RecordingHandler(_ => Json(HttpStatusCode.OK, "{\"value\":null}"));
        var client = new SeleniumGridClient(new HttpClient(handler));

        var result = await client.TerminateSessionAsync(4444, "abc-123");

        Assert.True(result.IsSuccess);
        Assert.Equal(HttpMethod.Delete, handler.LastRequest!.Method);
        Assert.Equal("/session/abc-123", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    private static HttpResponseMessage Json(HttpStatusCode status, string content) => new(status)
    {
        Content = new StringContent(content, Encoding.UTF8, "application/json")
    };

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(responseFactory(request));
        }
    }
}
