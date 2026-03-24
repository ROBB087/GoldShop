using GoldShopCore.Models;
using Microsoft.Data.Sqlite;

namespace GoldShopCore.Data;

public class TransactionRepository
{
    public List<SupplierTransaction> GetAll(DateTime? from, DateTime? to)
    {
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();
        return GetTransactions(connection, null, from, to);
    }

    public List<SupplierTransaction> GetBySupplier(int supplierId, DateTime? from, DateTime? to)
    {
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();
        return GetTransactions(connection, supplierId, from, to);
    }

    public void Add(SupplierTransaction transaction)
    {
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO SupplierTransactions
    (SupplierId, TxnDate, Type, Description, Amount, Weight, Purity, OriginalWeight, OriginalKarat, Equivalent21,
     ManufacturingPerGram, ImprovementPerGram, TotalManufacturing, TotalImprovement, Notes, CreatedAt, UpdatedAt)
VALUES
    ($supplierId, $date, $type, $description, $equivalent21, $originalWeight, $purity, $originalWeight, $originalKarat, $equivalent21,
     $manufacturingPerGram, $improvementPerGram, $totalManufacturing, $totalImprovement, $notes, $createdAt, $updatedAt);";

        BindTransaction(command, transaction);
        command.ExecuteNonQuery();
    }

    public void Update(SupplierTransaction transaction)
    {
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE SupplierTransactions
SET SupplierId = $supplierId,
    TxnDate = $date,
    Type = $type,
    Description = $description,
    Amount = $equivalent21,
    Weight = $originalWeight,
    Purity = $purity,
    OriginalWeight = $originalWeight,
    OriginalKarat = $originalKarat,
    Equivalent21 = $equivalent21,
    ManufacturingPerGram = $manufacturingPerGram,
    ImprovementPerGram = $improvementPerGram,
    TotalManufacturing = $totalManufacturing,
    TotalImprovement = $totalImprovement,
    Notes = $notes,
    UpdatedAt = $updatedAt
WHERE Id = $id;";

        command.Parameters.AddWithValue("$id", transaction.Id);
        BindTransaction(command, transaction);
        command.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM SupplierTransactions WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    public TraderSummary GetSummary(int supplierId, DateTime? from, DateTime? to)
    {
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

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

        var discounts = new DiscountRepository().GetDiscountTotals(supplierId, from, to);
        summary.ManufacturingDiscounts = discounts.manufacturingDiscounts;
        summary.ImprovementDiscounts = discounts.improvementDiscounts;
        return summary;
    }

    public TraderSummary GetSummaryAll(DateTime? from, DateTime? to)
    {
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

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

        return summary;
    }

    public Dictionary<int, decimal> GetTotalGold21BySupplier()
    {
        var result = new Dictionary<int, decimal>();
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $@"
SELECT SupplierId, COALESCE(SUM({GetSignedEquivalent21Sql()}), 0)
FROM SupplierTransactions
GROUP BY SupplierId;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result[reader.GetInt32(0)] = ReadDecimal(reader, 1);
        }

        return result;
    }

    public Dictionary<int, decimal> GetNetGold21BySupplier()
    {
        var result = new Dictionary<int, decimal>();
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = $@"
SELECT SupplierId,
       COALESCE(SUM({GetSignedEquivalent21Sql()}), 0)
FROM SupplierTransactions
GROUP BY SupplierId;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            result[reader.GetInt32(0)] = ReadDecimal(reader, 1);
        }

        return result;
    }

    public Dictionary<int, DateTime> GetLastTransactionDates()
    {
        var result = new Dictionary<int, DateTime>();
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT SupplierId, MAX(UpdatedAt) AS LastDate
FROM SupplierTransactions
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

    private static List<SupplierTransaction> GetTransactions(SqliteConnection connection, int? supplierId, DateTime? from, DateTime? to)
    {
        var transactions = new List<SupplierTransaction>();
        using var command = connection.CreateCommand();
        var where = BuildDateFilter(command, supplierId, from, to);
        command.CommandText = $@"
SELECT Id, SupplierId, TxnDate, Type, Description, OriginalWeight, OriginalKarat, Equivalent21,
       ManufacturingPerGram, ImprovementPerGram, TotalManufacturing, TotalImprovement, Notes, CreatedAt, UpdatedAt
FROM SupplierTransactions
{where}
ORDER BY TxnDate DESC, Id DESC;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            transactions.Add(new SupplierTransaction
            {
                Id = reader.GetInt32(0),
                SupplierId = reader.GetInt32(1),
                Date = DateTime.Parse(reader.GetString(2)),
                Type = ParseType(reader.GetString(3)),
                Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                OriginalWeight = ReadDecimal(reader, 5),
                OriginalKarat = reader.GetInt32(6),
                Equivalent21 = ReadDecimal(reader, 7),
                ManufacturingPerGram = ReadDecimal(reader, 8),
                ImprovementPerGram = ReadDecimal(reader, 9),
                TotalManufacturing = ReadDecimal(reader, 10),
                TotalImprovement = ReadDecimal(reader, 11),
                Notes = reader.IsDBNull(12) ? null : reader.GetString(12),
                CreatedAt = DateTime.Parse(reader.GetString(13)),
                UpdatedAt = DateTime.Parse(reader.GetString(14))
            });
        }

        return transactions;
    }

    private static string BuildDateFilter(SqliteCommand command, int? supplierId, DateTime? from, DateTime? to)
    {
        var where = "WHERE 1 = 1";
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
        command.Parameters.AddWithValue("$type", transaction.Type.ToString());
        command.Parameters.AddWithValue("$description", (object?)transaction.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("$originalWeight", (double)transaction.OriginalWeight);
        command.Parameters.AddWithValue("$originalKarat", transaction.OriginalKarat);
        command.Parameters.AddWithValue("$purity", transaction.OriginalKarat.ToString());
        command.Parameters.AddWithValue("$equivalent21", (double)transaction.Equivalent21);
        command.Parameters.AddWithValue("$manufacturingPerGram", (double)transaction.ManufacturingPerGram);
        command.Parameters.AddWithValue("$improvementPerGram", (double)transaction.ImprovementPerGram);
        command.Parameters.AddWithValue("$totalManufacturing", (double)transaction.TotalManufacturing);
        command.Parameters.AddWithValue("$totalImprovement", (double)transaction.TotalImprovement);
        command.Parameters.AddWithValue("$notes", (object?)transaction.Notes ?? DBNull.Value);
        command.Parameters.AddWithValue("$createdAt", transaction.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("$updatedAt", transaction.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    private static decimal ReadDecimal(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? 0m : (decimal)reader.GetDouble(ordinal);
    }

    private static string GetSignedEquivalent21Sql()
    {
        const string equivalent21Sql = @"
CASE
    WHEN COALESCE(Equivalent21, 0) <> 0 THEN Equivalent21
    ELSE (
        COALESCE(OriginalWeight, Weight, Amount, 0) *
        COALESCE(NULLIF(OriginalKarat, 0), CAST(NULLIF(TRIM(Purity), '') AS INTEGER), 21) / 21.0
    )
END";

        return $@"
CASE
    WHEN Type IN ('Out', 'GoldGiven', 'PaymentIssued', 'Gold') THEN ({equivalent21Sql})
    WHEN Type IN ('In', 'GoldReceived', 'PaymentReceived', 'Payment') THEN -({equivalent21Sql})
    ELSE 0
END";
    }

    private static TransactionType ParseType(string value)
    {
        if (Enum.TryParse<TransactionType>(value, out var parsed))
        {
            return parsed;
        }

        return value switch
        {
            "GoldGiven" => TransactionType.Out,
            "PaymentIssued" => TransactionType.Out,
            "GoldReceived" => TransactionType.In,
            "PaymentReceived" => TransactionType.In,
            "Gold" => TransactionType.Out,
            "Payment" => TransactionType.In,
            _ => TransactionType.Out
        };
    }
}
