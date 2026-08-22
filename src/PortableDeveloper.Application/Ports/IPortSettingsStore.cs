namespace PortableDeveloper.Application.Ports;

public interface IPortSettingsStore
{
    PortSettings Load(PortSettings fallback);

    void Save(PortSettings settings);
}
