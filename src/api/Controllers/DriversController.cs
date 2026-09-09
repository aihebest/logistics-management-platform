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
    // Coordinators register drivers day to day — drivers never self-register.
    [Authorize(Roles = "Coordinator,Manager,Admin")]
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

    /// <summary>
    /// Corrects a driver's record. Coordinators and above, since coordinators are
    /// the ones registering drivers in the first place.
    /// </summary>
    [HttpPatch("{id:guid}")]
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public async Task<IActionResult> Update(Guid id, UpdateDriverDto dto)
    {
        var driver = await db.Users.FindAsync(id);
        if (driver is null || driver.Role != "Driver") return NotFound();

        var changes = new List<string>();
        void Track<T>(string field, T oldValue, T newValue)
        {
            if (!Equals(oldValue, newValue))
                changes.Add($"{field}: {oldValue?.ToString() ?? "—"} → {newValue?.ToString() ?? "—"}");
        }

        if (!string.IsNullOrWhiteSpace(dto.FullName))
        {
            Track("Name", driver.FullName, dto.FullName.Trim());
            driver.FullName = dto.FullName.Trim();
        }

        if (dto.Email != null)
        {
            // Most Desicon drivers have no email at all, so blank is valid and is
            // stored as empty rather than null to match how they were registered.
            var email = dto.Email.Trim().ToLowerInvariant();
            if (email.Length > 0)
            {
                var taken = await db.Users.AnyAsync(u => u.Id != id && u.Email == email);
                if (taken) return BadRequest(new { error = "Another user already has that email address." });
            }
            Track("Email", driver.Email, email);
            driver.Email = email;
        }

        if (dto.PhoneNumber != null)   { var v = dto.PhoneNumber.Trim(); Track("Phone", driver.PhoneNumber, v); driver.PhoneNumber = v; }
        if (dto.LicenceNo != null)     { var v = dto.LicenceNo.Trim();   Track("Licence No", driver.LicenceNo, v); driver.LicenceNo = v; }
        if (dto.LicenceExpiry.HasValue){ Track("Licence Expiry", driver.LicenceExpiry, dto.LicenceExpiry.Value); driver.LicenceExpiry = dto.LicenceExpiry.Value; }
        if (dto.IsActive.HasValue)     { Track("Active", driver.IsActive, dto.IsActive.Value); driver.IsActive = dto.IsActive.Value; }

        if (changes.Count == 0)
            return Ok(new { message = "No changes were made." });

        await db.SaveChangesAsync();

        await audit.LogAsync("Driver", id.ToString(), "Updated",
            User.GetEntraObjectId() ?? "", User.GetEmail(), null,
            string.Join("; ", changes));

        return Ok(new { message = "Driver record updated.", changes });
    }

    /// <summary>
    /// Removes a driver.
    ///
    /// A driver who has been on a trip, held an assignment or logged fuel cannot
    /// be deleted outright — those records reference them, and losing that link
    /// would corrupt the history. Those drivers are deactivated instead, which
    /// takes them out of the assignment lists while leaving the audit trail
    /// intact. Drivers with no records at all — typically ones created in error
    /// during testing — are removed properly.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var driver = await db.Users.FindAsync(id);
        if (driver is null || driver.Role != "Driver") return NotFound();

        // Every table below has a Restrict foreign key onto Users, so a row in any
        // of them makes a hard delete fail at the database. Checking them here
        // turns that into a clear message instead of a 500.
        var hasHistory =
               await db.Assignments.AnyAsync(a => a.DriverId == id)
            || await db.TripRequests.AnyAsync(t => t.RequestedById == id)
            || await db.FuelLogs.AnyAsync(f => f.LoggedById == id)
            || await db.MovementRegisters.AnyAsync(m => m.DriverId == id || m.LoggedById == id)
            || await db.DriverSchedules.AnyAsync(s => s.DriverId == id || s.CreatedById == id)
            || await db.DriverIncidents.AnyAsync(i => i.DriverId == id || i.ReportedById == id);

        var callerId = User.GetEntraObjectId() ?? "";
        var callerEmail = User.GetEmail();

        if (hasHistory)
        {
            if (!driver.IsActive)
                return BadRequest(new { error = $"{driver.FullName} is already deactivated." });

            driver.IsActive = false;
            driver.DriverStatus = "OffDuty";
            driver.LastStatusChange = DateTime.UtcNow;
            await db.SaveChangesAsync();

            await audit.LogAsync("Driver", id.ToString(), "Deactivated", callerId, callerEmail, null,
                $"{driver.FullName} deactivated — has trip, fuel or movement history that must be preserved.");

            return Ok(new
            {
                message = $"{driver.FullName} has trip or fuel history, so the record was deactivated " +
                          "rather than deleted. They no longer appear for assignment.",
                deactivated = true
            });
        }

        db.Users.Remove(driver);
        await db.SaveChangesAsync();

        await audit.LogAsync("Driver", id.ToString(), "Deleted", callerId, callerEmail, null,
            $"{driver.FullName} deleted — no trip, fuel or movement records existed.");

        return Ok(new { message = $"{driver.FullName} was deleted.", deactivated = false });
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
