namespace LogisticsApi.Models.Entities;

public class AuditLog
{
    public Guid Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
    public string? Notes { get; set; }
    public string? OldValues { get; set; }                    // JSON snapshot
    public string? NewValues { get; set; }                    // JSON snapshot
}
