namespace LogisticsApi.Models.DTOs;

// ── Maintenance Records ───────────────────────────────────────────────────────

public record MaintenanceRecordDto(
    Guid Id,
    Guid VehicleId,
    string VehicleReg,
    string Type,
    string Category,          // Routine | FaultRepair
    DateOnly ScheduledDate,
    DateOnly? CompletedDate,
    decimal? Cost,
    string? VendorName,
    string? VendorContact,
    string? Notes,
    string Status,
    string? AttachmentBlobUrl,
    // Fault / Repair fields
    bool FaultReported,
    string? FaultDescription,
    DateOnly? DateReported,
    string? PartsReplaced,
    string? RepairRemarks,
    DateTime CreatedAt
);

public record CreateMaintenanceRecordDto(
    Guid VehicleId,
    string Type,
    string Category,          // Routine | FaultRepair
    DateOnly ScheduledDate,
    string? VendorName,
    string? VendorContact,
    string? Notes,
    // Fault fields (used when Category = FaultRepair)
    bool FaultReported = false,
    string? FaultDescription = null,
    DateOnly? DateReported = null,
    string? PartsReplaced = null,
    string? RepairRemarks = null
);

public record UpdateMaintenanceRecordDto(
    string? Status,
    DateOnly? CompletedDate,
    decimal? Cost,
    string? VendorName,
    string? VendorContact,
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
    bool IsCashPayment,
    int OdometerAtFill,
    int? OdometerFrom,
    int? OdometerTo,
    int? MileageCovered,
    decimal? FuelGaugeBefore,
    decimal? FuelGaugeAfter,
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
    int OdometerAtFill,
    bool IsCashPayment = false,
    int? OdometerFrom = null,
    int? OdometerTo = null,
    decimal? FuelGaugeBefore = null,
    decimal? FuelGaugeAfter = null,
    string? CostCentre = null,
    string? StationName = null,
    string? Notes = null,
    Guid? LocationId = null
);
