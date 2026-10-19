using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using LogisticsApi.Models.Entities;
using LogisticsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

/// <summary>
/// Parcel Dispatch — sending documents, plaques and certificates by courier.
///
/// Flow: sender submits → head of the sender's department approves → GM
/// Logistics approves → Logistics dispatches it and records the courier and
/// tracking number, which the sender is emailed.
/// </summary>
[ApiController]
[Route("api/parcels")]
[Authorize]
public class ParcelDispatchController(
    AppDbContext db,
    ICurrentUserService currentUser,
    INotificationService notifications,
    IAuditService audit,
    ILogger<ParcelDispatchController> logger) : ControllerBase
{
    private static readonly string[] Priorities = ["Normal", "High", "Urgent"];

    /// <summary>Which couriers serve which scope, as set by the Logistics Manager.</summary>
    private static readonly Dictionary<string, string[]> CarriersByScope = new()
    {
        ["Domestic"]      = ["FedEx", "Airport Dispatch"],
        ["International"] = ["DHL"],
    };

    // ── Read ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Staff see their own parcels; a head of department sees their
    /// department's; Logistics and management see everything.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ParcelDispatchDto>>> GetAll([FromQuery] string? status)
    {
        var caller = await currentUser.ResolveOrProvisionAsync(User);
        if (caller == null) return Unauthorized(new { error = "Cannot resolve user identity from token" });

        var q = BaseQuery();
        if (!string.IsNullOrEmpty(status)) q = q.Where(p => p.Status == status);

        if (!SeesAll())
        {
            var headed = await db.Departments
                .Where(d => d.HodUserId == caller.Id)
                .Select(d => d.Name)
                .ToListAsync();
            q = q.Where(p => p.RequestedById == caller.Id || headed.Contains(p.SenderDepartment));
        }

        var list = await q.OrderByDescending(p => p.CreatedAt).ToListAsync();
        return Ok(list.Select(ToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ParcelDispatchDto>> Get(Guid id)
    {
        var caller = await currentUser.ResolveOrProvisionAsync(User);
        if (caller == null) return Unauthorized(new { error = "Cannot resolve user identity from token" });

        var p = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id);
        if (p == null) return NotFound();

        if (!SeesAll() && p.RequestedById != caller.Id
            && !await db.Departments.AnyAsync(d => d.HodUserId == caller.Id && d.Name == p.SenderDepartment))
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "You can only view parcels you sent, or those from a department you head."
            });

        return ToDto(p);
    }

    // ── Submit ───────────────────────────────────────────────────────────────

    [HttpPost]
    public async Task<ActionResult<ParcelDispatchDto>> Create(CreateParcelDispatchDto dto)
    {
        var caller = await currentUser.ResolveOrProvisionAsync(User);
        if (caller == null) return Unauthorized(new { error = "Cannot resolve user identity from token" });

        var scope = CarriersByScope.Keys.FirstOrDefault(k => string.Equals(k, dto.Scope?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (scope == null)
            return BadRequest(new { error = "Choose Domestic or International." });

        var carrier = CarriersByScope[scope].FirstOrDefault(c => string.Equals(c, dto.Carrier?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (carrier == null)
            return BadRequest(new { error = $"{scope} parcels go by {string.Join(" or ", CarriersByScope[scope])}." });

        var priority = Priorities.FirstOrDefault(x => string.Equals(x, dto.Priority?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? "Normal";

        if (string.IsNullOrWhiteSpace(dto.SenderName) || string.IsNullOrWhiteSpace(dto.ReceiverName)
            || string.IsNullOrWhiteSpace(dto.ReceiverAddress) || string.IsNullOrWhiteSpace(dto.Description)
            || string.IsNullOrWhiteSpace(dto.SenderLocation))
            return BadRequest(new { error = "Sender name and location, receiver name and address, and a description are required." });

        var department = await db.Departments
            .FirstOrDefaultAsync(d => d.IsActive && d.Name == dto.SenderDepartment);
        if (department == null)
            return BadRequest(new { error = $"'{dto.SenderDepartment}' is not a recognised department." });

        // The head approves first — unless the head is the sender, or the
        // department has no head yet. Either way it goes straight to GM
        // Logistics rather than waiting on an approval nobody can give.
        var skipHod = department.HodUserId == null || department.HodUserId == caller.Id;

        var parcel = new ParcelDispatch
        {
            Id               = Guid.NewGuid(),
            FormNumber       = await NextFormNumberAsync(),
            RequestedById    = caller.Id,
            SenderName       = dto.SenderName.Trim(),
            SenderLocation   = dto.SenderLocation.Trim(),
            SenderDepartment = department.Name,
            SenderPhone      = Trim(dto.SenderPhone),
            ReceiverName     = dto.ReceiverName.Trim(),
            ReceiverAddress  = dto.ReceiverAddress.Trim(),
            ReceiverPhone    = Trim(dto.ReceiverPhone),
            Description      = dto.Description.Trim(),
            IsFragile        = dto.IsFragile,
            Priority         = priority,
            Scope            = scope,
            Carrier          = carrier,
            Status           = skipHod ? "PendingManager" : "PendingHOD",
            CreatedAt        = DateTime.UtcNow,
            UpdatedAt        = DateTime.UtcNow,
            RequestedBy      = caller,
        };

        db.ParcelDispatches.Add(parcel);
        await db.SaveChangesAsync();

        if (department.HodUserId == null)
            logger.LogWarning("Parcel {Form}: department '{Department}' has no head — sent straight to GM Logistics",
                parcel.FormNumber, department.Name);

        try
        {
            if (skipHod)
                await notifications.SendParcelAwaitingManagerAsync(parcel);
            else
            {
                var hod = await db.Users.FindAsync(department.HodUserId!.Value);
                if (hod != null) await notifications.SendParcelAwaitingHodAsync(parcel, hod);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Notification failed for parcel {Form} — the request itself was saved", parcel.FormNumber);
        }

        return Ok(await GetFullDto(parcel.Id));
    }

    // ── Stage 1: head of department ──────────────────────────────────────────

    [HttpPost("{id:guid}/hod-decision")]
    public async Task<IActionResult> HodDecision(Guid id, ParcelDecisionDto dto)
    {
        var parcel = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id);
        if (parcel == null) return NotFound();
        if (parcel.Status != "PendingHOD")
            return BadRequest(new { error = "This parcel is not awaiting head-of-department approval." });

        var caller = await currentUser.ResolveOrProvisionAsync(User);
        if (caller == null) return Unauthorized(new { error = "Cannot resolve user identity from token" });

        if (parcel.RequestedById == caller.Id)
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "You cannot approve a parcel you sent yourself." });

        var headsIt = await db.Departments.AnyAsync(d => d.Name == parcel.SenderDepartment && d.HodUserId == caller.Id);
        if (!headsIt && !User.IsInRole("Admin"))
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = $"Only the head of {parcel.SenderDepartment} can approve this parcel at this stage."
            });

        var approve = string.Equals(dto.Action, "Approve", StringComparison.OrdinalIgnoreCase);
        parcel.HodApprovedById = caller.Id;
        parcel.HodApprovedBy   = caller;
        parcel.HodApprovedAt   = DateTime.UtcNow;
        parcel.HodRemarks      = Trim(dto.Remarks);
        parcel.UpdatedAt       = DateTime.UtcNow;

        if (approve) parcel.Status = "PendingManager";
        else Reject(parcel, dto.Remarks);

        await db.SaveChangesAsync();
        await AuditAsync(parcel, approve ? "HodApproved" : "Rejected", caller);

        try
        {
            if (approve) await notifications.SendParcelAwaitingManagerAsync(parcel);
            else         await notifications.SendParcelDecisionAsync(parcel, false, caller.FullName);
        }
        catch (Exception ex) { logger.LogError(ex, "Notification failed after HOD decision on {Form}", parcel.FormNumber); }

        return Ok(new { message = approve ? $"{parcel.FormNumber} approved and sent to GM Logistics." : $"{parcel.FormNumber} rejected." });
    }

    // ── Stage 2: GM Logistics ────────────────────────────────────────────────

    [HttpPost("{id:guid}/manager-decision")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> ManagerDecision(Guid id, ParcelDecisionDto dto)
    {
        var parcel = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id);
        if (parcel == null) return NotFound();
        if (parcel.Status != "PendingManager")
            return BadRequest(new { error = "This parcel is not awaiting GM Logistics approval." });

        var caller = await currentUser.ResolveOrProvisionAsync(User);
        if (caller == null) return Unauthorized(new { error = "Cannot resolve user identity from token" });

        if (parcel.RequestedById == caller.Id)
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "You cannot approve a parcel you sent yourself." });

        // Two signatures mean two people. The head of Logistics also approves as
        // GM Logistics, so she can't do both on her own department's parcel.
        if (parcel.HodApprovedById == caller.Id)
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "You approved this parcel as head of department, so another manager must give the GM Logistics approval."
            });

        var approve = string.Equals(dto.Action, "Approve", StringComparison.OrdinalIgnoreCase);
        parcel.ManagerApprovedById = caller.Id;
        parcel.ManagerApprovedBy   = caller;
        parcel.ManagerApprovedAt   = DateTime.UtcNow;
        parcel.ManagerRemarks      = Trim(dto.Remarks);
        parcel.UpdatedAt           = DateTime.UtcNow;

        if (approve) parcel.Status = "Approved";
        else Reject(parcel, dto.Remarks);

        await db.SaveChangesAsync();
        await AuditAsync(parcel, approve ? "Approved" : "Rejected", caller);

        try { await notifications.SendParcelDecisionAsync(parcel, approve, caller.FullName); }
        catch (Exception ex) { logger.LogError(ex, "Notification failed after GM decision on {Form}", parcel.FormNumber); }

        return Ok(new { message = approve ? $"{parcel.FormNumber} approved — ready for dispatch." : $"{parcel.FormNumber} rejected." });
    }

    // ── Dispatch ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Logistics records that the parcel has gone, with the tracking number the
    /// sender needs to follow it.
    /// </summary>
    [HttpPost("{id:guid}/dispatch")]
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public async Task<IActionResult> MarkDispatched(Guid id, MarkParcelDispatchedDto dto)
    {
        var parcel = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id);
        if (parcel == null) return NotFound();
        if (parcel.Status != "Approved")
            return BadRequest(new { error = "Only an approved parcel can be marked as dispatched." });

        var caller = await currentUser.ResolveOrProvisionAsync(User);
        if (caller == null) return Unauthorized(new { error = "Cannot resolve user identity from token" });

        // Logistics may switch courier at the counter (e.g. FedEx to Airport
        // Dispatch) — allowed, but only to one that serves the parcel's scope.
        if (!string.IsNullOrWhiteSpace(dto.Carrier))
        {
            var carrier = CarriersByScope[parcel.Scope]
                .FirstOrDefault(c => string.Equals(c, dto.Carrier.Trim(), StringComparison.OrdinalIgnoreCase));
            if (carrier == null)
                return BadRequest(new { error = $"{parcel.Scope} parcels go by {string.Join(" or ", CarriersByScope[parcel.Scope])}." });
            parcel.Carrier = carrier;
        }

        parcel.Status         = "Dispatched";
        parcel.TrackingNumber = Trim(dto.TrackingNumber);
        parcel.DispatchedAt   = DateTime.UtcNow;
        parcel.DispatchedById = caller.Id;
        parcel.DispatchedBy   = caller;
        parcel.UpdatedAt      = DateTime.UtcNow;

        await db.SaveChangesAsync();
        await AuditAsync(parcel, "Dispatched", caller);

        try { await notifications.SendParcelDispatchedAsync(parcel); }
        catch (Exception ex) { logger.LogError(ex, "Dispatch notification failed for {Form}", parcel.FormNumber); }

        return Ok(new { message = $"{parcel.FormNumber} marked as dispatched." });
    }

    /// <summary>The sender, or Logistics, can withdraw a parcel before it is approved.</summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var parcel = await db.ParcelDispatches.FindAsync(id);
        if (parcel == null) return NotFound();

        var caller = await currentUser.ResolveOrProvisionAsync(User);
        if (caller == null) return Unauthorized(new { error = "Cannot resolve user identity from token" });

        if (parcel.RequestedById != caller.Id && !User.IsOperationsStaff())
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "You can only cancel parcels you sent." });

        if (parcel.Status is not ("PendingHOD" or "PendingManager"))
            return BadRequest(new { error = "Only a parcel still awaiting approval can be cancelled." });

        parcel.Status    = "Cancelled";
        parcel.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(new { message = $"{parcel.FormNumber} cancelled." });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private IQueryable<ParcelDispatch> BaseQuery() =>
        db.ParcelDispatches
          .Include(p => p.RequestedBy)
          .Include(p => p.HodApprovedBy)
          .Include(p => p.ManagerApprovedBy)
          .Include(p => p.DispatchedBy);

    private bool SeesAll() => User.IsOperationsStaff() || User.HasAnyRole("Management");

    private static void Reject(ParcelDispatch parcel, string? reason)
    {
        parcel.Status          = "Rejected";
        parcel.RejectionReason = string.IsNullOrWhiteSpace(reason) ? "No reason given" : reason.Trim();
        parcel.RejectedAt      = DateTime.UtcNow;
    }

    private Task AuditAsync(ParcelDispatch parcel, string action, User caller) =>
        audit.LogAsync("ParcelDispatch", parcel.Id.ToString(), action,
            User.GetEntraObjectId() ?? "", User.GetEmail(), null,
            $"{parcel.FormNumber} {action} by {caller.FullName}");

    /// <summary>"PD/26/001", restarting each year; derived from the highest so far.</summary>
    private async Task<string> NextFormNumberAsync()
    {
        var prefix = $"PD/{DateTime.UtcNow:yy}/";
        var last = await db.ParcelDispatches
            .Where(p => p.FormNumber.StartsWith(prefix))
            .OrderByDescending(p => p.FormNumber)
            .Select(p => p.FormNumber)
            .FirstOrDefaultAsync();

        var next = last != null && int.TryParse(last[prefix.Length..], out var n) ? n + 1 : 1;
        return $"{prefix}{next:D3}";
    }

    private static string? Trim(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>
    /// Non-urgent parcels from the Port Harcourt office go out on the weekly
    /// Friday run, so they get the next Friday (today, if it is Friday).
    /// Urgent parcels and other offices dispatch on approval — no date shown.
    /// </summary>
    public static DateOnly? ExpectedDispatch(ParcelDispatch p)
    {
        if (p.Priority == "Urgent" || !IsPortHarcourt(p.SenderLocation)) return null;
        if (p.Status is "Dispatched" or "Rejected" or "Cancelled") return null;

        var from = DateOnly.FromDateTime((p.ManagerApprovedAt ?? DateTime.UtcNow).AddHours(1)); // WAT
        var daysToFriday = ((int)DayOfWeek.Friday - (int)from.DayOfWeek + 7) % 7;
        return from.AddDays(daysToFriday);
    }

    public static bool IsPortHarcourt(string location) =>
        location.Contains("Port Harcourt", StringComparison.OrdinalIgnoreCase)
        || location.Contains("PHC", StringComparison.OrdinalIgnoreCase)
        || string.Equals(location.Trim(), "PH", StringComparison.OrdinalIgnoreCase);

    private async Task<ParcelDispatchDto> GetFullDto(Guid id) =>
        ToDto(await BaseQuery().FirstAsync(x => x.Id == id));

    private static ParcelDispatchDto ToDto(ParcelDispatch p) => new(
        p.Id, p.FormNumber, p.RequestedById, p.RequestedBy?.FullName ?? "",
        p.SenderName, p.SenderLocation, p.SenderDepartment, p.SenderPhone,
        p.ReceiverName, p.ReceiverAddress, p.ReceiverPhone,
        p.Description, p.IsFragile, p.Priority, p.Scope, p.Carrier,
        p.Status,
        p.HodApprovedBy?.FullName, p.HodApprovedAt, p.HodRemarks,
        p.ManagerApprovedBy?.FullName, p.ManagerApprovedAt, p.ManagerRemarks,
        p.RejectionReason,
        p.TrackingNumber, p.DispatchedAt, p.DispatchedBy?.FullName,
        ExpectedDispatch(p),
        p.CreatedAt);
}
