namespace LogisticsApi.Models.Entities;

/// <summary>
/// A request to send a parcel by courier — documents, plaques, certificates.
///
/// Flow: the sender submits, the head of their department approves, GM
/// Logistics approves, then Logistics dispatches it and records the courier
/// and tracking number, which the sender is emailed.
///
/// Non-urgent parcels from the Port Harcourt office go out on the weekly
/// Friday run; urgent ones, and parcels from other offices, go once approved.
/// </summary>
public class ParcelDispatch
{
    public Guid Id { get; set; }

    /// <summary>Sequential reference, e.g. "PD/26/007". Assigned by the server.</summary>
    public string FormNumber { get; set; } = string.Empty;

    // ── Sender ───────────────────────────────────────────────────────────────
    public Guid RequestedById { get; set; }
    public string SenderName { get; set; } = string.Empty;
    /// <summary>Office the parcel leaves from, e.g. "Port Harcourt".</summary>
    public string SenderLocation { get; set; } = string.Empty;
    public string SenderDepartment { get; set; } = string.Empty;
    public string? SenderPhone { get; set; }

    // ── Receiver ─────────────────────────────────────────────────────────────
    public string ReceiverName { get; set; } = string.Empty;
    public string ReceiverAddress { get; set; } = string.Empty;
    public string? ReceiverPhone { get; set; }

    // ── Parcel ───────────────────────────────────────────────────────────────
    /// <summary>What is being sent — documents, award plaques, certificates.</summary>
    public string Description { get; set; } = string.Empty;
    public bool IsFragile { get; set; }
    /// <summary>Normal | High | Urgent. Urgent parcels skip the Friday run.</summary>
    public string Priority { get; set; } = "Normal";
    /// <summary>Domestic | International.</summary>
    public string Scope { get; set; } = "Domestic";
    /// <summary>FedEx | Airport Dispatch (domestic), DHL (international).</summary>
    public string Carrier { get; set; } = string.Empty;

    // ── Approval chain ───────────────────────────────────────────────────────
    /// <summary>PendingHOD | PendingManager | Approved | Dispatched | Rejected | Cancelled</summary>
    public string Status { get; set; } = "PendingHOD";

    public Guid? HodApprovedById { get; set; }
    public DateTime? HodApprovedAt { get; set; }
    public string? HodRemarks { get; set; }

    public Guid? ManagerApprovedById { get; set; }
    public DateTime? ManagerApprovedAt { get; set; }
    public string? ManagerRemarks { get; set; }

    public string? RejectionReason { get; set; }
    public DateTime? RejectedAt { get; set; }

    // ── Dispatch ─────────────────────────────────────────────────────────────
    public string? TrackingNumber { get; set; }
    public DateTime? DispatchedAt { get; set; }
    public Guid? DispatchedById { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User RequestedBy { get; set; } = null!;
    public User? HodApprovedBy { get; set; }
    public User? ManagerApprovedBy { get; set; }
    public User? DispatchedBy { get; set; }
}
