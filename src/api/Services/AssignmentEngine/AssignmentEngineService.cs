using LogisticsApi.Data;
using LogisticsApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Services.AssignmentEngine;

public class AssignmentEngineService(
    AppDbContext db,
    INotificationService notifications,
    IAuditService audit,
    ILogger<AssignmentEngineService> logger) : IAssignmentEngine
{
    public async Task<Assignment?> AssignAsync(TripRequest tripRequest, Guid assignedById)
    {
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(int)DateTime.UtcNow.DayOfWeek);

        // Eligible drivers: active, Available, valid licence
        var eligibleDrivers = await db.Users
            .Where(u => u.Role == "Driver"
                     && u.IsActive
                     && u.DriverStatus == "Available"
                     && (u.LicenceExpiry == null || u.LicenceExpiry >= DateOnly.FromDateTime(today)))
            .ToListAsync();

        if (eligibleDrivers.Count == 0)
        {
            logger.LogInformation("No eligible drivers for TripRequest {Id} — leaving as Pending", tripRequest.Id);
            return null;
        }

        // Load assignment counts for scoring
        var driverIds = eligibleDrivers.Select(d => d.Id).ToList();

        var todayCounts = await db.Assignments
            .Where(a => driverIds.Contains(a.DriverId)
                     && a.StartTime >= today
                     && a.Status != "Cancelled")
            .GroupBy(a => a.DriverId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var weekCounts = await db.Assignments
            .Where(a => driverIds.Contains(a.DriverId)
                     && a.StartTime >= weekStart
                     && a.Status != "Cancelled")
            .GroupBy(a => a.DriverId)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);

        var lastAssignment = await db.Assignments
            .Where(a => driverIds.Contains(a.DriverId) && a.Status != "Cancelled")
            .GroupBy(a => a.DriverId)
            .Select(g => new { g.Key, Last = g.Max(a => a.CreatedAt) })
            .ToDictionaryAsync(x => x.Key, x => x.Last);

        // Score each driver — lower is better (sort ascending)
        var scored = eligibleDrivers
            .Select(d =>
            {
                var todayCount = todayCounts.GetValueOrDefault(d.Id, 0);
                var weekCount  = weekCounts.GetValueOrDefault(d.Id, 0);
                var lastTripAge = lastAssignment.TryGetValue(d.Id, out var last)
                    ? (DateTime.UtcNow - last).TotalHours
                    : double.MaxValue;

                // Weighted score: today 40%, week 30%, recency 20% (negative = longer gap is better)
                var score = (todayCount * 0.40) + (weekCount * 0.30) - (lastTripAge * 0.20);
                return (Driver: d, Score: score);
            })
            .OrderBy(x => x.Score)
            .ToList();

        var bestDriver = scored[0].Driver;

        // Pick first available vehicle (prefer one not recently used)
        var vehicle = await db.Vehicles
            .Where(v => v.Status == "Available")
            .OrderBy(v => v.OdometerKm)
            .FirstOrDefaultAsync();

        if (vehicle == null)
        {
            logger.LogInformation("No available vehicles for TripRequest {Id}", tripRequest.Id);
            return null;
        }

        // Create assignment
        var assignment = new Assignment
        {
            Id = Guid.NewGuid(),
            TripRequestId = tripRequest.Id,
            DriverId = bestDriver.Id,
            VehicleId = vehicle.Id,
            AssignedById = assignedById,
            AssignmentType = "Auto",
            Status = "Active",
            StartTime = tripRequest.RequestedDateTime,
            EstimatedEndTime = tripRequest.RequestedDateTime.AddHours(2),
            CreatedAt = DateTime.UtcNow
        };

        // Update statuses
        bestDriver.DriverStatus = "OnAssignment";
        bestDriver.LastStatusChange = DateTime.UtcNow;
        vehicle.Status = "Assigned";
        vehicle.UpdatedAt = DateTime.UtcNow;
        tripRequest.Status = "Active";

        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        // Load nav props for notification
        assignment.Driver = bestDriver;
        assignment.Vehicle = vehicle;
        assignment.TripRequest = tripRequest;

        await notifications.SendAssignmentConfirmedAsync(assignment);
        await audit.LogAsync("Assignment", assignment.Id.ToString(), "AutoAssigned",
            assignedById.ToString(), "", null,
            $"Auto-assigned {bestDriver.FullName} (score {scored[0].Score:F1})");

        logger.LogInformation("Auto-assigned driver {Driver} to trip {Trip}", bestDriver.FullName, tripRequest.Id);
        return assignment;
    }
}
