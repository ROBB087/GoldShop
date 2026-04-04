using GoldShopCore.Models;
using Microsoft.Data.Sqlite;

namespace GoldShopCore.Data;

public class TraderSummaryRepository
{
    public TraderSummarySnapshot? GetByTrader(int traderId)
    {
        using var connection = Database.OpenConnection();
        return GetByTrader(connection, null, traderId);
    }

    public TraderSummarySnapshot? GetByTrader(SqliteConnection connection, SqliteTransaction? transaction, int traderId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
SELECT TraderId, TotalEquivalent21, TotalManufacturing, TotalImprovement,
       ManufacturingDiscounts, ImprovementDiscounts, LastUpdated
FROM TraderSummaries
WHERE TraderId = $traderId;";
        command.Parameters.AddWithValue("$traderId", traderId);

        using var reader = command.ExecuteReader();
        return reader.Read() ? MapSnapshot(reader) : null;
    }

    public Dictionary<int, TraderSummarySnapshot> GetAll()
    {
        using var connection = Database.OpenConnection();
        return GetAll(connection, null);
    }

    public Dictionary<int, TraderSummarySnapshot> GetAll(SqliteConnection connection, SqliteTransaction? transaction)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
SELECT TraderId, TotalEquivalent21, TotalManufacturing, TotalImprovement,
       ManufacturingDiscounts, ImprovementDiscounts, LastUpdated
FROM TraderSummaries;";

        using var reader = command.ExecuteReader();
        var result = new Dictionary<int, TraderSummarySnapshot>();
        while (reader.Read())
        {
            var snapshot = MapSnapshot(reader);
            result[snapshot.TraderId] = snapshot;
        }

        return result;
    }

    public void RebuildAll(SqliteConnection connection, SqliteTransaction transaction)
    {
        using var delete = connection.CreateCommand();
        delete.Transaction = transaction;
        delete.CommandText = "DELETE FROM TraderSummaries;";
        delete.ExecuteNonQuery();

        using var suppliers = connection.CreateCommand();
        suppliers.Transaction = transaction;
        suppliers.CommandText = "SELECT Id FROM Suppliers ORDER BY Id;";
        using var supplierReader = suppliers.ExecuteReader();

        var supplierIds = new List<int>();
        while (supplierReader.Read())
        {
            supplierIds.Add(supplierReader.GetInt32(0));
        }

        foreach (var supplierId in supplierIds)
        {
            RefreshForTrader(connection, transaction, supplierId);
        }
    }

    public void InitializeTrader(SqliteConnection connection, SqliteTransaction transaction, int traderId)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
INSERT INTO TraderSummaries
    (TraderId, TotalEquivalent21, TotalManufacturing, TotalImprovement, ManufacturingDiscounts, ImprovementDiscounts, TotalDiscounts, NetValues, LastUpdated)
VALUES
    ($traderId, 0, 0, 0, 0, 0, 0, 0, $updatedAt)
ON CONFLICT(TraderId) DO NOTHING;";
        command.Parameters.AddWithValue("$traderId", traderId);
        command.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        command.ExecuteNonQuery();
    }

    public TraderSummarySnapshot ApplyTransactionInsert(SqliteConnection connection, SqliteTransaction transaction, SupplierTransaction transactionModel)
        => ApplyTransactionDelta(connection, transaction, transactionModel.SupplierId, transactionModel, 1m);

    public TraderSummarySnapshot ApplyTransactionDelete(SqliteConnection connection, SqliteTransaction transaction, SupplierTransaction transactionModel)
        => ApplyTransactionDelta(connection, transaction, transactionModel.SupplierId, transactionModel, -1m);

    public TraderSummarySnapshot ApplyTransactionUpdate(SqliteConnection connection, SqliteTransaction transaction, SupplierTransaction oldTransaction, SupplierTransaction newTransaction)
    {
        ApplyTransactionDelta(connection, transaction, oldTransaction.SupplierId, oldTransaction, -1m);
        return ApplyTransactionDelta(connection, transaction, newTransaction.SupplierId, newTransaction, 1m);
    }

    public TraderSummarySnapshot ApplyDiscountInsert(SqliteConnection connection, SqliteTransaction transaction, DiscountRecord discount)
        => ApplyDiscountDelta(connection, transaction, discount.SupplierId, discount.Type, discount.Amount);

    public TraderSummarySnapshot ApplyDiscountDelete(SqliteConnection connection, SqliteTransaction transaction, DiscountRecord discount)
        => ApplyDiscountDelta(connection, transaction, discount.SupplierId, discount.Type, -discount.Amount);

    public TraderSummarySnapshot ApplyDiscountUpdate(SqliteConnection connection, SqliteTransaction transaction, DiscountRecord oldDiscount, DiscountRecord newDiscount)
    {
        ApplyDiscountDelta(connection, transaction, oldDiscount.SupplierId, oldDiscount.Type, -oldDiscount.Amount);
        return ApplyDiscountDelta(connection, transaction, newDiscount.SupplierId, newDiscount.Type, newDiscount.Amount);
    }

    public void RefreshForTrader(SqliteConnection connection, SqliteTransaction transaction, int traderId)
    {
        using var delete = connection.CreateCommand();
        delete.Transaction = transaction;
        delete.CommandText = "DELETE FROM TraderSummaries WHERE TraderId = $traderId;";
        delete.Parameters.AddWithValue("$traderId", traderId);
        delete.ExecuteNonQuery();

        InitializeTrader(connection, transaction, traderId);

        var transactions = new List<SupplierTransaction>();
        using (var transactionCommand = connection.CreateCommand())
        {
            transactionCommand.Transaction = transaction;
            transactionCommand.CommandText = @"
SELECT Id, SupplierId, TxnDate, Type, Category, ItemName, Description, OriginalWeight, OriginalKarat, Equivalent21,
       ManufacturingPerGram, ImprovementPerGram, TotalManufacturing, TotalImprovement, Notes, CreatedAt, UpdatedAt
FROM SupplierTransactions
WHERE SupplierId = $traderId AND IsDeleted = 0
ORDER BY Id;";
            transactionCommand.Parameters.AddWithValue("$traderId", traderId);

            using var transactionReader = transactionCommand.ExecuteReader();
            while (transactionReader.Read())
            {
                transactions.Add(new SupplierTransaction
                {
                    Id = transactionReader.GetInt32(0),
                    SupplierId = transactionReader.GetInt32(1),
                    Date = DateTime.Parse(transactionReader.GetString(2)),
                    Type = (TransactionType)transactionReader.GetInt32(3),
                    Category = transactionReader.GetString(4),
                    ItemName = transactionReader.IsDBNull(5) ? null : transactionReader.GetString(5),
                    Description = transactionReader.IsDBNull(6) ? null : transactionReader.GetString(6),
                    OriginalWeight = ReadDecimal(transactionReader, 7),
                    OriginalKarat = transactionReader.GetInt32(8),
                    Equivalent21 = ReadDecimal(transactionReader, 9),
                    ManufacturingPerGram = ReadDecimal(transactionReader, 10),
                    ImprovementPerGram = ReadDecimal(transactionReader, 11),
                    TotalManufacturing = ReadDecimal(transactionReader, 12),
                    TotalImprovement = ReadDecimal(transactionReader, 13),
                    Notes = transactionReader.IsDBNull(14) ? null : transactionReader.GetString(14),
                    CreatedAt = DateTime.Parse(transactionReader.GetString(15)),
                    UpdatedAt = DateTime.Parse(transactionReader.GetString(16))
                });
            }
        }

        foreach (var supplierTransaction in transactions)
        {
            ApplyTransactionInsert(connection, transaction, supplierTransaction);
        }

        var discounts = new List<DiscountRecord>();
        using (var discountCommand = connection.CreateCommand())
        {
            discountCommand.Transaction = transaction;
            discountCommand.CommandText = @"
SELECT Id, SupplierId, Type, Amount, Notes, CreatedAt
FROM Discounts
WHERE SupplierId = $traderId AND IsDeleted = 0
ORDER BY Id;";
            discountCommand.Parameters.AddWithValue("$traderId", traderId);

            using var discountReader = discountCommand.ExecuteReader();
            while (discountReader.Read())
            {
                discounts.Add(new DiscountRecord
                {
                    Id = discountReader.GetInt32(0),
                    SupplierId = discountReader.GetInt32(1),
                    Type = Enum.TryParse<DiscountType>(discountReader.GetString(2), out var type) ? type : DiscountType.Manufacturing,
                    Amount = ReadDecimal(discountReader, 3),
                    Notes = discountReader.IsDBNull(4) ? null : discountReader.GetString(4),
                    CreatedAt = DateTime.Parse(discountReader.GetString(5))
                });
            }
        }

        foreach (var discount in discounts)
        {
            ApplyDiscountInsert(connection, transaction, discount);
        }
    }

    private TraderSummarySnapshot ApplyTransactionDelta(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int traderId,
        SupplierTransaction transactionModel,
        decimal multiplier)
    {
        var signedEquivalent21 = transactionModel.Type switch
        {
            TransactionType.Out => transactionModel.Equivalent21,
            TransactionType.In => -transactionModel.Equivalent21,
            _ => 0m
        };

        ApplyDelta(
            connection,
            transaction,
            traderId,
            multiplier * signedEquivalent21,
            multiplier * transactionModel.TotalManufacturing,
            multiplier * transactionModel.TotalImprovement,
            0m,
            0m);

        return GetByTrader(connection, transaction, traderId) ?? CreateEmptySnapshot(traderId);
    }

    private TraderSummarySnapshot ApplyDiscountDelta(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int traderId,
        DiscountType type,
        decimal amountDelta)
    {
        ApplyDelta(
            connection,
            transaction,
            traderId,
            0m,
            0m,
            0m,
            type == DiscountType.Manufacturing ? amountDelta : 0m,
            type == DiscountType.Improvement ? amountDelta : 0m);

        return GetByTrader(connection, transaction, traderId) ?? CreateEmptySnapshot(traderId);
    }

    private void ApplyDelta(
        SqliteConnection connection,
        SqliteTransaction transaction,
        int traderId,
        decimal equivalent21Delta,
        decimal manufacturingDelta,
        decimal improvementDelta,
        decimal manufacturingDiscountDelta,
        decimal improvementDiscountDelta)
    {
        InitializeTrader(connection, transaction, traderId);

        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
UPDATE TraderSummaries
SET
    TotalEquivalent21 = ROUND(TotalEquivalent21 + $equivalent21Delta, 4),
    TotalManufacturing = ROUND(TotalManufacturing + $manufacturingDelta, 4),
    TotalImprovement = ROUND(TotalImprovement + $improvementDelta, 4),
    ManufacturingDiscounts = ROUND(ManufacturingDiscounts + $manufacturingDiscountDelta, 4),
    ImprovementDiscounts = ROUND(ImprovementDiscounts + $improvementDiscountDelta, 4),
    TotalDiscounts = ROUND((ManufacturingDiscounts + $manufacturingDiscountDelta) + (ImprovementDiscounts + $improvementDiscountDelta), 4),
    NetValues = ROUND(
        (TotalManufacturing + $manufacturingDelta - (ManufacturingDiscounts + $manufacturingDiscountDelta)) +
        (TotalImprovement + $improvementDelta - (ImprovementDiscounts + $improvementDiscountDelta)),
        4),
    LastUpdated = $updatedAt
WHERE TraderId = $traderId;";
        command.Parameters.AddWithValue("$traderId", traderId);
        command.Parameters.AddWithValue("$equivalent21Delta", (double)equivalent21Delta);
        command.Parameters.AddWithValue("$manufacturingDelta", (double)manufacturingDelta);
        command.Parameters.AddWithValue("$improvementDelta", (double)improvementDelta);
        command.Parameters.AddWithValue("$manufacturingDiscountDelta", (double)manufacturingDiscountDelta);
        command.Parameters.AddWithValue("$improvementDiscountDelta", (double)improvementDiscountDelta);
        command.Parameters.AddWithValue("$updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        command.ExecuteNonQuery();
    }

    private static TraderSummarySnapshot MapSnapshot(SqliteDataReader reader)
    {
        return new TraderSummarySnapshot
        {
            TraderId = reader.GetInt32(0),
            TotalEquivalent21 = ReadDecimal(reader, 1),
            TotalManufacturing = ReadDecimal(reader, 2),
            TotalImprovement = ReadDecimal(reader, 3),
            ManufacturingDiscounts = ReadDecimal(reader, 4),
            ImprovementDiscounts = ReadDecimal(reader, 5),
            LastUpdated = DateTime.Parse(reader.GetString(6))
        };
    }

    private static decimal ReadDecimal(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetDouble(ordinal));
    }

    private static TraderSummarySnapshot CreateEmptySnapshot(int traderId)
    {
        return new TraderSummarySnapshot
        {
            TraderId = traderId,
            LastUpdated = DateTime.Now
        };
    }
}
