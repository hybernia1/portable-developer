using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using PortableDeveloper.App.ViewModels;

namespace PortableDeveloper.App.Views;

public partial class TerminalPageView : UserControl
{
    public static readonly RoutedEvent SubmitRequestedEvent = RegisterEvent(nameof(SubmitRequested));
    public static readonly RoutedEvent CancelRequestedEvent = RegisterEvent(nameof(CancelRequested));
    private TerminalPageViewModel? _page;

    public TerminalPageView()
    {
        InitializeComponent();
        DataContextChanged += TerminalPageView_DataContextChanged;
        Loaded += TerminalPageView_Loaded;
        Unloaded += TerminalPageView_Unloaded;
    }

    public event RoutedEventHandler SubmitRequested
    {
        add => AddHandler(SubmitRequestedEvent, value);
        remove => RemoveHandler(SubmitRequestedEvent, value);
    }

    public event RoutedEventHandler CancelRequested
    {
        add => AddHandler(CancelRequestedEvent, value);
        remove => RemoveHandler(CancelRequestedEvent, value);
    }

    private static RoutedEvent RegisterEvent(string name) => EventManager.RegisterRoutedEvent(
        name,
        RoutingStrategy.Bubble,
        typeof(RoutedEventHandler),
        typeof(TerminalPageView));

    private void TerminalPageView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        DetachPage();
        if (IsLoaded)
        {
            AttachPage(e.NewValue as TerminalPageViewModel);
        }
    }

    private void TerminalPageView_Loaded(object sender, RoutedEventArgs e) =>
        AttachPage(DataContext as TerminalPageViewModel);

    private void TerminalPageView_Unloaded(object sender, RoutedEventArgs e) => DetachPage();

    private void Page_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(TerminalPageViewModel.TextContent) or nameof(TerminalPageViewModel.FocusRevision))
        {
            _ = Dispatcher.BeginInvoke(
                DispatcherPriority.Input,
                () =>
                {
                    MoveCaretToEnd();
                    if (e.PropertyName == nameof(TerminalPageViewModel.FocusRevision))
                    {
                        ConsoleTextBox.Focus();
                    }
                });
        }
    }

    private void ConsoleTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (_page is null)
        {
            return;
        }

        if (_page.IsSessionRunning && Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C &&
            ConsoleTextBox.SelectionLength == 0)
        {
            e.Handled = true;
            RaiseEvent(new RoutedEventArgs(CancelRequestedEvent));
            return;
        }

        if (_page.IsBusy && !_page.IsSessionRunning)
        {
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            RaiseEvent(new RoutedEventArgs(SubmitRequestedEvent));
            return;
        }

        if (e.Key is Key.Up or Key.Down)
        {
            e.Handled = true;
            if (!_page.IsSessionRunning)
            {
                _page.NavigateHistory(e.Key == Key.Up ? -1 : 1);
            }
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.A)
        {
            e.Handled = true;
            ConsoleTextBox.Select(_page.InputStart, ConsoleTextBox.Text.Length - _page.InputStart);
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
        {
            return;
        }

        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.V &&
            ConsoleTextBox.SelectionStart < _page.InputStart)
        {
            MoveCaretToEnd();
            return;
        }

        if (e.Key is Key.Back or Key.Delete or Key.Left or Key.Home ||
            (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.X))
        {
            var selectionTouchesOutput = ConsoleTextBox.SelectionLength > 0 &&
                                         ConsoleTextBox.SelectionStart < _page.InputStart;
            var caretTouchesOutput = ConsoleTextBox.SelectionLength == 0 &&
                                     (ConsoleTextBox.CaretIndex < _page.InputStart ||
                                      (e.Key is Key.Back or Key.Left or Key.Home &&
                                       ConsoleTextBox.CaretIndex == _page.InputStart));
            if (selectionTouchesOutput || caretTouchesOutput)
            {
                e.Handled = true;
                MoveCaretToEnd();
            }
        }
    }

    private void ConsoleTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (_page is null)
        {
            return;
        }

        if (_page.IsBusy && !_page.IsSessionRunning)
        {
            e.Handled = true;
            return;
        }

        if (ConsoleTextBox.SelectionStart < _page.InputStart)
        {
            MoveCaretToEnd();
        }
    }

    private void MoveCaretToEnd()
    {
        ConsoleTextBox.CaretIndex = ConsoleTextBox.Text.Length;
        ConsoleTextBox.SelectionLength = 0;
        ConsoleTextBox.ScrollToEnd();
    }

    private void AttachPage(TerminalPageViewModel? page)
    {
        if (ReferenceEquals(_page, page))
        {
            return;
        }

        DetachPage();
        _page = page;
        if (_page is not null)
        {
            _page.PropertyChanged += Page_PropertyChanged;
        }

        MoveCaretToEnd();
    }

    private void DetachPage()
    {
        if (_page is not null)
        {
            _page.PropertyChanged -= Page_PropertyChanged;
            _page = null;
        }
    }
}
