namespace LogisticsApi.Models.Entities;

public class Notification
{
    public Guid Id { get; set; }
    public Guid RecipientId { get; set; }
    public string Channel { get; set; } = "InApp";            // InApp | Email | SMS
    public string Type { get; set; } = string.Empty;          // AssignmentConfirmed | MaintenanceDue | etc.
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public string Status { get; set; } = "Pending";           // Pending | Sent | Failed
    public DateTime? SentAt { get; set; }
    public string? RelatedEntityType { get; set; }
    public string? RelatedEntityId { get; set; }
    public DateTime CreatedAt { get; set; }

    public User Recipient { get; set; } = null!;
}
