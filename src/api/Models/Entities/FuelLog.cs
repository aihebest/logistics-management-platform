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
    /// <summary>Legacy percentage reading. Superseded by FuelGaugeBeforePosition.</summary>
    public decimal? FuelGaugeBefore { get; set; }
    /// <summary>Legacy percentage reading. Superseded by FuelGaugeAfterPosition.</summary>
    public decimal? FuelGaugeAfter { get; set; }

    /// <summary>
    /// Tank position before filling, in the wording the team already uses on
    /// their own fuel report — "Reserve", "Below 1/4 tank", "Full tank" and so
    /// on. Drivers read a needle, not a percentage, so asking for a number was
    /// inviting invented precision.
    /// </summary>
    public string? FuelGaugeBeforePosition { get; set; }
    /// <summary>Tank position after filling. Same wording as the before reading.</summary>
    public string? FuelGaugeAfterPosition { get; set; }

    /// <summary>Odometer when the vehicle arrived to refuel.</summary>
    public int OdometerAtFill { get; set; }
    /// <summary>
    /// Odometer recorded after the fuel was used. KM Covered is the gap between
    /// this and OdometerAtFill.
    /// </summary>
    public int? OdometerAfterFill { get; set; }

    /// <summary>Legacy "Mileage From" reading. Superseded by OdometerAtFill.</summary>
    public int? OdometerFrom { get; set; }
    /// <summary>Legacy "Mileage To" reading. Superseded by OdometerAfterFill.</summary>
    public int? OdometerTo { get; set; }

    /// <summary>Server-calculated: OdometerAfterFill − OdometerAtFill.</summary>
    public int? MileageCovered { get; set; }
    public bool IsCashPayment { get; set; }              // legacy — superseded by PaymentMethod
    /// <summary>Card | Cash | Credit | Transfer</summary>
    public string PaymentMethod { get; set; } = "Card";
    public string? StationName { get; set; }
    public string? ReceiptBlobUrl { get; set; }
    public string? Notes { get; set; }
    public Guid? LocationId { get; set; }              // Operational location (PH, Lagos, etc.)
    public DateTime CreatedAt { get; set; }

    public Vehicle Vehicle { get; set; } = null!;
    public User LoggedBy { get; set; } = null!;
    public Location? Location { get; set; }
}
