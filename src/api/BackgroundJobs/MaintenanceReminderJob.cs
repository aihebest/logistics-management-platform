using LogisticsApi.Data;
using LogisticsApi.Services;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.BackgroundJobs;

public class MaintenanceReminderJob(
    IServiceScopeFactory scopeFactory,
    ILogger<MaintenanceReminderJob> logger) : BackgroundService
{
    // Run once per day at 07:00 UTC
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextRun = now.Date.AddDays(1).AddHours(7);
            var delay = nextRun - now;

            // First iteration: run after a short warm-up so the API is ready
            if (delay > TimeSpan.FromHours(23))
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            else
                await Task.Delay(delay, stoppingToken);

            if (stoppingToken.IsCancellationRequested) break;

            await RunCheckAsync(stoppingToken);
        }
    }

    private async Task RunCheckAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

        try
        {
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var checkpoints = new[] { 14, 7, 3, 0 }; // days before due

            var upcoming = await db.MaintenanceRecords
                .Include(m => m.Vehicle)
                .Where(m => m.Status == "Scheduled"
                         && m.ScheduledDate >= today
                         && m.ScheduledDate <= today.AddDays(14))
                .ToListAsync(ct);

            foreach (var record in upcoming)
            {
                var daysUntil = record.ScheduledDate.DayNumber - today.DayNumber;
                if (checkpoints.Contains(daysUntil))
                {
                    await notifications.SendMaintenanceDueAsync(record, daysUntil);
                    logger.LogInformation("Maintenance reminder sent: {Vehicle} — {Days} days",
                        record.Vehicle.RegistrationNo, daysUntil);
                }
            }

            // Overdue
            var overdue = await db.MaintenanceRecords
                .Include(m => m.Vehicle)
                .Where(m => m.Status != "Completed" && m.Status != "Cancelled"
                         && m.ScheduledDate < today)
                .ToListAsync(ct);

            foreach (var record in overdue)
            {
                record.Status = "Overdue";
                await notifications.SendMaintenanceOverdueAsync(record);
                logger.LogWarning("Overdue maintenance: {Vehicle} ({Type}) — was due {Date}",
                    record.Vehicle.RegistrationNo, record.Type, record.ScheduledDate);
            }

            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            logger.LogError(ex, "Maintenance reminder job failed");
        }
    }
}
