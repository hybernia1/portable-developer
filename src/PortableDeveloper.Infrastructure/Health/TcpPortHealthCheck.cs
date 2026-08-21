using System.Diagnostics;
using System.Net.Sockets;
using PortableDeveloper.Application.Health;

namespace PortableDeveloper.Infrastructure.Health;

public sealed class TcpPortHealthCheck : ITcpPortHealthCheck
{
    public async Task<HealthCheckResult> CheckAsync(
        string host,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(port);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(port, 65535);

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout must be positive.");
        }

        var stopwatch = Stopwatch.StartNew();
        using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(timeout);
        using var client = new TcpClient();

        try
        {
            await client.ConnectAsync(host, port, timeoutCancellation.Token);
            return new HealthCheckResult(true, stopwatch.Elapsed, $"TCP connection to {host}:{port} succeeded.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new HealthCheckResult(false, stopwatch.Elapsed, $"TCP connection to {host}:{port} timed out.");
        }
        catch (SocketException exception)
        {
            return new HealthCheckResult(false, stopwatch.Elapsed, exception.Message);
        }
    }
}
