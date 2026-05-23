namespace LogisticsApi.Models.Entities;

public class MaintenanceRecord
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public string Type { get; set; } = string.Empty;           // Routine Service | Oil Change | Tyre Replacement | etc.
    public DateOnly ScheduledDate { get; set; }
    public DateOnly? CompletedDate { get; set; }
    public decimal? Cost { get; set; }
    public string? VendorName { get; set; }
    public string? VendorContact { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Scheduled";          // Scheduled | InProgress | Completed | Overdue | Cancelled
    public string? AttachmentBlobUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
}
