using GoldShopCore.Data;
using GoldShopCore.Models;

namespace GoldShopCore.Services;

public class TransactionService
{
    private static readonly HashSet<int> ValidKarats = new([18, 21, 22, 24]);
    private readonly TransactionRepository _transactionRepository;

    public TransactionService(TransactionRepository transactionRepository)
    {
        _transactionRepository = transactionRepository;
    }

    public List<SupplierTransaction> GetTransactions(int supplierId, DateTime? from, DateTime? to)
        => _transactionRepository.GetBySupplier(supplierId, from, to);

    public List<SupplierTransaction> GetTransactions(DateTime? from, DateTime? to)
        => _transactionRepository.GetAll(from, to);

    public TraderSummary GetSummary(int supplierId, DateTime? from, DateTime? to)
        => _transactionRepository.GetSummary(supplierId, from, to);

    public TraderSummary GetSummaryAll(DateTime? from, DateTime? to)
        => _transactionRepository.GetSummaryAll(from, to);

    public Dictionary<int, decimal> GetTotalGold21BySupplier()
        => _transactionRepository.GetTotalGold21BySupplier();

    public Dictionary<int, decimal> GetNetGold21BySupplier()
        => _transactionRepository.GetNetGold21BySupplier();

    public void AddTransaction(
        int supplierId,
        DateTime date,
        string category,
        decimal originalWeight,
        string? itemName,
        int originalKarat,
        decimal manufacturingValue,
        decimal improvementValue,
        string? notes)
    {
        var now = DateTime.Now;
        var transaction = CreateTransaction(
            supplierId,
            date,
            category,
            originalWeight,
            itemName,
            originalKarat,
            manufacturingValue,
            improvementValue,
            notes,
            now,
            now);

        _transactionRepository.Add(transaction);
    }

    public void UpdateTransaction(
        int id,
        int supplierId,
        DateTime date,
        string category,
        decimal originalWeight,
        string? itemName,
        int originalKarat,
        decimal manufacturingValue,
        decimal improvementValue,
        string? notes)
    {
        var existing = _transactionRepository.GetBySupplier(supplierId, null, null).FirstOrDefault(t => t.Id == id);
        var createdAt = existing?.CreatedAt ?? DateTime.Now;
        var transaction = CreateTransaction(
            supplierId,
            date,
            category,
            originalWeight,
            itemName,
            originalKarat,
            manufacturingValue,
            improvementValue,
            notes,
            createdAt,
            DateTime.Now);

        transaction.Id = id;
        _transactionRepository.Update(transaction);
    }

    public void DeleteTransaction(int id)
    {
        _transactionRepository.Delete(id);
    }

    private static SupplierTransaction CreateTransaction(
        int supplierId,
        DateTime date,
        string category,
        decimal originalWeight,
        string? itemName,
        int originalKarat,
        decimal manufacturingValue,
        decimal improvementValue,
        string? notes,
        DateTime createdAt,
        DateTime updatedAt)
    {
        category = TransactionCategories.Normalize(category, TransactionType.Out);
        Validate(category, originalWeight, originalKarat, manufacturingValue, improvementValue);

        var type = category == TransactionCategories.GoldOutbound ? TransactionType.Out : TransactionType.In;
        var roundedWeight = 0m;
        var roundedManufacturing = 0m;
        var roundedImprovement = 0m;
        var equivalent21 = 0m;
        var totalManufacturing = 0m;
        var totalImprovement = 0m;
        string? description;

        if (category == TransactionCategories.CashPayment)
        {
            roundedManufacturing = decimal.Round(manufacturingValue, 4, MidpointRounding.AwayFromZero);
            roundedImprovement = decimal.Round(improvementValue, 4, MidpointRounding.AwayFromZero);
            totalManufacturing = -roundedManufacturing;
            totalImprovement = -roundedImprovement;
            description = BuildCashPaymentTraceabilityText(roundedManufacturing, roundedImprovement);
            originalKarat = 21;
        }
        else
        {
            roundedWeight = decimal.Round(originalWeight, 4, MidpointRounding.AwayFromZero);
            equivalent21 = CalculateEquivalent21(roundedWeight, originalKarat);
            description = BuildTraceabilityText(roundedWeight, originalKarat, equivalent21);

            if (category == TransactionCategories.GoldOutbound)
            {
                roundedManufacturing = decimal.Round(manufacturingValue, 4, MidpointRounding.AwayFromZero);
                roundedImprovement = decimal.Round(improvementValue, 4, MidpointRounding.AwayFromZero);
                totalManufacturing = decimal.Round(roundedWeight * roundedManufacturing, 4, MidpointRounding.AwayFromZero);
                totalImprovement = decimal.Round(equivalent21 * roundedImprovement, 4, MidpointRounding.AwayFromZero);
            }
        }

        return new SupplierTransaction
        {
            SupplierId = supplierId,
            Date = date.Date,
            Type = type,
            Category = category,
            ItemName = string.IsNullOrWhiteSpace(itemName) ? null : itemName.Trim(),
            Description = description,
            OriginalWeight = roundedWeight,
            OriginalKarat = originalKarat,
            Equivalent21 = equivalent21,
            ManufacturingPerGram = roundedManufacturing,
            ImprovementPerGram = roundedImprovement,
            TotalManufacturing = totalManufacturing,
            TotalImprovement = totalImprovement,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            CreatedAt = createdAt,
            UpdatedAt = updatedAt
        };
    }

    public static decimal CalculateEquivalent21(decimal originalWeight, int originalKarat)
        => decimal.Round((originalWeight * originalKarat) / 21m, 4, MidpointRounding.AwayFromZero);

    public static string BuildTraceabilityText(decimal originalWeight, int originalKarat, decimal equivalent21)
        => $"21K eq {equivalent21:0.####} g from {originalWeight:0.####} g of {originalKarat}K";

    public static string BuildCashPaymentTraceabilityText(decimal manufacturingAmount, decimal improvementAmount)
        => $"Cash payment posted. Manufacturing {manufacturingAmount:0.####}, Refining {improvementAmount:0.####}.";

    public static void Validate(string category, decimal originalWeight, int originalKarat, decimal manufacturingValue, decimal improvementValue)
    {
        category = TransactionCategories.Normalize(category, TransactionType.Out);

        if (category == TransactionCategories.CashPayment)
        {
            if (manufacturingValue < 0)
            {
                throw new ArgumentException("Manufacturing payment must be zero or greater.", nameof(manufacturingValue));
            }

            if (improvementValue < 0)
            {
                throw new ArgumentException("Refining payment must be zero or greater.", nameof(improvementValue));
            }

            if (manufacturingValue <= 0 && improvementValue <= 0)
            {
                throw new ArgumentException("Enter a manufacturing payment, a refining payment, or both.");
            }

            return;
        }

        if (originalWeight <= 0)
        {
            throw new ArgumentException("Weight must be greater than zero.", nameof(originalWeight));
        }

        if (!ValidKarats.Contains(originalKarat))
        {
            throw new ArgumentException("Karat must be one of the supported values: 18, 21, 22, 24.", nameof(originalKarat));
        }

        if (manufacturingValue < 0)
        {
            throw new ArgumentException("Manufacturing value must be zero or greater.", nameof(manufacturingValue));
        }

        if (improvementValue < 0)
        {
            throw new ArgumentException("Refining value must be zero or greater.", nameof(improvementValue));
        }

        if (category == TransactionCategories.GoldReceipt && (manufacturingValue != 0 || improvementValue != 0))
        {
            throw new ArgumentException("Gold receipt cannot include manufacturing or refining values.");
        }
    }
}
