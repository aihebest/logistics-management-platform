namespace LogisticsApi.Models.DTOs;

public record VehicleDto(
    Guid Id,
    string RegistrationNo,
    string Make,
    string Model,
    short Year,
    string Status,
    string FuelType,
    int OdometerKm,
    int? MileageAtPurchase,
    int? PreviousMileageAtPurchase,
    int ServiceIntervalKm,
    DateOnly? LastServiceDate,
    DateOnly? NextServiceDate,
    string? AssignedMechanicName,
    // Phase 1 lifecycle fields
    string? ChassisNo,
    short? PurchaseYear,
    string? Colour,
    int VehicleAge          // calculated: current year - Year
);

public record CreateVehicleDto(
    string RegistrationNo,
    string Make,
    string Model,
    short Year,
    string FuelType,
    int OdometerKm,
    int ServiceIntervalKm,
    string? ChassisNo = null,
    short? PurchaseYear = null,
    string? Colour = null,
    int? MileageAtPurchase = null,
    int? PreviousMileageAtPurchase = null
);

public record UpdateVehicleDto(
    string? Status,
    int? OdometerKm,
    DateOnly? LastServiceDate,
    DateOnly? NextServiceDate,
    Guid? AssignedMechanicId,
    string? ChassisNo,
    short? PurchaseYear,
    string? Colour
);
