namespace LogisticsApi.Models.Entities;

public class DriverSchedule
{
    public Guid Id { get; set; }
    public Guid DriverId { get; set; }
    public DateOnly ScheduleDate { get; set; }
    public string WorkLocation { get; set; } = string.Empty;   // Freetext deployment location
    public Guid? LocationId { get; set; }                       // FK → Locations table
    public string Shift { get; set; } = "Day Shift";
    // Day Shift | Night Shift | Off Duty | Leave |
    // Guest House Driver | Night Standby Driver | Expatriate Driver |
    // Management Driver | Project Assignment Driver
    public string? Notes { get; set; }
    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; set; }

    public User Driver { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
    public Location? Location { get; set; }
}
