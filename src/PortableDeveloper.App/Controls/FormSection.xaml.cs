using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PortableDeveloper.App.Controls;

public partial class FormSection : UserControl
{
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon),
        typeof(Geometry),
        typeof(FormSection),
        new PropertyMetadata(Geometry.Empty));

    public static readonly DependencyProperty HeadingProperty = RegisterString(nameof(Heading));
    public static readonly DependencyProperty DescriptionProperty = RegisterString(nameof(Description));
    public static readonly DependencyProperty FeedbackProperty = RegisterString(nameof(Feedback));
    public static readonly DependencyProperty BodyProperty = RegisterObject(nameof(Body));
    public static readonly DependencyProperty HeaderActionsProperty = RegisterObject(nameof(HeaderActions));
    public static readonly DependencyProperty ActionsProperty = RegisterObject(nameof(Actions));

    public FormSection()
    {
        InitializeComponent();
    }

    public Geometry Icon
    {
        get => (Geometry)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Heading
    {
        get => (string)GetValue(HeadingProperty);
        set => SetValue(HeadingProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public string Feedback
    {
        get => (string)GetValue(FeedbackProperty);
        set => SetValue(FeedbackProperty, value);
    }

    public object? Body
    {
        get => GetValue(BodyProperty);
        set => SetValue(BodyProperty, value);
    }

    public object? HeaderActions
    {
        get => GetValue(HeaderActionsProperty);
        set => SetValue(HeaderActionsProperty, value);
    }

    public object? Actions
    {
        get => GetValue(ActionsProperty);
        set => SetValue(ActionsProperty, value);
    }

    private static DependencyProperty RegisterString(string name) => DependencyProperty.Register(
        name,
        typeof(string),
        typeof(FormSection),
        new PropertyMetadata(string.Empty));

    private static DependencyProperty RegisterObject(string name) => DependencyProperty.Register(
        name,
        typeof(object),
        typeof(FormSection),
        new PropertyMetadata(null));
}
