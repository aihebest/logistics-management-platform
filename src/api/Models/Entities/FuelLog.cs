namespace LogisticsApi.Models.Entities;

public class FuelLog
{
    public Guid Id { get; set; }
    public Guid VehicleId { get; set; }
    public Guid LoggedById { get; set; }
    public DateOnly FuelDate { get; set; }
    public decimal LitresFilled { get; set; }
    public decimal CostPerLitre { get; set; }
    public decimal TotalCost { get; set; }
    public int OdometerAtFill { get; set; }
    public string? StationName { get; set; }
    public string? ReceiptBlobUrl { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
    public User LoggedBy { get; set; } = null!;
}
