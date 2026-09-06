using System.IO;
using System.Windows.Input;
using System.Windows.Threading;
using PortableDeveloper.Application.Abstractions;
using PortableDeveloper.Application.Workspace;

namespace PortableDeveloper.App;

public partial class MainWindow
{

    private async void TerminalConsoleTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        var sessionRunning = _terminalSession is { IsRunning: true };
        if (sessionRunning && Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C &&
            TerminalConsoleTextBox.SelectionLength == 0)
        {
            e.Handled = true;
            await StopTerminalSessionAsync();
            return;
        }

        if (sessionRunning && e.Key == Key.Enter)
        {
            e.Handled = true;
            await SendTerminalSessionInputAsync();
            return;
        }

        if (_terminalBusy && !sessionRunning)
        {
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            await ExecuteTerminalCommandAsync();
            return;
        }

        if (e.Key == Key.Up || e.Key == Key.Down)
        {
            e.Handled = true;
            if (!sessionRunning)
            {
                NavigateTerminalHistory(e.Key == Key.Up ? -1 : 1);
            }
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.A)
        {
            e.Handled = true;
            TerminalConsoleTextBox.Select(_terminalInputStart, TerminalConsoleTextBox.Text.Length - _terminalInputStart);
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
        {
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.V &&
            TerminalConsoleTextBox.SelectionStart < _terminalInputStart)
        {
            MoveTerminalCaretToEnd();
            return;
        }

        if (e.Key is Key.Back or Key.Delete or Key.Left or Key.Home ||
            (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.X))
        {
            var selectionTouchesOutput = TerminalConsoleTextBox.SelectionLength > 0 &&
                                         TerminalConsoleTextBox.SelectionStart < _terminalInputStart;
            var caretTouchesOutput = TerminalConsoleTextBox.SelectionLength == 0 && (
                TerminalConsoleTextBox.CaretIndex < _terminalInputStart ||
                (e.Key is Key.Back or Key.Left or Key.Home &&
                 TerminalConsoleTextBox.CaretIndex == _terminalInputStart));
            if (selectionTouchesOutput || caretTouchesOutput)
            {
                e.Handled = true;
                MoveTerminalCaretToEnd();
            }
        }
    }

    private void TerminalConsoleTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (_terminalBusy && _terminalSession is not { IsRunning: true })
        {
            e.Handled = true;
            return;
        }

        if (TerminalConsoleTextBox.SelectionStart < _terminalInputStart)
        {
            MoveTerminalCaretToEnd();
        }
    }

    private async Task ExecuteTerminalCommandAsync()
    {
        if (_terminalBusy)
        {
            return;
        }

        var command = TerminalConsoleTextBox.Text[_terminalInputStart..].TrimEnd('\r', '\n');
        if (string.IsNullOrWhiteSpace(command))
        {
            AppendTerminalRaw(Environment.NewLine);
            WriteTerminalPrompt();
            return;
        }

        _terminalBusy = true;
        TerminalConsoleTextBox.IsReadOnly = true;
        AppendTerminalRaw(Environment.NewLine);
        _terminalHistory.Remove(command);
        _terminalHistory.Add(command);
        _terminalHistoryIndex = _terminalHistory.Count;
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
                TerminalConsoleTextBox.IsReadOnly = false;
                _terminalInputStart = TerminalConsoleTextBox.Text.Length;
                MoveTerminalCaretToEnd();
                TerminalConsoleTextBox.Focus();
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
                TerminalConsoleTextBox.Clear();
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
                TerminalConsoleTextBox.IsReadOnly = false;
                WriteTerminalPrompt();
                TerminalConsoleTextBox.Focus();
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

        var input = TerminalConsoleTextBox.Text[_terminalInputStart..].TrimEnd('\r', '\n');
        AppendTerminalRaw(Environment.NewLine);
        _terminalInputStart = TerminalConsoleTextBox.Text.Length;
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
        _terminalInputStart = TerminalConsoleTextBox.Text.Length;
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
            TerminalConsoleTextBox.IsReadOnly = false;
            _terminalInputStart = TerminalConsoleTextBox.Text.Length;
            WriteTerminalPrompt();
            TerminalConsoleTextBox.Focus();
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
            case PortableServiceTarget.Web when (_dashboard.ApacheProcessState == PortableDeveloper.Domain.Processes.ManagedProcessState.Running) != shouldRun:
                await ToggleApacheAsync();
                break;
            case PortableServiceTarget.MariaDb when _dashboard.MariaDbIsRunning != shouldRun:
                await ToggleMariaDbAsync();
                break;
            case PortableServiceTarget.Selenium when _dashboard.SeleniumIsRunning != shouldRun:
                await ToggleSeleniumAsync();
                break;
        }
    }

    private string GetServiceStatusText() => string.Join(Environment.NewLine,
        $"apache: {_dashboard.Text.StackStatus(_dashboard.ApacheProcessState)}",
        $"mariadb: {_dashboard.Text.StackStatus(_dashboard.MariaDbProcessState)}",
        $"selenium: {_dashboard.Text.StackStatus(_dashboard.SeleniumProcessState)}");

    private void ResetTerminalConsole()
    {
        TerminalConsoleTextBox.Clear();
        WriteTerminalPrompt();
    }

    private void WriteTerminalPrompt()
    {
        if (TerminalConsoleTextBox.Text.Length > 0 &&
            !TerminalConsoleTextBox.Text.EndsWith(Environment.NewLine, StringComparison.Ordinal))
        {
            AppendTerminalRaw(Environment.NewLine);
        }

        AppendTerminalRaw($"{DisplayTerminalPath(_terminalWorkingDirectory)}> ");
        _terminalInputStart = TerminalConsoleTextBox.Text.Length;
        MoveTerminalCaretToEnd();
    }

    private void AppendTerminalLine(string text)
    {
        AppendTerminalRaw(text.TrimEnd('\r', '\n'));
        AppendTerminalRaw(Environment.NewLine);
    }

    private void AppendTerminalRaw(string text)
    {
        var next = TerminalConsoleTextBox.Text + text;
        SetTerminalText(next, _terminalInputStart);
    }

    private void AppendTerminalProcessOutput(string text)
    {
        var current = TerminalConsoleTextBox.Text;
        var inputStart = Math.Clamp(_terminalInputStart, 0, current.Length);
        var next = current[..inputStart] + text + current[inputStart..];
        SetTerminalText(next, inputStart + text.Length);
    }

    private void SetTerminalText(string next, int inputStart)
    {
        if (next.Length > MaximumTerminalCharacters)
        {
            var truncationNotice = _dashboard.Text.TerminalOutputTruncated + Environment.NewLine;
            var retainedCharacters = MaximumTerminalCharacters - truncationNotice.Length;
            var removed = next.Length - retainedCharacters;
            next = truncationNotice + next[removed..];
            inputStart = Math.Max(truncationNotice.Length, inputStart - removed + truncationNotice.Length);
        }

        TerminalConsoleTextBox.Text = next;
        _terminalInputStart = Math.Clamp(inputStart, 0, next.Length);
        MoveTerminalCaretToEnd();
    }

    private void MoveTerminalCaretToEnd()
    {
        TerminalConsoleTextBox.CaretIndex = TerminalConsoleTextBox.Text.Length;
        TerminalConsoleTextBox.SelectionLength = 0;
        TerminalConsoleTextBox.ScrollToEnd();
    }

    private void NavigateTerminalHistory(int offset)
    {
        if (_terminalHistory.Count == 0)
        {
            return;
        }

        _terminalHistoryIndex = Math.Clamp(_terminalHistoryIndex + offset, 0, _terminalHistory.Count);
        var command = _terminalHistoryIndex == _terminalHistory.Count
            ? string.Empty
            : _terminalHistory[_terminalHistoryIndex];
        TerminalConsoleTextBox.Text = TerminalConsoleTextBox.Text[.._terminalInputStart] + command;
        MoveTerminalCaretToEnd();
    }

    private string DisplayTerminalPath(string relativePath) =>
        string.IsNullOrEmpty(relativePath)
            ? $"{_projectContext.ActiveProject.Id}:/"
            : $"{_projectContext.ActiveProject.Id}:/{relativePath}";

    private sealed class DelegateProgress<T>(Action<T> report) : IProgress<T>
    {
        public void Report(T value) => report(value);
    }
}
