namespace LogisticsApi.Models.Entities;

public class TripRequest
{
    public Guid Id { get; set; }
    public Guid RequestedById { get; set; }
    public string Purpose { get; set; } = string.Empty;
    public string PickupLocation { get; set; } = string.Empty;
    public string DestinationLocation { get; set; } = string.Empty;
    public DateTime RequestedDateTime { get; set; }
    public string Status { get; set; } = "Pending";           // Pending | Active | Completed | Cancelled
    public string Priority { get; set; } = "Normal";          // Normal | High | Urgent
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public User RequestedBy { get; set; } = null!;
    public Assignment? Assignment { get; set; }
}
