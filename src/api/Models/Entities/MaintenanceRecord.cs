namespace LogisticsApi.Models.Entities;

public class MaintenanceRecord
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public string Category { get; set; } = "Routine";         // Routine | FaultRepair
    public string Type { get; set; } = string.Empty;          // Oil Change | Tyre | Engine | Screen | Brakes | AC | Electrical | Other
    public bool FaultReported { get; set; }
    public string? FaultDescription { get; set; }
    public DateOnly? DateReported { get; set; }
    public DateOnly ScheduledDate { get; set; }
    public DateOnly? CompletedDate { get; set; }
    public string? PartsReplaced { get; set; }
    public string? RepairRemarks { get; set; }
    public string? VendorName { get; set; }
    public string? VendorContact { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Scheduled";         // Scheduled | InProgress | Completed | Overdue | Cancelled
    public decimal? Cost { get; set; }
    public string? AttachmentBlobUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
}
