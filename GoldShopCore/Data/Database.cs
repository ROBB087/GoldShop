using Microsoft.Data.Sqlite;

namespace GoldShopCore.Data;

public static class Database
{
    public static string DbFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "goldshop.db");

    public static string ConnectionString => $"Data Source={DbFilePath}";

    public static void Initialize()
    {
        Directory.CreateDirectory(AppDomain.CurrentDomain.BaseDirectory);

        using var connection = new SqliteConnection(ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
PRAGMA foreign_keys = ON;

CREATE TABLE IF NOT EXISTS Suppliers (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Name TEXT NOT NULL,
    Phone TEXT,
    Notes TEXT,
    CreatedAt TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS SupplierTransactions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    SupplierId INTEGER NOT NULL,
    TxnDate TEXT NOT NULL,
    Type TEXT NOT NULL,
    Description TEXT,
    Amount REAL NOT NULL,
    Weight REAL,
    Purity TEXT,
    Category TEXT,
    Notes TEXT,
    FOREIGN KEY (SupplierId) REFERENCES Suppliers(Id) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS IX_SupplierTransactions_SupplierId ON SupplierTransactions(SupplierId);
CREATE INDEX IF NOT EXISTS IX_SupplierTransactions_TxnDate ON SupplierTransactions(TxnDate);
";
        command.ExecuteNonQuery();

        EnsureColumn(connection, "SupplierTransactions", "Weight", "REAL");
        EnsureColumn(connection, "SupplierTransactions", "Purity", "TEXT");
        EnsureColumn(connection, "SupplierTransactions", "Category", "TEXT");

        using var migrate = connection.CreateCommand();
        migrate.CommandText = @"
UPDATE SupplierTransactions SET Type = 'GoldGiven' WHERE Type = 'Gold';
UPDATE SupplierTransactions SET Type = 'PaymentIssued' WHERE Type = 'Payment';
";
        migrate.ExecuteNonQuery();
    }

    private static void EnsureColumn(SqliteConnection connection, string table, string column, string definition)
    {
        using var check = connection.CreateCommand();
        check.CommandText = $"PRAGMA table_info({table});";
        using var reader = check.ExecuteReader();
        while (reader.Read())
        {
            var name = reader.GetString(1);
            if (string.Equals(name, column, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        using var alter = connection.CreateCommand();
        alter.CommandText = $"ALTER TABLE {table} ADD COLUMN {column} {definition};";
        alter.ExecuteNonQuery();
    }
}
