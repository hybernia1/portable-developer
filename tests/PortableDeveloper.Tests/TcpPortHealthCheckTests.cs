using System.Net;
using System.Net.Sockets;
using PortableDeveloper.Infrastructure.Health;

namespace PortableDeveloper.Tests;

public sealed class TcpPortHealthCheckTests
{
    [Fact]
    public async Task CheckAsync_returns_healthy_for_listening_port()
    {
        var listener = new TcpListener(IPAddress.Loopback, port: 0);
        listener.Start();

        try
        {
            var port = ((IPEndPoint)listener.LocalEndpoint).Port;
            var healthCheck = new TcpPortHealthCheck();

            var result = await healthCheck.CheckAsync("127.0.0.1", port, TimeSpan.FromSeconds(1));

            Assert.True(result.IsHealthy, result.Detail);
        }
        finally
        {
            listener.Stop();
        }
    }

    [Fact]
    public async Task CheckAsync_returns_unhealthy_after_timeout_or_connection_refusal()
    {
        var listener = new TcpListener(IPAddress.Loopback, port: 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        var healthCheck = new TcpPortHealthCheck();

        var result = await healthCheck.CheckAsync("127.0.0.1", port, TimeSpan.FromSeconds(1));

        Assert.False(result.IsHealthy);
    }
}
