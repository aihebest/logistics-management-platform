using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using LogisticsApi.Models.Entities;
using LogisticsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

/// <summary>
/// Travel Request Form — DEL-LG-FRM-002 Rev 07.
///
/// Flow: the traveller submits, their own head of department verifies, the
/// DMD/MD approves, and Logistics then downloads the completed form to book
/// from. Each step is an authenticated action recorded against a named account,
/// which is what prints in the signature blocks on the form.
/// </summary>
[ApiController]
[Route("api/travel")]
[Authorize]
public class TravelRequestController(
    AppDbContext db,
    ICurrentUserService currentUser,
    INotificationService notifications,
    IAuditService audit,
    ILogger<TravelRequestController> logger) : ControllerBase
{
    /// <summary>
    /// Travel plans reveal who is away and when, and the form carries personal
    /// phone and email, so the list is scoped rather than open to all staff.
    /// A requester sees their own; heads of department see their department's;
    /// management and Logistics see everything.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TravelRequestDto>>> GetAll([FromQuery] string? status)
    {
        var caller = await currentUser.ResolveOrProvisionAsync(User);
        if (caller == null) return Unauthorized(new { error = "Cannot resolve user identity from token" });

        var q = BaseQuery();

        if (!string.IsNullOrEmpty(status)) q = q.Where(r => r.Status == status);

        if (!CanSeeAllRequests())
        {
            // Departments this person heads — usually none, sometimes more than
            // one, since a single head can cover several.
            var headedDepartments = await db.Departments
                .Where(d => d.HodUserId == caller.Id)
                .Select(d => d.Name)
                .ToListAsync();

            q = q.Where(r => r.RequestedById == caller.Id
                          || headedDepartments.Contains(r.Department));
        }

        var list = await q.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return Ok(list.Select(ToDto).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TravelRequestDto>> Get(Guid id)
    {
        var caller = await currentUser.ResolveOrProvisionAsync(User);
        if (caller == null) return Unauthorized(new { error = "Cannot resolve user identity from token" });

        var r = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id);
        if (r == null) return NotFound();

        if (!CanSeeAllRequests() && r.RequestedById != caller.Id)
        {
            var headsIt = await db.Departments
                .AnyAsync(d => d.HodUserId == caller.Id && d.Name == r.Department);
            if (!headsIt)
                return StatusCode(StatusCodes.Status403Forbidden, new
                {
                    error = "You can only view travel requests you raised, or those from a department you head."
                });
        }

        return ToDto(r);
    }

    /// <summary>
    /// Submits a travel request. The form number and date are assigned here, not
    /// accepted from the client, so a form cannot be given a duplicate reference
    /// or back-dated.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TravelRequestDto>> Create(CreateTravelRequestDto dto)
    {
        var caller = await currentUser.ResolveOrProvisionAsync(User);
        if (caller == null) return Unauthorized(new { error = "Cannot resolve user identity from token" });

        if (dto.Legs == null || dto.Legs.Count == 0)
            return BadRequest(new { error = "Add at least one outbound journey under Routing Required." });

        if (!dto.Legs.Any(l => string.Equals(l.Direction, "Outbound", StringComparison.OrdinalIgnoreCase)))
            return BadRequest(new { error = "A travel request needs at least one outbound journey." });

        var department = await db.Departments
            .FirstOrDefaultAsync(d => d.Name == dto.Department && d.IsActive);
        if (department == null)
            return BadRequest(new { error = $"'{dto.Department}' is not a recognised department." });

        var request = new TravelRequest
        {
            Id                    = Guid.NewGuid(),
            FormNumber            = await NextFormNumberAsync(),
            FormDate              = DateTime.UtcNow,   // server-stamped
            ProjectCostCentreCode = Trim(dto.ProjectCostCentreCode),
            RequestedById         = caller.Id,
            Surname               = dto.Surname.Trim(),
            GivenName             = dto.GivenName.Trim(),
            Department            = department.Name,
            Position              = Trim(dto.Position),
            PhoneNumber           = Trim(dto.PhoneNumber),
            Email                 = Trim(dto.Email),
            PurposeOfTravel       = dto.PurposeOfTravel.Trim(),
            HotelBookingRequired  = dto.HotelBookingRequired,
            OtherInformation      = Trim(dto.OtherInformation),
            Status                = "PendingVerification",
            CreatedAt             = DateTime.UtcNow,
            UpdatedAt             = DateTime.UtcNow
        };

        var outboundSeq = 0;
        var inboundSeq  = 0;
        foreach (var leg in dto.Legs)
        {
            var isInbound = string.Equals(leg.Direction, "Inbound", StringComparison.OrdinalIgnoreCase);
            request.Legs.Add(new TravelRequestLeg
            {
                Id               = Guid.NewGuid(),
                TravelRequestId  = request.Id,
                Direction        = isInbound ? "Inbound" : "Outbound",
                Sequence         = isInbound ? inboundSeq++ : outboundSeq++,
                TravelDate       = leg.TravelDate,
                From             = leg.From.Trim(),
                To               = leg.To.Trim(),
                PreferredAirline = Trim(leg.PreferredAirline),
                PreferredTime    = Trim(leg.PreferredTime)
            });
        }

        db.TravelRequests.Add(request);
        await db.SaveChangesAsync();

        // Needed for the confirmation email back to whoever raised it.
        request.RequestedBy = caller;

        // Notification failures must never lose a submitted form.
        try { await NotifyVerifierAsync(request, department); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Notification failed for travel request {Form} — the request itself was saved",
                request.FormNumber);
        }

        return CreatedAtAction(nameof(Get), new { id = request.Id }, await GetFullDto(request.Id));
    }

    /// <summary>
    /// Head of department verification — the middle signature block on the form.
    /// Only the head of the requester's own department may verify, so a head of
    /// another department cannot sign off work they have no visibility of.
    /// </summary>
    [HttpPatch("{id:guid}/verify")]
    [Authorize(Roles = "HOD,Admin")]
    public async Task<IActionResult> Verify(Guid id, [FromBody] TravelDecisionDto? dto)
    {
        var request = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id);
        if (request == null) return NotFound();

        if (request.Status != "PendingVerification")
            return BadRequest(new { error = $"This request is {Describe(request.Status)} and cannot be verified again." });

        var caller = await currentUser.ResolveOrProvisionAsync(User);
        if (caller == null) return Unauthorized(new { error = "Cannot resolve user identity from token" });

        // Nobody signs off their own travel, whatever else they hold. This is the
        // first question an auditor asks of an approval chain.
        if (request.RequestedById == caller.Id)
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "You cannot verify a travel request you raised yourself. Ask another head of department."
            });

        var department = await db.Departments.FirstOrDefaultAsync(d => d.Name == request.Department);

        // When the department has a head, only they may verify. When it has none
        // yet, any head may — otherwise the request would be stuck with no route
        // forward, which is worse than a slightly looser control.
        if (department?.HodUserId != null && department.HodUserId != caller.Id && !User.IsInRole("Admin"))
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = $"Only the head of {request.Department} can verify this request."
            });

        request.Status            = "PendingApproval";
        request.VerifiedById      = caller.Id;
        request.VerifiedAt        = DateTime.UtcNow;
        request.VerificationNotes = Trim(dto?.Notes);
        request.UpdatedAt         = DateTime.UtcNow;

        await db.SaveChangesAsync();

        await audit.LogAsync("TravelRequest", id.ToString(), "Verified",
            User.GetEntraObjectId() ?? "", User.GetEmail(), null,
            $"{request.FormNumber} verified by {caller.FullName}");

        try { await notifications.SendTravelRequestVerifiedAsync(request); }
        catch (Exception ex) { logger.LogError(ex, "Verification notification failed for {Form}", request.FormNumber); }

        return Ok(new { message = $"{request.FormNumber} verified and sent to management for approval." });
    }

    /// <summary>
    /// Final management approval by the DMD/MD — the right-hand signature block.
    /// Once approved, Logistics can download the completed form and book.
    /// </summary>
    [HttpPatch("{id:guid}/approve")]
    [Authorize(Roles = "Management,Admin")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] TravelDecisionDto? dto)
    {
        var request = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id);
        if (request == null) return NotFound();

        if (request.Status == "PendingVerification")
            return BadRequest(new { error = "This request has not yet been verified by the head of department." });

        if (request.Status != "PendingApproval")
            return BadRequest(new { error = $"This request is {Describe(request.Status)} and cannot be approved again." });

        var caller = await currentUser.ResolveOrProvisionAsync(User);
        if (caller == null) return Unauthorized(new { error = "Cannot resolve user identity from token" });

        if (request.RequestedById == caller.Id)
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "You cannot approve a travel request you raised yourself."
            });

        request.Status        = "Approved";
        request.ApprovedById  = caller.Id;
        request.ApprovedAt    = DateTime.UtcNow;
        request.ApprovalNotes = Trim(dto?.Notes);
        request.UpdatedAt     = DateTime.UtcNow;

        await db.SaveChangesAsync();

        await audit.LogAsync("TravelRequest", id.ToString(), "Approved",
            User.GetEntraObjectId() ?? "", User.GetEmail(), null,
            $"{request.FormNumber} approved by {caller.FullName}");

        try { await notifications.SendTravelRequestApprovedAsync(request); }
        catch (Exception ex) { logger.LogError(ex, "Approval notification failed for {Form}", request.FormNumber); }

        return Ok(new { message = $"{request.FormNumber} approved. Logistics has been notified." });
    }

    /// <summary>Turns a request down. Available at either approval stage.</summary>
    [HttpPatch("{id:guid}/reject")]
    [Authorize(Roles = "HOD,Management,Admin")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectTravelRequestDto dto)
    {
        var request = await BaseQuery().FirstOrDefaultAsync(x => x.Id == id);
        if (request == null) return NotFound();

        if (request.Status is "Approved" or "Rejected" or "Cancelled")
            return BadRequest(new { error = $"This request is already {Describe(request.Status)}." });

        var caller = await currentUser.ResolveOrProvisionAsync(User);
        if (caller == null) return Unauthorized(new { error = "Cannot resolve user identity from token" });

        request.Status          = "Rejected";
        request.RejectionReason = string.IsNullOrWhiteSpace(dto?.Reason) ? "No reason given" : dto.Reason.Trim();
        request.RejectedAt      = DateTime.UtcNow;
        request.UpdatedAt       = DateTime.UtcNow;

        await db.SaveChangesAsync();

        await audit.LogAsync("TravelRequest", id.ToString(), "Rejected",
            User.GetEntraObjectId() ?? "", User.GetEmail(), null,
            $"{request.FormNumber} rejected by {caller.FullName}: {request.RejectionReason}");

        try { await notifications.SendTravelRequestRejectedAsync(request, caller.FullName); }
        catch (Exception ex) { logger.LogError(ex, "Rejection notification failed for {Form}", request.FormNumber); }

        return Ok(new { message = $"{request.FormNumber} rejected. The requester has been notified." });
    }

    /// <summary>Lets a requester withdraw their own request before it is approved.</summary>
    [HttpPatch("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        var request = await db.TravelRequests.FindAsync(id);
        if (request == null) return NotFound();

        var caller = await currentUser.ResolveOrProvisionAsync(User);
        if (caller == null) return Unauthorized(new { error = "Cannot resolve user identity from token" });

        if (request.RequestedById != caller.Id && !User.IsOperationsStaff())
            return StatusCode(StatusCodes.Status403Forbidden, new
            {
                error = "You can only cancel travel requests you raised."
            });

        if (request.Status is "Approved" or "Rejected" or "Cancelled")
            return BadRequest(new { error = $"This request is already {Describe(request.Status)}." });

        request.Status    = "Cancelled";
        request.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return Ok(new { message = $"{request.FormNumber} cancelled." });
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private IQueryable<TravelRequest> BaseQuery() =>
        db.TravelRequests
          .Include(r => r.RequestedBy)
          .Include(r => r.VerifiedBy)
          .Include(r => r.ApprovedBy)
          .Include(r => r.Legs);

    /// <summary>
    /// Management, Logistics operations staff and Admin see every request —
    /// Logistics because they do the booking, management because they approve.
    /// Everyone else sees their own, plus any department they head.
    /// </summary>
    private bool CanSeeAllRequests() =>
        User.IsInRole("Management") || User.IsOperationsStaff();

    /// <summary>
    /// Next reference in the form "TRF/26/001", restarting each year. Derived
    /// from the highest existing number rather than a count, so deleting a row
    /// can never cause a reference to be reused.
    /// </summary>
    private async Task<string> NextFormNumberAsync()
    {
        var yy     = DateTime.UtcNow.ToString("yy");
        var prefix = $"TRF/{yy}/";

        var lastNumber = await db.TravelRequests
            .Where(r => r.FormNumber.StartsWith(prefix))
            .OrderByDescending(r => r.FormNumber)
            .Select(r => r.FormNumber)
            .FirstOrDefaultAsync();

        var next = 1;
        if (lastNumber != null && int.TryParse(lastNumber[prefix.Length..], out var parsed))
            next = parsed + 1;

        return $"{prefix}{next:D3}";
    }

    /// <summary>
    /// Emails whoever must verify. When the department has a registered head it
    /// goes to them alone; when it does not, every head is told and a warning is
    /// logged naming the department, so the gap is visible rather than silent.
    /// </summary>
    private async Task NotifyVerifierAsync(TravelRequest request, Department department)
    {
        if (department.HodUserId != null)
        {
            var hod = await db.Users.FindAsync(department.HodUserId.Value);
            if (hod != null && !string.IsNullOrWhiteSpace(hod.Email))
            {
                await notifications.SendTravelRequestSubmittedAsync(request, hod);
                return;
            }
        }

        logger.LogWarning(
            "Travel request {Form}: department '{Department}' has no head with an email address. " +
            "Notifying all HODs instead — assign a head on the Departments screen.",
            request.FormNumber, department.Name);

        await notifications.SendTravelRequestSubmittedAsync(request, null);
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    /// <summary>Turns a status into something readable in an error message.</summary>
    private static string Describe(string status) => status switch
    {
        "PendingVerification" => "awaiting verification",
        "PendingApproval"     => "awaiting management approval",
        "Approved"            => "already approved",
        "Rejected"            => "rejected",
        "Cancelled"           => "cancelled",
        _                     => status
    };

    private async Task<TravelRequestDto> GetFullDto(Guid id) =>
        ToDto(await BaseQuery().FirstAsync(x => x.Id == id));

    private static TravelRequestDto ToDto(TravelRequest r) => new(
        r.Id,
        r.FormNumber,
        r.FormDate,
        r.ProjectCostCentreCode,
        r.RequestedById,
        r.RequestedBy?.FullName ?? "",
        r.Surname,
        r.GivenName,
        r.Department,
        r.Position,
        r.PhoneNumber,
        r.Email,
        r.PurposeOfTravel,
        r.HotelBookingRequired,
        r.OtherInformation,
        r.Legs
            .OrderBy(l => l.Direction == "Outbound" ? 0 : 1)
            .ThenBy(l => l.Sequence)
            .Select(l => new TravelLegDto(
                l.Direction, l.Sequence, l.TravelDate,
                l.From, l.To, l.PreferredAirline, l.PreferredTime))
            .ToList(),
        r.Status,
        r.VerifiedBy?.FullName,
        r.VerifiedAt,
        r.VerificationNotes,
        r.ApprovedBy?.FullName,
        r.ApprovedAt,
        r.ApprovalNotes,
        r.RejectionReason,
        r.RejectedAt,
        r.CreatedAt);
}
