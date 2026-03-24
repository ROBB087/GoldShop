using System.Globalization;
using System.Windows.Data;
using GoldShopCore.Models;

namespace GoldShopWpf.Converters;

public class TransactionCategoryToLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var category = value as string;
        var normalized = TransactionCategories.Normalize(category, TransactionType.Out);
        var isArabic = Services.LocalizationService.CurrentLanguage == "ar";

        return normalized switch
        {
            TransactionCategories.GoldOutbound => isArabic ? "صرف ذهب" : "Gold Out",
            TransactionCategories.GoldReceipt => isArabic ? "استلام ذهب" : "Gold Receipt",
            TransactionCategories.CashPayment => isArabic ? "سداد نقدي" : "Cash Payment",
            _ => string.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
