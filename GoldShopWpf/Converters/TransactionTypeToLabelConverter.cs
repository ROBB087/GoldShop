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
                    TransactionType.GoldGiven => "ذهب طالع",
                    TransactionType.GoldReceived => "ذهب داخل",
                    TransactionType.PaymentIssued => "دفعة خارجة",
                    TransactionType.PaymentReceived => "دفعة داخلة",
                    _ => string.Empty
                };
            }

            return type switch
            {
                TransactionType.GoldGiven => "Gold Given",
                TransactionType.GoldReceived => "Gold Received",
                TransactionType.PaymentIssued => "Payment Issued",
                TransactionType.PaymentReceived => "Payment Received",
                _ => string.Empty
            };
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
