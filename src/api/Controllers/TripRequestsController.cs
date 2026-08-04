using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using LogisticsApi.Services;
using LogisticsApi.Services.AssignmentEngine;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

[ApiController]
[Route("api/trips")]
[Authorize]
public class TripRequestsController(
    AppDbContext db,
    IAssignmentEngine engine,
    INotificationService notifications,
    ICurrentUserService currentUser,
    ILogger<TripRequestsController> logger) : ControllerBase
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
        var caller = await currentUser.ResolveOrProvisionAsync(User);
        if (caller == null)
            return Unauthorized(new { error = "Cannot resolve user identity from token" });

        // ── Business rule: submit at least 24 hours in advance ────────────────
        // Urgent-priority requests are exempt so genuine emergencies aren't blocked.
        var isUrgent = string.Equals(dto.Priority, "Urgent", StringComparison.OrdinalIgnoreCase);
        if (!isUrgent && dto.RequestedDateTime < DateTime.UtcNow.AddHours(24))
        {
            return BadRequest(new
            {
                error = "Trip requests must be submitted at least 24 hours in advance. " +
                        "For same-day or next-day travel, set the priority to Urgent."
            });
        }

        var trip = new Models.Entities.TripRequest
        {
            Id                  = Guid.NewGuid(),
            RequestedById       = caller.Id,
            Purpose             = dto.Purpose,
            PickupLocation      = dto.PickupLocation,
            DestinationLocation = dto.DestinationLocation,
            RequestedDateTime   = dto.RequestedDateTime,
            Status              = "Pending",   // awaits coordinator/manager approval
            Priority            = dto.Priority,
            Notes               = dto.Notes,
            MovementType        = dto.MovementType,
            // DepartureDate arrives as "yyyy-MM-dd" string; DepartureTime as "HH:mm"
            DepartureDate       = DateOnly.TryParse(dto.DepartureDate, out var depDate) ? depDate : null,
            DepartureTime       = dto.DepartureTime,   // already "HH:mm" — store as-is
            CreatedAt           = DateTime.UtcNow
        };

        db.TripRequests.Add(trip);
        await db.SaveChangesAsync();

        // Notify coordinators/managers to review + confirm receipt to the requester.
        // Assignment does NOT happen here — a coordinator/manager must approve first
        // (Interstate/International approvals are restricted to Manager/Admin).
        // Wrapped in try/catch so notification failures never block trip creation.
        trip.RequestedBy = caller;
        try { await notifications.SendTripRequestSubmittedAsync(trip); }
        catch (Exception ex) { logger.LogError(ex, "Notification failed for trip {TripId} — trip was saved successfully", trip.Id); }

        var result = await db.TripRequests
            .Include(x => x.RequestedBy)
            .Include(x => x.Assignment).ThenInclude(a => a!.Driver)
            .Include(x => x.Assignment).ThenInclude(a => a!.Vehicle)
            .FirstAsync(x => x.Id == trip.Id);

        return CreatedAtAction(nameof(Get), new { id = trip.Id }, ToDto(result));
    }

    /// <summary>
    /// Coordinator/Manager approves a pending trip request and optionally assigns
    /// a driver and vehicle. If no driver/vehicle IDs supplied, auto-assignment is attempted.
    /// </summary>
    [HttpPatch("{id:guid}/approve")]
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public async Task<ActionResult<TripRequestDto>> Approve(Guid id, [FromBody] ApproveTripDto? dto)
    {
        var trip = await db.TripRequests
            .Include(t => t.RequestedBy)
            .Include(t => t.Assignment).ThenInclude(a => a!.Driver)
            .Include(t => t.Assignment).ThenInclude(a => a!.Vehicle)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trip == null) return NotFound();
        if (trip.Status is "Completed" or "Cancelled")
            return BadRequest(new { error = $"Cannot approve a {trip.Status} request." });

        // ── Interstate / International require Manager or Admin approval ───────
        // Coordinators can approve IntraState trips, but interstate and international
        // movements must be signed off by a Manager or Admin per company policy.
        var isLongDistance = trip.MovementType is "Interstate" or "International";
        var isManager = User.IsInRole("Manager") || User.IsInRole("Admin");
        if (isLongDistance && !isManager)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = $"{trip.MovementType} movements require Manager or Admin approval before a driver is assigned."
            });
        }

        // If manual driver/vehicle supplied, create assignment now
        if (dto?.DriverId.HasValue == true && dto?.VehicleId.HasValue == true)
        {
            var driver  = await db.Users.FindAsync(dto.DriverId!.Value);
            var vehicle = await db.Vehicles.FindAsync(dto.VehicleId!.Value);

            if (driver == null)  return BadRequest(new { error = "Driver not found" });
            if (vehicle == null) return BadRequest(new { error = "Vehicle not found" });

            var assignment = new Models.Entities.Assignment
            {
                Id            = Guid.NewGuid(),
                TripRequestId = trip.Id,
                DriverId      = driver.Id,
                VehicleId     = vehicle.Id,
                Status        = "Active",
                StartTime     = trip.RequestedDateTime,
                CreatedAt     = DateTime.UtcNow
            };

            driver.DriverStatus     = "OnAssignment";
            driver.LastStatusChange = DateTime.UtcNow;
            vehicle.Status          = "Assigned";
            vehicle.UpdatedAt       = DateTime.UtcNow;

            db.Assignments.Add(assignment);
            trip.Status = "Active";   // approved & driver assigned — trip is now live
            await db.SaveChangesAsync();

            // Reload navigation properties for notifications
            assignment.Driver      = driver;
            assignment.Vehicle     = vehicle;
            assignment.TripRequest = trip;

            await notifications.SendAssignmentConfirmedAsync(assignment);
        }
        else
        {
            // Auto-assignment
            var caller = await currentUser.ResolveOrProvisionAsync(User);
            await engine.AssignAsync(trip, caller?.Id ?? Guid.Empty);

            // Reload to check if auto-assignment succeeded
            await db.Entry(trip).ReloadAsync();
            if (trip.Status == "Pending")
            {
                // Approved, but no driver/vehicle available right now.
                trip.Status = "Approved";
                await db.SaveChangesAsync();
                // Tell the requester it's approved, and alert coordinators/managers
                // that the request is stuck waiting for capacity.
                await notifications.SendTripRequestApprovedAsync(trip);
                await notifications.SendNoDriverAvailableAsync(trip);
            }
        }

        var result = await db.TripRequests
            .Include(x => x.RequestedBy)
            .Include(x => x.Assignment).ThenInclude(a => a!.Driver)
            .Include(x => x.Assignment).ThenInclude(a => a!.Vehicle)
            .FirstAsync(x => x.Id == id);

        return Ok(ToDto(result));
    }

    /// <summary>
    /// Coordinator/Manager rejects a trip request with a reason.
    /// The requester receives an email notification.
    /// </summary>
    [HttpPatch("{id:guid}/reject")]
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectTripDto dto)
    {
        var trip = await db.TripRequests
            .Include(t => t.RequestedBy)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trip == null) return NotFound();
        if (trip.Status is "Completed" or "Cancelled")
            return BadRequest(new { error = $"Cannot reject a {trip.Status} request." });

        trip.Status = "Rejected";
        await db.SaveChangesAsync();

        await notifications.SendTripRequestRejectedAsync(trip, dto.Reason ?? "No reason provided");

        return NoContent();
    }

    /// <summary>Mark a trip as complete. Driver status and vehicle revert to Available.</summary>
    [HttpPatch("{id:guid}/complete")]
    [Authorize(Roles = "Driver,Coordinator,Manager,Admin")]
    public async Task<IActionResult> Complete(Guid id)
    {
        var trip = await db.TripRequests
            .Include(t => t.RequestedBy)
            .Include(t => t.Assignment).ThenInclude(a => a!.Driver)
            .Include(t => t.Assignment).ThenInclude(a => a!.Vehicle)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (trip == null) return NotFound();

        trip.Status = "Completed";
        if (trip.Assignment != null)
        {
            trip.Assignment.Status      = "Completed";
            trip.Assignment.ActualEndTime = DateTime.UtcNow;

            if (trip.Assignment.Driver != null)
            {
                trip.Assignment.Driver.DriverStatus     = "Available";
                trip.Assignment.Driver.LastStatusChange = DateTime.UtcNow;
            }
            if (trip.Assignment.Vehicle != null)
            {
                trip.Assignment.Vehicle.Status    = "Available";
                trip.Assignment.Vehicle.UpdatedAt = DateTime.UtcNow;
            }
        }

        // Auto-log the completed trip in the Movement Register (closed entry),
        // so every completed movement has a permanent timestamped record.
        var caller = await currentUser.ResolveOrProvisionAsync(User);
        db.MovementRegisters.Add(new Models.Entities.MovementRegister
        {
            Id               = Guid.NewGuid(),
            MovementType     = "VehicleOut",
            VehicleId        = trip.Assignment?.VehicleId,
            DriverId         = trip.Assignment?.DriverId,
            RelatedRefNo     = trip.Id.ToString()[..8].ToUpper(),
            Purpose          = trip.Purpose,
            Origin           = trip.PickupLocation,
            Destination      = trip.DestinationLocation,
            MovementDateTime = trip.Assignment?.StartTime ?? trip.RequestedDateTime,
            ReturnDateTime   = DateTime.UtcNow,
            Status           = "Closed",
            Notes            = $"Auto-logged on trip completion. Priority: {trip.Priority}.",
            LoggedById       = caller?.Id ?? trip.RequestedById,
            CreatedAt        = DateTime.UtcNow
        });

        await db.SaveChangesAsync();
        await notifications.SendTripCompletedAsync(trip);

        return NoContent();
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
            var driver  = await db.Users.FindAsync(trip.Assignment.DriverId);
            var vehicle = await db.Vehicles.FindAsync(trip.Assignment.VehicleId);
            if (driver  != null) { driver.DriverStatus  = "Available"; driver.LastStatusChange = DateTime.UtcNow; }
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
