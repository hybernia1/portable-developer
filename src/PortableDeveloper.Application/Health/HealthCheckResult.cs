namespace PortableDeveloper.Application.Health;

public sealed record HealthCheckResult(
    bool IsHealthy,
    TimeSpan Elapsed,
    string Detail);
