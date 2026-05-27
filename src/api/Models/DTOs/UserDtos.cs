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

// Used by Admin/Manager to pre-register a driver before they have an Entra account.
// The system generates a placeholder EntraObjectId; when the driver first logs in,
// GET /api/auth/me reconciles their real Entra OID by matching on email.
public record RegisterDriverDto(
    string FullName,
    string Email,
    string? PhoneNumber = null,
    string? LicenceNo = null,
    DateOnly? LicenceExpiry = null
);
