namespace LogisticsApi.Models.Entities;

public class User
{
    public Guid Id { get; set; }
    public string EntraObjectId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;          // Driver | Coordinator | Manager | Mechanic | Admin
    /// <summary>
    /// The department this person belongs to.
    ///
    /// Drives approval routing: a travel request goes to the head of the
    /// requester's own department, rather than to whichever HOD happens to be
    /// listed first. Without it, any HOD could verify anyone's request, which
    /// defeats the point of the verification step.
    /// </summary>
    public Guid? DepartmentId { get; set; }
    /// <summary>Job title, printed on the Travel Request Form.</summary>
    public string? Position { get; set; }
    public string? DriverStatus { get; set; }                 // Available | OnAssignment | OffDuty | OnBreak
    public string? LicenceNo { get; set; }
    public DateOnly? LicenceExpiry { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? LastStatusChange { get; set; }
    public DateTime CreatedAt { get; set; }

    public Department? Department { get; set; }
    public ICollection<Assignment> AssignmentsAsDriver { get; set; } = [];
    public ICollection<Assignment> AssignmentsCreated { get; set; } = [];
    public ICollection<FuelLog> FuelLogs { get; set; } = [];
    public ICollection<Notification> Notifications { get; set; } = [];
}
