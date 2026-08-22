namespace PortableDeveloper.Application.Selenium;

public interface ISeleniumSettingsStore
{
    SeleniumServerOptions Load();

    void Save(SeleniumServerOptions settings);
}
