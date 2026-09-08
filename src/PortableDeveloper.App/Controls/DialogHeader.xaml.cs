using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PortableDeveloper.App.Controls;

public partial class DialogHeader : UserControl
{
    public static readonly DependencyProperty HeadingProperty = DependencyProperty.Register(
        nameof(Heading),
        typeof(string),
        typeof(DialogHeader),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ContextProperty = DependencyProperty.Register(
        nameof(Context),
        typeof(string),
        typeof(DialogHeader),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon),
        typeof(Geometry),
        typeof(DialogHeader),
        new PropertyMetadata(Geometry.Empty));

    public static readonly DependencyProperty IconForegroundProperty = DependencyProperty.Register(
        nameof(IconForeground),
        typeof(Brush),
        typeof(DialogHeader),
        new FrameworkPropertyMetadata(SystemColors.ControlTextBrush));

    public DialogHeader()
    {
        InitializeComponent();
        SetResourceReference(IconForegroundProperty, "TextFillColorSecondaryBrush");
    }

    public string Heading
    {
        get => (string)GetValue(HeadingProperty);
        set => SetValue(HeadingProperty, value);
    }

    public string Context
    {
        get => (string)GetValue(ContextProperty);
        set => SetValue(ContextProperty, value);
    }

    public Geometry Icon
    {
        get => (Geometry)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public Brush IconForeground
    {
        get => (Brush)GetValue(IconForegroundProperty);
        set => SetValue(IconForegroundProperty, value);
    }
}
