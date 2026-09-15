namespace LogisticsApi.Models.Entities;

/// <summary>
/// Travel Request Form — DEL-LG-FRM-002 Rev 07.
///
/// A direct translation of the paper form the logistics team already uses, so
/// the printed output can be laid out field for field against the original.
/// Nothing is captured here that the paper form does not ask for.
///
/// Lifecycle: Draft is never persisted — a request exists from the moment it is
/// submitted. It then waits on the requester's own head of department, and after
/// that on the DMD/MD. Only once fully approved does Logistics act on it.
/// </summary>
public class TravelRequest
{
    public Guid Id { get; set; }

    /// <summary>Sequential reference, e.g. "TRF/26/018". Assigned by the server.</summary>
    public string FormNumber { get; set; } = string.Empty;
    /// <summary>When the form was raised. Server-stamped, never client-supplied.</summary>
    public DateTime FormDate { get; set; }

    public string? ProjectCostCentreCode { get; set; }

    // ── Traveller ────────────────────────────────────────────────────────────
    // Defaulted from the signed-in user's profile, but editable: a PA may raise
    // the form on someone else's behalf, which the paper form also allows.
    public Guid RequestedById { get; set; }
    public string Surname { get; set; } = string.Empty;
    public string GivenName { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string? Position { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }

    public string PurposeOfTravel { get; set; } = string.Empty;

    // ── Routing ──────────────────────────────────────────────────────────────
    public bool HotelBookingRequired { get; set; }
    public string? OtherInformation { get; set; }

    // ── Approval chain ───────────────────────────────────────────────────────
    /// <summary>PendingVerification | PendingApproval | Approved | Rejected | Cancelled</summary>
    public string Status { get; set; } = "PendingVerification";

    /// <summary>
    /// The head of department who verified the request. Their name and the moment
    /// they acted are what print in the "Verified by Head of Dept" box — an
    /// authenticated action recorded against a named account, which is stronger
    /// evidence than a handwritten signature and traceable after the fact.
    /// </summary>
    public Guid? VerifiedById { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public string? VerificationNotes { get; set; }

    /// <summary>The DMD/MD who gave final approval.</summary>
    public Guid? ApprovedById { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public string? ApprovalNotes { get; set; }

    /// <summary>Set when the request is turned down at either stage.</summary>
    public string? RejectionReason { get; set; }
    public DateTime? RejectedAt { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User RequestedBy { get; set; } = null!;
    public User? VerifiedBy { get; set; }
    public User? ApprovedBy { get; set; }
    public ICollection<TravelRequestLeg> Legs { get; set; } = [];
}

/// <summary>
/// One row of the Outbound or Inbound routing table. The paper form gives
/// several blank rows for each, so a trip with connections is one request with
/// several legs rather than several requests.
/// </summary>
public class TravelRequestLeg
{
    public Guid Id { get; set; }
    public Guid TravelRequestId { get; set; }

    /// <summary>Outbound | Inbound</summary>
    public string Direction { get; set; } = "Outbound";
    /// <summary>Order within its direction, so the rows print as they were entered.</summary>
    public int Sequence { get; set; }

    public DateOnly TravelDate { get; set; }
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string? PreferredAirline { get; set; }
    /// <summary>Free text — the form accepts "Morning" as readily as "08:30".</summary>
    public string? PreferredTime { get; set; }

    public TravelRequest TravelRequest { get; set; } = null!;
}
