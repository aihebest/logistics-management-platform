namespace LogisticsApi.Models.Entities;

public class TripRequest
{
    public Guid Id { get; set; }
    public Guid RequestedById { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string PickupLocation { get; set; } = string.Empty;
    public string DestinationLocation { get; set; } = string.Empty;
    public string MovementType { get; set; } = "IntraState";  // IntraState | Interstate | International
    public DateOnly? DepartureDate { get; set; }
    public string? DepartureTime { get; set; }                // HH:mm
    /// <summary>
    /// When the request was raised. Set by the server at submission — never
    /// accepted from the client, so a request cannot be back- or forward-dated.
    /// Travel timing is held in DepartureDate/DepartureTime.
    /// </summary>
    public DateTime RequestedDateTime { get; set; }

    // ── Personnel travelling ─────────────────────────────────────────────────
    public int PersonnelCount { get; set; } = 1;
    /// <summary>Names of everyone travelling — required when more than one person.</summary>
    public string? PersonnelNames { get; set; }
    /// <summary>Director | Manager | MidManagement | SeniorStaff | JuniorStaff</summary>
    public string? PersonnelCategory { get; set; }

    // ── Movement detail ──────────────────────────────────────────────────────
    /// <summary>Expected duration, e.g. "Half Day", "Full Day", "2-3 Days".</summary>
    public string? MovementDuration { get; set; }
    /// <summary>
    /// Shown to users as "Drop Off and Pick Up" — the vehicle drops the party off
    /// and returns for them, rather than waiting on site. The column keeps its
    /// original IsDropOff name so no migration is needed; the label is the source
    /// of truth for what it means to the business.
    /// </summary>
    public bool IsDropOff { get; set; }

    // ── Materials carried ────────────────────────────────────────────────────
    public bool HasMaterials { get; set; }
    public string? MaterialDescription { get; set; }
    public string Status { get; set; } = "Pending";           // Pending | Active | Ongoing | Completed | Cancelled | Unattended
    public string Priority { get; set; } = "Normal";          // Normal | High | Urgent
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public User RequestedBy { get; set; } = null!;
    public Assignment? Assignment { get; set; }
}
