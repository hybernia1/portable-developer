using PortableDeveloper.Infrastructure.Lifecycle;

namespace PortableDeveloper.Tests;

public sealed class SingleInstanceCoordinatorTests
{
    [Fact]
    public async Task SecondInstanceSignalsPrimaryInstance()
    {
        var applicationId = $"PortableDeveloper.Tests.{Guid.NewGuid():N}";
        await using var primary = new SingleInstanceCoordinator(applicationId);
        await using var secondary = new SingleInstanceCoordinator(applicationId);
        Assert.True(primary.IsPrimaryInstance);
        Assert.False(secondary.IsPrimaryInstance);

        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var activated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var listener = primary.ListenForActivationAsync(
            _ =>
            {
                activated.TrySetResult();
                return Task.CompletedTask;
            },
            cancellation.Token);

        Assert.True(await secondary.SignalActivationAsync(cancellation.Token));
        await activated.Task.WaitAsync(cancellation.Token);
        cancellation.Cancel();
        await listener;
    }

    [Fact]
    public async Task NewInstanceCanAcquireAfterPrimaryIsDisposed()
    {
        var applicationId = $"PortableDeveloper.Tests.{Guid.NewGuid():N}";
        await using (var primary = new SingleInstanceCoordinator(applicationId))
        {
            Assert.True(primary.IsPrimaryInstance);
        }

        await using var replacement = new SingleInstanceCoordinator(applicationId);
        Assert.True(replacement.IsPrimaryInstance);
    }

    [Fact]
    public async Task Secondary_signal_does_not_capture_callers_synchronization_context()
    {
        var applicationId = $"PortableDeveloper.Tests.{Guid.NewGuid():N}";
        await using var primary = new SingleInstanceCoordinator(applicationId);
        await using var secondary = new SingleInstanceCoordinator(applicationId);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        var activated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var previous = SynchronizationContext.Current;
        SynchronizationContext.SetSynchronizationContext(new NeverPumpedSynchronizationContext());
        Task listener;
        Task<bool> signal;
        try
        {
            listener = primary.ListenForActivationAsync(
                _ =>
                {
                    activated.TrySetResult();
                    return Task.CompletedTask;
                },
                cancellation.Token);
            signal = secondary.SignalActivationAsync(cancellation.Token);
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previous);
        }

        Assert.True(await signal.WaitAsync(cancellation.Token));
        await activated.Task.WaitAsync(cancellation.Token);
        cancellation.Cancel();
        await listener;
    }

    private sealed class NeverPumpedSynchronizationContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback callback, object? state)
        {
            // Intentionally never pumped: library awaits must not depend on a UI context.
        }
    }
}
