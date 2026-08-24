using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace PortableDeveloper.App.Controls;

public partial class AppTitleBar : UserControl
{
    public static readonly DependencyProperty ShowMinimizeButtonProperty = DependencyProperty.Register(
        nameof(ShowMinimizeButton),
        typeof(bool),
        typeof(AppTitleBar),
        new PropertyMetadata(true));

    public static readonly DependencyProperty ShowMaximizeButtonProperty = DependencyProperty.Register(
        nameof(ShowMaximizeButton),
        typeof(bool),
        typeof(AppTitleBar),
        new PropertyMetadata(true));

    public static readonly DependencyProperty ShowIconProperty = DependencyProperty.Register(
        nameof(ShowIcon),
        typeof(bool),
        typeof(AppTitleBar),
        new PropertyMetadata(true));

    public static readonly DependencyProperty ShowTitleProperty = DependencyProperty.Register(
        nameof(ShowTitle),
        typeof(bool),
        typeof(AppTitleBar),
        new PropertyMetadata(true));

    private Window? _window;
    private HwndSource? _source;
    private bool _handledMaximizeButtonDown;
    private const int WmNcHitTest = 0x0084;
    private const int WmNcLeftButtonDown = 0x00A1;
    private const int WmNcLeftButtonUp = 0x00A2;
    private const int HtMaxButton = 9;

    public AppTitleBar()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    public bool ShowMinimizeButton
    {
        get => (bool)GetValue(ShowMinimizeButtonProperty);
        set => SetValue(ShowMinimizeButtonProperty, value);
    }

    public bool ShowMaximizeButton
    {
        get => (bool)GetValue(ShowMaximizeButtonProperty);
        set => SetValue(ShowMaximizeButtonProperty, value);
    }

    public bool ShowIcon
    {
        get => (bool)GetValue(ShowIconProperty);
        set => SetValue(ShowIconProperty, value);
    }

    public bool ShowTitle
    {
        get => (bool)GetValue(ShowTitleProperty);
        set => SetValue(ShowTitleProperty, value);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _window = Window.GetWindow(this);
        if (_window is not null)
        {
            _window.StateChanged += Window_StateChanged;
            _window.SourceInitialized += Window_SourceInitialized;
            AttachWindowHook();
            UpdateMaximizeGlyph();
        }
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (_window is not null)
        {
            _window.StateChanged -= Window_StateChanged;
            _window.SourceInitialized -= Window_SourceInitialized;
            _source?.RemoveHook(WindowProc);
            _source = null;
            _window = null;
        }
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        if (_window is not null)
        {
            _window.WindowState = WindowState.Minimized;
        }
    }

    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        ToggleMaximize();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => _window?.Close();

    private void Window_StateChanged(object? sender, EventArgs e) => UpdateMaximizeGlyph();

    private void Window_SourceInitialized(object? sender, EventArgs e) => AttachWindowHook();

    private void AttachWindowHook()
    {
        if (_window is null || _source is not null)
        {
            return;
        }

        _source = PresentationSource.FromVisual(_window) as HwndSource;
        _source?.AddHook(WindowProc);
    }

    private IntPtr WindowProc(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == WmNcLeftButtonDown && wParam.ToInt32() == HtMaxButton)
        {
            ToggleMaximize();
            _handledMaximizeButtonDown = true;
            handled = true;
            return IntPtr.Zero;
        }

        if (message == WmNcLeftButtonUp && wParam.ToInt32() == HtMaxButton)
        {
            if (!_handledMaximizeButtonDown)
            {
                ToggleMaximize();
            }

            _handledMaximizeButtonDown = false;
            handled = true;
            return IntPtr.Zero;
        }

        if (message != WmNcHitTest || !ShowMaximizeButton || !MaximizeButton.IsVisible)
        {
            return IntPtr.Zero;
        }

        var packed = lParam.ToInt64();
        var screenPoint = new Point(unchecked((short)(packed & 0xffff)), unchecked((short)((packed >> 16) & 0xffff)));
        var topLeft = MaximizeButton.PointToScreen(new Point(0, 0));
        var bottomRight = MaximizeButton.PointToScreen(new Point(MaximizeButton.ActualWidth, MaximizeButton.ActualHeight));
        if (screenPoint.X >= topLeft.X && screenPoint.X < bottomRight.X
            && screenPoint.Y >= topLeft.Y && screenPoint.Y < bottomRight.Y)
        {
            handled = true;
            return new IntPtr(HtMaxButton);
        }

        return IntPtr.Zero;
    }

    private void ToggleMaximize()
    {
        if (_window is not null && _window.ResizeMode is ResizeMode.CanResize or ResizeMode.CanResizeWithGrip)
        {
            _window.WindowState = _window.WindowState == WindowState.Maximized
                ? WindowState.Normal
                : WindowState.Maximized;
        }
    }

    private void UpdateMaximizeGlyph()
    {
        if (_window is not null)
        {
            MaximizeGlyph.Text = _window.WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
        }
    }
}
