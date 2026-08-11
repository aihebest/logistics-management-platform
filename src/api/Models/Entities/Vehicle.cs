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
    public int OdometerKm { get; set; }                        // Kept for odometer tracking
    public int? MileageAtPurchase { get; set; }                // Odometer reading when purchased
    public int? PreviousMileageAtPurchase { get; set; }        // Prior odometer (for second-hand vehicles)
    public int ServiceIntervalKm { get; set; } = 10000;
    public DateOnly? LastServiceDate { get; set; }
    public DateOnly? NextServiceDate { get; set; }
    public Guid? AssignedMechanicId { get; set; }
    /// <summary>
    /// Fixed-asset tag from the company asset register (e.g. "5550000190").
    /// Lets the platform be reconciled against the Repairs &amp; Maintenance
    /// Register and the finance asset list.
    /// </summary>
    public string? AssetTagNo { get; set; }
    // Phase 1 additions
    public string? ChassisNo { get; set; }
    public short? PurchaseYear { get; set; }
    public string? Colour { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User? AssignedMechanic { get; set; }
    public ICollection<Assignment> Assignments { get; set; } = [];
    public ICollection<MaintenanceRecord> MaintenanceRecords { get; set; } = [];
    public ICollection<FuelLog> FuelLogs { get; set; } = [];
}
