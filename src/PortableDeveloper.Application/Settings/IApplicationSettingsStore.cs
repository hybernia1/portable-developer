namespace PortableDeveloper.Application.Settings;

public interface IApplicationSettingsStore
{
    ApplicationSettings Load();

    void Save(ApplicationSettings settings);
}
