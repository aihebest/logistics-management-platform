namespace LogisticsApi.Models.Entities;

public class DriverIncident
{
    public Guid Id { get; set; }
    public Guid DriverId { get; set; }
    public DateOnly IncidentDate { get; set; }
    public string Type { get; set; } = string.Empty;          // Accident | TrafficViolation | VehicleDamage | Other
    public string Description { get; set; } = string.Empty;
    public string Severity { get; set; } = "Minor";           // Minor | Moderate | Major
    public string? ActionTaken { get; set; }
    public string? Notes { get; set; }
    public Guid ReportedById { get; set; }
    public DateTime CreatedAt { get; set; }

    public User Driver { get; set; } = null!;
    public User ReportedBy { get; set; } = null!;
}
