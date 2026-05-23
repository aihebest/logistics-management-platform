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

        // Drivers can only update their own status; coordinators/managers can update anyone
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

    private static UserDto ToDto(Models.Entities.User u) => new(
        u.Id, u.FullName, u.Email, u.PhoneNumber, u.Role,
        u.DriverStatus, u.LicenceNo, u.LicenceExpiry, u.IsActive, u.LastStatusChange);

    private static AssignmentDto ToAssignmentDto(Models.Entities.Assignment a) => new(
        a.Id, a.TripRequestId, a.TripRequest.Purpose,
        a.DriverId, a.Driver?.FullName ?? "",
        a.VehicleId, a.Vehicle.RegistrationNo,
        a.AssignmentType, a.Status, a.StartTime,
        a.EstimatedEndTime, a.ActualEndTime, a.Notes, a.CreatedAt);
}
