namespace PortableDeveloper.Application.MariaDb;

public interface IMariaDbInstanceInitializer
{
    Task<MariaDbInitializationResult> InitializeAsync(
        MariaDbInstanceOptions options,
        CancellationToken cancellationToken = default);
}
