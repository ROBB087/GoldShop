using GoldShopCore.Models;
using Microsoft.Data.Sqlite;

namespace GoldShopCore.Data;

public sealed record SupplierDiscountSummaryRow(
    int SupplierId,
    decimal ManufacturingDiscounts,
    decimal ImprovementDiscounts);

public class DiscountRepository
{
    public List<DiscountRecord> GetAll(DateTime? from, DateTime? to)
    {
        using var connection = Database.OpenConnection();
        return GetDiscounts(connection, null, from, to, null, null).Items.ToList();
    }

    public List<DiscountRecord> GetBySupplier(int supplierId, DateTime? from, DateTime? to)
    {
        using var connection = Database.OpenConnection();
        return GetDiscounts(connection, supplierId, from, to, null, null).Items.ToList();
    }

    public PagedResult<DiscountRecord> GetPaged(int? supplierId, DateTime? from, DateTime? to, int pageNumber, int pageSize)
    {
        using var connection = Database.OpenConnection();
        return GetDiscounts(connection, supplierId, from, to, pageNumber, pageSize);
    }

    public DiscountRecord? GetById(int id)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, SupplierId, Type, Amount, Notes, CreatedAt, UpdatedAt, IsDeleted, DeletedAt
FROM Discounts
WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? MapDiscount(reader) : null;
    }

    public int Add(SqliteConnection connection, SqliteTransaction transaction, DiscountRecord discount)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
INSERT INTO Discounts (SupplierId, Type, Amount, Notes, UpdatedAt, IsDeleted, DeletedAt, CreatedAt)
VALUES ($supplierId, $type, $amount, $notes, $updatedAt, $isDeleted, $deletedAt, $createdAt);
SELECT last_insert_rowid();";

        BindDiscount(command, discount);
        return (int)(long)command.ExecuteScalar()!;
    }

    public void Update(SqliteConnection connection, SqliteTransaction transaction, DiscountRecord discount)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
UPDATE Discounts
SET SupplierId = $supplierId,
    Type = $type,
    Amount = $amount,
    Notes = $notes,
    UpdatedAt = $updatedAt,
    IsDeleted = $isDeleted,
    DeletedAt = $deletedAt
WHERE Id = $id;";
        BindDiscount(command, discount);
        command.Parameters.AddWithValue("$id", discount.Id);
        command.ExecuteNonQuery();
    }

    public void SoftDelete(SqliteConnection connection, SqliteTransaction transaction, int id, DateTime deletedAt)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
UPDATE Discounts
SET IsDeleted = 1,
    DeletedAt = $deletedAt,
    UpdatedAt = $updatedAt
WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$deletedAt", deletedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("$updatedAt", deletedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.ExecuteNonQuery();
    }

    public (decimal manufacturingDiscounts, decimal improvementDiscounts) GetDiscountTotals(int supplierId, DateTime? from, DateTime? to)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        var where = BuildDateFilter(command, supplierId, from, to);
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

        return (ReadDecimal(reader, 0), ReadDecimal(reader, 1));
    }

    public (decimal manufacturingDiscounts, decimal improvementDiscounts) GetDiscountTotalsAll(DateTime? from, DateTime? to)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        var where = BuildDateFilter(command, null, from, to);
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

        return (ReadDecimal(reader, 0), ReadDecimal(reader, 1));
    }

    public List<SupplierDiscountSummaryRow> GetSupplierDiscountSummaries(DateTime? from, DateTime? to)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        var where = BuildDateFilter(command, null, from, to);
        command.CommandText = $@"
SELECT
    SupplierId,
    COALESCE(SUM(CASE WHEN Type = 'Manufacturing' THEN Amount ELSE 0 END), 0),
    COALESCE(SUM(CASE WHEN Type = 'Improvement' THEN Amount ELSE 0 END), 0)
FROM Discounts
{where}
GROUP BY SupplierId;";

        using var reader = command.ExecuteReader();
        var rows = new List<SupplierDiscountSummaryRow>();
        while (reader.Read())
        {
            rows.Add(new SupplierDiscountSummaryRow(
                reader.GetInt32(0),
                ReadDecimal(reader, 1),
                ReadDecimal(reader, 2)));
        }

        return rows;
    }

    private static PagedResult<DiscountRecord> GetDiscounts(
        SqliteConnection connection,
        int? supplierId,
        DateTime? from,
        DateTime? to,
        int? pageNumber,
        int? pageSize)
    {
        using var countCommand = connection.CreateCommand();
        var countWhere = BuildDateFilter(countCommand, supplierId, from, to);
        countCommand.CommandText = $"SELECT COUNT(1) FROM Discounts {countWhere};";
        var totalCount = Convert.ToInt32(countCommand.ExecuteScalar());

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
SELECT Id, SupplierId, Type, Amount, Notes, CreatedAt, UpdatedAt, IsDeleted, DeletedAt
FROM Discounts
{where}
ORDER BY CreatedAt DESC, Id DESC{pagingSql};";

        using var reader = command.ExecuteReader();
        var items = new List<DiscountRecord>();
        while (reader.Read())
        {
            items.Add(MapDiscount(reader));
        }

        return new PagedResult<DiscountRecord>
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
            where += " AND CreatedAt >= $from";
            command.Parameters.AddWithValue("$from", from.Value.ToString("yyyy-MM-dd"));
        }

        if (to.HasValue)
        {
            where += " AND CreatedAt <= $to";
            command.Parameters.AddWithValue("$to", to.Value.ToString("yyyy-MM-dd 23:59:59"));
        }

        return where;
    }

    private static void BindDiscount(SqliteCommand command, DiscountRecord discount)
    {
        command.Parameters.AddWithValue("$supplierId", discount.SupplierId);
        command.Parameters.AddWithValue("$type", discount.Type.ToString());
        command.Parameters.AddWithValue("$amount", (double)discount.Amount);
        command.Parameters.AddWithValue("$notes", (object?)discount.Notes ?? DBNull.Value);
        command.Parameters.AddWithValue("$updatedAt", discount.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("$isDeleted", discount.IsDeleted ? 1 : 0);
        command.Parameters.AddWithValue("$deletedAt", discount.DeletedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
        command.Parameters.AddWithValue("$createdAt", discount.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    private static decimal ReadDecimal(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetDouble(ordinal));
    }

    private static DiscountRecord MapDiscount(SqliteDataReader reader)
    {
        return new DiscountRecord
        {
            Id = reader.GetInt32(0),
            SupplierId = reader.GetInt32(1),
            Type = Enum.TryParse<DiscountType>(reader.GetString(2), out var parsed) ? parsed : DiscountType.Manufacturing,
            Amount = ReadDecimal(reader, 3),
            Notes = reader.IsDBNull(4) ? null : reader.GetString(4),
            CreatedAt = DateTime.Parse(reader.GetString(5)),
            UpdatedAt = reader.IsDBNull(6) || string.IsNullOrWhiteSpace(reader.GetString(6))
                ? DateTime.Parse(reader.GetString(5))
                : DateTime.Parse(reader.GetString(6)),
            IsDeleted = !reader.IsDBNull(7) && reader.GetInt32(7) == 1,
            DeletedAt = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8))
        };
    }
}
