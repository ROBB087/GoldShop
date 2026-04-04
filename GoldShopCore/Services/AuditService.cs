using System.Text.Json;
using GoldShopCore.Data;
using GoldShopCore.Models;

namespace GoldShopCore.Services;

public class AuditService
{
    private readonly AuditLogRepository _auditLogRepository;

    public AuditService(AuditLogRepository auditLogRepository)
    {
        _auditLogRepository = auditLogRepository;
    }

    public void Log(string entityType, int entityId, string action, object? oldValues, object? newValues)
    {
        _auditLogRepository.Add(new AuditLogEntry
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Actor = Environment.UserName,
            OldValues = oldValues == null ? null : JsonSerializer.Serialize(oldValues),
            NewValues = newValues == null ? null : JsonSerializer.Serialize(newValues),
            CreatedAt = DateTime.Now
        });
    }
}
