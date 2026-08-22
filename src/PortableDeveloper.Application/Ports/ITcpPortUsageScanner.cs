namespace PortableDeveloper.Application.Ports;

public interface ITcpPortUsageScanner
{
    IReadOnlyList<TcpPortListenerInfo> Scan();

    bool IsAvailable(int port);
}
