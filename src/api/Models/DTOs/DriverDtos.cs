namespace LogisticsApi.Models.DTOs;

// ── Driver Schedule ───────────────────────────────────────────────────────────

public record DriverScheduleDto(
    Guid Id,
    Guid DriverId,
    string DriverName,
    DateOnly ScheduleDate,
    string WorkLocation,     // Freetext deployment location
    Guid? LocationId,        // FK to Locations
    string? LocationName,    // e.g. "Port Harcourt"
    string Shift,
    // Day Shift | Night Shift | Off Duty | Leave |
    // Guest House Driver | Night Standby Driver | Expatriate Driver |
    // Management Driver | Project Assignment Driver
    string? Notes,
    string CreatedByName,
    DateTime CreatedAt
);

public record CreateDriverScheduleDto(
    Guid DriverId,
    DateOnly ScheduleDate,
    string WorkLocation,
    string Shift,
    string? Notes,
    Guid? LocationId = null
);

// ── Driver Incidents ──────────────────────────────────────────────────────────

public record DriverIncidentDto(
    Guid Id,
    Guid DriverId,
    string DriverName,
    DateOnly IncidentDate,
    string Type,        // Accident | TrafficViolation | VehicleDamage | Other
    string Description,
    string Severity,    // Minor | Moderate | Major
    string? ActionTaken,
    string ReportedByName,
    DateTime CreatedAt
);

public record CreateDriverIncidentDto(
    Guid DriverId,
    DateOnly IncidentDate,
    string Type,
    string Description,
    string Severity,
    string? ActionTaken
);

// ── Driver Performance Summary ────────────────────────────────────────────────

public record DriverPerformanceDto(
    Guid DriverId,
    string DriverName,
    string CurrentStatus,
    int TotalTrips,
    int CompletedTrips,
    int CancelledTrips,
    int TotalIncidents,
    int MajorIncidents,
    int AccidentFreeStreak,   // consecutive days without incident
    List<AssignmentDto> RecentTrips,
    List<DriverIncidentDto> RecentIncidents
);
