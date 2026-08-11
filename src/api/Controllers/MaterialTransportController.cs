using LogisticsApi.Data;
using LogisticsApi.Models.DTOs;
using LogisticsApi.Models.Entities;
using LogisticsApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LogisticsApi.Controllers;

[ApiController]
[Route("api/material-transport")]
[Authorize]
public class MaterialTransportController(
    AppDbContext db,
    ICurrentUserService currentUser,
    ILogger<MaterialTransportController> logger) : ControllerBase
{
    private Task<Models.Entities.User?> GetCallerAsync() =>
        currentUser.ResolveOrProvisionAsync(User);

    // ── Generate form number: DEL-LG-FRM-009/YYYY/NNN ─────────────────────────
    private async Task<string> GenerateFormNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var count = await db.MaterialTransportRequests
            .CountAsync(r => r.CreatedAt.Year == year);
        return $"DEL-LG-FRM-009/{year}/{(count + 1):D3}";
    }

    [HttpGet]
    public async Task<IEnumerable<MaterialTransportRequestDto>> GetAll(
        [FromQuery] string? status,
        [FromQuery] int? year)
    {
        var q = db.MaterialTransportRequests
            .Include(r => r.RequestedBy)
            .Include(r => r.HodApprovedBy)
            .Include(r => r.ManagerApprovedBy)
            .Include(r => r.AssignedDriver)
            .Include(r => r.AssignedVehicle)
            .Include(r => r.Items)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status)) q = q.Where(r => r.Status == status);
        if (year.HasValue) q = q.Where(r => r.CreatedAt.Year == year.Value);

        return await q.OrderByDescending(r => r.CreatedAt).Select(r => ToDto(r)).ToListAsync();
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<MaterialTransportRequestDto>> Get(Guid id)
    {
        var r = await db.MaterialTransportRequests
            .Include(x => x.RequestedBy)
            .Include(x => x.HodApprovedBy)
            .Include(x => x.ManagerApprovedBy)
            .Include(x => x.AssignedDriver)
            .Include(x => x.AssignedVehicle)
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == id);
        return r == null ? NotFound() : ToDto(r);
    }

    [HttpPost]
    public async Task<ActionResult<MaterialTransportRequestDto>> Create(CreateMaterialTransportRequestDto dto)
    {
        var caller = await GetCallerAsync();
        if (caller == null) return Unauthorized();

        var formNo = await GenerateFormNumberAsync();

        var request = new MaterialTransportRequest
        {
            Id = Guid.NewGuid(),
            FormNumber = formNo,
            RequestedById = caller.Id,
            ProjectName = dto.ProjectName,
            Purpose = dto.Purpose,
            LoadingPoint = dto.LoadingPoint,
            LoadingContactPerson = dto.LoadingContactPerson,
            LoadingContactPhone = dto.LoadingContactPhone,
            LoadingDate = dto.LoadingDate,
            DeliveryPoint = dto.DeliveryPoint,
            DeliveryContactPerson = dto.DeliveryContactPerson,
            DeliveryContactPhone = dto.DeliveryContactPhone,
            DeliveryDate = dto.DeliveryDate,
            Status = "PendingHOD",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Guard against a null/empty Items list — a request with no materials is
        // invalid, and iterating a null list would throw a 500.
        if (dto.Items is null || dto.Items.Count == 0)
            return BadRequest(new { error = "Add at least one material row before submitting." });

        foreach (var item in dto.Items)
        {
            request.Items.Add(new MaterialTransportItem
            {
                Id = Guid.NewGuid(),
                SNo = item.SNo,
                Material = item.Material,
                Description = item.Description,
                Quantity = item.Quantity
            });
        }

        try
        {
            db.MaterialTransportRequests.Add(request);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Material transport save failed for {Email}. Form {FormNo}, {ItemCount} items",
                caller.Email, formNo, request.Items.Count);
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Could not save the material transport request. " +
                        "The logistics team has been notified — please try again."
            });
        }

        logger.LogInformation("Material transport request {FormNo} created by {Email}", formNo, caller.Email);

        // Return the saved record directly. (Previously used CreatedAtAction, whose
        // route generation is an avoidable extra failure mode on the success path.)
        return Ok(await GetFullDto(request.Id));
    }

    // ── HOD Approval ──────────────────────────────────────────────────────────
    [HttpPost("{id:guid}/hod-approval")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> HodApprove(Guid id, ApproveMaterialTransportDto dto)
    {
        var request = await db.MaterialTransportRequests.FindAsync(id);
        if (request == null) return NotFound();
        if (request.Status != "PendingHOD")
            return BadRequest(new { error = "Request is not awaiting HOD approval" });

        var caller = await GetCallerAsync();
        if (caller == null) return Unauthorized();

        if (dto.Action == "Approve")
        {
            request.Status = "PendingManager";
            request.HodApprovedById = caller.Id;
            request.HodApprovedAt = DateTime.UtcNow;
            request.HodRemarks = dto.Remarks;
        }
        else
        {
            request.Status = "Rejected";
            request.HodApprovedById = caller.Id;
            request.HodApprovedAt = DateTime.UtcNow;
            request.HodRemarks = dto.Remarks;
        }

        request.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── GM Logistics / Manager Approval ───────────────────────────────────────
    [HttpPost("{id:guid}/manager-approval")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> ManagerApprove(Guid id, ApproveMaterialTransportDto dto)
    {
        var request = await db.MaterialTransportRequests.FindAsync(id);
        if (request == null) return NotFound();
        if (request.Status != "PendingManager")
            return BadRequest(new { error = "Request is not awaiting Manager approval" });

        var caller = await GetCallerAsync();
        if (caller == null) return Unauthorized();

        request.Status = dto.Action == "Approve" ? "Approved" : "Rejected";
        request.ManagerApprovedById = caller.Id;
        request.ManagerApprovedAt = DateTime.UtcNow;
        request.ManagerRemarks = dto.Remarks;
        request.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return NoContent();
    }

    // ── Assign Driver & Vehicle ───────────────────────────────────────────────
    [HttpPost("{id:guid}/assign")]
    [Authorize(Roles = "Coordinator,Manager,Admin")]
    public async Task<IActionResult> Assign(Guid id, AssignMaterialTransportDto dto)
    {
        var request = await db.MaterialTransportRequests.FindAsync(id);
        if (request == null) return NotFound();
        if (request.Status != "Approved")
            return BadRequest(new { error = "Request must be Approved before assigning resources" });

        var driver = await db.Users.FindAsync(dto.DriverId);
        if (driver == null || driver.Role != "Driver") return BadRequest(new { error = "Driver not found" });

        var vehicle = await db.Vehicles.FindAsync(dto.VehicleId);
        if (vehicle == null) return BadRequest(new { error = "Vehicle not found" });

        request.AssignedDriverId = dto.DriverId;
        request.AssignedVehicleId = dto.VehicleId;
        request.Status = "InProgress";
        request.UpdatedAt = DateTime.UtcNow;

        driver.DriverStatus = "OnAssignment";
        driver.LastStatusChange = DateTime.UtcNow;
        vehicle.Status = "Assigned";
        vehicle.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return NoContent();
    }

    private async Task<MaterialTransportRequestDto> GetFullDto(Guid id)
    {
        var r = await db.MaterialTransportRequests
            .Include(x => x.RequestedBy)
            .Include(x => x.HodApprovedBy)
            .Include(x => x.ManagerApprovedBy)
            .Include(x => x.AssignedDriver)
            .Include(x => x.AssignedVehicle)
            .Include(x => x.Items)
            .FirstAsync(x => x.Id == id);
        return ToDto(r);
    }

    private static MaterialTransportRequestDto ToDto(MaterialTransportRequest r) => new(
        r.Id,
        r.FormNumber,
        r.RequestedBy?.FullName ?? "",
        r.ProjectName,
        r.Purpose,
        r.LoadingPoint,
        r.LoadingContactPerson,
        r.LoadingContactPhone,
        r.LoadingDate,
        r.DeliveryPoint,
        r.DeliveryContactPerson,
        r.DeliveryContactPhone,
        r.DeliveryDate,
        r.Status,
        r.HodApprovedBy?.FullName,
        r.HodApprovedAt,
        r.HodRemarks,
        r.ManagerApprovedBy?.FullName,
        r.ManagerApprovedAt,
        r.ManagerRemarks,
        r.AssignedDriver?.FullName,
        r.AssignedVehicle?.RegistrationNo,
        r.Items.OrderBy(i => i.SNo).Select(i => new MaterialTransportItemDto(
            i.Id, i.SNo, i.Material, i.Description, i.Quantity)).ToList(),
        r.CreatedAt);
}
