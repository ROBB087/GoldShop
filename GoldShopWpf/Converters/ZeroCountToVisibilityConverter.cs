using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GoldShopWpf.Converters;

public class ZeroCountToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var isInverse = string.Equals(parameter?.ToString(), "Inverse", StringComparison.OrdinalIgnoreCase);
        var count = value switch
        {
            int intValue => intValue,
            long longValue => (int)longValue,
            _ => 0
        };

        var show = isInverse ? count > 0 : count == 0;
        return show ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
