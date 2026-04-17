namespace GoldShopCore.Models;

public static class TransactionCategories
{
    public const string GoldOutbound = "GoldOutbound";
    public const string GoldReceipt = "GoldReceipt";
    public const string FinishedGoldReceipt = "FinishedGoldReceipt";
    public const string CashPayment = "CashPayment";

    public static string Normalize(string? category, TransactionType type)
    {
        return category switch
        {
            GoldOutbound => GoldOutbound,
            GoldReceipt => GoldReceipt,
            FinishedGoldReceipt => FinishedGoldReceipt,
            CashPayment => CashPayment,
            _ => type == TransactionType.Out ? GoldOutbound : GoldReceipt
        };
    }

    public static TransactionType ResolveType(string? category, TransactionType fallbackType = TransactionType.Out)
    {
        return Normalize(category, fallbackType) switch
        {
            GoldOutbound => TransactionType.Out,
            GoldReceipt => TransactionType.In,
            FinishedGoldReceipt => TransactionType.In,
            CashPayment => TransactionType.In,
            _ => fallbackType
        };
    }

    public static bool SupportsCharges(string? category, TransactionType fallbackType = TransactionType.Out)
    {
        return Normalize(category, fallbackType) is GoldOutbound or FinishedGoldReceipt or CashPayment;
    }
}
