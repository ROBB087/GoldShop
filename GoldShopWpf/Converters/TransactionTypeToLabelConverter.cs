using System.Globalization;
using System.Windows.Data;
using GoldShopCore.Models;

namespace GoldShopWpf.Converters;

public class TransactionTypeToLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is TransactionType type)
        {
            var isArabic = GoldShopWpf.Services.LocalizationService.CurrentLanguage == "ar";
            if (isArabic)
            {
                return type switch
                {
                    TransactionType.Out => "خارج",
                    TransactionType.In => "داخل",
                    _ => string.Empty
                };
            }

            return type switch
            {
                TransactionType.Out => "OUT",
                TransactionType.In => "IN",
                _ => string.Empty
            };
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}

