namespace LogisticsApi.Models.DTOs;

// ── Maintenance Records ───────────────────────────────────────────────────────

public record MaintenanceRecordDto(
    Guid Id,
    Guid VehicleId,
    string VehicleReg,
    string Type,
    string Category,          // Routine | FaultRepair
    DateOnly ScheduledDate,   // shown to users as "Date Reported"
    DateOnly? CompletedDate,
    DateOnly? DateReturned,   // vehicle back from the workshop
    decimal? Cost,
    string? VendorName,
    string? Notes,
    string Status,
    string? AttachmentBlobUrl,
    // Fault / Repair fields
    bool FaultReported,
    string? FaultDescription,
    DateOnly? DateReported,
    string? PartsReplaced,
    string? RepairRemarks,
    DateTime CreatedAt,
    // ── General Service link ─────────────────────────────────────────────────
    /// <summary>Their reference, e.g. "V/26/022" — what the workshop quotes on the phone.</summary>
    string?   GenServiceRequestNumber = null,
    /// <summary>Their status verbatim: Pending, Approved, InWorkshop, AwaitingParts, AwaitingFunds, Completed, Rejected.</summary>
    string?   GenServiceStatus = null,
    /// <summary>Plain-English rendering of the above, for the coordinator's screen.</summary>
    string?   GenServiceStatusLabel = null,
    string?   GenServiceFaultIdentified = null,
    string?   GenServiceWorkDone = null,
    string?   GenServiceWorkshopName = null,
    DateTime? GenServiceSyncedAt = null,
    /// <summary>"Logistics" if raised here, "GenService" if it started on their platform.</summary>
    string?   SourceSystem = null
);

public record CreateMaintenanceRecordDto(
    Guid VehicleId,
    string Type,
    string Category,          // Routine | FaultRepair
    DateOnly ScheduledDate,   // captured on the form as "Date Reported"
    string? VendorName,
    string? Notes,
    // Fault fields (used when Category = FaultRepair)
    bool FaultReported = false,
    string? FaultDescription = null,
    DateOnly? DateReturned = null,
    string? PartsReplaced = null,
    string? RepairRemarks = null
);

public record UpdateMaintenanceRecordDto(
    string? Status,
    DateOnly? CompletedDate,
    DateOnly? DateReturned,
    decimal? Cost,
    string? VendorName,
    string? Notes,
    string? AttachmentBlobUrl,
    string? PartsReplaced,
    string? RepairRemarks
);

// ── Fuel Logs ─────────────────────────────────────────────────────────────────

public record FuelLogDto(
    Guid Id,
    Guid VehicleId,
    string VehicleReg,
    string LoggedByName,
    DateOnly FuelDate,
    string ProductType,       // PMS | AGO | DPK | CNG
    decimal LitresFilled,
    decimal CostPerLitre,
    decimal TotalCost,
    string PaymentMethod,     // Card | Cash | Credit | Transfer
    bool IsCashPayment,       // legacy mirror, kept so older clients keep working
    int OdometerAtFill,          // mileage before the purchase
    int? OdometerAfterFill,      // mileage after the purchase
    int? MileageCovered,         // calculated: after − before
    string? FuelGaugeBeforePosition,
    string? FuelGaugeAfterPosition,
    string? CostCentre,
    string? StationName,
    string? ReceiptBlobUrl,
    string? Notes,
    Guid? LocationId,
    string? LocationName,
    DateTime CreatedAt
);

public record CreateFuelLogDto(
    Guid VehicleId,
    DateOnly FuelDate,
    string ProductType,
    decimal LitresFilled,
    decimal CostPerLitre,
    int OdometerAtFill,              // mileage before the purchase
    string PaymentMethod = "Card",   // Card | Cash | Credit | Transfer
    int? OdometerAfterFill = null,   // mileage after the purchase
    string? FuelGaugeBeforePosition = null,
    string? FuelGaugeAfterPosition = null,
    string? CostCentre = null,
    string? StationName = null,
    string? Notes = null,
    Guid? LocationId = null
);

/// <summary>
/// Correction to an existing fuel log. Every field is optional — only supplied
/// values are applied. Changes are written to the audit trail, since these
/// figures reconcile against vendor invoices.
/// </summary>
public record UpdateFuelLogDto(
    DateOnly? FuelDate = null,
    string? ProductType = null,
    decimal? LitresFilled = null,
    decimal? CostPerLitre = null,
    int? OdometerAtFill = null,      // mileage before the purchase
    int? OdometerAfterFill = null,   // mileage after the purchase
    string? FuelGaugeBeforePosition = null,
    string? FuelGaugeAfterPosition = null,
    string? PaymentMethod = null,    // Card | Cash | Credit | Transfer
    string? CostCentre = null,
    string? StationName = null,
    string? Notes = null,
    Guid? LocationId = null,
    Guid? VehicleId = null,
    string? CorrectionReason = null   // recorded in the audit trail
);
