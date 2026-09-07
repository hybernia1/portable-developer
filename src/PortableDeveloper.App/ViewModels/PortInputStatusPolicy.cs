using PortableDeveloper.Application.Ports;

namespace PortableDeveloper.App.ViewModels;

public static class PortInputStatusPolicy
{
    public static string Resolve(
        UiText text,
        int port,
        IReadOnlyCollection<int> proposedPorts,
        int currentPort,
        bool ownedByApplication,
        IReadOnlyCollection<TcpPortListenerInfo> listeners,
        Func<int, bool> isAvailable)
    {
        if (port is < PortSettingsValidator.MinimumPort or > PortSettingsValidator.MaximumPort)
        {
            return text.PortInvalid;
        }

        if (proposedPorts.Count(candidate => candidate == port) > 1)
        {
            return text.PortDuplicate;
        }

        if (ownedByApplication && port == currentPort)
        {
            return text.PortUsedByApplication;
        }

        return listeners.Any(listener => listener.Port == port) || !isAvailable(port)
            ? text.PortOccupied
            : text.PortAvailable;
    }
}
