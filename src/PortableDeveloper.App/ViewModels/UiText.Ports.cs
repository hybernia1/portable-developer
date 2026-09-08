namespace PortableDeveloper.App.ViewModels;

public sealed partial class UiText
{
    public string ApplicationPortsTab => IsCzech ? "Porty aplikace" : "Application ports";

    public string ListeningPortsTab => IsCzech ? "Obsazené porty" : "Listening ports";

    public string CentralPortManager => IsCzech ? "Centrální nastavení portů" : "Central port settings";

    public string ApacheHttpPort => "Apache HTTP";

    public string PhpFastCgiPortLabel => "PHP FastCGI";

    public string MariaDbPortLabel => "MariaDB";

    public string SeleniumPortLabel => "Selenium";

    public string PortAvailable => IsCzech ? "Volný" : "Available";

    public string PortOccupied => IsCzech ? "Obsazený jiným procesem" : "Occupied by another process";

    public string PortInvalid => IsCzech ? "Neplatný port" : "Invalid port";

    public string PortDuplicate => IsCzech ? "Duplicitní port aplikace" : "Duplicate application port";

    public string PortUsedByApplication => IsCzech ? "Používá Portable Developer" : "Used by Portable Developer";

    public string PortSettingsReady => IsCzech
        ? "Služby jsou zastavené; porty lze upravit."
        : "Services are stopped; ports can be edited.";

    public string PortSettingsRequireStoppedServices => IsCzech
        ? "Před změnou portů zastavte Apache, MariaDB i Selenium."
        : "Stop Apache, MariaDB, and Selenium before changing ports.";

    public string RefreshPortList => IsCzech ? "Obnovit obsazené porty" : "Refresh occupied ports";

    public string SavePorts => IsCzech ? "Uložit porty" : "Save ports";

    public string PortsSaved => IsCzech ? "Porty byly uloženy." : "Ports were saved.";

    public string PortsInvalid => IsCzech
        ? "Zadejte čtyři různé porty v rozsahu 1024–65535."
        : "Enter four different ports in the 1024–65535 range.";

    public string PortsOccupied(IEnumerable<int> ports) => IsCzech
        ? $"Nelze uložit: porty {string.Join(", ", ports)} již používá jiný proces."
        : $"Cannot save: ports {string.Join(", ", ports)} are already used by another process.";

    public string PortScanFailed(string detail) => IsCzech
        ? $"Obsazené porty se nepodařilo načíst: {detail}"
        : $"Occupied ports could not be loaded: {detail}";

    public string TcpListenerEndpoint(string address, int port) => $"{address}:{port}";

}
