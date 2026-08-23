using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Lifecycle;
using PortableDeveloper.Infrastructure.Lifecycle;
using PortableDeveloper.Infrastructure.Logging;
using PortableDeveloper.Infrastructure.Paths;

namespace PortableDeveloper.App;

public partial class App : System.Windows.Application
{
    private JsonLinesApplicationLogger? _logger;
    private ISingleInstanceCoordinator? _singleInstance;
    private CancellationTokenSource? _activationLifetime;
    private Task? _activationListener;

    public IPortablePathResolver Paths { get; private set; } = null!;

    public IApplicationLogger Logger => _logger ?? throw new InvalidOperationException("Application logger is not initialized.");

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
        base.OnStartup(e);
#if DEBUG
        _singleInstance = new SingleInstanceCoordinator("PortableDeveloper.Debug");
#else
        _singleInstance = new SingleInstanceCoordinator();
#endif
        if (!_singleInstance.IsPrimaryInstance)
        {
            _ = _singleInstance.SignalActivationAsync().GetAwaiter().GetResult();
            _singleInstance.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _singleInstance = null;
            Shutdown();
            return;
        }

        Paths = new PortablePathResolver(AppContext.BaseDirectory);
        _logger = new JsonLinesApplicationLogger(Paths);
        DispatcherUnhandledException += (_, args) =>
        {
            try
            {
                _logger.LogAsync(
                        ApplicationLogLevel.Error,
                        "application",
                        "application.unhandled",
                        args.Exception.ToString())
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();
            }
            catch
            {
                // Preserve the original UI exception even if diagnostic logging is unavailable.
            }
        };
        _logger.LogAsync(ApplicationLogLevel.Information, "application", "application.started", "Portable Developer started.")
            .AsTask()
            .GetAwaiter()
            .GetResult();

        var window = new MainWindow();
        MainWindow = window;
        window.Show();
        _activationLifetime = new CancellationTokenSource();
        _activationListener = _singleInstance.ListenForActivationAsync(
            ActivateMainWindowAsync,
            _activationLifetime.Token);
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
        _activationLifetime?.Cancel();
        if (_activationListener is not null)
        {
            try
            {
                _activationListener.GetAwaiter().GetResult();
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown of the activation pipe.
            }
        }

        _activationLifetime?.Dispose();
        _singleInstance?.DisposeAsync().AsTask().GetAwaiter().GetResult();
        if (_logger is not null)
        {
            _logger.LogAsync(ApplicationLogLevel.Information, "application", "application.stopped", "Portable Developer stopped.")
                .AsTask()
                .GetAwaiter()
                .GetResult();
            _logger.DisposeAsync().AsTask().GetAwaiter().GetResult();
        }

        base.OnExit(e);
    }

    private Task ActivateMainWindowAsync(CancellationToken cancellationToken) =>
        Dispatcher.InvokeAsync(() =>
        {
            if (MainWindow is not { } window)
            {
                return;
            }

            if (!window.IsVisible)
            {
                window.Show();
            }

            if (window.WindowState == System.Windows.WindowState.Minimized)
            {
                window.WindowState = System.Windows.WindowState.Normal;
            }

            _ = window.Activate();
            window.Topmost = true;
            window.Topmost = false;
            window.Focus();
        }).Task;
}
