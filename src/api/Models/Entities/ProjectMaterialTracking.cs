namespace LogisticsApi.Models.Entities;

public class ProjectMaterialTracking
{
    public Guid Id { get; set; }
    public int TrackingYear { get; set; }
    public string? PoNumber { get; set; }
    public string? PoLineItem { get; set; }
    public string Project { get; set; } = string.Empty;
    public string Buyer { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public string? Supplier { get; set; }
    public string? FreightForwarder { get; set; }
    public DateOnly? ReadinessDate { get; set; }
    public DateOnly? PickupAuthDate { get; set; }
    public DateOnly? PickupDate { get; set; }
    public string? ModeOfTransport { get; set; }             // Air | Sea | Road
    public string? FormMNumber { get; set; }
    public string? BlAwbNumber { get; set; }
    public string? VesselName { get; set; }
    public DateOnly? Etd { get; set; }
    public DateOnly? Eta { get; set; }
    public string DeliveryStatus { get; set; } = "Pending";  // Pending | InTransit | Customs | Delivered | Partial
    public string? Remarks { get; set; }
    public DateOnly? ActualDeliveryDate { get; set; }
    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User CreatedBy { get; set; } = null!;
}
