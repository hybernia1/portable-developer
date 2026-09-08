using System.Windows;
using System.Windows.Controls;

namespace PortableDeveloper.App.Controls;

public partial class RuntimeHeader : UserControl
{
    public static readonly DependencyProperty BrandProperty = RegisterString(nameof(Brand));
    public static readonly DependencyProperty HeadingProperty = RegisterString(nameof(Heading));
    public static readonly DependencyProperty StateProperty = RegisterString(nameof(State));
    public static readonly DependencyProperty IsRunningProperty = DependencyProperty.Register(
        nameof(IsRunning),
        typeof(bool),
        typeof(RuntimeHeader),
        new PropertyMetadata(false));
    public static readonly DependencyProperty VersionProperty = RegisterString(nameof(Version));
    public static readonly DependencyProperty DetailProperty = RegisterString(nameof(Detail));
    public static readonly DependencyProperty MetadataProperty = RegisterString(nameof(Metadata));
    public static readonly DependencyProperty FeedbackProperty = RegisterString(nameof(Feedback));

    public static readonly DependencyProperty ActionsProperty = DependencyProperty.Register(
        nameof(Actions),
        typeof(object),
        typeof(RuntimeHeader),
        new PropertyMetadata(null));

    public RuntimeHeader()
    {
        InitializeComponent();
    }

    public string Brand
    {
        get => (string)GetValue(BrandProperty);
        set => SetValue(BrandProperty, value);
    }

    public string Heading
    {
        get => (string)GetValue(HeadingProperty);
        set => SetValue(HeadingProperty, value);
    }

    public string State
    {
        get => (string)GetValue(StateProperty);
        set => SetValue(StateProperty, value);
    }

    public bool IsRunning
    {
        get => (bool)GetValue(IsRunningProperty);
        set => SetValue(IsRunningProperty, value);
    }

    public string Version
    {
        get => (string)GetValue(VersionProperty);
        set => SetValue(VersionProperty, value);
    }

    public string Detail
    {
        get => (string)GetValue(DetailProperty);
        set => SetValue(DetailProperty, value);
    }

    public string Metadata
    {
        get => (string)GetValue(MetadataProperty);
        set => SetValue(MetadataProperty, value);
    }

    public string Feedback
    {
        get => (string)GetValue(FeedbackProperty);
        set => SetValue(FeedbackProperty, value);
    }

    public object? Actions
    {
        get => GetValue(ActionsProperty);
        set => SetValue(ActionsProperty, value);
    }

    private static DependencyProperty RegisterString(string name) => DependencyProperty.Register(
        name,
        typeof(string),
        typeof(RuntimeHeader),
        new PropertyMetadata(string.Empty));
}
