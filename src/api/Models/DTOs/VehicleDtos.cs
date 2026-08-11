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
    string? AssetTagNo,     // fixed-asset register tag
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
    int? PreviousMileageAtPurchase = null,
    string? AssetTagNo = null
);

/// <summary>
/// Partial update — every field is optional and only supplied values are applied.
/// Includes the core descriptive fields (make, model, year, fuel type, asset tag)
/// so records imported from the asset register with missing data can be completed
/// in the UI later.
/// </summary>
public record UpdateVehicleDto(
    string? Status,
    int? OdometerKm,
    DateOnly? LastServiceDate,
    DateOnly? NextServiceDate,
    Guid? AssignedMechanicId,
    string? ChassisNo,
    short? PurchaseYear,
    string? Colour,
    // Editable descriptive fields — fill in blanks left by the bulk import
    string? RegistrationNo = null,
    string? Make = null,
    string? Model = null,
    short? Year = null,
    string? FuelType = null,
    string? AssetTagNo = null,
    int? ServiceIntervalKm = null,
    int? MileageAtPurchase = null,
    int? PreviousMileageAtPurchase = null
);
