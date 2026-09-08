using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PortableDeveloper.App.Controls;

public sealed class AppIcon : Control
{
    public static readonly DependencyProperty DataProperty = DependencyProperty.Register(
        nameof(Data),
        typeof(Geometry),
        typeof(AppIcon),
        new FrameworkPropertyMetadata(Geometry.Empty, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty StrokeThicknessProperty = DependencyProperty.Register(
        nameof(StrokeThickness),
        typeof(double),
        typeof(AppIcon),
        new FrameworkPropertyMetadata(1.7d, FrameworkPropertyMetadataOptions.AffectsRender));

    public AppIcon()
    {
        Focusable = false;
        IsHitTestVisible = false;
    }

    public Geometry Data
    {
        get => (Geometry)GetValue(DataProperty);
        set => SetValue(DataProperty, value);
    }

    public double StrokeThickness
    {
        get => (double)GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }
}
