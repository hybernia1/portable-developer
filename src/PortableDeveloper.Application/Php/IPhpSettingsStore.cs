namespace PortableDeveloper.Application.Php;

public interface IPhpSettingsStore
{
    PhpSettings Load(string instanceId = "default");

    void Save(PhpSettings settings, string instanceId = "default");
}
