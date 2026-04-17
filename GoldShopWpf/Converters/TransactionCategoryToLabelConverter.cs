using System.Globalization;
using System.Windows.Data;
using GoldShopCore.Models;
using GoldShopWpf.Services;

namespace GoldShopWpf.Converters;

public class TransactionCategoryToLabelConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var category = value as string;
        var normalized = TransactionCategories.Normalize(category, TransactionType.Out);
        return normalized switch
        {
            TransactionCategories.GoldOutbound => UiText.L("LblGoldOutboundReport"),
            TransactionCategories.GoldReceipt => UiText.L("LblGoldReceiptReport"),
            TransactionCategories.FinishedGoldReceipt => UiText.L("LblFinishedGoldReceiptReport"),
            TransactionCategories.CashPayment => UiText.L("LblCashPaymentReport"),
            _ => string.Empty
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => Binding.DoNothing;
}
