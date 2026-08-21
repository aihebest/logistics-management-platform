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

/// <summary>
/// Pre-registers a colleague so the platform can notify them before they have
/// ever signed in. Their account links automatically on first login, matched by
/// email address.
/// </summary>
public record RegisterPlatformUserDto(
    string FullName,
    string Email,
    string Role,                 // HOD | Manager | Coordinator | Mechanic | Driver | Staff
    string? PhoneNumber = null
);

/// <summary>Admin correction to an existing platform user.</summary>
public record UpdatePlatformUserDto(
    string? FullName = null,
    string? Email = null,
    string? Role = null,
    string? PhoneNumber = null,
    bool? IsActive = null
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
    string? PhoneNumber = null,
    string? LicenceNo = null,
    DateOnly? LicenceExpiry = null,
    string? Email = null   // Optional — only needed if driver will log in via Microsoft account
);
