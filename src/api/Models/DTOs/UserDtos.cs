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
    DateTime? LastStatusChange,
    // Defaulted so the driver and auth endpoints, which don't carry department
    // detail, keep working without change.
    Guid? DepartmentId = null,
    string? DepartmentName = null,
    string? Position = null
);

/// <summary>
/// Pre-registers a colleague so the platform can notify them before they have
/// ever signed in. Their account links automatically on first login, matched by
/// email address.
/// </summary>
public record RegisterPlatformUserDto(
    string FullName,
    string Email,
    string Role,                 // Management | HOD | Manager | Coordinator | Mechanic | Driver | Staff
    string? PhoneNumber = null,
    Guid? DepartmentId = null,   // routes their travel requests to the right head
    string? Position = null      // printed on the Travel Request Form
);

/// <summary>Admin correction to an existing platform user.</summary>
public record UpdatePlatformUserDto(
    string? FullName = null,
    string? Email = null,
    string? Role = null,
    string? PhoneNumber = null,
    bool? IsActive = null,
    Guid? DepartmentId = null,
    string? Position = null
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

/// <summary>
/// Correction to a driver's record. Every field is optional — only the values
/// supplied are applied, so one detail can be fixed without resending the rest.
/// </summary>
public record UpdateDriverDto(
    string? FullName = null,
    string? PhoneNumber = null,
    string? LicenceNo = null,
    DateOnly? LicenceExpiry = null,
    string? Email = null,
    bool? IsActive = null
);
