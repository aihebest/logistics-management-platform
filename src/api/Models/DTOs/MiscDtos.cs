namespace LogisticsApi.Models.DTOs;

public record NotificationDto(
    Guid Id,
    string Type,
    string Subject,
    string Body,
    bool IsRead,
    string Status,
    DateTime? SentAt,
    string? RelatedEntityType,
    string? RelatedEntityId,
    DateTime CreatedAt
);

public record AuditLogDto(
    Guid Id,
    string EntityType,
    string EntityId,
    string Action,
    string UserEmail,
    DateTime Timestamp,
    string? IpAddress,
    string? Notes
);

public record DashboardSummaryDto(
    int AvailableDrivers,
    int DriversOnAssignment,
    int DriversOffDuty,
    int DriversOnBreak,
    int AvailableVehicles,
    int VehiclesAssigned,
    int VehiclesInMaintenance,
    int PendingTripRequests,
    int ActiveAssignments,
    int OverdueMaintenanceCount,
    int UpcomingMaintenanceCount
);

public record SasTokenDto(string Url, string BlobName, DateTimeOffset ExpiresAt);

public record PagedResult<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize);
