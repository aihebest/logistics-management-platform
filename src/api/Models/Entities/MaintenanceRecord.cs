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

    // ── General Service link ──────────────────────────────────────────────────
    // Vehicles are ours, but the repair is carried out by the General Service
    // department on their own platform (genservice.desiconapp.com). When we
    // report a fault it is raised there automatically, and every status change
    // they make is pushed back onto this record. These columns are the
    // cross-reference plus the last thing they told us.

    /// <summary>Id of the matching Vehicle Maintenance Request in GenService.</summary>
    public Guid? GenServiceRequestId { get; set; }

    /// <summary>GenService reference, e.g. "V/26/022" — what the workshop quotes on the phone.</summary>
    public string? GenServiceRequestNumber { get; set; }

    /// <summary>
    /// GenService's own status word, kept verbatim alongside our mapped Status.
    /// Their vocabulary is finer than ours — "AwaitingParts" and "AwaitingFunds"
    /// both map to our "InProgress", and the difference is exactly what a
    /// coordinator chasing a vehicle needs to know.
    /// </summary>
    public string? GenServiceStatus { get; set; }

    /// <summary>Fault the GenService workshop actually found, as opposed to what was reported.</summary>
    public string? GenServiceFaultIdentified { get; set; }

    /// <summary>Work GenService carried out.</summary>
    public string? GenServiceWorkDone { get; set; }

    /// <summary>Where the vehicle physically is, when GenService has sent it out.</summary>
    public string? GenServiceWorkshopName { get; set; }

    /// <summary>When GenService last pushed an update onto this record.</summary>
    public DateTime? GenServiceSyncedAt { get; set; }

    /// <summary>Where this record started life: Logistics (here) or GenService.</summary>
    public string SourceSystem { get; set; } = "Logistics";

    // ── Reminder de-duplication ───────────────────────────────────────────────
    // These stop the reminder job re-sending the same email on every pass. They
    // are persisted rather than held in memory so a restart or a second instance
    // can't start the mail over again.

    /// <summary>When a "maintenance due" reminder was last emailed for this record.</summary>
    public DateTime? LastReminderSentAt { get; set; }

    /// <summary>When an "overdue" notice was last emailed for this record.</summary>
    public DateTime? LastOverdueNoticeAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
}
