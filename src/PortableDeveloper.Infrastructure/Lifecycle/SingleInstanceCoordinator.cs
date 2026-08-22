using System.IO.Pipes;
using PortableDeveloper.Application.Lifecycle;

namespace PortableDeveloper.Infrastructure.Lifecycle;

public sealed class SingleInstanceCoordinator : ISingleInstanceCoordinator
{
    private const byte ActivationMessage = 1;
    private readonly string _mutexName;
    private readonly string _pipeName;
    private readonly ManualResetEventSlim _mutexReady = new(false);
    private readonly ManualResetEventSlim _mutexStop = new(false);
    private readonly Thread _mutexThread;
    private Exception? _mutexFailure;
    private bool _disposed;

    public SingleInstanceCoordinator(string applicationId = "PortableDeveloper")
    {
        if (string.IsNullOrWhiteSpace(applicationId)
            || applicationId.Any(character => !char.IsAsciiLetterOrDigit(character) && character is not '.' and not '-' and not '_'))
        {
            throw new ArgumentException(
                "The single-instance application ID may only contain ASCII letters, digits, dots, hyphens, and underscores.",
                nameof(applicationId));
        }

        _mutexName = $@"Local\{applicationId}.SingleInstance";
        _pipeName = $"{applicationId}.Activation";
        _mutexThread = new Thread(OwnMutex)
        {
            IsBackground = true,
            Name = "Portable Developer single-instance mutex"
        };
        _mutexThread.Start();
        _mutexReady.Wait();
        if (_mutexFailure is not null)
        {
            throw new InvalidOperationException("Single-instance coordination could not be initialized.", _mutexFailure);
        }
    }

    public bool IsPrimaryInstance { get; private set; }

    public async Task ListenForActivationAsync(
        Func<CancellationToken, Task> activationHandler,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(activationHandler);
        if (!IsPrimaryInstance)
        {
            throw new InvalidOperationException("Only the primary instance can listen for activation requests.");
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await using var server = new NamedPipeServerStream(
                    _pipeName,
                    PipeDirection.In,
                    1,
                    PipeTransmissionMode.Byte,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                var message = new byte[1];
                var read = await server.ReadAsync(message, cancellationToken).ConfigureAwait(false);
                if (read == 1 && message[0] == ActivationMessage)
                {
                    await activationHandler(cancellationToken).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (IOException) when (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task<bool> SignalActivationAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsPrimaryInstance)
        {
            return false;
        }

        for (var attempt = 0; attempt < 20; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                await using var client = new NamedPipeClientStream(
                    ".",
                    _pipeName,
                    PipeDirection.Out,
                    PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
                await client.ConnectAsync(150, cancellationToken).ConfigureAwait(false);
                await client.WriteAsync(new[] { ActivationMessage }, cancellationToken).ConfigureAwait(false);
                await client.FlushAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (TimeoutException)
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
            catch (IOException)
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
        }

        return false;
    }

    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;
        _mutexStop.Set();
        if (!_mutexThread.Join(TimeSpan.FromSeconds(3)))
        {
            throw new InvalidOperationException("The single-instance mutex thread did not stop.");
        }

        _mutexReady.Dispose();
        _mutexStop.Dispose();
        return ValueTask.CompletedTask;
    }

    private void OwnMutex()
    {
        try
        {
            using var mutex = new Mutex(false, _mutexName);
            try
            {
                IsPrimaryInstance = mutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                IsPrimaryInstance = true;
            }

            _mutexReady.Set();
            if (!IsPrimaryInstance)
            {
                return;
            }

            _mutexStop.Wait();
            mutex.ReleaseMutex();
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or IOException or WaitHandleCannotBeOpenedException)
        {
            _mutexFailure = exception;
            _mutexReady.Set();
        }
    }
}
