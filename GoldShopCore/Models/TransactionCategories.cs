namespace GoldShopCore.Models;

public static class TransactionCategories
{
    public const string GoldOutbound = "GoldOutbound";
    public const string GoldReceipt = "GoldReceipt";
    public const string CashPayment = "CashPayment";

    public static string Normalize(string? category, TransactionType type)
    {
        return category switch
        {
            GoldOutbound => GoldOutbound,
            GoldReceipt => GoldReceipt,
            CashPayment => CashPayment,
            _ => type == TransactionType.Out ? GoldOutbound : GoldReceipt
        };
    }
}
