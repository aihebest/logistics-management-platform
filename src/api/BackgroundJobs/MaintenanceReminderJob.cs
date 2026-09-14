using LogisticsApi.Data;
using LogisticsApi.Services;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.BackgroundJobs;

public class MaintenanceReminderJob(
    IServiceScopeFactory scopeFactory,
    ILogger<MaintenanceReminderJob> logger) : BackgroundService
{
    /// <summary>
    /// Runs once per day at 07:00 UTC, after a short warm-up on startup.
    ///
    /// The warm-up used to be selected by testing whether the delay to the next
    /// run exceeded 23 hours. That is true for the whole window between midnight
    /// and 07:00 UTC, not just on the first pass — so during those hours the job
    /// looped every two minutes and re-sent every reminder each time. The flag
    /// below says what was actually meant: warm up once, then sleep until 07:00.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var firstPass = true;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (firstPass)
                {
                    await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
                    firstPass = false;
                }
                else
                {
                    var now  = DateTime.UtcNow;
                    var next = now.Date.AddHours(7);
                    if (next <= now) next = next.AddDays(1);
                    await Task.Delay(next - now, stoppingToken);
                }
            }
            catch (OperationCanceledException) { break; }

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
                if (!checkpoints.Contains(daysUntil)) continue;

                // At most one reminder per record per day, whatever else happens.
                // These go to a distribution list, so a repeat is not a harmless
                // duplicate — it trains the team to ignore the alert entirely.
                if (AlreadySentToday(record.LastReminderSentAt, today)) continue;

                await notifications.SendMaintenanceDueAsync(record, daysUntil);
                record.LastReminderSentAt = DateTime.UtcNow;

                logger.LogInformation("Maintenance reminder sent: {Vehicle} — {Days} days",
                    record.Vehicle.RegistrationNo, daysUntil);
            }

            // Overdue
            var overdue = await db.MaintenanceRecords
                .Include(m => m.Vehicle)
                .Where(m => m.Status != "Completed" && m.Status != "Cancelled"
                         && m.ScheduledDate < today)
                .ToListAsync(ct);

            foreach (var record in overdue)
            {
                // Flag it regardless — the status is how the UI shows the problem.
                record.Status = "Overdue";

                // But chase by email only once a day. An overdue vehicle stays
                // overdue for weeks; without this it would mail the team on
                // every single pass for the whole period.
                if (AlreadySentToday(record.LastOverdueNoticeAt, today)) continue;

                await notifications.SendMaintenanceOverdueAsync(record);
                record.LastOverdueNoticeAt = DateTime.UtcNow;

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

    /// <summary>
    /// Has a notice already gone out for this record today? Compared on the UTC
    /// calendar day, matching the day the job itself works in.
    /// </summary>
    private static bool AlreadySentToday(DateTime? lastSentAt, DateOnly today) =>
        lastSentAt is DateTime t && DateOnly.FromDateTime(t) >= today;
}
