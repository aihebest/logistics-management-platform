namespace LogisticsApi.Models.DTOs;

public record MaintenanceRecordDto(
    Guid Id,
    Guid VehicleId,
    string VehicleReg,
    string Type,
    DateOnly ScheduledDate,
    DateOnly? CompletedDate,
    decimal? Cost,
    string? VendorName,
    string? VendorContact,
    string? Notes,
    string Status,
    string? AttachmentBlobUrl,
    DateTime CreatedAt
);

public record CreateMaintenanceRecordDto(
    Guid VehicleId,
    string Type,
    DateOnly ScheduledDate,
    string? VendorName,
    string? VendorContact,
    string? Notes
);

public record UpdateMaintenanceRecordDto(
    string? Status,
    DateOnly? CompletedDate,
    decimal? Cost,
    string? VendorName,
    string? VendorContact,
    string? Notes,
    string? AttachmentBlobUrl
);

public record FuelLogDto(
    Guid Id,
    Guid VehicleId,
    string VehicleReg,
    string LoggedByName,
    DateOnly FuelDate,
    decimal LitresFilled,
    decimal CostPerLitre,
    decimal TotalCost,
    int OdometerAtFill,
    string? StationName,
    string? ReceiptBlobUrl,
    string? Notes,
    DateTime CreatedAt
);

public record CreateFuelLogDto(
    Guid VehicleId,
    DateOnly FuelDate,
    decimal LitresFilled,
    decimal CostPerLitre,
    int OdometerAtFill,
    string? StationName,
    string? Notes
);
