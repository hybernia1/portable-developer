namespace PortableDeveloper.Application.Lifecycle;

public interface ISingleInstanceCoordinator : IAsyncDisposable
{
    bool IsPrimaryInstance { get; }

    Task ListenForActivationAsync(
        Func<CancellationToken, Task> activationHandler,
        CancellationToken cancellationToken = default);

    Task<bool> SignalActivationAsync(CancellationToken cancellationToken = default);
}
