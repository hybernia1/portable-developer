using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PortableDeveloper.App.Converters;

public sealed class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is string text && !string.IsNullOrWhiteSpace(text)
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}
