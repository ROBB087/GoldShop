using System.Windows;

namespace GoldShopWpf.Services;

public static class UiText
{
    public static string L(string key, string? fallback = null)
        => Application.Current.TryFindResource(key)?.ToString() ?? fallback ?? key;

    public static string Format(string key, params object[] args)
        => string.Format(L(key), args);

    public static string LocalizeException(string message)
    {
        return message switch
        {
            "Manufacturing payment must be zero or greater." => L("MsgManufacturingPaymentNonNegative"),
            "Refining payment must be zero or greater." => L("MsgRefiningPaymentNonNegative"),
            "Enter a manufacturing payment, a refining payment, or both." => L("MsgCashPaymentRequired"),
            "Weight must be greater than zero." => L("MsgWeightPositive"),
            "Karat must be one of the supported values: 18, 21, 24." => L("MsgKaratSupported"),
            "Manufacturing value must be zero or greater." => L("MsgManufacturingValueNonNegative"),
            "Refining value must be zero or greater." => L("MsgRefiningValueNonNegative"),
            "Default manufacturing value must be zero or greater." => L("MsgDefaultManufacturingNonNegative"),
            "Default 24K manufacturing value must be zero or greater." => L("MsgDefaultManufacturing24NonNegative"),
            "Default refining value must be zero or greater." => L("MsgDefaultRefiningNonNegative"),
            "Gold receipt cannot include manufacturing or refining values." => L("MsgGoldReceiptNoCharges"),
            "Discount amount must be greater than zero." => L("MsgDiscountGreaterThanZero"),
            "Discount cannot exceed the available total." => L("MsgDiscountExceedsTotal"),
            "Historical transactions are immutable and cannot be updated." => L("MsgTransactionsImmutable"),
            "Historical transactions are immutable and cannot be deleted." => L("MsgTransactionsImmutable"),
            "Discount records are immutable and cannot be deleted." => L("MsgDiscountsImmutable"),
            "Transaction was not found." => L("MsgTransactionNotFound", "Transaction was not found."),
            "Discount was not found." => L("MsgDiscountNotFound", "Discount was not found."),
            "Opening balance adjustment was not found." => L("MsgOpeningBalanceAdjustmentNotFound", "Opening balance adjustment was not found."),
            _ => message
        };
    }
}
