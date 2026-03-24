using GoldShopCore.Models;
using Microsoft.Data.Sqlite;

namespace GoldShopCore.Data;

public class SupplierRepository
{
    public List<Supplier> GetAll()
    {
        var suppliers = new List<Supplier>();
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Phone, WorkerName, WorkerPhone, Notes, CreatedAt FROM Suppliers ORDER BY Name";

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
                CreatedAt = DateTime.Parse(reader.GetString(6))
            });
        }

        return suppliers;
    }

    public Supplier? GetById(int id)
    {
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, Name, Phone, WorkerName, WorkerPhone, Notes, CreatedAt FROM Suppliers WHERE Id = $id";
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
            CreatedAt = DateTime.Parse(reader.GetString(6))
        };
    }

    public int Add(Supplier supplier)
    {
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO Suppliers (Name, Phone, WorkerName, WorkerPhone, Notes, CreatedAt)
VALUES ($name, $phone, $workerName, $workerPhone, $notes, $createdAt);
SELECT last_insert_rowid();
";
        command.Parameters.AddWithValue("$name", supplier.Name);
        command.Parameters.AddWithValue("$phone", (object?)supplier.Phone ?? DBNull.Value);
        command.Parameters.AddWithValue("$workerName", (object?)supplier.WorkerName ?? DBNull.Value);
        command.Parameters.AddWithValue("$workerPhone", (object?)supplier.WorkerPhone ?? DBNull.Value);
        command.Parameters.AddWithValue("$notes", (object?)supplier.Notes ?? DBNull.Value);
        command.Parameters.AddWithValue("$createdAt", supplier.CreatedAt.ToString("yyyy-MM-dd"));

        var id = (long)command.ExecuteScalar()!;
        return (int)id;
    }

    public void Update(Supplier supplier)
    {
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

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

    public void Delete(int id)
    {
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Suppliers WHERE Id = $id;";
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }
}
