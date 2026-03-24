using System.Globalization;
using System.Windows.Data;
namespace GoldShopWpf.Converters;

public class MoneyFormatConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
        {
            return string.Empty;
        }

        decimal amount = value switch
        {
            decimal d => d,
            double db => (decimal)db,
            float f => (decimal)f,
            int i => i,
            long l => l,
            _ => 0m
        };

        var suffix = parameter?.ToString();
        return string.IsNullOrWhiteSpace(suffix)
            ? amount.ToString("0.00", culture)
            : $"{amount.ToString("0.00", culture)} {suffix}";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
