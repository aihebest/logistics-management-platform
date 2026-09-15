namespace LogisticsApi.Models.DTOs;

// ── Departments ───────────────────────────────────────────────────────────────

public record DepartmentDto(
    Guid Id,
    string Name,
    Guid? HodUserId,
    string? HodName,
    string? HodEmail,
    bool IsActive,
    int MemberCount
);

public record CreateDepartmentDto(string Name, Guid? HodUserId = null);

public record UpdateDepartmentDto(
    string? Name = null,
    Guid? HodUserId = null,
    bool? ClearHod = null,      // set true to remove the head without naming a new one
    bool? IsActive = null
);

// ── Travel Request Form (DEL-LG-FRM-002 Rev 07) ───────────────────────────────

/// <summary>One row of the Outbound or Inbound routing table.</summary>
public record TravelLegDto(
    string Direction,        // Outbound | Inbound
    int Sequence,
    DateOnly TravelDate,
    string From,
    string To,
    string? PreferredAirline,
    string? PreferredTime
);

public record TravelRequestDto(
    Guid Id,
    string FormNumber,
    DateTime FormDate,
    string? ProjectCostCentreCode,

    // Traveller
    Guid RequestedById,
    string RequestedByName,
    string Surname,
    string GivenName,
    string Department,
    string? Position,
    string? PhoneNumber,
    string? Email,

    string PurposeOfTravel,
    bool HotelBookingRequired,
    string? OtherInformation,
    IReadOnlyList<TravelLegDto> Legs,

    // Signature blocks — these are what print on the form
    string Status,
    string? VerifiedByName,
    DateTime? VerifiedAt,
    string? VerificationNotes,
    string? ApprovedByName,
    DateTime? ApprovedAt,
    string? ApprovalNotes,
    string? RejectionReason,
    DateTime? RejectedAt,

    DateTime CreatedAt
);

/// <summary>
/// NOTE: FormNumber and FormDate are deliberately absent — the server assigns
/// both at submission, so a form cannot be given a duplicate reference or
/// back-dated.
/// </summary>
public record CreateTravelRequestDto(
    string Surname,
    string GivenName,
    string Department,
    string PurposeOfTravel,
    IReadOnlyList<CreateTravelLegDto> Legs,
    string? ProjectCostCentreCode = null,
    string? Position = null,
    string? PhoneNumber = null,
    string? Email = null,
    bool HotelBookingRequired = false,
    string? OtherInformation = null
);

public record CreateTravelLegDto(
    string Direction,        // Outbound | Inbound
    DateOnly TravelDate,
    string From,
    string To,
    string? PreferredAirline = null,
    string? PreferredTime = null
);

/// <summary>Body for the HOD verification and the DMD/MD approval steps.</summary>
public record TravelDecisionDto(string? Notes);

/// <summary>Body for turning a request down at either stage.</summary>
public record RejectTravelRequestDto(string? Reason);

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
    string MovementType,   // VehicleOut | VehicleIn | MaterialOut | MaterialIn | GatePass | StaffMovement | Other
    string? MovementTypeOther,   // detail when MovementType is "Other"
    string? Passengers,          // names of people carried
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
    string? Passengers,
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
    string? GatePassNo = null,
    string? MovementTypeOther = null,   // detail when MovementType is "Other"
    string? Passengers = null           // names of people carried
);

public record CloseMovementDto(
    DateTime ReturnDateTime,
    int? MileageIn,
    string? Notes
);

/// <summary>
/// Correction to an existing register entry. Every field is optional — only the
/// values supplied are applied, so the caller can fix one cell without resending
/// the whole record. Changes are written to the audit trail because the register
/// is a gate document that feeds distance and vendor reconciliation.
/// </summary>
public record UpdateMovementRegisterDto(
    string? MovementType = null,
    string? MovementTypeOther = null,
    string? Passengers = null,
    Guid? VehicleId = null,
    Guid? DriverId = null,
    string? Purpose = null,
    string? Origin = null,
    string? Destination = null,
    DateTime? MovementDateTime = null,
    DateTime? ReturnDateTime = null,
    int? MileageOut = null,
    int? MileageIn = null,
    string? GatePassNo = null,
    string? Status = null,          // Open | Closed
    string? Notes = null,
    string? CorrectionReason = null // recorded in the audit trail
);
