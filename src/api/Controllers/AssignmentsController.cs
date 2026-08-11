using System.Security.Claims;
using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using LogisticsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

[ApiController]
[Route("api/assignments")]
[Authorize]
public class AssignmentsController(AppDbContext db, INotificationService notifications, IAuditService audit, ICurrentUserService currentUser) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public async Task<IEnumerable<AssignmentDto>> GetAll(
        [FromQuery] string? status, [FromQuery] Guid? driverId, [FromQuery] Guid? vehicleId)
    {
        var q = db.Assignments
            .Include(a => a.TripRequest)
            .Include(a => a.Driver)
            .Include(a => a.Vehicle)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status)) q = q.Where(a => a.Status == status);
        if (driverId.HasValue) q = q.Where(a => a.DriverId == driverId);
        if (vehicleId.HasValue) q = q.Where(a => a.VehicleId == vehicleId);

        return await q.OrderByDescending(a => a.CreatedAt).Select(a => ToDto(a)).ToListAsync();
    }

    [HttpPost]
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public async Task<ActionResult<AssignmentDto>> Create(CreateAssignmentDto dto)
    {
        var trip = await db.TripRequests.FindAsync(dto.TripRequestId);
        if (trip == null) return NotFound(new { error = "Trip request not found" });
        if (trip.Status != "Pending") return BadRequest(new { error = "Trip request is not in Pending status" });

        var driver = await db.Users.FindAsync(dto.DriverId);
        if (driver == null || driver.Role != "Driver") return BadRequest(new { error = "Driver not found" });

        var vehicle = await db.Vehicles.FindAsync(dto.VehicleId);
        if (vehicle == null) return BadRequest(new { error = "Vehicle not found" });

        var caller = await currentUser.ResolveOrProvisionAsync(User);

        var assignment = new Models.Entities.Assignment
        {
            Id = Guid.NewGuid(),
            TripRequestId = dto.TripRequestId,
            DriverId = dto.DriverId,
            VehicleId = dto.VehicleId,
            AssignedById = caller?.Id ?? Guid.Empty,
            AssignmentType = "Manual",
            Status = "Active",
            StartTime = dto.StartTime,
            EstimatedEndTime = dto.EstimatedEndTime,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        driver.DriverStatus = "OnAssignment";
        driver.LastStatusChange = DateTime.UtcNow;
        vehicle.Status = "Assigned";
        vehicle.UpdatedAt = DateTime.UtcNow;
        trip.Status = "Active";

        db.Assignments.Add(assignment);
        await db.SaveChangesAsync();

        assignment.Driver = driver;
        assignment.Vehicle = vehicle;
        assignment.TripRequest = trip;
        await notifications.SendAssignmentConfirmedAsync(assignment);

        return CreatedAtAction(nameof(GetAll), new { id = assignment.Id }, ToDto(assignment));
    }

    [HttpPost("{id:guid}/override")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Override(Guid id, OverrideAssignmentDto dto)
    {
        var assignment = await db.Assignments
            .Include(a => a.Driver).Include(a => a.Vehicle).Include(a => a.TripRequest)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (assignment == null) return NotFound();

        var newDriver = await db.Users.FindAsync(dto.DriverId);
        if (newDriver == null || newDriver.Role != "Driver") return BadRequest(new { error = "Driver not found" });
        var newVehicle = await db.Vehicles.FindAsync(dto.VehicleId);
        if (newVehicle == null) return BadRequest(new { error = "Vehicle not found" });

        // Release old driver/vehicle
        assignment.Driver.DriverStatus = "Available";
        assignment.Driver.LastStatusChange = DateTime.UtcNow;
        assignment.Vehicle.Status = "Available";
        assignment.Vehicle.UpdatedAt = DateTime.UtcNow;

        assignment.DriverId = dto.DriverId;
        assignment.VehicleId = dto.VehicleId;
        assignment.AssignmentType = "Manual";
        if (!string.IsNullOrEmpty(dto.Notes)) assignment.Notes = dto.Notes;

        newDriver.DriverStatus = "OnAssignment";
        newDriver.LastStatusChange = DateTime.UtcNow;
        newVehicle.Status = "Assigned";
        newVehicle.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        var callerId = (User.GetEntraObjectId() ?? "");
        var email = User.GetEmail();
        await audit.LogAsync("Assignment", id.ToString(), "Overridden", callerId, email, null,
            $"Driver changed to {newDriver.FullName}. {dto.Notes}");

        return NoContent();
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public async Task<IActionResult> UpdateStatus(Guid id, UpdateAssignmentStatusDto dto)
    {
        var assignment = await db.Assignments
            .Include(a => a.Driver).Include(a => a.Vehicle)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (assignment == null) return NotFound();

        var validStatuses = new[] { "Active", "Completed", "Ongoing", "Unattended", "Cancelled" };
        if (!validStatuses.Contains(dto.Status))
            return BadRequest(new { error = $"Invalid status. Valid values: {string.Join(", ", validStatuses)}" });

        assignment.Status = dto.Status;
        if (dto.Notes != null) assignment.Notes = dto.Notes;

        if (dto.Status == "Completed")
        {
            assignment.ActualEndTime = dto.ActualEndTime ?? DateTime.UtcNow;
            assignment.Driver.DriverStatus = "Available";
            assignment.Driver.LastStatusChange = DateTime.UtcNow;
            assignment.Vehicle.Status = "Available";
            assignment.Vehicle.UpdatedAt = DateTime.UtcNow;

            var trip = await db.TripRequests.FindAsync(assignment.TripRequestId);
            if (trip != null) trip.Status = "Completed";
        }
        else if (dto.Status == "Unattended")
        {
            // Driver still holds vehicle but trip flagged
            assignment.Driver.DriverStatus = "OnAssignment";
        }
        else if (dto.Status == "Ongoing")
        {
            // Extended trip — keep statuses as-is
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    [HttpPatch("{id:guid}/complete")]
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public async Task<IActionResult> Complete(Guid id)
    {
        var assignment = await db.Assignments
            .Include(a => a.Driver).Include(a => a.Vehicle)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (assignment == null) return NotFound();

        assignment.Status = "Completed";
        assignment.ActualEndTime = DateTime.UtcNow;
        assignment.Driver.DriverStatus = "Available";
        assignment.Driver.LastStatusChange = DateTime.UtcNow;
        assignment.Vehicle.Status = "Available";
        assignment.Vehicle.UpdatedAt = DateTime.UtcNow;

        var trip = await db.TripRequests.FindAsync(assignment.TripRequestId);
        if (trip != null) trip.Status = "Completed";

        await db.SaveChangesAsync();
        return NoContent();
    }

    private static AssignmentDto ToDto(Models.Entities.Assignment a) => new(
        a.Id, a.TripRequestId, a.TripRequest?.Purpose ?? "",
        a.DriverId, a.Driver?.FullName ?? "",
        a.VehicleId, a.Vehicle?.RegistrationNo ?? "",
        a.AssignmentType, a.Status, a.StartTime,
        a.EstimatedEndTime, a.ActualEndTime, a.Notes, a.CreatedAt);
}
