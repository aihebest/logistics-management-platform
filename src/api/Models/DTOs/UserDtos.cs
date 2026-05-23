namespace LogisticsApi.Models.DTOs;

public record UserDto(
    Guid Id,
    string FullName,
    string Email,
    string? PhoneNumber,
    string Role,
    string? DriverStatus,
    string? LicenceNo,
    DateOnly? LicenceExpiry,
    bool IsActive,
    DateTime? LastStatusChange
);

public record UpdateDriverStatusDto(string Status);

public record CreateUserDto(
    string EntraObjectId,
    string FullName,
    string Email,
    string? PhoneNumber,
    string Role,
    string? LicenceNo,
    DateOnly? LicenceExpiry
);
