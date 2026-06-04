using GoldShopCore.Models;
using Microsoft.Data.Sqlite;

namespace GoldShopCore.Data;

public class SupplierRepository
{
    public List<Supplier> GetAll()
    {
        var suppliers = new List<Supplier>();
        using var connection = Database.OpenConnection();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Phone, WorkerName, WorkerPhone, Notes, CreatedAt, IsDeleted, DeletedAt FROM Suppliers WHERE IsDeleted = 0 ORDER BY Name";

        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            suppliers.Add(new Supplier
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Phone = reader.IsDBNull(2) ? null : reader.GetString(2),
                WorkerName = reader.IsDBNull(3) ? null : reader.GetString(3),
                WorkerPhone = reader.IsDBNull(4) ? null : reader.GetString(4),
                Notes = reader.IsDBNull(5) ? null : reader.GetString(5),
                CreatedAt = DateTime.Parse(reader.GetString(6)),
                IsDeleted = !reader.IsDBNull(7) && reader.GetInt32(7) == 1,
                DeletedAt = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8))
            });
        }

        return suppliers;
    }

    public Supplier? GetById(int id)
    {
        using var connection = Database.OpenConnection();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Phone, WorkerName, WorkerPhone, Notes, CreatedAt, IsDeleted, DeletedAt FROM Suppliers WHERE Id = $id AND IsDeleted = 0";
        command.Parameters.AddWithValue("$id", id);

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new Supplier
        {
            Id = reader.GetInt32(0),
            Name = reader.GetString(1),
            Phone = reader.IsDBNull(2) ? null : reader.GetString(2),
            WorkerName = reader.IsDBNull(3) ? null : reader.GetString(3),
            WorkerPhone = reader.IsDBNull(4) ? null : reader.GetString(4),
            Notes = reader.IsDBNull(5) ? null : reader.GetString(5),
            CreatedAt = DateTime.Parse(reader.GetString(6)),
            IsDeleted = !reader.IsDBNull(7) && reader.GetInt32(7) == 1,
            DeletedAt = reader.IsDBNull(8) ? null : DateTime.Parse(reader.GetString(8))
        };
    }

    public int Add(Supplier supplier)
    {
        using var connection = Database.OpenConnection();
        using var transaction = connection.BeginTransaction();
        var id = Add(connection, transaction, supplier);
        transaction.Commit();
        return id;
    }

    public int Add(SqliteConnection connection, SqliteTransaction transaction, Supplier supplier)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
INSERT INTO Suppliers (Name, Phone, WorkerName, WorkerPhone, Notes, CreatedAt, IsDeleted, DeletedAt)
VALUES ($name, $phone, $workerName, $workerPhone, $notes, $createdAt, 0, NULL);
SELECT last_insert_rowid();
";
        command.Parameters.AddWithValue("$name", supplier.Name);
        command.Parameters.AddWithValue("$phone", (object?)supplier.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("$workerName", (object?)supplier.WorkerName ?? DBNull.Value);
        command.Parameters.AddWithValue("$workerPhone", (object?)supplier.WorkerPhone ?? DBNull.Value);
        command.Parameters.AddWithValue("$notes", (object?)supplier.Notes ?? DBNull.Value);
        command.Parameters.AddWithValue("$createdAt", supplier.CreatedAt.ToString("yyyy-MM-dd"));

        return (int)(long)command.ExecuteScalar()!;
    }

    public void Update(Supplier supplier)
    {
        using var connection = Database.OpenConnection();

        using var command = connection.CreateCommand();
        command.CommandText = @"
UPDATE Suppliers
SET Name = $name, Phone = $phone, WorkerName = $workerName, WorkerPhone = $workerPhone, Notes = $notes
WHERE Id = $id;
";
        command.Parameters.AddWithValue("$id", supplier.Id);
        command.Parameters.AddWithValue("$name", supplier.Name);
        command.Parameters.AddWithValue("$phone", (object?)supplier.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("$workerName", (object?)supplier.WorkerName ?? DBNull.Value);
        command.Parameters.AddWithValue("$workerPhone", (object?)supplier.WorkerPhone ?? DBNull.Value);
        command.Parameters.AddWithValue("$notes", (object?)supplier.Notes ?? DBNull.Value);

        command.ExecuteNonQuery();
    }

    public void SoftDelete(SqliteConnection connection, SqliteTransaction transaction, int id, DateTime deletedAt)
    {
        using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = @"
UPDATE Suppliers
SET IsDeleted = 1,
    DeletedAt = $deletedAt
WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.Parameters.AddWithValue("$deletedAt", deletedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.ExecuteNonQuery();
    }
}
