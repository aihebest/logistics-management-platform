namespace LogisticsApi.Models.Entities;

public class Assignment
{
    public Guid Id { get; set; }
    public Guid TripRequestId { get; set; }
    public Guid DriverId { get; set; }
    public Guid VehicleId { get; set; }
    public Guid AssignedById { get; set; }
    public string AssignmentType { get; set; } = "Auto";      // Auto | Manual
    public string Status { get; set; } = "Active";            // Active | Completed | Cancelled
    public DateTime StartTime { get; set; }
    public DateTime? EstimatedEndTime { get; set; }
    public DateTime? ActualEndTime { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public TripRequest TripRequest { get; set; } = null!;
    public User Driver { get; set; } = null!;
    public Vehicle Vehicle { get; set; } = null!;
    public User AssignedBy { get; set; } = null!;
}
