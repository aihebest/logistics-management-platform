using LogisticsApi.Data;
using LogisticsApi.Models.Entities;

namespace LogisticsApi.Services;

public interface IAuditService
{
    Task LogAsync(string entityType, string entityId, string action,
                  string userId, string userEmail, string? ipAddress, string? notes = null);
}

public class AuditService(AppDbContext db) : IAuditService
{
    public async Task LogAsync(string entityType, string entityId, string action,
                               string userId, string userEmail, string? ipAddress, string? notes = null)
    {
        db.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            UserId = userId,
            UserEmail = userEmail,
            Timestamp = DateTime.UtcNow,
            IpAddress = ipAddress,
            Notes = notes
        });
        await db.SaveChangesAsync();
    }
}
