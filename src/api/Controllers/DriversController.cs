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
    // ── Register a driver ────────────────────────────────────────────────────
    // Works whether or not the person has used the platform before. If they
    // already have an account (e.g. they signed in and were provisioned as
    // Staff), they are promoted to Driver rather than rejected — previously this
    // returned a conflict, so anyone who had ever logged in could not be
    // registered as a driver at all.
    [HttpPost]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<ActionResult<UserDto>> Register(RegisterDriverDto dto)
    {
        var emailNorm = dto.Email?.ToLowerInvariant().Trim() ?? string.Empty;

        if (!string.IsNullOrEmpty(emailNorm))
        {
            var existing = await db.Users.FirstOrDefaultAsync(u => u.Email == emailNorm);
            if (existing != null)
            {
                if (existing.Role == "Driver" && existing.IsActive)
                    return Conflict(new { error = $"{existing.FullName} is already registered as a driver." });

                // Promote to Driver, keeping their account and history intact.
                var previousRole = existing.Role;
                existing.Role         = "Driver";
                existing.DriverStatus = existing.DriverStatus ?? "OffDuty";
                existing.IsActive     = true;
                if (!string.IsNullOrWhiteSpace(dto.FullName))    existing.FullName      = dto.FullName;
                if (!string.IsNullOrWhiteSpace(dto.PhoneNumber)) existing.PhoneNumber   = dto.PhoneNumber;
                if (!string.IsNullOrWhiteSpace(dto.LicenceNo))   existing.LicenceNo     = dto.LicenceNo;
                if (dto.LicenceExpiry.HasValue)                  existing.LicenceExpiry = dto.LicenceExpiry;

                await db.SaveChangesAsync();

                await audit.LogAsync("Driver", existing.Id.ToString(), "PromotedToDriver",
                    User.GetEntraObjectId() ?? "", User.GetEmail(), null,
                    $"{existing.FullName} ({existing.Email}) changed from {previousRole} to Driver");

                return Ok(ToDto(existing));
            }
        }

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

        var callerEmail = User.GetEmail();
        var callerId = User.GetEntraObjectId();
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

        // Drivers may set their own status; operations staff may set anyone's.
        // Claims are read tolerantly — a single-spelling lookup here previously
        // meant roles came back empty and legitimate users were blocked.
        var callerId = User.GetEntraObjectId();
        if (!User.IsOperationsStaff() && driver.EntraObjectId != callerId)
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "You can only change your own driver status."
            });

        driver.DriverStatus = dto.Status;
        driver.LastStatusChange = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var email = User.GetEmail();
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
