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
        if (Matches(message, "Manufacturing payment must be zero or greater."))
        {
            return L("MsgManufacturingPaymentNonNegative");
        }

        if (Matches(message, "Refining payment must be zero or greater."))
        {
            return L("MsgRefiningPaymentNonNegative");
        }

        if (Matches(message, "Enter a manufacturing payment, a refining payment, or both."))
        {
            return L("MsgCashPaymentRequired");
        }

        if (Matches(message, "Weight must be greater than zero."))
        {
            return L("MsgWeightPositive");
        }

        if (Matches(message, "Karat must be one of the supported values: 18, 21, 24."))
        {
            return L("MsgKaratSupported");
        }

        if (Matches(message, "Manufacturing value must be zero or greater."))
        {
            return L("MsgManufacturingValueNonNegative");
        }

        if (Matches(message, "Refining value must be zero or greater."))
        {
            return L("MsgRefiningValueNonNegative");
        }

        if (Matches(message, "Default manufacturing value must be zero or greater."))
        {
            return L("MsgDefaultManufacturingNonNegative");
        }

        if (Matches(message, "Default 24K manufacturing value must be zero or greater."))
        {
            return L("MsgDefaultManufacturing24NonNegative");
        }

        if (Matches(message, "Default refining value must be zero or greater."))
        {
            return L("MsgDefaultRefiningNonNegative");
        }

        if (Matches(message, "Gold receipt cannot include manufacturing or refining values."))
        {
            return L("MsgGoldReceiptNoCharges");
        }

        if (Matches(message, "Discount amount must be greater than zero."))
        {
            return L("MsgDiscountGreaterThanZero");
        }

        if (Matches(message, "Discount cannot exceed the available total."))
        {
            return L("MsgDiscountExceedsTotal");
        }

        if (Matches(message, "Historical transactions are immutable and cannot be updated.")
            || Matches(message, "Historical transactions are immutable and cannot be deleted."))
        {
            return L("MsgTransactionsImmutable");
        }

        if (Matches(message, "Discount records are immutable and cannot be deleted."))
        {
            return L("MsgDiscountsImmutable");
        }

        if (Matches(message, "Transaction was not found."))
        {
            return L("MsgTransactionNotFound", "Transaction was not found.");
        }

        if (Matches(message, "Discount was not found."))
        {
            return L("MsgDiscountNotFound", "Discount was not found.");
        }

        if (Matches(message, "Opening balance adjustment was not found."))
        {
            return L("MsgOpeningBalanceAdjustmentNotFound", "Opening balance adjustment was not found.");
        }

        if (Matches(message, "The selected backup file is invalid or incomplete."))
        {
            return L("MsgInvalidBackupFile", "The selected backup file is invalid or incomplete.");
        }

        if (Matches(message, "Database restore could not continue safely."))
        {
            return L("MsgRestoreCouldNotContinue", "Database restore could not continue safely.");
        }

        return message;
    }

    private static bool Matches(string message, string expectedPrefix)
        => message.StartsWith(expectedPrefix, StringComparison.Ordinal);
}
