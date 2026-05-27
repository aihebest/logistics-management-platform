using System.Security.Claims;
using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using LogisticsApi.Services.AssignmentEngine;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

[ApiController]
[Route("api/trips")]
[Authorize]
public class TripRequestsController(AppDbContext db, IAssignmentEngine engine) : ControllerBase
{
    [HttpGet]
    public async Task<IEnumerable<TripRequestDto>> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? movementType)
    {
        var query = db.TripRequests
            .Include(t => t.RequestedBy)
            .Include(t => t.Assignment).ThenInclude(a => a!.Driver)
            .Include(t => t.Assignment).ThenInclude(a => a!.Vehicle)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
            query = query.Where(t => t.Status == status);

        if (!string.IsNullOrEmpty(movementType))
            query = query.Where(t => t.MovementType == movementType);

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => ToDto(t))
            .ToListAsync();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TripRequestDto>> Get(Guid id)
    {
        var t = await db.TripRequests
            .Include(x => x.RequestedBy)
            .Include(x => x.Assignment).ThenInclude(a => a!.Driver)
            .Include(x => x.Assignment).ThenInclude(a => a!.Vehicle)
            .FirstOrDefaultAsync(x => x.Id == id);
        return t == null ? NotFound() : ToDto(t);
    }

    [HttpPost]
    public async Task<ActionResult<TripRequestDto>> Create(CreateTripRequestDto dto)
    {
        var callerId = User.FindFirstValue("oid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        var caller = await db.Users.FirstOrDefaultAsync(u => u.EntraObjectId == callerId);
        if (caller == null) return Unauthorized(new { error = "User not provisioned in platform" });

        var trip = new Models.Entities.TripRequest
        {
            Id = Guid.NewGuid(),
            RequestedById = caller.Id,
            Purpose = dto.Purpose,
            PickupLocation = dto.PickupLocation,
            DestinationLocation = dto.DestinationLocation,
            RequestedDateTime = dto.RequestedDateTime,
            Status = "Pending",
            Priority = dto.Priority,
            Notes = dto.Notes,
            MovementType = dto.MovementType,
            DepartureDate = dto.DepartureDate,
            DepartureTime = dto.DepartureTime?.ToString("HH:mm"),
            CreatedAt = DateTime.UtcNow
        };

        db.TripRequests.Add(trip);
        await db.SaveChangesAsync();

        // Attempt auto-assignment immediately
        trip.RequestedBy = caller;
        await engine.AssignAsync(trip, caller.Id);

        // Reload with nav props
        return CreatedAtAction(nameof(Get), new { id = trip.Id },
            ToDto(await db.TripRequests
                .Include(x => x.RequestedBy)
                .Include(x => x.Assignment).ThenInclude(a => a!.Driver)
                .Include(x => x.Assignment).ThenInclude(a => a!.Vehicle)
                .FirstAsync(x => x.Id == trip.Id)));
    }

    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var trip = await db.TripRequests.Include(t => t.Assignment).FirstOrDefaultAsync(t => t.Id == id);
        if (trip == null) return NotFound();

        trip.Status = "Cancelled";
        if (trip.Assignment != null)
        {
            trip.Assignment.Status = "Cancelled";
            var driver = await db.Users.FindAsync(trip.Assignment.DriverId);
            if (driver != null) { driver.DriverStatus = "Available"; driver.LastStatusChange = DateTime.UtcNow; }
            var vehicle = await db.Vehicles.FindAsync(trip.Assignment.VehicleId);
            if (vehicle != null) { vehicle.Status = "Available"; vehicle.UpdatedAt = DateTime.UtcNow; }
        }

        await db.SaveChangesAsync();
        return NoContent();
    }

    private static TripRequestDto ToDto(Models.Entities.TripRequest t)
    {
        var a = t.Assignment;
        return new TripRequestDto(
            t.Id, t.RequestedById, t.RequestedBy?.FullName ?? "",
            t.Purpose, t.PickupLocation, t.DestinationLocation,
            t.RequestedDateTime, t.Status, t.Priority, t.Notes, t.CreatedAt,
            a == null ? null : new AssignmentSummaryDto(
                a.Id, a.Driver?.FullName ?? "", a.Vehicle?.RegistrationNo ?? "",
                a.Status, a.StartTime, a.EstimatedEndTime),
            t.MovementType ?? "IntraState",
            t.DepartureDate,
            string.IsNullOrEmpty(t.DepartureTime) ? null : TimeOnly.Parse(t.DepartureTime));
    }
}
