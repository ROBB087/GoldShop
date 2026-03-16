using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using GoldShopCore.Models;

namespace GoldShopWpf.Converters;

public class TransactionTypeToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TransactionType type)
        {
            return type is TransactionType.GoldGiven or TransactionType.GoldReceived
                ? new SolidColorBrush(Color.FromRgb(212, 175, 55))
                : new SolidColorBrush(Color.FromRgb(46, 125, 50));
        }
        return Brushes.Black;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
