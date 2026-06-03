using System.Security.Claims;
using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using LogisticsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

[ApiController]
[Route("api/drivers")]
[Authorize]
public class DriversController(AppDbContext db, IAuditService audit) : ControllerBase
{
    // ── Register a new driver (pre-registration before Entra account exists) ─────
    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<UserDto>> Register(RegisterDriverDto dto)
    {
        var emailNorm = dto.Email?.ToLowerInvariant().Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(emailNorm) && await db.Users.AnyAsync(u => u.Email == emailNorm))
            return Conflict(new { error = "A user with this email already exists." });

        var driver = new Models.Entities.User
        {
            Id = Guid.NewGuid(),
            EntraObjectId = $"pre-{Guid.NewGuid():N}",
            FullName = dto.FullName,
            Email = emailNorm,
            PhoneNumber = dto.PhoneNumber,
            Role = "Driver",
            DriverStatus = "OffDuty",
            LicenceNo = dto.LicenceNo,
            LicenceExpiry = dto.LicenceExpiry,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(driver);
        await db.SaveChangesAsync();

        var callerEmail = User.FindFirstValue("preferred_username") ?? "";
        var callerId = User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        await audit.LogAsync("Driver", driver.Id.ToString(), "Registered", callerId ?? "", callerEmail, null,
            $"Driver pre-registered: {driver.FullName} ({driver.Email})");

        return CreatedAtAction(nameof(Get), new { id = driver.Id }, ToDto(driver));
    }

    [HttpGet]
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public async Task<IEnumerable<UserDto>> GetAll()
    {
        return await db.Users
            .Where(u => u.Role == "Driver" && u.IsActive)
            .OrderBy(u => u.FullName)
            .Select(u => ToDto(u))
            .ToListAsync();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserDto>> Get(Guid id)
    {
        var user = await db.Users.FindAsync(id);
        if (user == null) return NotFound();
        return ToDto(user);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateDriverStatusDto dto)
    {
        var allowed = new[] { "Available", "OnAssignment", "OffDuty", "OnBreak" };
        if (!allowed.Contains(dto.Status))
            return BadRequest(new { error = $"Invalid status. Must be one of: {string.Join(", ", allowed)}" });

        var driver = await db.Users.FindAsync(id);
        if (driver is null || driver.Role != "Driver") return NotFound();

        var callerId = User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet();
        if (!roles.Overlaps(new[] { "Coordinator", "Manager", "Admin" }) && driver.EntraObjectId != callerId)
            return Forbid();

        driver.DriverStatus = dto.Status;
        driver.LastStatusChange = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var email = User.FindFirstValue("preferred_username") ?? "";
        await audit.LogAsync("Driver", id.ToString(), "StatusChanged", callerId ?? "", email, null,
            $"Status → {dto.Status}");

        return NoContent();
    }

    [HttpGet("{id:guid}/assignments")]
    public async Task<IEnumerable<AssignmentDto>> GetAssignments(Guid id)
    {
        return await db.Assignments
            .Include(a => a.TripRequest)
            .Include(a => a.Vehicle)
            .Where(a => a.DriverId == id)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => ToAssignmentDto(a))
            .ToListAsync();
    }

    // ── Driver Performance Summary ─────────────────────────────────────────────
    [HttpGet("{id:guid}/performance")]
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public async Task<ActionResult<DriverPerformanceDto>> GetPerformance(Guid id)
    {
        var driver = await db.Users.FindAsync(id);
        if (driver == null || driver.Role != "Driver") return NotFound();

        var assignments = await db.Assignments
            .Include(a => a.TripRequest)
            .Include(a => a.Vehicle)
            .Where(a => a.DriverId == id)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        var incidents = await db.DriverIncidents
            .Include(i => i.ReportedBy)
            .Where(i => i.DriverId == id)
            .OrderByDescending(i => i.IncidentDate)
            .ToListAsync();

        // Calculate accident-free streak (consecutive days from today with no incident)
        var lastIncident = incidents.FirstOrDefault();
        var streakDays = lastIncident == null
            ? (int)(DateTime.UtcNow - driver.CreatedAt).TotalDays
            : (int)(DateTime.UtcNow - lastIncident.IncidentDate.ToDateTime(TimeOnly.MinValue)).TotalDays;

        return new DriverPerformanceDto(
            driver.Id,
            driver.FullName,
            driver.DriverStatus ?? "Unknown",
            assignments.Count,
            assignments.Count(a => a.Status == "Completed"),
            assignments.Count(a => a.Status == "Cancelled"),
            incidents.Count,
            incidents.Count(i => i.Severity == "Major"),
            streakDays,
            assignments.Take(10).Select(a => ToAssignmentDto(a)).ToList(),
            incidents.Take(10).Select(i => new DriverIncidentDto(
                i.Id, i.DriverId, driver.FullName,
                i.IncidentDate, i.Type, i.Description, i.Severity,
                i.ActionTaken, i.ReportedBy?.FullName ?? "", i.CreatedAt)).ToList()
        );
    }

    private static UserDto ToDto(Models.Entities.User u) => new(
        u.Id, u.FullName, u.Email, u.PhoneNumber, u.Role,
        u.DriverStatus, u.LicenceNo, u.LicenceExpiry, u.IsActive, u.LastStatusChange);

    private static AssignmentDto ToAssignmentDto(Models.Entities.Assignment a) => new(
        a.Id, a.TripRequestId, a.TripRequest?.Purpose ?? "",
        a.DriverId, a.Driver?.FullName ?? "",
        a.VehicleId, a.Vehicle?.RegistrationNo ?? "",
        a.AssignmentType, a.Status, a.StartTime,
        a.EstimatedEndTime, a.ActualEndTime, a.Notes, a.CreatedAt);
}
