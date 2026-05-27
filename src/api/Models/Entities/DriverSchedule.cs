namespace LogisticsApi.Models.Entities;

public class DriverSchedule
{
    public Guid Id { get; set; }
    public Guid DriverId { get; set; }
    public DateOnly ScheduleDate { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Shift { get; set; } = "Day";               // Day | Night | Off | Leave
    public string? Notes { get; set; }
    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; set; }

    public User Driver { get; set; } = null!;
    public User CreatedBy { get; set; } = null!;
}
