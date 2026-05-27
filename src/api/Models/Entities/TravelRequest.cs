namespace LogisticsApi.Models.Entities;

public class TravelRequest
{
    public Guid Id { get; set; }
    public Guid RequestedById { get; set; }
    public string TravelType { get; set; } = string.Empty;    // LocalFlight | InternationalFlight | Hotel | Guesthouse | Immigration
    public string TravellerName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string Origin { get; set; } = string.Empty;
    public string Destination { get; set; } = string.Empty;
    public DateOnly TravelDate { get; set; }
    public DateOnly? ReturnDate { get; set; }
    public string? FlightPreference { get; set; }
    public string? HotelName { get; set; }
    public int? NumberOfNights { get; set; }
    public string? PassportNumber { get; set; }
    public string Status { get; set; } = "Pending";           // Pending | Approved | Rejected | Booked
    public string? ApprovalNotes { get; set; }
    public Guid? ApprovedById { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User RequestedBy { get; set; } = null!;
    public User? ApprovedBy { get; set; }
}
