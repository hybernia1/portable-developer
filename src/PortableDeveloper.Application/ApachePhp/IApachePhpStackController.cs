namespace PortableDeveloper.Application.ApachePhp;

public interface IApachePhpStackController : IAsyncDisposable
{
    Task<ApachePhpStackSnapshot> StartAsync(
        ApachePhpStackOptions options,
        CancellationToken cancellationToken = default);

    Task<ApachePhpStackSnapshot> StopAsync(CancellationToken cancellationToken = default);

    ApachePhpStackSnapshot GetSnapshot();
}
