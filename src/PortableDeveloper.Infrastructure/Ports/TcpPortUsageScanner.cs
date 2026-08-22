using System.Net.NetworkInformation;
using System.Net;
using System.Net.Sockets;
using PortableDeveloper.Application.Ports;

namespace PortableDeveloper.Infrastructure.Ports;

public sealed class TcpPortUsageScanner : ITcpPortUsageScanner
{
    public IReadOnlyList<TcpPortListenerInfo> Scan() =>
        IPGlobalProperties.GetIPGlobalProperties()
            .GetActiveTcpListeners()
            .Select(endpoint => new TcpPortListenerInfo(endpoint.Address.ToString(), endpoint.Port))
            .Distinct()
            .OrderBy(listener => listener.Port)
            .ThenBy(listener => listener.Address, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public bool IsAvailable(int port)
    {
        if (port is < PortSettingsValidator.MinimumPort or > PortSettingsValidator.MaximumPort)
        {
            return false;
        }

        try
        {
            using var listener = new TcpListener(IPAddress.Loopback, port);
            listener.Start();
            return true;
        }
        catch (SocketException)
        {
            return false;
        }
    }
}
