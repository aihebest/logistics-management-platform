namespace LogisticsApi.Models.DTOs;

// ── Travel / Ticketing / Accommodation Requests ───────────────────────────────

public record TravelRequestDto(
    Guid Id,
    string RequestedByName,
    string TravellerName,
    string TravelType,    // LocalFlight | InternationalFlight | Hotel | Guesthouse | Immigration
    string Purpose,
    string Origin,
    string Destination,
    DateOnly TravelDate,
    DateOnly? ReturnDate,
    string? FlightPreference,
    string? HotelName,
    int? NumberOfNights,
    string? PassportNumber,
    string Status,        // Pending | Approved | Rejected | Booked
    string? ApprovedByName,
    DateTime? ApprovedAt,
    string? ApprovalNotes,
    DateTime CreatedAt
);

public record CreateTravelRequestDto(
    string TravellerName,
    string TravelType,
    string Purpose,
    string Origin,
    string Destination,
    DateOnly TravelDate,
    DateOnly? ReturnDate = null,
    string? FlightPreference = null,
    string? HotelName = null,
    int? NumberOfNights = null,
    string? PassportNumber = null
);

public record ApproveTravelRequestDto(
    string Action,    // Approve | Reject
    string? Notes
);

// ── Project Material Tracking (mirrors STATUS REPORT xlsx) ───────────────────

public record ProjectMaterialTrackingDto(
    Guid Id,
    int TrackingYear,
    string? PoNumber,
    string? PoLineItem,
    string? Project,
    string? Buyer,
    string Description,
    decimal? Quantity,
    string? Supplier,
    string? FreightForwarder,
    DateOnly? ReadinessDate,
    DateOnly? PickupAuthDate,
    DateOnly? PickupDate,
    string? ModeOfTransport,
    string? FormMNumber,
    string? BlAwbNumber,
    string? VesselName,
    DateOnly? Etd,
    DateOnly? Eta,
    string DeliveryStatus,
    DateOnly? ActualDeliveryDate,
    string? Remarks,
    DateTime UpdatedAt
);

public record CreateProjectMaterialTrackingDto(
    int TrackingYear,
    string? PoNumber,
    string? PoLineItem,
    string? Project,
    string? Buyer,
    string Description,
    decimal? Quantity,
    string? Supplier,
    string? FreightForwarder,
    DateOnly? ReadinessDate,
    string? ModeOfTransport
);

public record UpdateProjectMaterialTrackingDto(
    string? DeliveryStatus,
    DateOnly? PickupAuthDate,
    DateOnly? PickupDate,
    string? FormMNumber,
    string? BlAwbNumber,
    string? VesselName,
    DateOnly? Etd,
    DateOnly? Eta,
    DateOnly? ActualDeliveryDate,
    string? Remarks,
    string? FreightForwarder
);

// ── Movement Register ─────────────────────────────────────────────────────────

public record MovementRegisterDto(
    Guid Id,
    string MovementType,   // VehicleOut | VehicleIn | MaterialOut | MaterialIn | GatePass | StaffMovement
    string? VehicleReg,
    string? DriverName,
    string? RelatedRefNo,
    string Purpose,
    string Origin,         // Departure location
    string Destination,
    DateTime MovementDateTime,   // Time Out
    DateTime? ReturnDateTime,    // Time In
    int? MileageOut,       // Odometer at departure
    int? MileageIn,        // Odometer at return
    string? GatePassNo,
    string Status,         // Open | Closed
    string LoggedByName,
    DateTime CreatedAt
);

public record CreateMovementRegisterDto(
    string MovementType,
    Guid? VehicleId,
    Guid? DriverId,
    string? RelatedRefNo,
    string Purpose,
    string Origin,
    string Destination,
    DateTime MovementDateTime,
    int? MileageOut = null,
    int? MileageIn = null,
    DateTime? ReturnDateTime = null,
    string? GatePassNo = null
);

public record CloseMovementDto(
    DateTime ReturnDateTime,
    int? MileageIn,
    string? Notes
);
