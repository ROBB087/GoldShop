using GoldShopCore.Models;
using Microsoft.Data.Sqlite;

namespace GoldShopCore.Data;

public class AuditLogRepository
{
    public void Add(AuditLogEntry entry)
    {
        using var connection = Database.OpenConnection();
        using var command = connection.CreateCommand();
        command.CommandText = @"
INSERT INTO AuditLogs (EntityType, EntityId, Action, Actor, OldValues, NewValues, CreatedAt)
VALUES ($entityType, $entityId, $action, $actor, $oldValues, $newValues, $createdAt);";
        command.Parameters.AddWithValue("$entityType", entry.EntityType);
        command.Parameters.AddWithValue("$entityId", entry.EntityId);
        command.Parameters.AddWithValue("$action", entry.Action);
        command.Parameters.AddWithValue("$actor", entry.Actor);
        command.Parameters.AddWithValue("$oldValues", (object?)entry.OldValues ?? DBNull.Value);
        command.Parameters.AddWithValue("$newValues", (object?)entry.NewValues ?? DBNull.Value);
        command.Parameters.AddWithValue("$createdAt", entry.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        command.ExecuteNonQuery();
    }
}
