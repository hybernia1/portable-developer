using System.Net;
using System.Net.Sockets;
using PortableDeveloper.Infrastructure.Ports;

namespace PortableDeveloper.Tests;

public sealed class TcpPortUsageScannerTests
{
    [Fact]
    public async Task Scan_reports_listener_without_changing_or_stopping_it()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        var scanner = new TcpPortUsageScanner();

        var listeners = scanner.Scan();

        Assert.Contains(listeners, item => item.Port == port);
        Assert.False(scanner.IsAvailable(port));
        using var client = new TcpClient();
        var accept = listener.AcceptTcpClientAsync();
        await client.ConnectAsync(IPAddress.Loopback, port);
        using var accepted = await accept;
        Assert.True(client.Connected);
    }

    [Fact]
    public void IsAvailable_accepts_a_released_ephemeral_port()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();

        Assert.True(new TcpPortUsageScanner().IsAvailable(port));
    }
}
