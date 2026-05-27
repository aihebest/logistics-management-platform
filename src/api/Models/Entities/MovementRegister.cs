namespace LogisticsApi.Models.Entities;

public class MovementRegister
{
    public Guid Id { get; set; }
    public string MovementType { get; set; } = string.Empty;  // VehicleOut | VehicleIn | MaterialOut | MaterialIn | GatePass | StaffMovement
    public Guid? VehicleId { get; set; }
    public Guid? DriverId { get; set; }
    public string? RelatedRefNo { get; set; }                 // Trip/Material request ref
    public string Purpose { get; set; } = string.Empty;
    public string? Origin { get; set; }
    public string? Destination { get; set; }
    public DateTime MovementDateTime { get; set; }
    public DateTime? ReturnDateTime { get; set; }
    public string? GatePassNo { get; set; }
    public string Status { get; set; } = "Open";              // Open | Closed
    public string? Notes { get; set; }
    public Guid LoggedById { get; set; }
    public DateTime CreatedAt { get; set; }

    public Vehicle? Vehicle { get; set; }
    public User? Driver { get; set; }
    public User LoggedBy { get; set; } = null!;
}
