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

        // Departure is when they actually travel. DepartureDate arrives as
        // "yyyy-MM-dd" and DepartureTime as "HH:mm" from the browser inputs.
        DateOnly? departureDate = DateOnly.TryParse(dto.DepartureDate, out var depDate) ? depDate : null;
        if (departureDate == null)
            return BadRequest(new { error = "A departure date is required." });

        // Departure time is optional. When it's left blank, judge the 24-hour rule
        // on the date alone (end of that day) rather than assuming midnight, so a
        // request isn't rejected over a time the requester never entered.
        var departureAt = TimeOnly.TryParse(dto.DepartureTime, out var depTime)
            ? departureDate.Value.ToDateTime(depTime)
            : departureDate.Value.ToDateTime(new TimeOnly(23, 59));

        // ── Business rule: minimum notice, by movement type ───────────────────
        // Intrastate travel needs 4 hours' notice; interstate and international
        // need 24, since they take longer to resource. Measured against departure,
        // not a date the requester types, so it cannot be sidestepped. Urgent
        // priority is exempt for genuine emergencies.
        var noticeHours = MinimumNoticeHours(dto.MovementType);
        var isUrgent = string.Equals(dto.Priority, "Urgent", StringComparison.OrdinalIgnoreCase);
        if (!isUrgent && departureAt < DateTime.UtcNow.AddHours(noticeHours))
        {
            return BadRequest(new
            {
                error = $"{dto.MovementType} movements need at least {noticeHours} hours' notice. " +
                        "For shorter notice, set the priority to Urgent."
            });
        }

        // ── Personnel details ────────────────────────────────────────────────
        var personnelCount = dto.PersonnelCount < 1 ? 1 : dto.PersonnelCount;
        var personnelNames = string.IsNullOrWhiteSpace(dto.PersonnelNames) ? null : dto.PersonnelNames.Trim();

        // Names matter for accountability once more than one person travels.
        if (personnelCount > 1 && personnelNames == null)
            return BadRequest(new { error = "Please list the names of everyone travelling when more than one person is on the trip." });

        if (dto.HasMaterials && string.IsNullOrWhiteSpace(dto.MaterialDescription))
            return BadRequest(new { error = "Please describe the materials travelling with this movement." });

        var trip = new Models.Entities.TripRequest
        {
            Id                  = Guid.NewGuid(),
            RequestedById       = caller.Id,
            Purpose             = dto.Purpose,
            PickupLocation      = dto.PickupLocation,
            DestinationLocation = dto.DestinationLocation,
            // Server-stamped: the moment the request was raised. Never taken from
            // the client, so it cannot be back- or forward-dated.
            RequestedDateTime   = DateTime.UtcNow,
            Status              = "Pending",   // awaits coordinator/manager approval
            Priority            = dto.Priority,
            Notes               = dto.Notes,
            MovementType        = dto.MovementType,
            DepartureDate       = departureDate,
            DepartureTime       = dto.DepartureTime,   // already "HH:mm" — store as-is
            PersonnelCount      = personnelCount,
            PersonnelNames      = personnelNames,
            PersonnelCategory   = string.IsNullOrWhiteSpace(dto.PersonnelCategory) ? null : dto.PersonnelCategory,
            MovementDuration    = string.IsNullOrWhiteSpace(dto.MovementDuration) ? null : dto.MovementDuration,
            IsDropOff           = dto.IsDropOff,
            HasMaterials        = dto.HasMaterials,
            MaterialDescription = dto.HasMaterials && !string.IsNullOrWhiteSpace(dto.MaterialDescription)
                                    ? dto.MaterialDescription.Trim() : null,
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

        // Resolve the approver once — needed for the assignment's AssignedById,
        // which is a required foreign key.
        var approver = await currentUser.ResolveOrProvisionAsync(User);
        if (approver == null)
            return Unauthorized(new { error = "Cannot resolve user identity from token" });

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
                // Required FK — leaving this unset inserted Guid.Empty and the
                // foreign key violation surfaced as "An unexpected error occurred".
                AssignedById   = approver.Id,
                AssignmentType = "Manual",
                Status         = "Active",
                StartTime      = trip.RequestedDateTime,
                CreatedAt      = DateTime.UtcNow
            };

            driver.DriverStatus     = "OnAssignment";
            driver.LastStatusChange = DateTime.UtcNow;
            vehicle.Status          = "Assigned";
            vehicle.UpdatedAt       = DateTime.UtcNow;

            db.Assignments.Add(assignment);
            trip.Status = "Active";   // approved & driver assigned — trip is now live

            // The approved trip opens its own Movement Register entry, so the gate
            // and the register reflect the movement without anyone re-typing it.
            await OpenMovementRegisterEntryAsync(trip, driver.Id, vehicle.Id, approver.Id);

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
            await engine.AssignAsync(trip, approver.Id);

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
            else
            {
                // Auto-assignment found a driver and vehicle — open the register
                // entry for that pairing, same as the manual path above.
                var auto = await db.Assignments.FirstOrDefaultAsync(a => a.TripRequestId == trip.Id);
                if (auto != null)
                {
                    await OpenMovementRegisterEntryAsync(trip, auto.DriverId, auto.VehicleId, approver.Id);
                    await db.SaveChangesAsync();
                }
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

        // The register entry was opened when the trip was approved. Close that one
        // rather than adding a second row, so the gate log shows one movement out
        // and back, and whatever mileage the gate recorded is preserved.
        var caller = await currentUser.ResolveOrProvisionAsync(User);
        var refNo  = TripRef(trip.Id);

        var open = await db.MovementRegisters
            .Where(m => m.RelatedRefNo == refNo && m.Status == "Open")
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync();

        if (open != null)
        {
            open.Status         = "Closed";
            open.ReturnDateTime ??= DateTime.UtcNow;
            open.Notes = string.IsNullOrWhiteSpace(open.Notes)
                ? "Closed automatically on trip completion."
                : $"{open.Notes} Closed automatically on trip completion.";
        }
        else
        {
            // Older trips approved before auto-opening existed, or entries a
            // coordinator deleted — fall back to a single closed record.
            db.MovementRegisters.Add(new Models.Entities.MovementRegister
            {
                Id               = Guid.NewGuid(),
                MovementType     = "VehicleOut",
                VehicleId        = trip.Assignment?.VehicleId,
                DriverId         = trip.Assignment?.DriverId,
                RelatedRefNo     = refNo,
                Purpose          = trip.Purpose,
                Passengers       = trip.PersonnelNames,
                Origin           = trip.PickupLocation,
                Destination      = trip.DestinationLocation,
                MovementDateTime = trip.Assignment?.StartTime ?? trip.RequestedDateTime,
                ReturnDateTime   = DateTime.UtcNow,
                Status           = "Closed",
                Notes            = $"Auto-logged on trip completion. Priority: {trip.Priority}.",
                LoggedById       = caller?.Id ?? trip.RequestedById,
                CreatedAt        = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
        await notifications.SendTripCompletedAsync(trip);

        return NoContent();
    }

    /// <summary>
    /// Cancels a trip request.
    ///
    /// Only the person who raised the request, or operations staff
    /// (Coordinator/Manager/Admin), may cancel it. Without this check any
    /// signed-in user could cancel anyone else's trip.
    /// </summary>
    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var trip = await db.TripRequests.Include(t => t.Assignment).FirstOrDefaultAsync(t => t.Id == id);
        if (trip == null) return NotFound();

        var caller = await currentUser.ResolveOrProvisionAsync(User);
        if (caller == null)
            return Unauthorized(new { error = "Cannot resolve user identity from token" });

        var isOwner = trip.RequestedById == caller.Id;
        if (!isOwner && !User.IsOperationsStaff())
        {
            logger.LogWarning(
                "User {Email} attempted to cancel trip {TripId} raised by another user",
                caller.Email, trip.Id);
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "You can only cancel trip requests that you raised. " +
                        "Ask a coordinator or manager to cancel this one."
            });
        }

        if (trip.Status is "Completed" or "Cancelled")
            return BadRequest(new { error = $"This request is already {trip.Status}." });

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

    /// <summary>
    /// Minimum notice a request needs, in hours, set by the Director of Logistics:
    /// intrastate 4 hours, interstate and international 24. Anything unrecognised
    /// gets the stricter figure rather than the looser one.
    /// </summary>
    private static int MinimumNoticeHours(string? movementType) =>
        string.Equals(movementType, "IntraState", StringComparison.OrdinalIgnoreCase) ? 4 : 24;

    /// <summary>Short human-readable reference linking a trip to its register entry.</summary>
    private static string TripRef(Guid tripId) => tripId.ToString()[..8].ToUpper();

    /// <summary>
    /// Opens a Movement Register entry for an approved trip.
    ///
    /// Requested by the HOD and Director of Logistics: once a trip is approved and
    /// a driver and vehicle are assigned, the register should already carry the
    /// movement so the gate only has to fill in mileage and time in. Does not call
    /// SaveChanges — the caller saves so the assignment and the entry commit together.
    /// </summary>
    private async Task OpenMovementRegisterEntryAsync(
        Models.Entities.TripRequest trip, Guid driverId, Guid vehicleId, Guid loggedById)
    {
        var refNo = TripRef(trip.Id);

        // Approving twice, or re-approving after a change, must not duplicate the row.
        var exists = await db.MovementRegisters.AnyAsync(m => m.RelatedRefNo == refNo);
        if (exists) return;

        // Prefer the planned departure; fall back to now if none was given.
        var movementAt = trip.DepartureDate.HasValue
            ? trip.DepartureDate.Value.ToDateTime(
                TimeOnly.TryParse(trip.DepartureTime, out var dt) ? dt : TimeOnly.MinValue)
            : DateTime.UtcNow;

        var detail = new List<string> { $"Auto-created from approved trip request {refNo}." };
        if (trip.PersonnelCount > 1)                          detail.Add($"{trip.PersonnelCount} personnel.");
        if (!string.IsNullOrWhiteSpace(trip.PersonnelCategory)) detail.Add($"Category: {trip.PersonnelCategory}.");
        if (!string.IsNullOrWhiteSpace(trip.MovementDuration))  detail.Add($"Duration: {trip.MovementDuration}.");
        if (trip.IsDropOff)                                   detail.Add("Drop-off only.");
        if (trip.HasMaterials)                                detail.Add($"Materials: {trip.MaterialDescription}.");

        db.MovementRegisters.Add(new Models.Entities.MovementRegister
        {
            Id               = Guid.NewGuid(),
            MovementType     = "VehicleOut",
            VehicleId        = vehicleId,
            DriverId         = driverId,
            RelatedRefNo     = refNo,
            Purpose          = trip.Purpose,
            Passengers       = trip.PersonnelNames,
            Origin           = trip.PickupLocation,
            Destination      = trip.DestinationLocation,
            MovementDateTime = movementAt,
            Status           = "Open",       // gate closes it with mileage and time in
            Notes            = string.Join(" ", detail),
            LoggedById       = loggedById,
            CreatedAt        = DateTime.UtcNow
        });

        logger.LogInformation("Opened Movement Register entry for approved trip {Ref}", refNo);
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
            string.IsNullOrEmpty(t.DepartureTime) ? null : TimeOnly.Parse(t.DepartureTime),
            t.PersonnelCount,
            t.PersonnelNames,
            t.PersonnelCategory,
            t.MovementDuration,
            t.IsDropOff,
            t.HasMaterials,
            t.MaterialDescription);
    }
}
