using GoldShopCore.Models;
using Microsoft.Data.Sqlite;

namespace GoldShopCore.Data;

public class TransactionRepository
{
    public List<SupplierTransaction> GetAll(DateTime? from, DateTime? to)
    {
        var transactions = new List<SupplierTransaction>();
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        var where = "WHERE 1=1";
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

        command.CommandText = $@"
SELECT Id, SupplierId, TxnDate, Type, Description, Amount, Weight, Purity, Category, Notes
FROM SupplierTransactions
{where}
ORDER BY TxnDate, Id;";

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
                Amount = (decimal)reader.GetDouble(5),
                Weight = reader.IsDBNull(6) ? null : (decimal?)reader.GetDouble(6),
                Purity = reader.IsDBNull(7) ? null : reader.GetString(7),
                Category = ParseCategory(reader.IsDBNull(8) ? null : reader.GetString(8)),
                Notes = reader.IsDBNull(9) ? null : reader.GetString(9)
            });
        }

        return transactions;
    }

    public List<SupplierTransaction> GetBySupplier(int supplierId, DateTime? from, DateTime? to)
    {
        var transactions = new List<SupplierTransaction>();
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        var where = "WHERE SupplierId = $supplierId";
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

        command.CommandText = $@"
SELECT Id, SupplierId, TxnDate, Type, Description, Amount, Weight, Purity, Category, Notes
FROM SupplierTransactions
{where}
ORDER BY TxnDate, Id;";
        command.Parameters.AddWithValue("$supplierId", supplierId);

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
                Amount = (decimal)reader.GetDouble(5),
                Weight = reader.IsDBNull(6) ? null : (decimal?)reader.GetDouble(6),
                Purity = reader.IsDBNull(7) ? null : reader.GetString(7),
                Category = ParseCategory(reader.IsDBNull(8) ? null : reader.GetString(8)),
                Notes = reader.IsDBNull(9) ? null : reader.GetString(9)
            });
        }

        return transactions;
    }

    public void Add(SupplierTransaction transaction)
    {
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO SupplierTransactions (SupplierId, TxnDate, Type, Description, Amount, Weight, Purity, Category, Notes)
VALUES ($supplierId, $date, $type, $description, $amount, $weight, $purity, $category, $notes);
";
        command.Parameters.AddWithValue("$supplierId", transaction.SupplierId);
        command.Parameters.AddWithValue("$date", transaction.Date.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$type", transaction.Type.ToString());
        command.Parameters.AddWithValue("$description", (object?)transaction.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("$amount", (double)transaction.Amount);
        command.Parameters.AddWithValue("$weight", transaction.Weight.HasValue ? (double)transaction.Weight.Value : DBNull.Value);
        command.Parameters.AddWithValue("$purity", (object?)transaction.Purity ?? DBNull.Value);
        command.Parameters.AddWithValue("$category", transaction.Category == TransactionCategory.None ? DBNull.Value : transaction.Category.ToString());
        command.Parameters.AddWithValue("$notes", (object?)transaction.Notes ?? DBNull.Value);

        command.ExecuteNonQuery();
    }

    public void Update(SupplierTransaction transaction)
    {
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE SupplierTransactions
SET TxnDate = $date, Type = $type, Description = $description, Amount = $amount, Weight = $weight, Purity = $purity, Category = $category, Notes = $notes
WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", transaction.Id);
        command.Parameters.AddWithValue("$date", transaction.Date.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$type", transaction.Type.ToString());
        command.Parameters.AddWithValue("$description", (object?)transaction.Description ?? DBNull.Value);
        command.Parameters.AddWithValue("$amount", (double)transaction.Amount);
        command.Parameters.AddWithValue("$weight", transaction.Weight.HasValue ? (double)transaction.Weight.Value : DBNull.Value);
        command.Parameters.AddWithValue("$purity", (object?)transaction.Purity ?? DBNull.Value);
        command.Parameters.AddWithValue("$category", transaction.Category == TransactionCategory.None ? DBNull.Value : transaction.Category.ToString());
        command.Parameters.AddWithValue("$notes", (object?)transaction.Notes ?? DBNull.Value);
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

    public (decimal goldGiven, decimal goldReceived, decimal paymentsIssued, decimal paymentsReceived) GetTotals(int supplierId, DateTime? from, DateTime? to)
    {
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        var where = "WHERE SupplierId = $supplierId";
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

        command.CommandText = $@"
SELECT
    SUM(CASE WHEN Type IN ('GoldGiven','Gold') THEN Amount ELSE 0 END) AS GoldGivenTotal,
    SUM(CASE WHEN Type = 'GoldReceived' THEN Amount ELSE 0 END) AS GoldReceivedTotal,
    SUM(CASE WHEN Type IN ('PaymentIssued','Payment') THEN Amount ELSE 0 END) AS PaymentIssuedTotal,
    SUM(CASE WHEN Type = 'PaymentReceived' THEN Amount ELSE 0 END) AS PaymentReceivedTotal
FROM SupplierTransactions
{where};";
        command.Parameters.AddWithValue("$supplierId", supplierId);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return (0m, 0m, 0m, 0m);
        }

        var goldGiven = reader.IsDBNull(0) ? 0m : (decimal)reader.GetDouble(0);
        var goldReceived = reader.IsDBNull(1) ? 0m : (decimal)reader.GetDouble(1);
        var paymentsIssued = reader.IsDBNull(2) ? 0m : (decimal)reader.GetDouble(2);
        var paymentsReceived = reader.IsDBNull(3) ? 0m : (decimal)reader.GetDouble(3);
        return (goldGiven, goldReceived, paymentsIssued, paymentsReceived);
    }

    public (decimal goldGiven, decimal goldReceived, decimal paymentsIssued, decimal paymentsReceived) GetTotalsAll(DateTime? from, DateTime? to)
    {
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        var where = "WHERE 1=1";
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

        command.CommandText = $@"
SELECT
    SUM(CASE WHEN Type IN ('GoldGiven','Gold') THEN Amount ELSE 0 END) AS GoldGivenTotal,
    SUM(CASE WHEN Type = 'GoldReceived' THEN Amount ELSE 0 END) AS GoldReceivedTotal,
    SUM(CASE WHEN Type IN ('PaymentIssued','Payment') THEN Amount ELSE 0 END) AS PaymentIssuedTotal,
    SUM(CASE WHEN Type = 'PaymentReceived' THEN Amount ELSE 0 END) AS PaymentReceivedTotal
FROM SupplierTransactions
{where};";

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return (0m, 0m, 0m, 0m);
        }

        var goldGiven = reader.IsDBNull(0) ? 0m : (decimal)reader.GetDouble(0);
        var goldReceived = reader.IsDBNull(1) ? 0m : (decimal)reader.GetDouble(1);
        var paymentsIssued = reader.IsDBNull(2) ? 0m : (decimal)reader.GetDouble(2);
        var paymentsReceived = reader.IsDBNull(3) ? 0m : (decimal)reader.GetDouble(3);
        return (goldGiven, goldReceived, paymentsIssued, paymentsReceived);
    }

    public Dictionary<int, decimal> GetBalancesBySupplier()
    {
        var result = new Dictionary<int, decimal>();
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT SupplierId,
       SUM(CASE WHEN Type IN ('GoldGiven','Gold','PaymentReceived') THEN Amount ELSE 0 END) -
       SUM(CASE WHEN Type IN ('GoldReceived','PaymentIssued','Payment') THEN Amount ELSE 0 END) AS Balance
FROM SupplierTransactions
GROUP BY SupplierId;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var supplierId = reader.GetInt32(0);
            var balance = reader.IsDBNull(1) ? 0m : (decimal)reader.GetDouble(1);
            result[supplierId] = balance;
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
SELECT SupplierId, MAX(TxnDate) AS LastDate
FROM SupplierTransactions
GROUP BY SupplierId;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            var supplierId = reader.GetInt32(0);
            if (!reader.IsDBNull(1))
            {
                result[supplierId] = DateTime.Parse(reader.GetString(1));
            }
        }

        return result;
    }

    private static TransactionCategory ParseCategory(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return TransactionCategory.None;
        }

        return Enum.TryParse<TransactionCategory>(value, out var parsed) ? parsed : TransactionCategory.None;
    }

    private static TransactionType ParseType(string value)
    {
        if (Enum.TryParse<TransactionType>(value, out var parsed))
        {
            return parsed;
        }

        return value switch
        {
            "Gold" => TransactionType.GoldGiven,
            "Payment" => TransactionType.PaymentIssued,
            _ => TransactionType.PaymentIssued
        };
    }
}
