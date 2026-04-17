using GoldShopCore.Models;
using Microsoft.Data.Sqlite;

namespace GoldShopCore.Data;

public sealed record SupplierOpeningBalanceAdjustmentSummaryRow(
    int SupplierId,
    decimal ManufacturingAdjustments,
    decimal ImprovementAdjustments);

public class OpeningBalanceAdjustmentRepository
{
    public List<OpeningBalanceAdjustment> GetAll(DateTime? from, DateTime? to)
    {
        using var connection = Database.OpenConnection();
        return GetAdjustments(connection, null, from, to, null, null).Items.ToList();
    }

    public List<OpeningBalanceAdjustment> GetBySupplier(int supplierId, DateTime? from, DateTime? to)
    {
        using var connection = Database.OpenConnection();
        return GetAdjustments(connection, supplierId, from, to, null, null).Items.ToList();
    }

    public PagedResult<OpeningBalanceAdjustment> GetPaged(int? supplierId, DateTime? from, DateTime? to, int pageNumber, int pageSize)
    {
        using var connection = Database.OpenConnection();
        return GetAdjustments(connection, supplierId, from, to, pageNumber, pageSize);
    }

    public OpeningBalanceAdjustment? GetById(int id)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, SupplierId, Type, Amount, AdjustmentDate, Notes, CreatedAt, UpdatedAt, IsDeleted, DeletedAt
FROM OpeningBalanceAdjustments
WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        using var reader = command.ExecuteReader();
        return reader.Read() ? MapAdjustment(reader) : null;
    }

    public int Add(SqliteConnection connection, SqliteTransaction transaction, OpeningBalanceAdjustment adjustment)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
INSERT INTO OpeningBalanceAdjustments
    (SupplierId, Type, Amount, AdjustmentDate, Notes, CreatedAt, UpdatedAt, IsDeleted, DeletedAt)
VALUES
    ($supplierId, $type, $amount, $adjustmentDate, $notes, $createdAt, $updatedAt, $isDeleted, $deletedAt);
SELECT last_insert_rowid();";
        BindAdjustment(command, adjustment);
        return (int)(long)command.ExecuteScalar()!;
    }

    public void Update(SqliteConnection connection, SqliteTransaction transaction, OpeningBalanceAdjustment adjustment)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
UPDATE OpeningBalanceAdjustments
SET SupplierId = $supplierId,
    Type = $type,
    Amount = $amount,
    AdjustmentDate = $adjustmentDate,
    Notes = $notes,
    CreatedAt = $createdAt,
    UpdatedAt = $updatedAt,
    IsDeleted = $isDeleted,
    DeletedAt = $deletedAt
WHERE Id = $id;";
        BindAdjustment(command, adjustment);
        command.Parameters.AddWithValue("$id", adjustment.Id);
        command.ExecuteNonQuery();
    }

    public void SoftDelete(SqliteConnection connection, SqliteTransaction transaction, int id, DateTime deletedAt)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
UPDATE OpeningBalanceAdjustments
SET IsDeleted = 1,
    DeletedAt = $deletedAt,
    UpdatedAt = $updatedAt
WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$deletedAt", deletedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("$updatedAt", deletedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.ExecuteNonQuery();
    }

    public (decimal manufacturingAdjustments, decimal improvementAdjustments) GetAdjustmentTotals(int supplierId, DateTime? from, DateTime? to)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        var where = BuildDateFilter(command, supplierId, from, to);
        command.CommandText = $@"
SELECT
    COALESCE(SUM(CASE WHEN Type = 'Manufacturing' THEN Amount ELSE 0 END), 0),
    COALESCE(SUM(CASE WHEN Type = 'Improvement' THEN Amount ELSE 0 END), 0)
FROM OpeningBalanceAdjustments
{where};";

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return (0m, 0m);
        }

        return (ReadDecimal(reader, 0), ReadDecimal(reader, 1));
    }

    public (decimal manufacturingAdjustments, decimal improvementAdjustments) GetAdjustmentTotalsAll(DateTime? from, DateTime? to)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        var where = BuildDateFilter(command, null, from, to);
        command.CommandText = $@"
SELECT
    COALESCE(SUM(CASE WHEN Type = 'Manufacturing' THEN Amount ELSE 0 END), 0),
    COALESCE(SUM(CASE WHEN Type = 'Improvement' THEN Amount ELSE 0 END), 0)
FROM OpeningBalanceAdjustments
{where};";

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return (0m, 0m);
        }

        return (ReadDecimal(reader, 0), ReadDecimal(reader, 1));
    }

    public List<SupplierOpeningBalanceAdjustmentSummaryRow> GetSupplierAdjustmentSummaries(DateTime? from, DateTime? to)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        var where = BuildDateFilter(command, null, from, to);
        command.CommandText = $@"
SELECT
    SupplierId,
    COALESCE(SUM(CASE WHEN Type = 'Manufacturing' THEN Amount ELSE 0 END), 0),
    COALESCE(SUM(CASE WHEN Type = 'Improvement' THEN Amount ELSE 0 END), 0)
FROM OpeningBalanceAdjustments
{where}
GROUP BY SupplierId;";

        using var reader = command.ExecuteReader();
        var rows = new List<SupplierOpeningBalanceAdjustmentSummaryRow>();
        while (reader.Read())
        {
            rows.Add(new SupplierOpeningBalanceAdjustmentSummaryRow(
                reader.GetInt32(0),
                ReadDecimal(reader, 1),
                ReadDecimal(reader, 2)));
        }

        return rows;
    }

    private static PagedResult<OpeningBalanceAdjustment> GetAdjustments(
        SqliteConnection connection,
        int? supplierId,
        DateTime? from,
        DateTime? to,
        int? pageNumber,
        int? pageSize)
    {
        using var countCommand = connection.CreateCommand();
        var countWhere = BuildDateFilter(countCommand, supplierId, from, to);
        countCommand.CommandText = $"SELECT COUNT(1) FROM OpeningBalanceAdjustments {countWhere};";
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
SELECT Id, SupplierId, Type, Amount, AdjustmentDate, Notes, CreatedAt, UpdatedAt, IsDeleted, DeletedAt
FROM OpeningBalanceAdjustments
{where}
ORDER BY AdjustmentDate DESC, Id DESC{pagingSql};";

        using var reader = command.ExecuteReader();
        var items = new List<OpeningBalanceAdjustment>();
        while (reader.Read())
        {
            items.Add(MapAdjustment(reader));
        }

        return new PagedResult<OpeningBalanceAdjustment>
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
            where += " AND AdjustmentDate >= $from";
            command.Parameters.AddWithValue("$from", from.Value.ToString("yyyy-MM-dd"));
        }

        if (to.HasValue)
        {
            where += " AND AdjustmentDate <= $to";
            command.Parameters.AddWithValue("$to", to.Value.ToString("yyyy-MM-dd"));
        }

        return where;
    }

    private static void BindAdjustment(SqliteCommand command, OpeningBalanceAdjustment adjustment)
    {
        command.Parameters.AddWithValue("$supplierId", adjustment.SupplierId);
        command.Parameters.AddWithValue("$type", adjustment.Type.ToString());
        command.Parameters.AddWithValue("$amount", (double)adjustment.Amount);
        command.Parameters.AddWithValue("$adjustmentDate", adjustment.AdjustmentDate.ToString("yyyy-MM-dd"));
        command.Parameters.AddWithValue("$notes", (object?)adjustment.Notes ?? DBNull.Value);
        command.Parameters.AddWithValue("$createdAt", adjustment.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("$updatedAt", adjustment.UpdatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.Parameters.AddWithValue("$isDeleted", adjustment.IsDeleted ? 1 : 0);
        command.Parameters.AddWithValue("$deletedAt", adjustment.DeletedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? (object)DBNull.Value);
    }

    private static OpeningBalanceAdjustment MapAdjustment(SqliteDataReader reader)
    {
        return new OpeningBalanceAdjustment
        {
            Id = reader.GetInt32(0),
            SupplierId = reader.GetInt32(1),
            Type = Enum.TryParse<OpeningBalanceAdjustmentType>(reader.GetString(2), out var parsed)
                ? parsed
                : OpeningBalanceAdjustmentType.Manufacturing,
            Amount = ReadDecimal(reader, 3),
            AdjustmentDate = DateTime.Parse(reader.GetString(4)),
            Notes = reader.IsDBNull(5) ? null : reader.GetString(5),
            CreatedAt = DateTime.Parse(reader.GetString(6)),
            UpdatedAt = DateTime.Parse(reader.GetString(7)),
            IsDeleted = !reader.IsDBNull(8) && reader.GetInt32(8) == 1,
            DeletedAt = reader.IsDBNull(9) ? null : DateTime.Parse(reader.GetString(9))
        };
    }

    private static decimal ReadDecimal(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? 0m : Convert.ToDecimal(reader.GetDouble(ordinal));
    }
}
