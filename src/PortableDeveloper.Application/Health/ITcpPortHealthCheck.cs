namespace PortableDeveloper.Application.Health;

public interface ITcpPortHealthCheck
{
    Task<HealthCheckResult> CheckAsync(
        string host,
        int port,
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
}
