using GoldShopCore.Models;
using Microsoft.Data.Sqlite;

namespace GoldShopCore.Data;

public sealed record SupplierSummaryRow(
    int SupplierId,
    decimal TotalGold21,
    decimal TotalManufacturing,
    decimal TotalImprovement);

public sealed record DailyTransactionTotals(
    DateTime Date,
    decimal TotalGold21,
    decimal TotalCharges);

public class TransactionRepository
{
    public List<SupplierTransaction> GetAll(DateTime? from, DateTime? to)
    {
        using var connection = Database.OpenConnection();
        return GetTransactions(connection, null, from, to, null, null).Items.ToList();
    }

    public List<SupplierTransaction> GetBySupplier(int supplierId, DateTime? from, DateTime? to)
    {
        using var connection = Database.OpenConnection();
        return GetTransactions(connection, supplierId, from, to, null, null).Items.ToList();
    }

    public PagedResult<SupplierTransaction> GetPaged(int? supplierId, DateTime? from, DateTime? to, int pageNumber, int pageSize)
    {
        using var connection = Database.OpenConnection();
        return GetTransactions(connection, supplierId, from, to, pageNumber, pageSize);
    }

    public SupplierTransaction? GetById(int id)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, SupplierId, TxnDate, Type, Category, ItemName, Description, OriginalWeight, OriginalKarat, Equivalent21,
       ManufacturingPerGram, ImprovementPerGram, TotalManufacturing, TotalImprovement, IdempotencyKey, Notes, IsDeleted, DeletedAt, CreatedAt, UpdatedAt
FROM SupplierTransactions
WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? MapTransaction(reader) : null;
    }

    public SupplierTransaction? GetBySupplierAndIdempotencyKey(int supplierId, string idempotencyKey)
    {
        using var connection = Database.OpenConnection();
        return GetBySupplierAndIdempotencyKey(connection, null, supplierId, idempotencyKey);
    }

    public SupplierTransaction? GetBySupplierAndIdempotencyKey(SqliteConnection connection, SqliteTransaction? transaction, int supplierId, string idempotencyKey)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
SELECT Id, SupplierId, TxnDate, Type, Category, ItemName, Description, OriginalWeight, OriginalKarat, Equivalent21,
       ManufacturingPerGram, ImprovementPerGram, TotalManufacturing, TotalImprovement, IdempotencyKey, Notes, IsDeleted, DeletedAt, CreatedAt, UpdatedAt
FROM SupplierTransactions
WHERE SupplierId = $supplierId AND IdempotencyKey = $idempotencyKey
LIMIT 1;";
        command.Parameters.AddWithValue("$supplierId", supplierId);
        command.Parameters.AddWithValue("$idempotencyKey", idempotencyKey);
        using var reader = command.ExecuteReader();
        return reader.Read() ? MapTransaction(reader) : null;
    }

    public int Add(SqliteConnection connection, SqliteTransaction transaction, SupplierTransaction transactionModel)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
INSERT INTO SupplierTransactions
    (SupplierId, TxnDate, Type, ItemName, Description, Amount, Weight, Purity, OriginalWeight, OriginalKarat, Equivalent21,
     ManufacturingPerGram, ImprovementPerGram, TotalManufacturing, TotalImprovement, Category, IdempotencyKey, Notes, IsDeleted, DeletedAt, CreatedAt, UpdatedAt)
VALUES
    ($supplierId, $date, $type, $itemName, $description, $amount, $weight, $purity, $originalWeight, $originalKarat, $equivalent21,
     $manufacturingPerGram, $improvementPerGram, $totalManufacturing, $totalImprovement, $category, $idempotencyKey, $notes, $isDeleted, $deletedAt, $createdAt, $updatedAt);
SELECT last_insert_rowid();";

        BindTransaction(command, transactionModel);
        return (int)(long)command.ExecuteScalar()!;
    }

    public void Update(SqliteConnection connection, SqliteTransaction transaction, SupplierTransaction transactionModel)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
UPDATE SupplierTransactions
SET SupplierId = $supplierId,
    TxnDate = $date,
    Type = $type,
    ItemName = $itemName,
    Description = $description,
    Amount = $amount,
    Weight = $weight,
    Purity = $purity,
    OriginalWeight = $originalWeight,
    OriginalKarat = $originalKarat,
    Equivalent21 = $equivalent21,
    ManufacturingPerGram = $manufacturingPerGram,
    ImprovementPerGram = $improvementPerGram,
    TotalManufacturing = $totalManufacturing,
    TotalImprovement = $totalImprovement,
    Category = $category,
    IdempotencyKey = $idempotencyKey,
    Notes = $notes,
    IsDeleted = $isDeleted,
    DeletedAt = $deletedAt,
    UpdatedAt = $updatedAt
WHERE Id = $id;";

        BindTransaction(command, transactionModel);
        command.Parameters.AddWithValue("$id", transactionModel.Id);
        command.ExecuteNonQuery();
    }

    public void SoftDelete(SqliteConnection connection, SqliteTransaction transaction, int id, DateTime deletedAt)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
UPDATE SupplierTransactions
SET IsDeleted = 1,
    DeletedAt = $deletedAt,
    UpdatedAt = $updatedAt
WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$deletedAt", deletedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("$updatedAt", deletedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.ExecuteNonQuery();
    }

    public TraderSummary GetSummary(int supplierId, DateTime? from, DateTime? to)
    {
        if (!from.HasValue && !to.HasValue)
        {
            var snapshot = new TraderSummaryRepository().GetByTrader(supplierId);
            if (snapshot != null)
            {
                return new TraderSummary
                {
                    TotalGold21 = snapshot.TotalEquivalent21,
                    TotalManufacturing = snapshot.TotalManufacturing,
                    TotalImprovement = snapshot.TotalImprovement,
                    ManufacturingAdjustments = snapshot.ManufacturingAdjustments,
                    ImprovementAdjustments = snapshot.ImprovementAdjustments,
                    ManufacturingDiscounts = snapshot.ManufacturingDiscounts,
                    ImprovementDiscounts = snapshot.ImprovementDiscounts
                };
            }
        }

        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        var where = BuildDateFilter(command, supplierId, from, to);
        command.CommandText = $@"
SELECT
    COALESCE(SUM({GetSignedEquivalent21Sql()}), 0),
    COALESCE(SUM(TotalManufacturing), 0),
    COALESCE(SUM(TotalImprovement), 0)
FROM SupplierTransactions
{where};";

        using var reader = command.ExecuteReader();
        var summary = new TraderSummary();
        if (reader.Read())
        {
            summary.TotalGold21 = ReadDecimal(reader, 0);
            summary.TotalManufacturing = ReadDecimal(reader, 1);
            summary.TotalImprovement = ReadDecimal(reader, 2);
        }

        var adjustments = new OpeningBalanceAdjustmentRepository().GetAdjustmentTotals(supplierId, from, to);
        summary.TotalManufacturing += adjustments.manufacturingAdjustments;
        summary.TotalImprovement += adjustments.improvementAdjustments;
        summary.ManufacturingAdjustments = adjustments.manufacturingAdjustments;
        summary.ImprovementAdjustments = adjustments.improvementAdjustments;

        return summary;
    }

    public TraderSummary GetSummaryAll(DateTime? from, DateTime? to)
    {
        if (!from.HasValue && !to.HasValue)
        {
            var snapshots = new TraderSummaryRepository().GetAll().Values;
            return new TraderSummary
            {
                TotalGold21 = snapshots.Sum(x => x.TotalEquivalent21),
                TotalManufacturing = snapshots.Sum(x => x.TotalManufacturing),
                TotalImprovement = snapshots.Sum(x => x.TotalImprovement),
                ManufacturingAdjustments = snapshots.Sum(x => x.ManufacturingAdjustments),
                ImprovementAdjustments = snapshots.Sum(x => x.ImprovementAdjustments),
                ManufacturingDiscounts = snapshots.Sum(x => x.ManufacturingDiscounts),
                ImprovementDiscounts = snapshots.Sum(x => x.ImprovementDiscounts)
            };
        }

        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        var where = BuildDateFilter(command, null, from, to);
        command.CommandText = $@"
SELECT
    COALESCE(SUM({GetSignedEquivalent21Sql()}), 0),
    COALESCE(SUM(TotalManufacturing), 0),
    COALESCE(SUM(TotalImprovement), 0)
FROM SupplierTransactions
{where};";

        using var reader = command.ExecuteReader();
        var summary = new TraderSummary();
        if (reader.Read())
        {
            summary.TotalGold21 = ReadDecimal(reader, 0);
            summary.TotalManufacturing = ReadDecimal(reader, 1);
            summary.TotalImprovement = ReadDecimal(reader, 2);
        }

        var adjustments = new OpeningBalanceAdjustmentRepository().GetAdjustmentTotalsAll(from, to);
        summary.TotalManufacturing += adjustments.manufacturingAdjustments;
        summary.TotalImprovement += adjustments.improvementAdjustments;
        summary.ManufacturingAdjustments = adjustments.manufacturingAdjustments;
        summary.ImprovementAdjustments = adjustments.improvementAdjustments;

        return summary;
    }

    public List<SupplierSummaryRow> GetSupplierSummaries(DateTime? from, DateTime? to)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        var where = BuildDateFilter(command, null, from, to);
        command.CommandText = $@"
SELECT
    SupplierId,
    COALESCE(SUM({GetSignedEquivalent21Sql()}), 0),
    COALESCE(SUM(TotalManufacturing), 0),
    COALESCE(SUM(TotalImprovement), 0)
FROM SupplierTransactions
{where}
GROUP BY SupplierId;";

        using var reader = command.ExecuteReader();
        var rows = new List<SupplierSummaryRow>();
        var adjustmentLookup = new OpeningBalanceAdjustmentRepository()
            .GetSupplierAdjustmentSummaries(from, to)
            .ToDictionary(row => row.SupplierId);

        while (reader.Read())
        {
            var supplierId = reader.GetInt32(0);
            adjustmentLookup.TryGetValue(supplierId, out var adjustment);
            rows.Add(new SupplierSummaryRow(
                supplierId,
                ReadDecimal(reader, 1),
                ReadDecimal(reader, 2) + (adjustment?.ManufacturingAdjustments ?? 0m),
                ReadDecimal(reader, 3) + (adjustment?.ImprovementAdjustments ?? 0m)));
        }

        return rows;
    }

    public Dictionary<int, decimal> GetTotalGold21BySupplier()
    {
        var result = new Dictionary<int, decimal>();
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = $@"
SELECT SupplierId, COALESCE(SUM({GetSignedEquivalent21Sql()}), 0)
FROM SupplierTransactions
WHERE IsDeleted = 0
GROUP BY SupplierId;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result[reader.GetInt32(0)] = ReadDecimal(reader, 1);
        }

        return result;
    }

    public Dictionary<int, decimal> GetNetGold21BySupplier() => GetTotalGold21BySupplier();

    public Dictionary<int, DateTime> GetLastTransactionDates()
    {
        var result = new Dictionary<int, DateTime>();
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT SupplierId, MAX(TxnDate) AS LastDate
FROM SupplierTransactions
WHERE IsDeleted = 0
GROUP BY SupplierId;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (!reader.IsDBNull(1))
            {
                result[reader.GetInt32(0)] = DateTime.Parse(reader.GetString(1));
            }
        }

        return result;
    }

    public List<DailyTransactionTotals> GetDailyTotals(DateTime from, DateTime to)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.Parameters.AddWithValue("$from", from.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$to", to.ToString("yyyy-MM-dd"));
        command.CommandText = $@"
SELECT
    TxnDate,
    COALESCE(SUM({GetSignedEquivalent21Sql()}), 0),
    COALESCE(SUM(TotalManufacturing + TotalImprovement), 0)
FROM SupplierTransactions
WHERE IsDeleted = 0 AND TxnDate >= $from AND TxnDate <= $to
GROUP BY TxnDate
ORDER BY TxnDate;";

        using var reader = command.ExecuteReader();
        var rows = new List<DailyTransactionTotals>();
        while (reader.Read())
        {
            rows.Add(new DailyTransactionTotals(
                DateTime.Parse(reader.GetString(0)),
                ReadDecimal(reader, 1),
                ReadDecimal(reader, 2)));
        }

        var adjustmentTotals = new OpeningBalanceAdjustmentRepository().GetAll(from, to)
            .GroupBy(item => item.AdjustmentDate.Date)
            .ToDictionary(
                group => group.Key,
                group => group.Sum(item => item.Amount));

        for (var index = 0; index < rows.Count; index++)
        {
            if (adjustmentTotals.TryGetValue(rows[index].Date.Date, out var adjustmentAmount))
            {
                rows[index] = rows[index] with { TotalCharges = rows[index].TotalCharges + adjustmentAmount };
            }
        }

        foreach (var adjustmentOnlyDay in adjustmentTotals.Keys.Where(day => rows.All(row => row.Date.Date != day)).OrderBy(day => day))
        {
            rows.Add(new DailyTransactionTotals(adjustmentOnlyDay, 0m, adjustmentTotals[adjustmentOnlyDay]));
        }

        rows.Sort((left, right) => left.Date.CompareTo(right.Date));

        return rows;
    }

    private static PagedResult<SupplierTransaction> GetTransactions(
        SqliteConnection connection,
        int? supplierId,
        DateTime? from,
        DateTime? to,
        int? pageNumber,
        int? pageSize)
    {
        using var countCommand = connection.CreateCommand();
        var countWhere = BuildDateFilter(countCommand, supplierId, from, to);
        countCommand.CommandText = $"SELECT COUNT(1) FROM SupplierTransactions {countWhere};";
        var totalCount = Convert.ToInt32(countCommand.ExecuteScalar());

        var items = new List<SupplierTransaction>();
        using var command = connection.CreateCommand();
        var where = BuildDateFilter(command, supplierId, from, to);
        var pagingSql = string.Empty;

        if (pageNumber.HasValue && pageSize.HasValue)
        {
            var safePageNumber = Math.Max(pageNumber.Value, 1);
            var safePageSize = Math.Max(pageSize.Value, 1);
            command.Parameters.AddWithValue("$limit", safePageSize);
            command.Parameters.AddWithValue("$offset", (safePageNumber - 1) * safePageSize);
            pagingSql = " LIMIT $limit OFFSET $offset";
        }

        command.CommandText = $@"
SELECT Id, SupplierId, TxnDate, Type, Category, ItemName, Description, OriginalWeight, OriginalKarat, Equivalent21,
       ManufacturingPerGram, ImprovementPerGram, TotalManufacturing, TotalImprovement, IdempotencyKey, Notes, IsDeleted, DeletedAt, CreatedAt, UpdatedAt
FROM SupplierTransactions
{where}
ORDER BY TxnDate DESC, Id DESC{pagingSql};";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            items.Add(MapTransaction(reader));
        }

        return new PagedResult<SupplierTransaction>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber ?? 1,
            PageSize = pageSize ?? Math.Max(totalCount, 1)
        };
    }

    private static string BuildDateFilter(SqliteCommand command, int? supplierId, DateTime? from, DateTime? to)
    {
        var where = "WHERE IsDeleted = 0";
        if (supplierId.HasValue)
        {
            where += " AND SupplierId = $supplierId";
            command.Parameters.AddWithValue("$supplierId", supplierId.Value);
        }
        if (from.HasValue)
        {
            where += " AND TxnDate >= $from";
            command.Parameters.AddWithValue("$from", from.Value.ToString("yyyy-MM-dd"));
        }
        if (to.HasValue)
        {
            where += " AND TxnDate <= $to";
            command.Parameters.AddWithValue("$to", to.Value.ToString("yyyy-MM-dd"));
        }

        return where;
    }

    private static void BindTransaction(SqliteCommand command, SupplierTransaction transaction)
    {
        command.Parameters.AddWithValue("$supplierId", transaction.SupplierId);
        command.Parameters.AddWithValue("$date", transaction.Date.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$type", (int)transaction.Type);
        command.Parameters.AddWithValue("$itemName", (object?)transaction.ItemName ?? DBNull.Value);
        command.Parameters.AddWithValue("$description", (object?)transaction.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("$amount", (double)transaction.Equivalent21);
        command.Parameters.AddWithValue("$weight", (double)transaction.OriginalWeight);
        command.Parameters.AddWithValue("$purity", transaction.OriginalKarat.ToString());
        command.Parameters.AddWithValue("$originalWeight", (double)transaction.OriginalWeight);
        command.Parameters.AddWithValue("$originalKarat", transaction.OriginalKarat);
        command.Parameters.AddWithValue("$equivalent21", (double)transaction.Equivalent21);
        command.Parameters.AddWithValue("$manufacturingPerGram", (double)transaction.ManufacturingPerGram);
        command.Parameters.AddWithValue("$improvementPerGram", (double)transaction.ImprovementPerGram);
        command.Parameters.AddWithValue("$totalManufacturing", (double)transaction.TotalManufacturing);
        command.Parameters.AddWithValue("$totalImprovement", (double)transaction.TotalImprovement);
        command.Parameters.AddWithValue("$category", transaction.Category);
        command.Parameters.AddWithValue("$idempotencyKey", (object?)transaction.IdempotencyKey ?? DBNull.Value);
        command.Parameters.AddWithValue("$notes", (object?)transaction.Notes ?? DBNull.Value);
        command.Parameters.AddWithValue("$isDeleted", transaction.IsDeleted ? 1 : 0);
        command.Parameters.AddWithValue("$deletedAt", transaction.DeletedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$createdAt", transaction.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("$updatedAt", transaction.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    private static decimal ReadDecimal(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetDouble(ordinal));
    }

    private static string GetSignedEquivalent21Sql()
    {
        return @"
CASE
    WHEN Type = 1 THEN Equivalent21
    WHEN Type = 2 THEN -Equivalent21
    ELSE 0
END";
    }

    private static SupplierTransaction MapTransaction(SqliteDataReader reader)
    {
        return new SupplierTransaction
        {
            Id = reader.GetInt32(0),
            SupplierId = reader.GetInt32(1),
            Date = DateTime.Parse(reader.GetString(2)),
            Type = (TransactionType)reader.GetInt32(3),
            Category = TransactionCategories.Normalize(reader.GetString(4), (TransactionType)reader.GetInt32(3)),
            ItemName = reader.IsDBNull(5) ? null : reader.GetString(5),
            Description = reader.IsDBNull(6) ? null : reader.GetString(6),
            OriginalWeight = ReadDecimal(reader, 7),
            OriginalKarat = reader.GetInt32(8),
            Equivalent21 = ReadDecimal(reader, 9),
            ManufacturingPerGram = ReadDecimal(reader, 10),
            ImprovementPerGram = ReadDecimal(reader, 11),
            TotalManufacturing = ReadDecimal(reader, 12),
            TotalImprovement = ReadDecimal(reader, 13),
            IdempotencyKey = reader.IsDBNull(14) ? null : reader.GetString(14),
            Notes = reader.IsDBNull(15) ? null : reader.GetString(15),
            IsDeleted = !reader.IsDBNull(16) && reader.GetInt32(16) == 1,
            DeletedAt = reader.IsDBNull(17) ? null : DateTime.Parse(reader.GetString(17)),
            CreatedAt = DateTime.Parse(reader.GetString(18)),
            UpdatedAt = DateTime.Parse(reader.GetString(19))
        };
    }
}
