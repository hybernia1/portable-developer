namespace PortableDeveloper.Application.Selenium;

public interface ISeleniumConfigurationGenerator
{
    string Generate(SeleniumServerOptions options, IReadOnlyList<SeleniumBrowserEnvironmentInfo> environments);
}
