using System.Security.Claims;
using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using LogisticsApi.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

[ApiController]
[Route("api/driver-schedules")]
[Authorize]
public class DriverScheduleController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public async Task<IEnumerable<DriverScheduleDto>> GetAll(
        [FromQuery] Guid? driverId,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to)
    {
        var q = db.DriverSchedules
            .Include(s => s.Driver)
            .Include(s => s.CreatedBy)
            .Include(s => s.Location)
            .AsQueryable();

        if (driverId.HasValue) q = q.Where(s => s.DriverId == driverId);
        if (from.HasValue) q = q.Where(s => s.ScheduleDate >= from);
        if (to.HasValue) q = q.Where(s => s.ScheduleDate <= to);

        return await q.OrderBy(s => s.ScheduleDate).ThenBy(s => s.Driver.FullName)
            .Select(s => ToDto(s)).ToListAsync();
    }

    [HttpGet("week")]
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public async Task<IEnumerable<DriverScheduleDto>> GetWeek([FromQuery] DateOnly? startDate)
    {
        var start = startDate ?? DateOnly.FromDateTime(DateTime.UtcNow);
        // Go back to Monday of the current week
        var dayOfWeek = (int)start.DayOfWeek;
        var monday = start.AddDays(dayOfWeek == 0 ? -6 : -(dayOfWeek - 1));
        var sunday = monday.AddDays(6);

        return await db.DriverSchedules
            .Include(s => s.Driver)
            .Include(s => s.CreatedBy)
            .Include(s => s.Location)
            .Where(s => s.ScheduleDate >= monday && s.ScheduleDate <= sunday)
            .OrderBy(s => s.ScheduleDate)
            .ThenBy(s => s.Driver.FullName)
            .Select(s => ToDto(s))
            .ToListAsync();
    }

    [HttpPost]
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public async Task<ActionResult<DriverScheduleDto>> Create(CreateDriverScheduleDto dto)
    {
        var driver = await db.Users.FindAsync(dto.DriverId);
        if (driver == null || driver.Role != "Driver")
            return BadRequest(new { error = "Driver not found" });

        var callerId = User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var caller = await db.Users.FirstOrDefaultAsync(u => u.EntraObjectId == callerId);
        if (caller == null) return Unauthorized();

        // Remove existing schedule for that driver on that date if any
        var existing = await db.DriverSchedules
            .FirstOrDefaultAsync(s => s.DriverId == dto.DriverId && s.ScheduleDate == dto.ScheduleDate);
        if (existing != null) db.DriverSchedules.Remove(existing);

        var schedule = new DriverSchedule
        {
            Id = Guid.NewGuid(),
            DriverId = dto.DriverId,
            ScheduleDate = dto.ScheduleDate,
            WorkLocation = dto.WorkLocation,
            LocationId = dto.LocationId,
            Shift = dto.Shift,
            Notes = dto.Notes,
            CreatedById = caller.Id,
            CreatedAt = DateTime.UtcNow
        };

        db.DriverSchedules.Add(schedule);
        await db.SaveChangesAsync();

        schedule.Driver = driver;
        schedule.CreatedBy = caller;
        if (schedule.LocationId.HasValue)
            schedule.Location = await db.Locations.FindAsync(schedule.LocationId);

        return CreatedAtAction(nameof(GetAll), new { id = schedule.Id }, ToDto(schedule));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var schedule = await db.DriverSchedules.FindAsync(id);
        if (schedule == null) return NotFound();
        db.DriverSchedules.Remove(schedule);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static DriverScheduleDto ToDto(DriverSchedule s) => new(
        s.Id, s.DriverId, s.Driver?.FullName ?? "",
        s.ScheduleDate, s.WorkLocation, s.LocationId, s.Location?.Name,
        s.Shift, s.Notes,
        s.CreatedBy?.FullName ?? "", s.CreatedAt);
}
