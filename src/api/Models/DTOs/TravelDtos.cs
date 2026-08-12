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
    // ── ISO audit fields ────────────────────────────────────────────────────
    DateOnly? ExpectedDeliveryDateProjectTeam,
    DateOnly? StoreNotificationDate,
    DateOnly? ExpectedDeliveryDateStoreTeam,
    DateOnly? ExpectedDeliveryDateAgreed,
    string? PaarNumber,
    DateOnly? PaarDate,
    string? BlNumber,
    string? AwbNumber,
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
    string? ModeOfTransport,
    // ── ISO audit fields — optional at creation, completed as the shipment
    //    progresses. Available here so a coordinator entering a consignment
    //    that is already in flight can record everything in one go.
    DateOnly? ExpectedDeliveryDateProjectTeam = null,
    DateOnly? StoreNotificationDate = null,
    DateOnly? ExpectedDeliveryDateStoreTeam = null,
    DateOnly? ExpectedDeliveryDateAgreed = null,
    string? PaarNumber = null,
    DateOnly? PaarDate = null,
    string? BlNumber = null,
    string? AwbNumber = null,
    string? FormMNumber = null,
    string? VesselName = null,
    DateOnly? Etd = null,
    DateOnly? Eta = null,
    DateOnly? ActualDeliveryDate = null,
    string? DeliveryStatus = null,
    string? Remarks = null
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
    string? FreightForwarder,
    // ── ISO audit fields ────────────────────────────────────────────────────
    DateOnly? ExpectedDeliveryDateProjectTeam = null,
    DateOnly? StoreNotificationDate = null,
    DateOnly? ExpectedDeliveryDateStoreTeam = null,
    DateOnly? ExpectedDeliveryDateAgreed = null,
    string? PaarNumber = null,
    DateOnly? PaarDate = null,
    string? BlNumber = null,
    string? AwbNumber = null
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
    int? DistanceKm,       // Calculated: MileageIn - MileageOut (null until closed)
    string? GatePassNo,
    string Status,         // Open | Closed
    string LoggedByName,
    DateTime CreatedAt
);

// ── Movement Register summary (for vendor / accounts reconciliation) ──────────

/// <summary>One movement line within a vehicle's summary block.</summary>
public record MovementSummaryLineDto(
    DateTime MovementDateTime,
    DateTime? ReturnDateTime,
    string Purpose,
    string Origin,
    string Destination,
    string? DriverName,
    string? RelatedRefNo,
    string? GatePassNo,
    int? MileageOut,
    int? MileageIn,
    int? DistanceKm,
    string Status
);

/// <summary>All movements for one vehicle in the period, with totals.</summary>
public record VehicleMovementSummaryDto(
    string VehicleReg,
    int TripCount,
    int TotalDistanceKm,
    int? OpeningOdometer,   // lowest MileageOut in the period
    int? ClosingOdometer,   // highest MileageIn in the period
    int OpenMovements,      // still out / not closed
    List<MovementSummaryLineDto> Movements
);

/// <summary>Full report: one block per vehicle plus grand totals.</summary>
public record MovementRegisterSummaryDto(
    DateOnly FromDate,
    DateOnly ToDate,
    int VehicleCount,
    int TotalTrips,
    int GrandTotalDistanceKm,
    List<VehicleMovementSummaryDto> Vehicles
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
