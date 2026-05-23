namespace LogisticsApi.Models.Entities;

public class User
{
    public Guid Id { get; set; }
    public string EntraObjectId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;          // Driver | Coordinator | Manager | Mechanic | Admin
    public string? DriverStatus { get; set; }                 // Available | OnAssignment | OffDuty | OnBreak
    public string? LicenceNo { get; set; }
    public DateOnly? LicenceExpiry { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastStatusChange { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Assignment> AssignmentsAsDriver { get; set; } = [];
    public ICollection<Assignment> AssignmentsCreated { get; set; } = [];
    public ICollection<FuelLog> FuelLogs { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
}
