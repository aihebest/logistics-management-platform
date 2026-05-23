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
    int ServiceIntervalKm,
    DateOnly? LastServiceDate,
    DateOnly? NextServiceDate,
    string? AssignedMechanicName
);

public record CreateVehicleDto(
    string RegistrationNo,
    string Make,
    string Model,
    short Year,
    string FuelType,
    int OdometerKm,
    int ServiceIntervalKm
);

public record UpdateVehicleDto(
    string? Status,
    int? OdometerKm,
    DateOnly? LastServiceDate,
    DateOnly? NextServiceDate,
    Guid? AssignedMechanicId
);
