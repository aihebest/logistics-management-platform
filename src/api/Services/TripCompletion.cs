using LogisticsApi.Data;
using LogisticsApi.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Services;

/// <summary>
/// Finishing a trip, and putting its vehicle and driver back in the pool.
///
/// Shared by the trip's own Complete action and by the gate closing the trip's
/// movement-register entry. Before this, only the Complete button freed the
/// vehicle — and in practice the gate records the return, nobody presses
/// Complete, so vehicles and drivers stayed "in use / on a trip" for good.
/// </summary>
public static class TripCompletion
{
    /// <summary>
    /// Marks the trip and its assignment complete and frees the vehicle and
    /// driver — unless either is already booked on another live trip, in which
    /// case they stay in use for that one. Does not save.
    /// </summary>
    public static async Task CompleteAsync(AppDbContext db, TripRequest trip, int? mileageIn = null)
    {
        trip.Status = "Completed";

        var a = trip.Assignment;
        if (a == null) return;

        a.Status        = "Completed";
        a.ActualEndTime = DateTime.UtcNow;

        if (a.Driver != null)
        {
            var otherTrip = await db.Assignments.AnyAsync(x =>
                x.Id != a.Id && x.DriverId == a.DriverId && (x.Status == "Active" || x.Status == "Ongoing"));
            if (!otherTrip)
            {
                a.Driver.DriverStatus     = "Available";
                a.Driver.LastStatusChange = DateTime.UtcNow;
            }
        }

        if (a.Vehicle != null)
        {
            var otherTrip = await db.Assignments.AnyAsync(x =>
                x.Id != a.Id && x.VehicleId == a.VehicleId && (x.Status == "Active" || x.Status == "Ongoing"));
            if (!otherTrip && a.Vehicle.Status == "Assigned")
                a.Vehicle.Status = "Available";

            // The gate's mileage-in reading is the vehicle's latest odometer.
            if (mileageIn.HasValue && mileageIn.Value > a.Vehicle.OdometerKm)
                a.Vehicle.OdometerKm = mileageIn.Value;

            a.Vehicle.UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// The live trip a movement-register entry was opened for, if any. Entries
    /// created on trip approval carry the trip's short reference (the first
    /// eight characters of its id) in RelatedRefNo.
    /// </summary>
    public static async Task<TripRequest?> FindLiveTripForMovementAsync(AppDbContext db, MovementRegister entry)
    {
        if (string.IsNullOrWhiteSpace(entry.RelatedRefNo)) return null;
        var refNo = entry.RelatedRefNo.Trim().ToUpperInvariant();

        // Few trips are live at any time, so match the reference in memory.
        var live = await db.TripRequests
            .Include(t => t.RequestedBy)
            .Include(t => t.Assignment).ThenInclude(x => x!.Driver)
            .Include(t => t.Assignment).ThenInclude(x => x!.Vehicle)
            .Where(t => t.Status == "Active" || t.Status == "Ongoing")
            .ToListAsync();

        return live.FirstOrDefault(t => t.Id.ToString()[..8].ToUpperInvariant() == refNo);
    }
}
