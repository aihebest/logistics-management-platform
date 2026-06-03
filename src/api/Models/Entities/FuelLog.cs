namespace LogisticsApi.Models.Entities;

public class FuelLog
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public Guid LoggedById { get; set; }
    public DateOnly FuelDate { get; set; }
    public string ProductType { get; set; } = "Petrol";        // Petrol | Diesel
    public string? CostCentre { get; set; }
    public decimal LitresFilled { get; set; }
    public decimal CostPerLitre { get; set; }
    public decimal TotalCost { get; set; }
    public decimal? FuelGaugeBefore { get; set; }              // percentage e.g. 25, 50, 75, 100
    public decimal? FuelGaugeAfter { get; set; }               // percentage e.g. 25, 50, 75, 100
    public int OdometerAtFill { get; set; }                   // kept for backward compat
    public int? OdometerFrom { get; set; }                    // Mileage reading From
    public int? OdometerTo { get; set; }                      // Mileage reading To
    public int? MileageCovered { get; set; }                  // Auto: OdometerTo - OdometerFrom
    public bool IsCashPayment { get; set; }
    public string? StationName { get; set; }
    public string? ReceiptBlobUrl { get; set; }
    public string? Notes { get; set; }
    public Guid? LocationId { get; set; }              // Operational location (PH, Lagos, etc.)
    public DateTime CreatedAt { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
    public User LoggedBy { get; set; } = null!;
    public Location? Location { get; set; }
}
