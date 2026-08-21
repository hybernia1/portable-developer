namespace PortableDeveloper.Application.MariaDb;

public interface IMariaDbInstanceInitializer
{
    MariaDbInstanceState GetState(MariaDbInstanceOptions options);

    Task<MariaDbInitializationResult> InitializeAsync(
        MariaDbInstanceOptions options,
        CancellationToken cancellationToken = default);
}
