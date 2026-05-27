namespace LogisticsApi.Models.Entities;

public class MaterialTransportRequest
{
    public Guid Id { get; set; }
    public string FormNumber { get; set; } = string.Empty;    // Auto: DEL-LG-FRM-009/YYYY/NNN
    public Guid RequestedById { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string Purpose { get; set; } = string.Empty;
    public string LoadingPoint { get; set; } = string.Empty;
    public string? LoadingContactPerson { get; set; }
    public string? LoadingContactPhone { get; set; }
    public DateOnly? LoadingDate { get; set; }
    public string DeliveryPoint { get; set; } = string.Empty;
    public string? DeliveryContactPerson { get; set; }
    public string? DeliveryContactPhone { get; set; }
    public DateOnly? DeliveryDate { get; set; }
    // Approval workflow: Draft → PendingHOD → PendingManager → Approved | Rejected
    public string Status { get; set; } = "Draft";
    public Guid? HodApprovedById { get; set; }
    public DateTime? HodApprovedAt { get; set; }
    public string? HodRemarks { get; set; }
    public Guid? ManagerApprovedById { get; set; }
    public DateTime? ManagerApprovedAt { get; set; }
    public string? ManagerRemarks { get; set; }
    public Guid? AssignedDriverId { get; set; }
    public Guid? AssignedVehicleId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User RequestedBy { get; set; } = null!;
    public User? HodApprovedBy { get; set; }
    public User? ManagerApprovedBy { get; set; }
    public User? AssignedDriver { get; set; }
    public Vehicle? AssignedVehicle { get; set; }
    public ICollection<MaterialTransportItem> Items { get; set; } = [];
}
