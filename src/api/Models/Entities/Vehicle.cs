namespace LogisticsApi.Models.Entities;

public class Vehicle
{
    public Guid Id { get; set; }
    public string RegistrationNo { get; set; } = string.Empty;
    public string Make { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public short Year { get; set; }
    public string Status { get; set; } = "Available";         // Available | Assigned | InMaintenance | OutOfService
    public string FuelType { get; set; } = "Diesel";
    public int OdometerKm { get; set; }
    public int ServiceIntervalKm { get; set; } = 10000;
    public DateOnly? LastServiceDate { get; set; }
    public DateOnly? NextServiceDate { get; set; }
    public Guid? AssignedMechanicId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User? AssignedMechanic { get; set; }
    public ICollection<Assignment> Assignments { get; set; } = [];
    public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = [];
    public ICollection<FuelLog> FuelLogs { get; set; } = [];
}
