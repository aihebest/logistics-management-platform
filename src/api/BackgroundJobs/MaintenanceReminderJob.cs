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

            // ── Upcoming services ────────────────────────────────────────────
            // Driven by the vehicle's own NextServiceDate, set when a service is
            // completed. This used to read a maintenance record's date, but that
            // column now records when a fault was *reported* — always today or
            // earlier — so the forward-looking window never matched and these
            // reminders had quietly stopped going out altogether.
            var horizon = today.AddDays(MaintenancePolicy.ServiceReminderCheckpoints.Max());

            var dueSoon = await db.Vehicles
                .Where(v => v.Status != "OutOfService"
                         && v.NextServiceDate != null
                         && v.NextServiceDate >= today
                         && v.NextServiceDate <= horizon)
                .ToListAsync(ct);

            foreach (var vehicle in dueSoon)
            {
                var daysUntil = vehicle.NextServiceDate!.Value.DayNumber - today.DayNumber;
                if (!MaintenancePolicy.ServiceReminderCheckpoints.Contains(daysUntil)) continue;

                // At most one reminder per vehicle per day, whatever else happens.
                // These go to a distribution list, so a repeat is not a harmless
                // duplicate — it trains the team to ignore the alert entirely.
                if (AlreadySentToday(vehicle.LastServiceReminderAt, today)) continue;

                await notifications.SendVehicleServiceDueAsync(vehicle, daysUntil);
                vehicle.LastServiceReminderAt = DateTime.UtcNow;

                logger.LogInformation("Service due reminder sent: {Vehicle} — {Days} days",
                    vehicle.RegistrationNo, daysUntil);
            }

            // ── Jobs left open too long ──────────────────────────────────────
            // A record is overdue once it has been open past the grace period,
            // not the day after it was raised. Without the grace period every
            // open job would turn red immediately and the badge would stop
            // meaning anything.
            var cutoff = MaintenancePolicy.OverdueCutoff(today);

            var overdue = await db.MaintenanceRecords
                .Include(m => m.Vehicle)
                .Where(m => m.Status != "Completed" && m.Status != "Cancelled"
                         && m.ScheduledDate <= cutoff)
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

                logger.LogWarning("Overdue maintenance: {Vehicle} ({Type}) — reported {Date}, still open",
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
