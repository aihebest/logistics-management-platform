namespace LogisticsApi.Models.DTOs;

public record TripRequestDto(
    Guid Id,
    Guid RequestedById,
    string RequestedByName,
    string Purpose,
    string PickupLocation,
    string DestinationLocation,
    DateTime RequestedDateTime,
    string Status,
    string Priority,
    string? Notes,
    DateTime CreatedAt,
    AssignmentSummaryDto? Assignment,
    // Phase 1 fields
    string MovementType,    // IntraState | Interstate | International
    DateOnly? DepartureDate,
    TimeOnly? DepartureTime
);

public record AssignmentSummaryDto(
    Guid Id,
    string DriverName,
    string VehicleReg,
    string Status,
    DateTime StartTime,
    DateTime? EstimatedEndTime
);

public record CreateTripRequestDto(
    string Purpose,
    string PickupLocation,
    string DestinationLocation,
    DateTime RequestedDateTime,
    string Priority,
    string? Notes,
    string MovementType = "IntraState",
    DateOnly? DepartureDate = null,
    TimeOnly? DepartureTime = null
);

public record AssignmentDto(
    Guid Id,
    Guid TripRequestId,
    string TripPurpose,
    Guid DriverId,
    string DriverName,
    Guid VehicleId,
    string VehicleReg,
    string AssignmentType,
    string Status,
    DateTime StartTime,
    DateTime? EstimatedEndTime,
    DateTime? ActualEndTime,
    string? Notes,
    DateTime CreatedAt
);

public record CreateAssignmentDto(
    Guid TripRequestId,
    Guid DriverId,
    Guid VehicleId,
    DateTime StartTime,
    DateTime? EstimatedEndTime,
    string? Notes
);

public record OverrideAssignmentDto(
    Guid DriverId,
    Guid VehicleId,
    string? Notes
);

public record UpdateAssignmentStatusDto(
    string Status,   // Active | Completed | Ongoing | Unattended | Cancelled
    string? Notes,
    DateTime? ActualEndTime
);
