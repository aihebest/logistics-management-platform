namespace LogisticsApi.Models.Entities;

public class Location
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;   // e.g. "Port Harcourt", "Lagos"
    public string Code { get; set; } = string.Empty;   // Short code for reports e.g. "PH", "LOS"
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }
}
