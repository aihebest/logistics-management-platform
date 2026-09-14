using System.Globalization;
using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using LogisticsApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

/// <summary>
/// Machine-to-machine endpoints shared with the General Service platform
/// (genservice.desiconapp.com).
///
/// Not [Authorize]d: General Service calls us as a server, with no Entra identity
/// behind the request. Every action is gated by
/// <see cref="RequireIntegrationKeyAttribute"/> instead.
/// </summary>
[ApiController]
[Route("api/integration/genservice")]
public class IntegrationController(
    AppDbContext db,
    INotificationService notifications,
    ILogger<IntegrationController> logger) : ControllerBase
{
    // ── Our fleet, so General Service can pick a real vehicle ─────────────────

    /// <summary>
    /// The fleet register, used to populate the vehicle picker on the General
    /// Service maintenance form and to match a typed plate to a real vehicle.
    /// </summary>
    [HttpGet("vehicles")]
    [RequireIntegrationKey]
    public async Task<IEnumerable<IntegrationVehicleDto>> Vehicles()
        => await db.Vehicles
            .AsNoTracking()
            .OrderBy(v => v.RegistrationNo)
            .Select(v => new IntegrationVehicleDto(
                v.Id, v.RegistrationNo, v.Make, v.Model,
                // Year is a non-nullable short here but optional in the contract;
                // 0 means "not recorded", so send null rather than a bogus year 0.
                v.Year == 0 ? (int?)null : (int?)v.Year,
                v.AssetTagNo, v.Status, (int?)v.OdometerKm))
            .ToListAsync();

    // ── Inbound status feed from General Service ──────────────────────────────

    /// <summary>
    /// General Service has moved one of our vehicles along. Update the matching
    /// maintenance record — or create one if the request was raised on their side
    /// — and put the vehicle in or out of service accordingly.
    ///
    /// Idempotent: re-posting the same state is a no-op beyond refreshing the
    /// timestamp, so their retries are safe.
    /// </summary>
    [HttpPost("maintenance-status")]
    [RequireIntegrationKey]
    public async Task<ActionResult<GenServiceStatusPushAck>> MaintenanceStatus(
        [FromBody] GenServiceStatusPushDto p)
    {
        if (string.IsNullOrWhiteSpace(p.VehicleRegNo))
            return BadRequest(new { error = "vehicleRegNo is required." });

        // Find the record: by our own id first, then by their request id.
        var record = await db.MaintenanceRecords
            .Include(m => m.Vehicle)
            .FirstOrDefaultAsync(m =>
                (p.LogisticsRecordId != null && m.Id == p.LogisticsRecordId) ||
                m.GenServiceRequestId == p.GenServiceRequestId);

        // Resolve the vehicle — by their supplied id, then by normalised plate.
        var vehicle = record?.Vehicle;
        if (vehicle is null)
        {
            if (p.LogisticsVehicleId is Guid vid && vid != Guid.Empty)
                vehicle = await db.Vehicles.FindAsync(vid);

            if (vehicle is null)
            {
                var key = Normalise(p.VehicleRegNo);
                // Normalisation can't be translated to SQL, so match in memory over
                // the plate list only — the fleet is small enough for this to be cheap.
                var candidates = await db.Vehicles
                    .Select(v => new { v.Id, v.RegistrationNo })
                    .ToListAsync();
                var hit = candidates.FirstOrDefault(v => Normalise(v.RegistrationNo) == key);
                if (hit is not null) vehicle = await db.Vehicles.FindAsync(hit.Id);
            }
        }

        if (vehicle is null)
        {
            // We deliberately do NOT invent a vehicle here. A stub would pollute
            // the fleet register with typos; General Service surfaces these on
            // their reconciliation screen instead.
            logger.LogWarning("GenService push for {Ref}: no vehicle matches '{Reg}' — ignored.",
                p.GenServiceRequestNumber, p.VehicleRegNo);
            return NotFound(new
            {
                error = $"No vehicle in the fleet register matches '{p.VehicleRegNo}'."
            });
        }

        var isNew = record is null;
        if (record is null)
        {
            record = new Models.Entities.MaintenanceRecord
            {
                Id            = Guid.NewGuid(),
                VehicleId     = vehicle.Id,
                Vehicle       = vehicle,
                Category      = string.Equals(p.MaintenanceType, "RoutineService", StringComparison.OrdinalIgnoreCase)
                                    ? "Routine" : "FaultRepair",
                Type          = ServiceTypeFrom(p),
                ScheduledDate = ParseDay(p.DateReported) ?? DateOnly.FromDateTime(DateTime.UtcNow),
                FaultReported = !string.Equals(p.MaintenanceType, "RoutineService", StringComparison.OrdinalIgnoreCase),
                SourceSystem  = "GenService",
                CreatedAt     = DateTime.UtcNow,
            };
            db.MaintenanceRecords.Add(record);
        }

        var wasCompleted = record.Status == "Completed";

        // ── Apply their update ────────────────────────────────────────────────
        // Every assignment is length-capped. The two platforms size these columns
        // independently, and a plate or a workshop name that is one character too
        // long for us would otherwise fail the whole push with a truncation error.
        record.GenServiceRequestId       = p.GenServiceRequestId;
        record.GenServiceRequestNumber   = Cap(p.GenServiceRequestNumber, 40);
        record.GenServiceStatus          = Cap(p.GenServiceStatus, 40);
        record.GenServiceFaultIdentified = Cap(p.FaultIdentified, 2000) ?? record.GenServiceFaultIdentified;
        record.GenServiceWorkDone        = Cap(p.WorkDone, 2000)        ?? record.GenServiceWorkDone;
        record.GenServiceWorkshopName    = Cap(p.WorkshopName, 200)     ?? record.GenServiceWorkshopName;
        record.GenServiceSyncedAt        = DateTime.UtcNow;

        // Re-derive rather than trusting their mapped value, so a change to their
        // vocabulary can never write a status we don't understand.
        record.Status = GenServiceStatusMap.ToLogistics(p.GenServiceStatus);

        if (!string.IsNullOrWhiteSpace(p.WorkshopName))    record.VendorName    = Cap(p.WorkshopName.Trim(), 100);
        if (!string.IsNullOrWhiteSpace(p.FaultIdentified)) record.RepairRemarks = p.FaultIdentified.Trim();
        if (p.Cost.HasValue) record.Cost = p.Cost;

        var completedOn = ParseDay(p.CompletedDate);
        if (completedOn.HasValue) record.CompletedDate = completedOn;

        var returnedOn = ParseDay(p.DateReturned);
        if (returnedOn.HasValue) record.DateReturned = returnedOn;

        // Keep their notes visible without destroying anything we wrote ourselves.
        var stamp = $"[GS {p.GenServiceRequestNumber}] {GenServiceStatusMap.Explain(p.GenServiceStatus)}";
        if (!string.IsNullOrWhiteSpace(p.RejectionReason))
            stamp += $" — {p.RejectionReason.Trim()}";
        record.Notes = Merge(record.Notes, stamp);

        record.UpdatedAt = DateTime.UtcNow;

        // ── Vehicle availability ──────────────────────────────────────────────
        // This is the point of the whole integration: the fleet register tells the
        // truth about which vehicles we can actually dispatch.
        var justCompleted = record.Status == "Completed" && !wasCompleted;

        if (p.VehicleOutOfService && vehicle.Status != "OutOfService")
        {
            vehicle.Status    = "InMaintenance";
            vehicle.UpdatedAt = DateTime.UtcNow;
        }
        else if (justCompleted && vehicle.Status == "InMaintenance")
        {
            vehicle.Status        = "Available";
            vehicle.LastServiceDate = record.CompletedDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
            vehicle.UpdatedAt     = DateTime.UtcNow;
        }

        await db.SaveChangesAsync();

        logger.LogInformation(
            "GenService {Ref} → {Reg}: {GsStatus} (ours: {Status}){New}",
            p.GenServiceRequestNumber, vehicle.RegistrationNo, p.GenServiceStatus,
            record.Status, isNew ? " [record created]" : "");

        // ── Tell our team ─────────────────────────────────────────────────────
        // Reuses the existing maintenance emails, so the Logistics Manager and
        // Supervisor hear about it through the channel they already watch.
        if (justCompleted)
        {
            try
            {
                await notifications.SendMaintenanceCompletedAsync(record);
                if (vehicle.Status == "Available")
                    await notifications.SendVehicleReturnedToServiceAsync(record);
            }
            catch (Exception ex)
            {
                // An email failure must never make us reject their push — they
                // would retry forever and the data is already saved.
                logger.LogError(ex, "Notification failed for {Ref} — update was still applied.",
                    p.GenServiceRequestNumber);
            }
        }

        return Ok(new GenServiceStatusPushAck(record.Id,
            isNew ? "Maintenance record created and linked." : "Maintenance record updated."));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Strip everything that varies between how people type the same plate.</summary>
    private static string Normalise(string s) =>
        new(s.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    private static DateOnly? ParseDay(string? s) =>
        DateOnly.TryParseExact(s, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                               DateTimeStyles.None, out var d)
            ? d
            : null;

    /// <summary>Service type is nvarchar(100) here, so a long description falls back to the category.</summary>
    private static string ServiceTypeFrom(GenServiceStatusPushDto p) =>
        !string.IsNullOrWhiteSpace(p.Description) && p.Description.Trim().Length <= 100
            ? p.Description.Trim()
            : p.MaintenanceType switch
            {
                "RoutineService" => "Routine Service",
                "MinorRepair"    => "Minor Repair",
                "MajorRepair"    => "Major Repair",
                _                => "Other",
            };

    /// <summary>Append the latest General Service line without losing what came before.</summary>
    private static string Merge(string? existing, string line)
    {
        if (string.IsNullOrWhiteSpace(existing)) return Cap(line);

        // Replace the previous GS stamp rather than stacking one per status change.
        var kept = existing
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(l => !l.TrimStart().StartsWith("[GS ", StringComparison.OrdinalIgnoreCase))
            .Select(l => l.TrimEnd());

        return Cap(string.Join('\n', kept.Append(line)).Trim());
    }

    /// <summary>Notes is nvarchar(1000) — never hand SQL Server more than it will take.</summary>
    private static string Cap(string s) => s.Length <= 1000 ? s : s[..1000];

    /// <summary>
    /// Length-cap for a nullable value. The two platforms size their columns
    /// independently, so anything arriving from General Service is trimmed to
    /// what our schema accepts rather than failing the push on a truncation error.
    /// </summary>
    private static string? Cap(string? s, int max) =>
        string.IsNullOrEmpty(s) ? s : s.Length <= max ? s : s[..max];
}
