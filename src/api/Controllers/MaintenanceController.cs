using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using LogisticsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

[ApiController]
[Route("api/maintenance")]
[Authorize]
public class MaintenanceController(
    AppDbContext db,
    INotificationService notifications,
    IGenServiceSyncService genService,
    ILogger<MaintenanceController> logger) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Coordinator,Manager,Mechanic,Admin")]
    public async Task<IEnumerable<MaintenanceRecordDto>> GetAll(
        [FromQuery] string? status,
        [FromQuery] Guid? vehicleId,
        [FromQuery] string? category)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var q = db.MaintenanceRecords.Include(m => m.Vehicle).AsQueryable();

        if (!string.IsNullOrEmpty(status))
            q = q.Where(m => m.Status == status);
        else
        {
            // Auto-flag overdue
            var overdue = await db.MaintenanceRecords
                .Where(m => m.Status == "Scheduled" && m.ScheduledDate < today)
                .ToListAsync();
            overdue.ForEach(m => m.Status = "Overdue");
            if (overdue.Any()) await db.SaveChangesAsync();
        }

        if (vehicleId.HasValue)
            q = q.Where(m => m.VehicleId == vehicleId);

        if (!string.IsNullOrEmpty(category))
            q = q.Where(m => m.Category == category);

        // Materialise before projecting: ToDto is a plain C# method (it now also
        // formats the General Service status label), so it must run client-side
        // rather than being handed to the query translator.
        var rows = await q.OrderBy(m => m.ScheduledDate).ToListAsync();
        return rows.Select(ToDto).ToList();
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Coordinator,Manager,Mechanic,Admin")]
    public async Task<ActionResult<MaintenanceRecordDto>> Get(Guid id)
    {
        var m = await db.MaintenanceRecords.Include(x => x.Vehicle).FirstOrDefaultAsync(x => x.Id == id);
        return m == null ? NotFound() : ToDto(m);
    }

    [HttpGet("vehicle/{vehicleId:guid}/history")]
    [Authorize(Roles = "Coordinator,Manager,Mechanic,Admin")]
    public async Task<IEnumerable<MaintenanceRecordDto>> GetHistory(Guid vehicleId)
    {
        var rows = await db.MaintenanceRecords
            .Include(m => m.Vehicle)
            .Where(m => m.VehicleId == vehicleId)
            .OrderByDescending(m => m.ScheduledDate)
            .ToListAsync();
        return rows.Select(ToDto).ToList();
    }

    /// <summary>
    /// Re-send a record to General Service — for when the hand-off failed at the
    /// time (their API was down, or the vehicle wasn't in their register yet).
    /// Safe to call repeatedly: already-linked records are returned unchanged.
    /// </summary>
    [HttpPost("{id:guid}/resend-to-genservice")]
    [Authorize(Roles = "Coordinator,Manager,Mechanic,Admin")]
    public async Task<ActionResult<MaintenanceRecordDto>> ResendToGenService(Guid id)
    {
        var record = await db.MaintenanceRecords
            .Include(m => m.Vehicle)
            .FirstOrDefaultAsync(m => m.Id == id);
        if (record == null) return NotFound();

        if (!genService.IsConfigured)
            return StatusCode(503, new
            {
                error = "The General Service link is not configured on this server "
                      + "(Integration__GenService__BaseUrl / __ApiKey)."
            });

        var handoff = await genService.RaiseMaintenanceRequestAsync(record, HttpContext.RequestAborted);

        // Surface the real reason rather than a generic failure — the remedy for a
        // wrong URL, a wrong key and an unconfigured server are entirely different.
        if (!handoff.Success && record.GenServiceRequestNumber is null)
            return StatusCode(502, new { error = handoff.Error ?? "General Service did not accept the record." });

        return Ok(ToDto(record));
    }

    [HttpPost]
    [Authorize(Roles = "Manager,Mechanic,Admin")]
    public async Task<ActionResult<MaintenanceRecordDto>> Create(CreateMaintenanceRecordDto dto)
    {
        var vehicle = await db.Vehicles.FindAsync(dto.VehicleId);
        if (vehicle == null) return BadRequest(new { error = "Vehicle not found" });

        var record = new Models.Entities.MaintenanceRecord
        {
            Id = Guid.NewGuid(),
            VehicleId = dto.VehicleId,
            Type = dto.Type,
            Category = dto.Category,
            ScheduledDate = dto.ScheduledDate,
            VendorName = dto.VendorName,
            Notes = dto.Notes,
            Status = "Scheduled",
            FaultReported = dto.FaultReported,
            FaultDescription = dto.FaultDescription,
            DateReturned = dto.DateReturned,
            PartsReplaced = dto.PartsReplaced,
            RepairRemarks = dto.RepairRemarks,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Emergency / fault repair: pull the vehicle out of service immediately
        // and alert the Logistics Manager and Supervisor.
        var isEmergency = dto.FaultReported || string.Equals(dto.Category, "FaultRepair", StringComparison.OrdinalIgnoreCase);
        if (isEmergency)
        {
            vehicle.Status = "InMaintenance";
            vehicle.UpdatedAt = DateTime.UtcNow;
        }

        db.MaintenanceRecords.Add(record);
        await db.SaveChangesAsync();
        record.Vehicle = vehicle;

        if (isEmergency)
        {
            try { await notifications.SendEmergencyMaintenanceLoggedAsync(record); }
            catch { /* email failure must not block record creation */ }
        }

        // Send the vehicle to General Service, who actually carry out the repair.
        // Their reference comes back onto the record; from then on every status
        // change they make is pushed to us. A failure here is not fatal — the
        // record stands and can be re-sent.
        try
        {
            var handoff = await genService.RaiseMaintenanceRequestAsync(record, HttpContext.RequestAborted);
            if (!handoff.Success && genService.IsConfigured)
                logger.LogWarning("Maintenance {Id} was not raised with General Service: {Reason}",
                    record.Id, handoff.Error);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "General Service hand-off threw for {Id} — record kept.", record.Id);
        }

        return CreatedAtAction(nameof(Get), new { id = record.Id }, ToDto(record));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Manager,Mechanic,Admin")]
    public async Task<IActionResult> Update(Guid id, UpdateMaintenanceRecordDto dto)
    {
        var record = await db.MaintenanceRecords.Include(m => m.Vehicle).FirstOrDefaultAsync(m => m.Id == id);
        if (record == null) return NotFound();

        var wasAlreadyCompleted = record.Status == "Completed";

        if (dto.Status != null) record.Status = dto.Status;

        var justCompleted = false;
        if (dto.CompletedDate.HasValue)
        {
            record.CompletedDate = dto.CompletedDate;
            record.Status = "Completed";
            justCompleted = !wasAlreadyCompleted;

            // Update vehicle's service dates
            record.Vehicle.LastServiceDate = dto.CompletedDate;
            record.Vehicle.NextServiceDate = dto.CompletedDate.Value.AddDays(
                record.Vehicle.ServiceIntervalKm / 100); // rough estimate by days

            // Return the vehicle to service if it was in maintenance
            var returnedToService = record.Vehicle.Status == "InMaintenance";
            if (returnedToService)
                record.Vehicle.Status = "Available";
            record.Vehicle.UpdatedAt = DateTime.UtcNow;
        }
        if (dto.DateReturned.HasValue) record.DateReturned = dto.DateReturned;
        if (dto.Cost.HasValue) record.Cost = dto.Cost;
        if (dto.VendorName != null) record.VendorName = dto.VendorName;
        if (dto.Notes != null) record.Notes = dto.Notes;
        if (dto.AttachmentBlobUrl != null) record.AttachmentBlobUrl = dto.AttachmentBlobUrl;
        if (dto.PartsReplaced != null) record.PartsReplaced = dto.PartsReplaced;
        if (dto.RepairRemarks != null) record.RepairRemarks = dto.RepairRemarks;
        record.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        // Notify on completion (maintenance done + vehicle back in service)
        if (justCompleted)
        {
            try
            {
                await notifications.SendMaintenanceCompletedAsync(record);
                if (record.Vehicle.Status == "Available")
                    await notifications.SendVehicleReturnedToServiceAsync(record);
            }
            catch { /* email failure must not block the update */ }
        }

        return NoContent();
    }

    private static MaintenanceRecordDto ToDto(Models.Entities.MaintenanceRecord m) => new(
        m.Id, m.VehicleId, m.Vehicle.RegistrationNo, m.Type,
        m.Category ?? "Routine",
        m.ScheduledDate, m.CompletedDate, m.DateReturned, m.Cost,
        m.VendorName, m.Notes,
        m.Status, m.AttachmentBlobUrl,
        m.FaultReported, m.FaultDescription, m.DateReported,
        m.PartsReplaced, m.RepairRemarks,
        m.CreatedAt,
        m.GenServiceRequestNumber,
        m.GenServiceStatus,
        m.GenServiceStatus is null ? null : GenServiceStatusMap.Explain(m.GenServiceStatus),
        m.GenServiceFaultIdentified,
        m.GenServiceWorkDone,
        m.GenServiceWorkshopName,
        m.GenServiceSyncedAt,
        m.SourceSystem);
}
