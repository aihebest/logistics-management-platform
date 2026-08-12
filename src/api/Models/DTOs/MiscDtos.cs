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
    int UpcomingMaintenanceCount,
    // ── Material movement ────────────────────────────────────────────────────
    int MaterialAwaitingHod,        // submitted, waiting on HOD sign-off
    int MaterialAwaitingManager,    // HOD approved, waiting on Manager
    int MaterialApprovedUnassigned, // approved but no driver/vehicle yet
    int MaterialDispatched,         // driver & vehicle assigned, on the road
    int ProjectMaterialsInTransit,  // project consignments en route
    int ProjectMaterialsOverdue     // past ETA and not yet delivered
);

public record SasTokenDto(string Url, string BlobName, DateTimeOffset ExpiresAt);

public record BroadcastNotificationDto(string Title, string Message, string Type);

public record PagedResult<T>(IEnumerable<T> Items, int TotalCount, int Page, int PageSize);
