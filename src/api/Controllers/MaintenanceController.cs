using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

[ApiController]
[Route("api/maintenance")]
[Authorize]
public class MaintenanceController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Coordinator,Manager,Mechanic,Admin")]
    public async Task<IEnumerable<MaintenanceRecordDto>> GetAll(
        [FromQuery] string? status, [FromQuery] Guid? vehicleId)
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

        if (vehicleId.HasValue) q = q.Where(m => m.VehicleId == vehicleId);

        return await q.OrderBy(m => m.ScheduledDate).Select(m => ToDto(m)).ToListAsync();
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Coordinator,Manager,Mechanic,Admin")]
    public async Task<ActionResult<MaintenanceRecordDto>> Get(Guid id)
    {
        var m = await db.MaintenanceRecords.Include(x => x.Vehicle).FirstOrDefaultAsync(x => x.Id == id);
        return m == null ? NotFound() : ToDto(m);
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
            ScheduledDate = dto.ScheduledDate,
            VendorName = dto.VendorName,
            VendorContact = dto.VendorContact,
            Notes = dto.Notes,
            Status = "Scheduled",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.MaintenanceRecords.Add(record);
        await db.SaveChangesAsync();
        record.Vehicle = vehicle;
        return CreatedAtAction(nameof(Get), new { id = record.Id }, ToDto(record));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Manager,Mechanic,Admin")]
    public async Task<IActionResult> Update(Guid id, UpdateMaintenanceRecordDto dto)
    {
        var record = await db.MaintenanceRecords.Include(m => m.Vehicle).FirstOrDefaultAsync(m => m.Id == id);
        if (record == null) return NotFound();

        if (dto.Status != null) record.Status = dto.Status;
        if (dto.CompletedDate.HasValue)
        {
            record.CompletedDate = dto.CompletedDate;
            record.Status = "Completed";
            // Update vehicle's service dates
            record.Vehicle.LastServiceDate = dto.CompletedDate;
            record.Vehicle.NextServiceDate = dto.CompletedDate.Value.AddDays(
                record.Vehicle.ServiceIntervalKm / 100); // rough estimate by days
            record.Vehicle.UpdatedAt = DateTime.UtcNow;
        }
        if (dto.Cost.HasValue) record.Cost = dto.Cost;
        if (dto.VendorName != null) record.VendorName = dto.VendorName;
        if (dto.VendorContact != null) record.VendorContact = dto.VendorContact;
        if (dto.Notes != null) record.Notes = dto.Notes;
        if (dto.AttachmentBlobUrl != null) record.AttachmentBlobUrl = dto.AttachmentBlobUrl;
        record.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return NoContent();
    }

    private static MaintenanceRecordDto ToDto(Models.Entities.MaintenanceRecord m) => new(
        m.Id, m.VehicleId, m.Vehicle.RegistrationNo, m.Type,
        m.ScheduledDate, m.CompletedDate, m.Cost,
        m.VendorName, m.VendorContact, m.Notes,
        m.Status, m.AttachmentBlobUrl, m.CreatedAt);
}
