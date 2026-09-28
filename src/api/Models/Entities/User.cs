namespace LogisticsApi.Models.Entities;

public class User
{
    public Guid Id { get; set; }
    public string EntraObjectId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    /// <summary>
    /// The person's primary role — what the UI shows and what a manually
    /// registered user (a driver, say) is known by.
    /// </summary>
    public string Role { get; set; } = string.Empty;          // Driver | Coordinator | Manager | Mechanic | HOD | Management | Admin

    /// <summary>
    /// Every app role Entra gives this person, stored as ",Manager,Management,".
    ///
    /// One role was not enough. The head of Logistics runs logistics operations
    /// (Manager) and also approves travel (Management); with a single column she
    /// could only have one, and whichever she lost took its screens and emails
    /// with it. The leading and trailing commas let a lookup match a whole role
    /// name, so "Manager" never matches inside "Management".
    /// </summary>
    public string? AppRoles { get; set; }
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
