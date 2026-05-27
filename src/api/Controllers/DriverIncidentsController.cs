using System.Security.Claims;
using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using LogisticsApi.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

[ApiController]
[Route("api/driver-incidents")]
[Authorize]
public class DriverIncidentsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public async Task<IEnumerable<DriverIncidentDto>> GetAll(
        [FromQuery] Guid? driverId,
        [FromQuery] string? type,
        [FromQuery] string? severity)
    {
        var q = db.DriverIncidents
            .Include(i => i.Driver)
            .Include(i => i.ReportedBy)
            .AsQueryable();

        if (driverId.HasValue) q = q.Where(i => i.DriverId == driverId);
        if (!string.IsNullOrEmpty(type)) q = q.Where(i => i.Type == type);
        if (!string.IsNullOrEmpty(severity)) q = q.Where(i => i.Severity == severity);

        return await q.OrderByDescending(i => i.IncidentDate).Select(i => ToDto(i)).ToListAsync();
    }

    [HttpPost]
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public async Task<ActionResult<DriverIncidentDto>> Create(CreateDriverIncidentDto dto)
    {
        var driver = await db.Users.FindAsync(dto.DriverId);
        if (driver == null || driver.Role != "Driver")
            return BadRequest(new { error = "Driver not found" });

        var callerId = User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var caller = await db.Users.FirstOrDefaultAsync(u => u.EntraObjectId == callerId);
        if (caller == null) return Unauthorized();

        var incident = new DriverIncident
        {
            Id = Guid.NewGuid(),
            DriverId = dto.DriverId,
            IncidentDate = dto.IncidentDate,
            Type = dto.Type,
            Description = dto.Description,
            Severity = dto.Severity,
            ActionTaken = dto.ActionTaken,
            ReportedById = caller.Id,
            CreatedAt = DateTime.UtcNow
        };

        db.DriverIncidents.Add(incident);
        await db.SaveChangesAsync();
        incident.Driver = driver;
        incident.ReportedBy = caller;
        return CreatedAtAction(nameof(GetAll), new { id = incident.Id }, ToDto(incident));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var incident = await db.DriverIncidents.FindAsync(id);
        if (incident == null) return NotFound();
        db.DriverIncidents.Remove(incident);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static DriverIncidentDto ToDto(DriverIncident i) => new(
        i.Id, i.DriverId, i.Driver?.FullName ?? "",
        i.IncidentDate, i.Type, i.Description, i.Severity,
        i.ActionTaken, i.ReportedBy?.FullName ?? "", i.CreatedAt);
}
