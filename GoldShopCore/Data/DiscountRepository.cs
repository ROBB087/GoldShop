using GoldShopCore.Models;
using Microsoft.Data.Sqlite;

namespace GoldShopCore.Data;

public class DiscountRepository
{
    public List<DiscountRecord> GetAll(DateTime? from, DateTime? to)
    {
        var discounts = new List<DiscountRecord>();
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        var where = "WHERE 1 = 1";

        if (from.HasValue)
        {
            where += " AND CreatedAt >= $from";
            command.Parameters.AddWithValue("$from", from.Value.ToString("yyyy-MM-dd"));
        }
        if (to.HasValue)
        {
            where += " AND CreatedAt <= $to";
            command.Parameters.AddWithValue("$to", to.Value.ToString("yyyy-MM-dd 23:59:59"));
        }

        command.CommandText = $@"
SELECT Id, SupplierId, Type, Amount, Notes, CreatedAt
FROM Discounts
{where}
ORDER BY CreatedAt DESC, Id DESC;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            discounts.Add(new DiscountRecord
            {
                Id = reader.GetInt32(0),
                SupplierId = reader.GetInt32(1),
                Type = ParseType(reader.GetString(2)),
                Amount = reader.IsDBNull(3) ? 0m : (decimal)reader.GetDouble(3),
                Notes = reader.IsDBNull(4) ? null : reader.GetString(4),
                CreatedAt = DateTime.Parse(reader.GetString(5))
            });
        }

        return discounts;
    }

    public List<DiscountRecord> GetBySupplier(int supplierId, DateTime? from, DateTime? to)
    {
        var discounts = new List<DiscountRecord>();
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        var where = "WHERE SupplierId = $supplierId";
        command.Parameters.AddWithValue("$supplierId", supplierId);

        if (from.HasValue)
        {
            where += " AND CreatedAt >= $from";
            command.Parameters.AddWithValue("$from", from.Value.ToString("yyyy-MM-dd"));
        }
        if (to.HasValue)
        {
            where += " AND CreatedAt <= $to";
            command.Parameters.AddWithValue("$to", to.Value.ToString("yyyy-MM-dd 23:59:59"));
        }

        command.CommandText = $@"
SELECT Id, SupplierId, Type, Amount, Notes, CreatedAt
FROM Discounts
{where}
ORDER BY CreatedAt DESC, Id DESC;";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            discounts.Add(new DiscountRecord
            {
                Id = reader.GetInt32(0),
                SupplierId = reader.GetInt32(1),
                Type = ParseType(reader.GetString(2)),
                Amount = reader.IsDBNull(3) ? 0m : (decimal)reader.GetDouble(3),
                Notes = reader.IsDBNull(4) ? null : reader.GetString(4),
                CreatedAt = DateTime.Parse(reader.GetString(5))
            });
        }

        return discounts;
    }

    public void Add(DiscountRecord discount)
    {
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO Discounts (SupplierId, Type, Amount, Notes, CreatedAt)
VALUES ($supplierId, $type, $amount, $notes, $createdAt);";

        command.Parameters.AddWithValue("$supplierId", discount.SupplierId);
        command.Parameters.AddWithValue("$type", discount.Type.ToString());
        command.Parameters.AddWithValue("$amount", (double)discount.Amount);
        command.Parameters.AddWithValue("$notes", (object?)discount.Notes ?? DBNull.Value);
        command.Parameters.AddWithValue("$createdAt", discount.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.ExecuteNonQuery();
    }

    public void Delete(int id)
    {
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Discounts WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    public (decimal manufacturingDiscounts, decimal improvementDiscounts) GetDiscountTotals(int supplierId, DateTime? from, DateTime? to)
    {
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        var where = "WHERE SupplierId = $supplierId";
        command.Parameters.AddWithValue("$supplierId", supplierId);

        if (from.HasValue)
        {
            where += " AND CreatedAt >= $from";
            command.Parameters.AddWithValue("$from", from.Value.ToString("yyyy-MM-dd"));
        }
        if (to.HasValue)
        {
            where += " AND CreatedAt <= $to";
            command.Parameters.AddWithValue("$to", to.Value.ToString("yyyy-MM-dd 23:59:59"));
        }

        command.CommandText = $@"
SELECT
    COALESCE(SUM(CASE WHEN Type = 'Manufacturing' THEN Amount ELSE 0 END), 0),
    COALESCE(SUM(CASE WHEN Type = 'Improvement' THEN Amount ELSE 0 END), 0)
FROM Discounts
{where};";

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return (0m, 0m);
        }

        return (
            reader.IsDBNull(0) ? 0m : (decimal)reader.GetDouble(0),
            reader.IsDBNull(1) ? 0m : (decimal)reader.GetDouble(1));
    }

    public (decimal manufacturingDiscounts, decimal improvementDiscounts) GetDiscountTotalsAll(DateTime? from, DateTime? to)
    {
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        var where = "WHERE 1 = 1";

        if (from.HasValue)
        {
            where += " AND CreatedAt >= $from";
            command.Parameters.AddWithValue("$from", from.Value.ToString("yyyy-MM-dd"));
        }
        if (to.HasValue)
        {
            where += " AND CreatedAt <= $to";
            command.Parameters.AddWithValue("$to", to.Value.ToString("yyyy-MM-dd 23:59:59"));
        }

        command.CommandText = $@"
SELECT
    COALESCE(SUM(CASE WHEN Type = 'Manufacturing' THEN Amount ELSE 0 END), 0),
    COALESCE(SUM(CASE WHEN Type = 'Improvement' THEN Amount ELSE 0 END), 0)
FROM Discounts
{where};";

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return (0m, 0m);
        }

        return (
            reader.IsDBNull(0) ? 0m : (decimal)reader.GetDouble(0),
            reader.IsDBNull(1) ? 0m : (decimal)reader.GetDouble(1));
    }

    private static DiscountType ParseType(string value)
    {
        return Enum.TryParse<DiscountType>(value, out var parsed) ? parsed : DiscountType.Manufacturing;
    }
}
