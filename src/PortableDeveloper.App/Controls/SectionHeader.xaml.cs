using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PortableDeveloper.App.Controls;

public partial class SectionHeader : UserControl
{
    public static readonly DependencyProperty IconProperty = DependencyProperty.Register(
        nameof(Icon),
        typeof(Geometry),
        typeof(SectionHeader),
        new PropertyMetadata(Geometry.Empty));

    public static readonly DependencyProperty HeadingProperty = RegisterString(nameof(Heading));
    public static readonly DependencyProperty DescriptionProperty = RegisterString(nameof(Description));
    public static readonly DependencyProperty MetadataProperty = RegisterString(nameof(Metadata));
    public static readonly DependencyProperty ActionsProperty = DependencyProperty.Register(
        nameof(Actions),
        typeof(object),
        typeof(SectionHeader),
        new PropertyMetadata(null));

    public SectionHeader()
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

    public string Metadata
    {
        get => (string)GetValue(MetadataProperty);
        set => SetValue(MetadataProperty, value);
    }

    public object? Actions
    {
        get => GetValue(ActionsProperty);
        set => SetValue(ActionsProperty, value);
    }

    private static DependencyProperty RegisterString(string name) => DependencyProperty.Register(
        name,
        typeof(string),
        typeof(SectionHeader),
        new PropertyMetadata(string.Empty));
}
