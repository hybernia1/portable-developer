namespace PortableDeveloper.Application.Selenium;

public interface ISeleniumServerController : IAsyncDisposable
{
    SeleniumServerSnapshot GetSnapshot();

    Task<SeleniumServerSnapshot> StartAsync(
        SeleniumServerOptions options,
        CancellationToken cancellationToken = default);

    Task<SeleniumServerSnapshot> StopAsync(CancellationToken cancellationToken = default);
}
