using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Infrastructure.Logging;
using PortableDeveloper.Infrastructure.Paths;

namespace PortableDeveloper.App;

public partial class App : System.Windows.Application
{
    private JsonLinesApplicationLogger? _logger;

    public IPortablePathResolver Paths { get; private set; } = null!;

    public IApplicationLogger Logger => _logger ?? throw new InvalidOperationException("Application logger is not initialized.");

    protected override void OnStartup(System.Windows.StartupEventArgs e)
    {
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
                        $"{args.Exception.GetType().FullName}: {args.Exception.Message}")
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
        base.OnStartup(e);
    }

    protected override void OnExit(System.Windows.ExitEventArgs e)
    {
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
}
