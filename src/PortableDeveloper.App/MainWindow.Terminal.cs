using System.IO;
using System.Windows;
using System.Windows.Threading;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Workspace;

namespace PortableDeveloper.App;

public partial class MainWindow
{
    private async void TerminalPage_SubmitRequested(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (_terminalSession is { IsRunning: true })
        {
            await SendTerminalSessionInputAsync();
            return;
        }

        await ExecuteTerminalCommandAsync();
    }

    private async void TerminalPage_CancelRequested(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        await StopTerminalSessionAsync();
    }

    private void TerminalPage_ClearRequested(object sender, RoutedEventArgs e)
    {
        e.Handled = true;
        if (_terminalBusy)
        {
            return;
        }

        ResetTerminalConsole();
        _dashboard.TerminalPage.RequestFocus();
    }

    private async Task ExecuteTerminalCommandAsync()
    {
        if (_terminalBusy)
        {
            return;
        }

        var page = _dashboard.TerminalPage;
        var command = page.CurrentInput;
        if (string.IsNullOrWhiteSpace(command))
        {
            AppendTerminalRaw(Environment.NewLine);
            WriteTerminalPrompt();
            return;
        }

        _terminalBusy = true;
        page.SetOperationState(isBusy: true, isSessionRunning: false, isReadOnly: true);
        AppendTerminalRaw(Environment.NewLine);
        page.RecordCommand(command);
        try
        {
            var sessionStart = await _terminalService.TryStartSessionAsync(
                command,
                _terminalWorkingDirectory,
                new DelegateProgress<PortableProcessOutput>(QueueTerminalProcessOutput),
                _applicationLifetime.Token);
            if (sessionStart.IsRuntimeCommand)
            {
                if (!sessionStart.IsSuccess)
                {
                    AppendTerminalLine(sessionStart.Error);
                    return;
                }

                _terminalSession = sessionStart.Session;
                page.SetOperationState(isBusy: true, isSessionRunning: true, isReadOnly: false);
                page.MarkInputStart();
                page.RequestFocus();
                _ = ObserveTerminalSessionAsync(sessionStart.Session!);
                return;
            }

            var result = await _terminalService.ExecuteAsync(
                command,
                _terminalWorkingDirectory,
                _applicationLifetime.Token);
            _terminalWorkingDirectory = result.WorkingDirectory;
            if (result.ClearScreen)
            {
                page.Clear();
            }

            if (result.ServiceRequest is not null)
            {
                await ExecuteTerminalServiceRequestAsync(result.ServiceRequest);
                AppendTerminalLine(GetServiceStatusText());
            }
            else if (!string.IsNullOrWhiteSpace(result.Output))
            {
                AppendTerminalLine(result.Output);
            }
        }
        catch (OperationCanceledException)
        {
            AppendTerminalLine(_dashboard.Text.OperationCanceled);
        }
        catch (Exception exception)
        {
            AppendTerminalLine(exception.Message);
        }
        finally
        {
            if (_terminalSession is null)
            {
                _terminalBusy = false;
                page.SetOperationState(isBusy: false, isSessionRunning: false, isReadOnly: false);
                WriteTerminalPrompt();
                page.RequestFocus();
            }
        }
    }

    private async Task SendTerminalSessionInputAsync()
    {
        var session = _terminalSession;
        if (session is null || !session.IsRunning)
        {
            return;
        }

        var input = _dashboard.TerminalPage.CurrentInput;
        AppendTerminalRaw(Environment.NewLine);
        _dashboard.TerminalPage.MarkInputStart();
        try
        {
            await session.WriteLineAsync(input, _applicationLifetime.Token);
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException or ObjectDisposedException)
        {
            AppendTerminalLine(exception.Message);
        }
    }

    private async Task StopTerminalSessionAsync()
    {
        var session = _terminalSession;
        if (session is null)
        {
            return;
        }

        AppendTerminalRaw("^C");
        AppendTerminalRaw(Environment.NewLine);
        _dashboard.TerminalPage.MarkInputStart();
        try
        {
            await session.StopAsync(_applicationLifetime.Token);
        }
        catch (OperationCanceledException)
        {
            // Application shutdown owns the final process cleanup.
        }
    }

    private async Task ObserveTerminalSessionAsync(IPortableProcessSession session)
    {
        try
        {
            var result = await session.Completion;
            FlushTerminalProcessOutput();
            if (result.TimedOut)
            {
                AppendTerminalLine(_dashboard.Text.TerminalProcessTimedOut);
            }
            else if (!result.WasStopped && result.ExitCode is not 0)
            {
                AppendTerminalLine(_dashboard.Text.TerminalProcessExited(result.ExitCode));
            }
        }
        catch (Exception exception) when (exception is IOException or InvalidOperationException)
        {
            AppendTerminalLine(exception.Message);
        }
        finally
        {
            FlushTerminalProcessOutput();
            await session.DisposeAsync();
            if (ReferenceEquals(_terminalSession, session))
            {
                _terminalSession = null;
            }

            _terminalBusy = false;
            _dashboard.TerminalPage.SetOperationState(isBusy: false, isSessionRunning: false, isReadOnly: false);
            _dashboard.TerminalPage.MarkInputStart();
            WriteTerminalPrompt();
            _dashboard.TerminalPage.RequestFocus();
        }
    }

    private void QueueTerminalProcessOutput(PortableProcessOutput output)
    {
        lock (_terminalOutputLock)
        {
            _terminalOutputBuffer.Append(output.Text);
            if (_terminalOutputBuffer.Length > MaximumPendingTerminalOutputCharacters)
            {
                _terminalOutputBuffer.Remove(
                    0,
                    _terminalOutputBuffer.Length - MaximumPendingTerminalOutputCharacters);
            }

            if (_terminalOutputFlushScheduled)
            {
                return;
            }

            _terminalOutputFlushScheduled = true;
        }

        _ = Dispatcher.BeginInvoke(DispatcherPriority.Background, FlushTerminalProcessOutput);
    }

    private void FlushTerminalProcessOutput()
    {
        string output;
        lock (_terminalOutputLock)
        {
            output = _terminalOutputBuffer.ToString();
            _terminalOutputBuffer.Clear();
            _terminalOutputFlushScheduled = false;
        }

        if (output.Length > 0)
        {
            AppendTerminalProcessOutput(output);
        }
    }

    private async Task ExecuteTerminalServiceRequestAsync(PortableTerminalServiceRequest request)
    {
        if (request.Operation == PortableTerminalServiceOperation.Status)
        {
            return;
        }

        var targets = request.Service == PortableServiceTarget.All
            ? new[] { PortableServiceTarget.MariaDb, PortableServiceTarget.Web, PortableServiceTarget.Selenium }
            : new[] { request.Service };
        if (request.Operation == PortableTerminalServiceOperation.Stop)
        {
            targets = targets.Reverse().ToArray();
        }

        foreach (var target in targets)
        {
            if (request.Operation == PortableTerminalServiceOperation.Restart)
            {
                await SetTerminalServiceStateAsync(target, shouldRun: false);
                await SetTerminalServiceStateAsync(target, shouldRun: true);
            }
            else
            {
                await SetTerminalServiceStateAsync(
                    target,
                    request.Operation == PortableTerminalServiceOperation.Start);
            }
        }
    }

    private async Task SetTerminalServiceStateAsync(PortableServiceTarget service, bool shouldRun)
    {
        switch (service)
        {
            case PortableServiceTarget.Web when (_dashboard.Runtime.ApacheProcessState == PortableDeveloper.Domain.Processes.ManagedProcessState.Running) != shouldRun:
                await ToggleApacheAsync();
                break;
            case PortableServiceTarget.MariaDb when _dashboard.Runtime.MariaDbIsRunning != shouldRun:
                await ToggleMariaDbAsync();
                break;
            case PortableServiceTarget.Selenium when _dashboard.Runtime.SeleniumIsRunning != shouldRun:
                await ToggleSeleniumAsync();
                break;
        }
    }

    private string GetServiceStatusText() => string.Join(Environment.NewLine,
        $"apache: {_dashboard.Text.StackStatus(_dashboard.Runtime.ApacheProcessState)}",
        $"mariadb: {_dashboard.Text.StackStatus(_dashboard.Runtime.MariaDbProcessState)}",
        $"selenium: {_dashboard.Text.StackStatus(_dashboard.Runtime.SeleniumProcessState)}");

    private void ResetTerminalConsole()
    {
        _dashboard.TerminalPage.Clear();
        WriteTerminalPrompt();
    }

    private void WriteTerminalPrompt() =>
        _dashboard.TerminalPage.WritePrompt(DisplayTerminalPath(_terminalWorkingDirectory));

    private void AppendTerminalLine(string text) => _dashboard.TerminalPage.AppendLine(text);

    private void AppendTerminalRaw(string text) => _dashboard.TerminalPage.AppendRaw(text);

    private void AppendTerminalProcessOutput(string text) => _dashboard.TerminalPage.AppendProcessOutput(text);

    private string DisplayTerminalPath(string relativePath) =>
        string.IsNullOrEmpty(relativePath)
            ? $"{_projectContext.ActiveProject.Id}:/"
            : $"{_projectContext.ActiveProject.Id}:/{relativePath}";

    private sealed class DelegateProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
