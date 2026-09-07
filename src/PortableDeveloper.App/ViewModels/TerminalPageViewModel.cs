using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace PortableDeveloper.App.ViewModels;

public sealed class TerminalPageViewModel : INotifyPropertyChanged
{
    private const int MaximumTerminalCharacters = 250_000;
    private readonly List<string> _history = [];
    private readonly UiText _text;
    private int _focusRevision;
    private int _historyIndex;
    private int _inputStart;
    private bool _isBusy;
    private bool _isReadOnly;
    private bool _isSessionRunning;
    private string _textContent = string.Empty;

    public TerminalPageViewModel(UiText text)
    {
        _text = text;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string TextContent
    {
        get => _textContent;
        set => SetField(ref _textContent, value);
    }

    public int InputStart
    {
        get => _inputStart;
        private set => SetField(ref _inputStart, Math.Clamp(value, 0, TextContent.Length));
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetField(ref _isBusy, value);
    }

    public bool IsReadOnly
    {
        get => _isReadOnly;
        private set => SetField(ref _isReadOnly, value);
    }

    public bool IsSessionRunning
    {
        get => _isSessionRunning;
        private set => SetField(ref _isSessionRunning, value);
    }

    public int FocusRevision
    {
        get => _focusRevision;
        private set => SetField(ref _focusRevision, value);
    }

    public string CurrentInput => TextContent[Math.Clamp(InputStart, 0, TextContent.Length)..].TrimEnd('\r', '\n');

    public void SetOperationState(bool isBusy, bool isSessionRunning, bool isReadOnly)
    {
        IsBusy = isBusy;
        IsSessionRunning = isSessionRunning;
        IsReadOnly = isReadOnly;
    }

    public void RecordCommand(string command)
    {
        _history.Remove(command);
        _history.Add(command);
        _historyIndex = _history.Count;
    }

    public void NavigateHistory(int offset)
    {
        if (_history.Count == 0)
        {
            return;
        }

        _historyIndex = Math.Clamp(_historyIndex + offset, 0, _history.Count);
        var command = _historyIndex == _history.Count ? string.Empty : _history[_historyIndex];
        TextContent = TextContent[..Math.Clamp(InputStart, 0, TextContent.Length)] + command;
    }

    public void Clear() => TextContent = string.Empty;

    public void MarkInputStart() => InputStart = TextContent.Length;

    public void WritePrompt(string path)
    {
        if (TextContent.Length > 0 && !TextContent.EndsWith(Environment.NewLine, StringComparison.Ordinal))
        {
            AppendRaw(Environment.NewLine);
        }

        AppendRaw($"{path}> ");
        MarkInputStart();
    }

    public void AppendLine(string text)
    {
        AppendRaw(text.TrimEnd('\r', '\n'));
        AppendRaw(Environment.NewLine);
    }

    public void AppendRaw(string text) => SetTerminalText(TextContent + text, InputStart);

    public void AppendProcessOutput(string text)
    {
        var inputStart = Math.Clamp(InputStart, 0, TextContent.Length);
        var next = TextContent[..inputStart] + text + TextContent[inputStart..];
        SetTerminalText(next, inputStart + text.Length);
    }

    public void RequestFocus() => FocusRevision++;

    private void SetTerminalText(string next, int inputStart)
    {
        if (next.Length > MaximumTerminalCharacters)
        {
            var truncationNotice = _text.TerminalOutputTruncated + Environment.NewLine;
            var retainedCharacters = MaximumTerminalCharacters - truncationNotice.Length;
            var removed = next.Length - retainedCharacters;
            next = truncationNotice + next[removed..];
            inputStart = Math.Max(truncationNotice.Length, inputStart - removed + truncationNotice.Length);
        }

        TextContent = next;
        InputStart = inputStart;
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
