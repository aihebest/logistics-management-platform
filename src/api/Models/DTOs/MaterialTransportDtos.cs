namespace LogisticsApi.Models.DTOs;

// ── Material Transport Items ──────────────────────────────────────────────────

public record MaterialTransportItemDto(
    Guid Id,
    int SNo,
    string Material,
    string? Description,
    decimal Quantity
);

public record CreateMaterialTransportItemDto(
    int SNo,
    string Material,
    string? Description,
    decimal Quantity
);

// ── Material Transport Requests (DEL-LG-FRM-009) ─────────────────────────────

public record MaterialTransportRequestDto(
    Guid Id,
    string FormNumber,
    string RequestedByName,
    string ProjectName,
    string Purpose,
    // Loading
    string LoadingPoint,
    string? LoadingContactPerson,
    string? LoadingContactPhone,
    DateOnly? LoadingDate,
    // Delivery
    string DeliveryPoint,
    string? DeliveryContactPerson,
    string? DeliveryContactPhone,
    DateOnly? DeliveryDate,
    // Status & approval
    string Status,
    string? HodApprovedByName,
    DateTime? HodApprovedAt,
    string? HodRemarks,
    string? ManagerApprovedByName,
    DateTime? ManagerApprovedAt,
    string? ManagerRemarks,
    // Assignment
    string? AssignedDriverName,
    string? AssignedVehicleReg,
    List<MaterialTransportItemDto> Items,
    DateTime CreatedAt
);

public record CreateMaterialTransportRequestDto(
    string ProjectName,
    string Purpose,
    string LoadingPoint,
    string? LoadingContactPerson,
    string? LoadingContactPhone,
    DateOnly? LoadingDate,
    string DeliveryPoint,
    string? DeliveryContactPerson,
    string? DeliveryContactPhone,
    DateOnly? DeliveryDate,
    List<CreateMaterialTransportItemDto> Items
);

public record ApproveMaterialTransportDto(
    string Action,      // Approve | Reject
    string? Remarks
);

public record AssignMaterialTransportDto(
    Guid DriverId,
    Guid VehicleId
);
