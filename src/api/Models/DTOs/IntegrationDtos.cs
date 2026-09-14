namespace LogisticsApi.Models.DTOs;

// ═══════════════════════════════════════════════════════════════════════════════
//  Logistics ↔ General Service (genservice.desiconapp.com) integration contract
//
//  We own the vehicles. General Service carries out the repairs. When we report
//  a fault it is raised on their platform automatically, and every status change
//  they make is pushed back onto our MaintenanceRecord — so a coordinator can
//  see where a vehicle is without chasing the workshop by phone.
//
//  Both directions authenticate with a shared secret in the X-Integration-Key
//  header. These records must stay field-for-field compatible with
//  GenService.API/Models/IntegrationModels.cs.
// ═══════════════════════════════════════════════════════════════════════════════

// ── Inbound: General Service tells us where our vehicle is ────────────────────

public record GenServiceStatusPushDto(
    Guid      GenServiceRequestId,
    string    GenServiceRequestNumber,
    Guid?     LogisticsRecordId,
    Guid?     LogisticsVehicleId,
    string    VehicleRegNo,
    /// <summary>Their status verbatim — Pending, Approved, InWorkshop, AwaitingParts, AwaitingFunds, Completed, Rejected.</summary>
    string    GenServiceStatus,
    /// <summary>Already mapped into our vocabulary — Scheduled, InProgress, Completed, Cancelled.</summary>
    string    Status,
    /// <summary>True while the vehicle is off the road.</summary>
    bool      VehicleOutOfService,
    string?   MaintenanceType   = null,
    string?   Description       = null,
    string?   WorkshopName      = null,
    string?   WorkshopLocation  = null,
    string?   FaultIdentified   = null,
    string?   ProposedSolution  = null,
    string?   WorkDone          = null,
    string?   ActionedBy        = null,
    string?   RejectionReason   = null,
    decimal?  Cost              = null,
    /// <summary>YYYY-MM-DD</summary>
    string?   DateReported      = null,
    string?   CompletedDate     = null,
    string?   DateReturned      = null,
    string?   Notes             = null,
    DateTime? UpdatedAt         = null
);

public record GenServiceStatusPushAck(
    Guid?  LogisticsRecordId,
    string Message
);

// ── Outbound: we report a fault to General Service ────────────────────────────

public record LogisticsVehicleRequestDto(
    Guid     LogisticsRecordId,
    Guid     LogisticsVehicleId,
    string   VehicleRegNo,
    string?  VehicleType       = null,
    string?  AssetNo           = null,
    string?  Category          = null,
    string?  ServiceType       = null,
    string?  Description       = null,
    string?  Priority          = null,
    string?  CurrentLocation   = null,
    int?     OdometerKm        = null,
    string?  DateReported      = null,
    string?  VendorName        = null,
    string?  RequestedByEmail  = null,
    string?  RequestedByName   = null,
    string?  Notes             = null
);

public record GenServiceRequestAck(
    Guid   RequestId,
    string RequestNumber,
    string Status,
    string Message
);

// ── Outbound: our fleet, exposed so General Service can pick a real vehicle ───

public record IntegrationVehicleDto(
    Guid    Id,
    string  RegistrationNo,
    string? Make,
    string? Model,
    int?    Year,
    string? AssetTagNo,
    string? Status,
    int?    OdometerKm
);

// ── Status mapping ────────────────────────────────────────────────────────────

/// <summary>
/// Single place the two status vocabularies are reconciled. Kept in sync with
/// IntegrationStatusMap on the General Service side.
/// </summary>
public static class GenServiceStatusMap
{
    /// <summary>Their status → ours. Defensive: we re-derive rather than trusting the mapped value blindly.</summary>
    public static string ToLogistics(string genServiceStatus) => genServiceStatus switch
    {
        "Pending"       => "Scheduled",
        "Approved"      => "Scheduled",
        "InWorkshop"    => "InProgress",
        "AwaitingParts" => "InProgress",
        "AwaitingFunds" => "InProgress",
        "Completed"     => "Completed",
        "Rejected"      => "Cancelled",
        _               => "Scheduled",
    };

    /// <summary>Plain-English reason a vehicle is still out, for the coordinator's screen.</summary>
    public static string Explain(string genServiceStatus) => genServiceStatus switch
    {
        "Pending"       => "Awaiting General Service approval",
        "Approved"      => "Approved — awaiting workshop dispatch",
        "InWorkshop"    => "In the workshop",
        "AwaitingParts" => "Waiting for spare parts",
        "AwaitingFunds" => "Waiting for funds to be released",
        "Completed"     => "Repairs completed",
        "Rejected"      => "Rejected by General Service",
        _               => genServiceStatus,
    };

    /// <summary>Our record → the category General Service should raise it under.</summary>
    public static string ToGenServiceCategory(string? category, bool faultReported) =>
        faultReported || string.Equals(category, "FaultRepair", StringComparison.OrdinalIgnoreCase)
            ? "FaultRepair"
            : "Routine";
}
