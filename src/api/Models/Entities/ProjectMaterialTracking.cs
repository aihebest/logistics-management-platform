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
    public string? BlAwbNumber { get; set; }                 // Legacy combined field — superseded by BlNumber / AwbNumber
    public string? VesselName { get; set; }
    public DateOnly? Etd { get; set; }
    public DateOnly? Eta { get; set; }
    public string DeliveryStatus { get; set; } = "Pending";  // Pending | InTransit | Customs | Delivered | Partial
    public string? Remarks { get; set; }
    public DateOnly? ActualDeliveryDate { get; set; }

    // ── ISO audit fields ─────────────────────────────────────────────────────
    // Added at the auditor's request so the delivery-date chain is traceable:
    // what the project team expected, when the store team was notified and what
    // they expected, what logistics and the supplier finally agreed, and what
    // actually happened (ActualDeliveryDate above).
    public DateOnly? ExpectedDeliveryDateProjectTeam { get; set; }
    public DateOnly? StoreNotificationDate { get; set; }
    public DateOnly? ExpectedDeliveryDateStoreTeam { get; set; }
    public DateOnly? ExpectedDeliveryDateAgreed { get; set; }

    // Shipping documents — the auditor requires BL and AWB recorded separately
    // rather than in one combined field.
    public string? PaarNumber { get; set; }                  // Pre-Arrival Assessment Report
    public DateOnly? PaarDate { get; set; }
    public string? BlNumber { get; set; }                    // Bill of Lading (sea)
    public string? AwbNumber { get; set; }                   // Air Waybill (air)
    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User CreatedBy { get; set; } = null!;
}
