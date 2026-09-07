using System.Windows;
using System.Windows.Controls;
using PortableDeveloper.App.Shell;

namespace PortableDeveloper.App.Controls;

public sealed class AdaptiveSplitPanel : Panel
{
    public static readonly DependencyProperty PrimaryWeightProperty = DependencyProperty.Register(
        nameof(PrimaryWeight),
        typeof(double),
        typeof(AdaptiveSplitPanel),
        new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty SecondaryWeightProperty = DependencyProperty.Register(
        nameof(SecondaryWeight),
        typeof(double),
        typeof(AdaptiveSplitPanel),
        new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty GapProperty = DependencyProperty.Register(
        nameof(Gap),
        typeof(double),
        typeof(AdaptiveSplitPanel),
        new FrameworkPropertyMetadata(18d, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public static readonly DependencyProperty CompactFillAvailableHeightProperty = DependencyProperty.Register(
        nameof(CompactFillAvailableHeight),
        typeof(bool),
        typeof(AdaptiveSplitPanel),
        new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsMeasure));

    public double PrimaryWeight
    {
        get => (double)GetValue(PrimaryWeightProperty);
        set => SetValue(PrimaryWeightProperty, value);
    }

    public double SecondaryWeight
    {
        get => (double)GetValue(SecondaryWeightProperty);
        set => SetValue(SecondaryWeightProperty, value);
    }

    public double Gap
    {
        get => (double)GetValue(GapProperty);
        set => SetValue(GapProperty, value);
    }

    public bool CompactFillAvailableHeight
    {
        get => (bool)GetValue(CompactFillAvailableHeightProperty);
        set => SetValue(CompactFillAvailableHeightProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var children = InternalChildren.Cast<UIElement>()
            .Where(child => child.Visibility != Visibility.Collapsed)
            .Take(2)
            .ToArray();
        if (children.Length == 0)
        {
            return default;
        }

        var width = double.IsInfinity(availableSize.Width) ? 0d : availableSize.Width;
        var compact = WorkspaceLayout.GetMode(this) == WorkspaceLayoutMode.Compact || width <= 0d;
        if (children.Length == 1)
        {
            children[0].Measure(availableSize);
            return children[0].DesiredSize;
        }

        if (compact)
        {
            if (CompactFillAvailableHeight && !double.IsInfinity(availableSize.Height))
            {
                var (primaryHeight, secondaryHeight) = ResolveCompactHeights(availableSize.Height);
                children[0].Measure(new Size(availableSize.Width, primaryHeight));
                children[1].Measure(new Size(availableSize.Width, secondaryHeight));
                return availableSize;
            }

            var childConstraint = new Size(availableSize.Width, double.PositiveInfinity);
            children[0].Measure(childConstraint);
            children[1].Measure(childConstraint);
            return new Size(
                Math.Max(children[0].DesiredSize.Width, children[1].DesiredSize.Width),
                children[0].DesiredSize.Height + Gap + children[1].DesiredSize.Height);
        }

        var (primaryWidth, secondaryWidth) = ResolveWideWidths(width, availableSize.Height, children);
        children[0].Measure(new Size(primaryWidth, availableSize.Height));
        children[1].Measure(new Size(secondaryWidth, availableSize.Height));
        return new Size(width, Math.Max(children[0].DesiredSize.Height, children[1].DesiredSize.Height));
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var children = InternalChildren.Cast<UIElement>()
            .Where(child => child.Visibility != Visibility.Collapsed)
            .Take(2)
            .ToArray();
        if (children.Length == 0)
        {
            return finalSize;
        }

        if (children.Length == 1)
        {
            children[0].Arrange(new Rect(finalSize));
            return finalSize;
        }

        if (WorkspaceLayout.GetMode(this) == WorkspaceLayoutMode.Compact)
        {
            if (CompactFillAvailableHeight)
            {
                var (primaryHeight, secondaryHeight) = ResolveCompactHeights(finalSize.Height);
                children[0].Arrange(new Rect(0d, 0d, finalSize.Width, primaryHeight));
                children[1].Arrange(new Rect(0d, primaryHeight + Gap, finalSize.Width, secondaryHeight));
                return finalSize;
            }

            var firstHeight = children[0].DesiredSize.Height;
            children[0].Arrange(new Rect(0d, 0d, finalSize.Width, firstHeight));
            children[1].Arrange(new Rect(0d, firstHeight + Gap, finalSize.Width, children[1].DesiredSize.Height));
            return finalSize;
        }

        var (primaryWidth, secondaryWidth) = ResolveWideWidths(finalSize.Width, finalSize.Height, children);
        children[0].Arrange(new Rect(0d, 0d, primaryWidth, finalSize.Height));
        children[1].Arrange(new Rect(primaryWidth + Gap, 0d, secondaryWidth, finalSize.Height));
        return finalSize;
    }

    private (double Primary, double Secondary) ResolveWideWidths(
        double availableWidth,
        double availableHeight,
        IReadOnlyList<UIElement> children)
    {
        var contentWidth = Math.Max(0d, availableWidth - Gap);
        if (SecondaryWeight <= 0d)
        {
            children[1].Measure(new Size(contentWidth, availableHeight));
            var secondary = Math.Min(contentWidth, children[1].DesiredSize.Width);
            return (Math.Max(0d, contentWidth - secondary), secondary);
        }

        var primaryWeight = Math.Max(0.01d, PrimaryWeight);
        var secondaryWeight = Math.Max(0.01d, SecondaryWeight);
        var primary = contentWidth * primaryWeight / (primaryWeight + secondaryWeight);
        return (primary, contentWidth - primary);
    }

    private (double Primary, double Secondary) ResolveCompactHeights(double availableHeight)
    {
        var contentHeight = Math.Max(0d, availableHeight - Gap);
        var primaryWeight = Math.Max(0.01d, PrimaryWeight);
        var secondaryWeight = Math.Max(0.01d, SecondaryWeight);
        var primary = contentHeight * primaryWeight / (primaryWeight + secondaryWeight);
        return (primary, contentHeight - primary);
    }
}
