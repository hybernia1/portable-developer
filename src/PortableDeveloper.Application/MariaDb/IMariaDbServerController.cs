namespace PortableDeveloper.Application.MariaDb;

public interface IMariaDbServerController : IAsyncDisposable
{
    MariaDbServerSnapshot GetSnapshot();

    Task<MariaDbServerSnapshot> StartAsync(
        MariaDbInstanceOptions options,
        CancellationToken cancellationToken = default);

    Task<MariaDbServerSnapshot> StopAsync(CancellationToken cancellationToken = default);
}
