namespace LogisticsApi.Models.Entities;

public class MaintenanceRecord
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public string Category { get; set; } = "Routine";         // Routine | FaultRepair
    public string Type { get; set; } = string.Empty;          // Oil Change | Tyre | Engine | Screen | Brakes | AC | Electrical | Other
    public bool FaultReported { get; set; }
    public string? FaultDescription { get; set; }
    /// <summary>
    /// Legacy fault-only date. Superseded by ScheduledDate, which the UI now
    /// labels "Date Reported". Kept so historic records don't lose the value.
    /// </summary>
    public DateOnly? DateReported { get; set; }
    /// <summary>
    /// Shown to users as "Date Reported" — when the vehicle was reported for
    /// service or repair. The column keeps its ScheduledDate name because the
    /// overdue flag, the reminder job and the reports all key off it.
    /// </summary>
    public DateOnly ScheduledDate { get; set; }
    public DateOnly? CompletedDate { get; set; }
    /// <summary>When the vehicle came back from the workshop and re-entered service.</summary>
    public DateOnly? DateReturned { get; set; }
    public string? PartsReplaced { get; set; }
    public string? RepairRemarks { get; set; }
    public string? VendorName { get; set; }
    public string? Notes { get; set; }
    public string Status { get; set; } = "Scheduled";         // Scheduled | InProgress | Completed | Overdue | Cancelled
    public decimal? Cost { get; set; }
    public string? AttachmentBlobUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
}
