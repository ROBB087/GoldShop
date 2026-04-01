using GoldShopCore.Models;
using Microsoft.Data.Sqlite;

namespace GoldShopCore.Data;

public class PricingSettingsRepository
{
    public PricingSettings? GetLatest()
    {
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT Id, DefaultManufacturingPerGram, DefaultImprovementPerGram, CreatedAt
FROM PricingSettings
ORDER BY datetime(CreatedAt) DESC, Id DESC
LIMIT 1;";

        using var reader = command.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new PricingSettings
        {
            Id = reader.GetInt32(0),
            DefaultManufacturingPerGram = ReadDecimal(reader, 1),
            DefaultImprovementPerGram = ReadDecimal(reader, 2),
            CreatedAt = DateTime.Parse(reader.GetString(3))
        };
    }

    public void Add(PricingSettings settings)
    {
        using var connection = new SqliteConnection(Database.ConnectionString);
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO PricingSettings
    (DefaultManufacturingPerGram, DefaultImprovementPerGram, CreatedAt)
VALUES
    ($defaultManufacturingPerGram, $defaultImprovementPerGram, $createdAt);";

        command.Parameters.AddWithValue("$defaultManufacturingPerGram", (double)settings.DefaultManufacturingPerGram);
        command.Parameters.AddWithValue("$defaultImprovementPerGram", (double)settings.DefaultImprovementPerGram);
        command.Parameters.AddWithValue("$createdAt", settings.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.ExecuteNonQuery();
    }

    private static decimal ReadDecimal(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? 0m : (decimal)reader.GetDouble(ordinal);
    }
}
