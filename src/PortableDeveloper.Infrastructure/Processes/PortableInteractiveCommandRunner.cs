using System.Diagnostics;
using System.Text;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Domain.Processes;

namespace PortableDeveloper.Infrastructure.Processes;

public sealed class PortableInteractiveCommandRunner : IPortableInteractiveCommandRunner
{
    private static readonly Encoding Utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
    private readonly IPortablePathResolver _paths;
    private readonly IApplicationLogger _logger;

    public PortableInteractiveCommandRunner(IPortablePathResolver paths, IApplicationLogger logger)
    {
        _paths = paths;
        _logger = logger;
    }

    public async Task<IPortableProcessSession> StartAsync(
        PortableCommandDefinition definition,
        IProgress<PortableProcessOutput> output,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentNullException.ThrowIfNull(output);

        var executablePath = _paths.Resolve(definition.ExecutableRelativePath);
        if (!File.Exists(executablePath))
        {
            throw new FileNotFoundException("The portable command executable was not found.", definition.ExecutableRelativePath);
        }

        var workingDirectory = _paths.Resolve(definition.WorkingDirectoryRelativePath);
        if (!Directory.Exists(workingDirectory))
        {
            throw new DirectoryNotFoundException("The portable command working directory was not found.");
        }

        var startInfo = new ProcessStartInfo(executablePath)
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            StandardOutputEncoding = Utf8WithoutBom,
            StandardErrorEncoding = Utf8WithoutBom,
            StandardInputEncoding = Utf8WithoutBom,
            CreateNoWindow = true
        };
        foreach (var argument in definition.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        ApplyPortableEnvironment(startInfo, definition.Environment);
        var process = new Process { StartInfo = startInfo };
        await LogSafelyAsync(
            ApplicationLogLevel.Information,
            definition.Id,
            "interactive-command.start.requested",
            "Starting interactive portable command.");

        try
        {
            if (!process.Start())
            {
                process.Dispose();
                throw new InvalidOperationException("The interactive command process did not start.");
            }

            return new PortableProcessSession(
                process,
                definition,
                output,
                _logger,
                cancellationToken);
        }
        catch
        {
            process.Dispose();
            throw;
        }
    }

    private void ApplyPortableEnvironment(
        ProcessStartInfo startInfo,
        IReadOnlyDictionary<string, string>? environment)
    {
        if (environment is not null)
        {
            foreach (var (key, value) in environment)
            {
                startInfo.Environment[key] = value;
            }
        }

        startInfo.Environment["TEMP"] = _paths.EnsureDirectory("temp");
        startInfo.Environment["TMP"] = _paths.EnsureDirectory("temp");
        startInfo.Environment["HOME"] = _paths.EnsureDirectory("state/home");
    }

    private async Task LogSafelyAsync(
        ApplicationLogLevel level,
        string component,
        string eventName,
        string message)
    {
        try
        {
            await _logger.LogAsync(level, component, eventName, message);
        }
        catch
        {
            // Process ownership must never depend on diagnostic logging.
        }
    }

    private sealed class PortableProcessSession : IPortableProcessSession
    {
        private readonly Process _process;
        private readonly PortableCommandDefinition _definition;
        private readonly IProgress<PortableProcessOutput> _output;
        private readonly IApplicationLogger _logger;
        private readonly CancellationToken _applicationCancellation;
        private readonly Task _standardOutputPump;
        private readonly Task _standardErrorPump;
        private int _stopRequested;
        private int _disposed;

        public PortableProcessSession(
            Process process,
            PortableCommandDefinition definition,
            IProgress<PortableProcessOutput> output,
            IApplicationLogger logger,
            CancellationToken applicationCancellation)
        {
            _process = process;
            _definition = definition;
            _output = output;
            _logger = logger;
            _applicationCancellation = applicationCancellation;
            _standardOutputPump = PumpAsync(process.StandardOutput, isError: false);
            _standardErrorPump = PumpAsync(process.StandardError, isError: true);
            Completion = CompleteAsync();
        }

        public bool IsRunning
        {
            get
            {
                try
                {
                    return !_process.HasExited;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }
        }

        public Task<PortableInteractiveProcessResult> Completion { get; }

        public async ValueTask WriteLineAsync(
            string input,
            CancellationToken cancellationToken = default)
        {
            ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
            if (!IsRunning)
            {
                throw new InvalidOperationException("The interactive process has already exited.");
            }

            await _process.StandardInput.WriteLineAsync(input.AsMemory(), cancellationToken);
            await _process.StandardInput.FlushAsync(cancellationToken);
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Exchange(ref _stopRequested, 1);
            KillProcessTree();
            await Completion.WaitAsync(cancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.Exchange(ref _disposed, 1) != 0)
            {
                return;
            }

            if (IsRunning)
            {
                Interlocked.Exchange(ref _stopRequested, 1);
                KillProcessTree();
            }

            try
            {
                await Completion;
            }
            finally
            {
                _process.Dispose();
            }
        }

        private async Task<PortableInteractiveProcessResult> CompleteAsync()
        {
            using var timeoutSource = _definition.Timeout is { } timeout
                ? new CancellationTokenSource(timeout)
                : new CancellationTokenSource();
            using var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(
                _applicationCancellation,
                timeoutSource.Token);
            var timedOut = false;
            try
            {
                await _process.WaitForExitAsync(linkedSource.Token);
            }
            catch (OperationCanceledException)
            {
                timedOut = timeoutSource.IsCancellationRequested && !_applicationCancellation.IsCancellationRequested;
                KillProcessTree();
                await _process.WaitForExitAsync(CancellationToken.None);
            }

            await Task.WhenAll(_standardOutputPump, _standardErrorPump);
            var stopped = Volatile.Read(ref _stopRequested) != 0 || _applicationCancellation.IsCancellationRequested;
            var result = new PortableInteractiveProcessResult(_process.ExitCode, stopped, timedOut);
            await LogCompletionSafelyAsync(result);
            return result;
        }

        private async Task PumpAsync(StreamReader reader, bool isError)
        {
            var buffer = new char[1024];
            while (true)
            {
                int read;
                try
                {
                    read = await reader.ReadAsync(buffer.AsMemory(), CancellationToken.None);
                }
                catch (ObjectDisposedException)
                {
                    return;
                }

                if (read == 0)
                {
                    return;
                }

                try
                {
                    _output.Report(new PortableProcessOutput(new string(buffer, 0, read), isError));
                }
                catch
                {
                    // A closed UI must not stop pipe drainage and deadlock process cleanup.
                }
            }
        }

        private void KillProcessTree()
        {
            try
            {
                if (!_process.HasExited)
                {
                    _process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
                // The process exited between the state check and termination.
            }
        }

        private async Task LogCompletionSafelyAsync(PortableInteractiveProcessResult result)
        {
            try
            {
                await _logger.LogAsync(
                    result.IsSuccess ? ApplicationLogLevel.Information : ApplicationLogLevel.Error,
                    _definition.Id,
                    result.IsSuccess ? "interactive-command.completed" : "interactive-command.failed",
                    $"Interactive portable command exited with code {result.ExitCode}; stopped={result.WasStopped}; timedOut={result.TimedOut}.");
            }
            catch
            {
                // Process completion and cleanup must not depend on diagnostic logging.
            }
        }
    }
}
